using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using cmpctircd.Configuration;
using cmpctircd.Configuration.Options;
using cmpctircd.Modes;
using cmpctircd.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace cmpctircd
{
    public class IRCd
    {
        public const string Version = "0.2.1-dev";
        public static char[] lastUID = { };
        public readonly IList<SocketConnector> Connectors = new List<SocketConnector>();
        private readonly IList<SocketListener> Listeners = new List<SocketListener>();
        private IOptions<LoggerOptions> _loggerOptions;

        public IRCd(Log log, IConfiguration config, IServiceProvider services, IOptions<SocketOptions> socketOptions, IOptions<LoggerOptions> loggerOptions)
        {
            _loggerOptions = loggerOptions;
            Log = log;
            Config = config;
            SocketOptions = socketOptions;

            // Interpret the ConfigData
            SID = config.GetValue<string>("SID");
            Host = config.GetValue<string>("Host");
            Desc = config.GetValue<string>("Description");
            Network = config.GetValue<string>("Network");

            if (SID == "auto") SID = GenerateSID(Host, Desc);

            PingTimeout = config.GetValue<int>("Advanced:PingTimeout");
            RequirePong = config.GetValue<bool>("Advanced:RequirePongCookie");

            Loggers = _loggerOptions.Value.Loggers;

            MaxTargets = config.GetValue<int>("Advanced:MaxTargets");
            CloakKey = config.GetValue<string>("Advanced:Cloak:Key");
            CloakFull = config.GetValue<bool>("Advanced:Cloak:Full");
            CloakPrefix = config.GetValue<string>("Advanced:Cloak:Prefix");
            CloakDomainParts = config.GetValue<int>("Advanced:Cloak:DomainParts");
            AutoModes = config.GetSection("Cmodes").Get<List<ModeElement>>().ToDictionary(m => m.Name, m => m.Param);
            AutoUModes = config.GetSection("Umodes").Get<List<ModeElement>>().ToDictionary(m => m.Name, m => m.Param);
            Opers = config.GetSection("Opers").Get<List<OperatorElement>>();
            OperChan = config.GetSection("OperChan").Get<List<string>>();

            PacketManager = new PacketManager(this, services);
            ChannelManager = new ChannelManager(this);

            // Create certificate refresh
            if (config.GetValue<string>("Tls") != null)
                Certificate =
                    new AutomaticCertificateCacheRefresh(new FileInfo(config.GetValue<string>("Tls:File")), password: config.GetValue<string>("Tls:Password"));
        }

        public PacketManager PacketManager { get; }
        public ChannelManager ChannelManager { get; }
        public IList<IList<Client>> ClientLists { get; } = new List<IList<Client>>();
        public IList<IList<Server>> ServerLists { get; } = new List<IList<Server>>();
        private IDictionary<string, IList<string>> ModeTypes { get; set; }
        private IDictionary<string, string> ModeDict { get; set; }
        private IList<string> UserModes { get; set; }

        public Log Log { get; }
        public IConfiguration Config { get; }
        public IOptions<SocketOptions> SocketOptions { get; }
        public string SID { get; }
        public string Host { get; }
        public string Desc { get; }
        public string Network { get; }
        public int MaxTargets { get; }
        public int MaxSeen { get; set; } = 0;
        public bool RequirePong { get; }
        public int PingTimeout { get; }
        public string CloakKey { get; }
        public bool CloakFull { get; }
        public static string CloakPrefix { get; private set; }
        public static int CloakDomainParts { get; private set; }
        public IDictionary<string, string> AutoModes { get; }
        public IDictionary<string, string> AutoUModes { get; }
        public IList<LoggerElement> Loggers { get; }
        public IDictionary<string, string> DNSCache { get; } = new Dictionary<string, string>();

        public IList<OperatorElement> Opers { get; }
        public IList<string> OperChan { get; }
        public DateTime CreateTime { get; private set; }

        public AutomaticFileCacheRefresh MOTD { get; } = new AutomaticFileCacheRefresh(new FileInfo("ircd.motd"));
        public AutomaticFileCacheRefresh Rules { get; } = new AutomaticFileCacheRefresh(new FileInfo("ircd.rules"));
        public AutomaticCertificateCacheRefresh Certificate { get; }

        public List<Client> Clients => ClientLists.SelectMany(clientList => clientList).ToList();
        public List<Server> Servers => ServerLists.SelectMany(serverList => serverList).ToList();

        public void Run()
        {
            Log.Info("==> Validating appsettings.json");
            var configurationValidator = new ConfigurationValidator(Config, SocketOptions, _loggerOptions);
            var validationResult = configurationValidator.ValidateConfiguration();

            if (!validationResult.IsValid) {
                Log.Error($"==> {string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage))}");
                return;
            }

            Log.Info($"==> Starting cmpctircd-{Version}");
            if (Version.Contains("-dev"))
            {
                Log.Info("===> You are running a development version of cmpctircd.NET.");
                Log.Info("===> If you are having problems, consider reverting to a stable version.");
                Log.Info(
                    "===> Please report any bugs or feedback to the developers via the bugtracker at https://bugs.cmpct.info/");
            }

            Log.Info($"==> Host: {Host}");

            PacketManager.Load();

            var sockets = SocketOptions.Value;
            foreach (var listener in sockets.Sockets)
            {
                var sl = new SocketListener(this, listener);
                Log.Info(
                    $"==> Listening on: {listener.Host}:{listener.Port} ({listener.Type}) ({(listener.Tls ? "TLS" : "Plain")})");

                Listeners.Add(sl);
                sl.Bind();
            }

            foreach (var server in Config.GetSection("Servers").Get<List<ServerElement>>())
                if (server.Outbound)
                {
                    // <server> tag with outbound="true"
                    // We want to connect out to this server, not have them connect to us
                    var sc = new SocketConnector(this, server);
                    Log.Info(
                        $"==> Connecting to: {server.Destination}:{server.Port} ({server.Host}) ({(server.Tls ? "TLS" : "Plain")})");

                    Connectors.Add(sc);
                    sc.Connect();
                }

            // Set create time
            CreateTime = DateTime.UtcNow;

            try
            {
                // HACK: You can't use await in async
                Listeners.ForEach(listener => listener.ListenToClients());
            }
            catch
            {
                Log.Error("Got an exception: shutting down all listeners");
                Listeners.ForEach(listener => listener.Stop());
            }

            if (Log.ShouldLogLevel(LogType.Debug))
            {
                var statTimer = new Timer();

                // Run the timer every 5 minutes
                statTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
                statTimer.Elapsed += delegate
                {
                    // Create a report on how each of the listeners is preforming (ratio of authenticated clients)
                    // Should let us see if there's some problem with a specific listener - or in general with the handshake
                    Log.Debug($"Since {CreateTime}, the following activity has occurred:");
                    foreach (var listener in Listeners)
                    {
                        if (listener.ClientCount == 0) continue;

                        var authRatio = decimal.Round(listener.AuthClientCount / (decimal) listener.ClientCount * 100);
                        var unauthCount = listener.ClientCount - listener.AuthClientCount;
                        var unauthRatio = decimal.Round(unauthCount / (decimal) listener.ClientCount * 100);
                        var prefixLine =
                            $"[{listener.Info.Host}:{listener.Info.Port} ({listener.Info.Type}) ({(listener.Info.Tls ? "SSL/TLS" : "Plain")})]";

                        Log.Debug(
                            $"==> {prefixLine} Authed: {listener.AuthClientCount} ({authRatio}%). Unauthed: {unauthCount} ({unauthRatio}%). Total: {listener.ClientCount}.");
                    }
                };
                statTimer.Start();
            }
        }

        public void Stop()
        {
            // TODO: Do other things?
            Listeners.ForEach(listener => listener.Stop());
        }

        public void WriteToAllServers(string message, List<Server> except = null)
        {
            foreach (List<Server> servers in ServerLists)
            foreach (var server in servers)
            {
                if (except != null && except.Contains(server)) // Skip a specified server
                    continue;
                server.Write(message + "\r\n");
            }
        }

        public Client GetClientByNick(string nick)
        {
            foreach (var client in Clients)
            {
                // User may not have a nick yet
                if (string.IsNullOrEmpty(client.Nick)) continue;

                // Check if user has the nick we're looking for
                if (client.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase)) return client;
            }

            throw new InvalidOperationException("No such user exists");
        }

        public Client GetClientByUUID(string UUID)
        {
            try
            {
                return Clients.Single(client => client.UUID == UUID);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("No such user exists");
            }
        }

        public Server GetServerBySID(string SID)
        {
            foreach (var serverList in ServerLists)
                try
                {
                    return serverList.Single(server => server.SID == SID);
                }
                catch (Exception)
                {
                }

            throw new InvalidOperationException("No such server exists");
        }

        public IDictionary<string, string> GetSupportedModes(bool requireSymbols)
        {
            if (ModeDict != null && ModeDict.Any()
                ) // Caching because this is still a relatively expensive operation to perform on each connection
                // (GetSupportedModesByType() is likely far more expensive given it uses reflection)
                // This is called by SendWelcome() to provide RPL_ISUPPORT
                return ModeDict;

            ModeDict = new Dictionary<string, string>();

            var chan = new Channel(ChannelManager, this);
            foreach (var modeList in ModeTypes)
            foreach (var mode in modeList.Value)
            {
                var modeObject = chan.Modes[mode];

                // TODO: Are two different caches needed?
                if (requireSymbols && string.IsNullOrEmpty(modeObject.Symbol)) continue;
                ModeDict.Add(modeObject.Character, modeObject.Symbol);
            }

            var modeCharacters = string.Join("", ModeDict.Select(p => p.Key));
            var modeSymbols = string.Join("", ModeDict.Select(p => p.Value));
            ModeDict.Add("Characters", modeCharacters);
            ModeDict.Add("Symbols", modeSymbols);

            return ModeDict;
        }

        public IList<string> GetSupportedUModes(Client client)
        {
            if (UserModes != null && UserModes.Any()
                ) // Caching because reflection is an expensive operation to perform on each connection
                // This is called by SendWelcome() to provide RPL_MYINFO
                return UserModes;

            UserModes = new List<string>();

            string[] badClasses = {"Mode", "ModeType"};
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(
                    t => t.IsClass &&
                         t.Namespace == "cmpctircd.Modes" &&
                         t.BaseType.Equals(typeof(UserMode)) &&
                         !badClasses.Contains(t.Name)
                );

            foreach (var className in classes)
            {
                var modeInstance = (UserMode) Activator.CreateInstance(Type.GetType(className.ToString()), client);
                UserModes.Add(modeInstance.Character);
            }

            return UserModes;
        }

        public IDictionary<string, IList<string>> GetSupportedModesByType()
        {
            if (ModeTypes != null && ModeTypes.Any()
            ) // Caching to only generate this list once - reflection is expensive
                return ModeTypes;

            ModeTypes = new Dictionary<string, IList<string>>();

            // http://www.irc.org/tech_docs/005.html
            var typeA = new List<string>();
            var typeB = new List<string>();
            var typeC = new List<string>();
            var typeD = new List<string>();
            var typeNone = new List<string>();

            string[] badClasses = {"Mode", "ModeType"};
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(
                    t => t.IsClass &&
                         t.Namespace == "cmpctircd.Modes" &&
                         t.BaseType.Equals(typeof(ChannelMode)) &&
                         !badClasses.Contains(t.Name)
                );

            foreach (var className in classes)
            {
                var modeInstance = (ChannelMode) Activator.CreateInstance(Type.GetType(className.ToString()),
                    new Channel(ChannelManager, this));
                var type = modeInstance.Type;
                var modeChar = modeInstance.Character;

                switch (type)
                {
                    case ChannelModeType.A:
                        typeA.Add(modeChar);
                        break;
                    case ChannelModeType.B:
                        typeB.Add(modeChar);
                        break;
                    case ChannelModeType.C:
                        typeC.Add(modeChar);
                        break;
                    case ChannelModeType.D:
                        typeD.Add(modeChar);
                        break;

                    default:
                        typeNone.Add(modeChar);
                        break;
                }
            }

            ModeTypes.Add("A", typeA);
            ModeTypes.Add("B", typeB);
            ModeTypes.Add("C", typeC);
            ModeTypes.Add("D", typeD);
            ModeTypes.Add("None", typeNone);
            return ModeTypes;
        }

        // UID <-> Nick translation helpers

        public string GenerateUID()
        {
            var UID = new char[6];
            var highestUid = 6 * 90; // Sum of 6(Z) 
            var aCharacter = Convert.ToChar(65); // A
            var zCharacter = Convert.ToChar(90); // Z

            if (new string(lastUID) == "" || UID.Sum(character => Convert.ToInt32(character)) == highestUid
                ) // We're at the start or we've hit the maximum possible ID (ZZZZZZ)
                // Start (again)...
                UID = new[] {'A', 'A', 'A', 'A', 'A', 'A'};
            else
                for (var i = UID.Length - 1; i >= 0; i--)
                {
                    // We need to increment every index, starting at UID[5] (6) until it reaches Z
                    // Once it reaches Z (this will depend on subsequent UIDs), hop to the next column and repeat

                    // If this column of the old UID was a Z, don't increment it
                    // Copy it over and work on the next column
                    if (lastUID[i] == zCharacter)
                    {
                        UID[i] = lastUID[i];
                        continue;
                    }

                    // Add one to the column if we're at the start
                    if (i == UID.Length - 1)
                    {
                        UID[i] = Convert.ToChar(lastUID[i] + 1);
                    }
                    else if (lastUID[i + 1] == zCharacter)
                    {
                        // Add one to the column if the previous column is Z
                        UID[i] = Convert.ToChar(lastUID[i] + 1);

                        // Once we've added one to THIS column, reset the one over to an A
                        if (lastUID[i + 1] == zCharacter
                            ) // If the next character over is a Z, change it to an A when we bump the next column
                            // e.g. if lastUID is AAAAAZ, make next AAAABA
                            UID[i + 1] = Convert.ToChar(aCharacter);
                    }
                    else
                    {
                        // Otherwise just copy that value
                        // e.g. with AAAAAB -> AAAAAC, only the B -> C has changed, so rest can be copied
                        UID[i] = lastUID[i];
                    }
                }

            // Don't allow this UID to be generated again...
            lastUID = UID;

            var string_UID = new string(UID);
            Log.Debug($"Generated a UID: {string_UID}");
            return string_UID;
        }

        public bool IsUUID(string message)
        {
            return Regex.IsMatch(message, "^[0-9][A-Z0-9][A-Z0-9][A-Z][A-Z0-9][A-Z0-9][A-Z0-9][A-Z0-9][A-Z0-9]$");
        }

        // Works on nick or (U)UID
        public string ExtractIdentifierFromMessage(string message, bool split = false)
        {
            var identifier = message;

            if (split)
            {
                var message_split = message.Split(new[] {" "}, StringSplitOptions.None);
                identifier = message_split[0];
            }

            identifier = identifier.Replace(":", "");
            // in case it is a nick with host format
            identifier = Regex.Replace(identifier, "!.*", "");

            return identifier;
        }

        // todo: UID -> UUID rename
        public string ReplaceUUIDWithNick(string message, int index = 0)
        {
            var split_message = message.Split(new[] {" "}, StringSplitOptions.None);
            if (IsUUID(split_message[index].Replace(":", "")))
            {
                split_message[index] = ExtractIdentifierFromMessage(split_message[index]);
                if (split_message[index] != Host)
                {
                    Log.Debug($"Looking for client with UUID (want their nick): {split_message[index]}");

                    var client = GetClientByUUID(split_message[index]);
                    split_message[index] = ":" + client.Nick;
                    // TODO exception if non existent?
                }
            }

            return string.Join(" ", split_message);
        }

        public string ReplaceNickWithUUID(string message, int index = 0)
        {
            var split_message = message.Split(new[] {" "}, StringSplitOptions.None);
            if (!IsUUID(split_message[index]))
            {
                split_message[index] = ExtractIdentifierFromMessage(split_message[index]);
                if (split_message[index] != Host)
                {
                    Log.Debug($"Looking for client with nick (want their UUID): {split_message[index]}");

                    var client = GetClientByNick(split_message[index]);
                    split_message[index] = ":" + client.UUID;
                    // TODO exception if non existent?
                }
            }

            return string.Join(" ", split_message);
        }


        // SID
        public static string GenerateSID(string name, string description)
        {
            // http://www.inspircd.org/wiki/Modules/spanningtree/UUIDs.html
            var SID = 0;

            for (var i = 0; i < name.Length; i++) SID = 5 * SID + Convert.ToInt32(name[i]);

            for (var n = 0; n < description.Length; n++) SID = 5 * SID + Convert.ToInt32(description[n]);

            SID = SID % 999;
            return SID.ToString("000");
        }
    }
}