using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;

using cmpctircd.Modes;
using System.Net;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace cmpctircd
{
    public class Client : SocketBase {
        // Metadata
        // TODO: Make these read-only apart from via setNick()?
        public string UID { get; }
        public string Nick { get; set; }
        public string Ident { get; set; }
        public string RealName { get; set; }
        public string AwayMessage { get; set; }
        public CapManager CapManager { get; private set; }
        public IList<Channel> Invites { get; } = new List<Channel>();

        // Connection information
        public Server OriginServer { get; set; }
        public bool RemoteClient { get; } = false;
        public string Cloak { get; set; }
        public String DNSHost { get; set; }
        public long IdleTime { get; set; }
        public ClientState State { get; set; }
        public bool ResolvingHost { get; set; } = false;

        public IDictionary<string, UserMode> Modes {
            get;
        } = new Dictionary<string, UserMode>();

        // TODO will this work for multiple hops? think so but it's something to bare in mind
        public string UUID;
        public void SendVersion() => Write(String.Format(":{0} {1} {2} :cmpctircd-{3}", IRCd.Host, IrcNumeric.RPL_VERSION.Printable(), Nick, IRCd.Version));
        public string OriginServerName() => RemoteClient ? OriginServer.Name : IRCd.Host;

        public string NickIfSet() => string.IsNullOrEmpty(Nick) ? "*" : Nick;

        public Client(IRCd ircd, TcpClient tc, SocketListener sl, Stream stream, string UID = null, Server OriginServer = null, bool RemoteClient = false) : base(ircd, tc, sl, stream) {
            if(ircd.Config.GetValue<bool>("Advanced:ResolveHostnames"))
                ResolvingHost = true;

            this.UID = UID;
            this.OriginServer = OriginServer;
            this.RemoteClient = RemoteClient;
            if (this.UID == null) {
                this.UID = ircd.GenerateUID();
                IRCd.Log.Debug($"UID {this.UID} generated for client with IP: {IP.ToString()}");
            }

            UUID = (RemoteClient ? OriginServer.SID : IRCd.SID) + this.UID;
            State = ClientState.PreAuth;
            IdleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Initialise modes
            Type[] except = { typeof(ChannelMode), typeof(ChannelModeType) };
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(
                                       t => t.IsClass &&
                                       t.Namespace == "cmpctircd.Modes" &&
                                       t.BaseType.Equals(typeof(UserMode)) &&
                                       !except.Contains(t)
                                    );

            foreach(Type className in classes) {
                UserMode modeInstance = (UserMode) Activator.CreateInstance(Type.GetType(className.ToString()), this);
                string modeChar = modeInstance.Character;

                if(Modes.Values.Any(m => m.Character == modeChar)) {
                    IRCd.Log.Error($"{modeInstance.Name} has the same character ({modeChar}) as another user mode! Skipping.");
                    continue;
                }
                Modes.Add(modeChar, modeInstance);
                ircd.Log.Debug($"Creating instance of {modeChar} - {modeInstance.Description}");
            }

            CapManager = new CapManager(this);
        }

        ~Client() {
            Stream?.Close();
            TcpClient?.Close();
        }

        public override void BeginTasks() {
            try {
                // Check for PING/PONG events due
                CheckTimeout();

                if(IRCd.Config.GetValue<bool>("Advanced:ResolveHostnames"))
                    Resolve();
            } catch(Exception e) {
                IRCd.Log.Debug($"Failed to access client: {e.ToString()}");
                Disconnect(false);
                return;
            }
        }


        public void Resolve() {
            // Don't resolve a user's IP twice
            if(DNSHost != null) return;

            bool failedResolve = false;
            var  ip            = IP.ToString();
            var hitCache       = false;

            Write($":{IRCd.Host} NOTICE Auth :*** Looking up your hostname...");

            // Check if this IP is in the cache
            if(IRCd.DNSCache.ContainsKey(ip)) {
                var cached = IRCd.DNSCache[ip];

                if(cached == ip) {
                    // See below comment
                    failedResolve = true;
                } else {
                    DNSHost = cached;
                    Write($":{IRCd.Host} NOTICE Auth :*** Found your hostname ({DNSHost}) -- cached");
                    ResolvingHost = false;
                    hitCache = true;
                }
            }

            if (!hitCache) {
                // Not in the cache, look up the IP
                // XXX: mono won't resolve IPs -> hostnames at present
                // XXX: so this code seems to only run on the MS .NET runtime (i.e. Windows)
                // XXX: it is now fixed in upstream mono, not yet in a release (see bug below)
                // XXX: http://bugs.cmpct.info/show_bug.cgi?id=246
                try {
                    var resolver = Dns.GetHostEntry(ip);

                    IRCd.Log.Debug($"IP: {ip}");
                    IRCd.Log.Debug($"Host: {DNSHost}");
                    IRCd.Log.Debug($"Count: {resolver.Aliases.Count()}");

                    if(resolver.HostName == ip) {
                        // Don't set the DNSHost of the user if it's the same as the IP
                        // This is so the right cloaking is used etc
                        failedResolve = true;
                    } else {
                        // This is only reached if:
                        // 1) IP != Hostname
                        // AND
                        // 2) No exception has been thrown by the resolver
                        DNSHost = resolver.HostName;
                    }
                } catch(Exception) {
                    failedResolve = true;
                } finally {
                    ResolvingHost = false;
                }
            }

            if(failedResolve) {
                Write($"*** Could not resolve your hostname; using your IP address ({ip}) instead.");
            } else {
                Write($":{IRCd.Host} NOTICE Auth :*** Found your hostname ({DNSHost})");
                // Cache it
                // Note we don't cache if IP because would give wrong message
                IRCd.DNSCache[ip] = DNSHost;
            }

            if (IRCd.Config.GetValue<bool>("Advanced:ResolveHostnames")) {
                // If the IRCd resolves all hostnames, then we will have
                // delayed calling SendWelcome until DNS resolution was complete
                SendWelcome();
            }
        }

        public void SendWelcome() {
            // Refuse if the user hasn't yet authenticated (or is already)
            if(String.IsNullOrWhiteSpace(Nick) || String.IsNullOrWhiteSpace(Ident)) return;
            if(State.CompareTo(ClientState.PreAuth) > 0) return;

            if (CapManager.Negotiating) {
                // Don't send welcome at the moment because we're in the CAP negotiation
                // This function should be called when CAP END is sent
                return;
            }

            // Wait if we're waiting for a PONG
            if (IRCd.RequirePong && LastPong == 0) {
                return;
            }

            // Wait if we're resolving the hostname
            if (ResolvingHost) {
                return;
            }

            // Henceforth, we assume user can become Authenticated!
            State = ClientState.Auth;
            ++Listener.AuthClientCount;

            Write(String.Format(":{0} {1} {2} :Welcome to the {3} IRC Network {4}", IRCd.Host, IrcNumeric.RPL_WELCOME.Printable(), Nick, IRCd.Network, Mask));
            Write(String.Format(":{0} {1} {2} :Your host is {3}, running version cmpctircd-{4}", IRCd.Host, IrcNumeric.RPL_YOURHOST.Printable(), Nick, IRCd.Host, IRCd.Version));
            Write(String.Format(":{0} {1} {2} :This server was created {3}", IRCd.Host, IrcNumeric.RPL_CREATED.Printable(), Nick, IRCd.CreateTime.ToString("h:mm:ss MMM d yyyy")));

            var UModeTypes = IRCd.GetSupportedUModes(this);
            var ModeTypes = IRCd.GetSupportedModesByType();
            var modes     = IRCd.GetSupportedModes(true);

            Write($":{IRCd.Host} {IrcNumeric.RPL_MYINFO.Printable()} {Nick} {IRCd.Host} {IRCd.Version} {string.Join("",UModeTypes)} {modes["Characters"]} {string.Join("",ModeTypes["A"])}{string.Join("",ModeTypes["B"])}{string.Join("",ModeTypes["C"])}");
            Write($":{IRCd.Host} {IrcNumeric.RPL_ISUPPORT.Printable()} {Nick} CASEMAPPING=rfc1459 PREFIX=({modes["Characters"]}){modes["Symbols"]} STATUSMSG={modes["Symbols"]} NETWORK={IRCd.Network} MAXTARGETS={IRCd.MaxTargets} :are supported by this server");
            Write($":{IRCd.Host} {IrcNumeric.RPL_ISUPPORT.Printable()} {Nick} CHANTYPES=# CHANMODES={string.Join("",ModeTypes["A"])},{string.Join("",ModeTypes["B"])},{string.Join("", ModeTypes["C"])},{string.Join("", ModeTypes["D"])} :are supported by this server");

            SendLusers();
            // Send MOTD
            SendMotd();
            SetModes();

            IRCd.ServerLists.ForEach(serverList => serverList.ForEach(
                server => server.SyncClient(this)
            ));
        }

        public void SetModes() {
            if(IsTlsEnabled)
                Modes["z"].Grant("", true, true);
            foreach(var mode in IRCd.AutoUModes) {
                if(Modes.ContainsKey(mode.Key)) {
                    var modeObject = Modes[mode.Key];
                    if(!modeObject.AllowAutoSet) {
                        IRCd.Log.Warn($"Attempting to set non-auto-settable user mode: {mode.Key}!");
                        IRCd.Log.Warn($"You may wish to remove {mode.Key} from <umodes> in config file.");
                        continue;
                    }
                    modeObject.Grant(mode.Value, true, true);
                } else {
                    IRCd.Log.Warn($"Attempting to autoset non-existent user mode: {mode.Key}!");
                    IRCd.Log.Warn($"You may wish to remove {mode.Key} from <umodes> in config file.");
                }
            }
        }

        public void SendLusers() {
            int users     = 0;
            int invisible = 0;
            int ircops    = 0;

            foreach(var list in IRCd.ClientLists) {
                // Count all of the users in their totality
                users += list.Count();
                // Count all of the users with the user mode +i (invisible)
                invisible += list.Where(client => client.Modes["i"].Enabled).Count();
                // Count all of the users with usermode +o (IRC Operators)
                ircops += list.Where(client => client.Modes["o"].Enabled).Count();
            }
            users -= invisible;

            int servers = 1; // TODO: Adjust this when we have linking
            int linkedServers = 0;
            int channels = IRCd.ChannelManager.Size;

            if (users > IRCd.MaxSeen) {
                IRCd.MaxSeen = users;
            }

            // RPL_LUSERCLIENT - Users, Invisible, Servers(?)
            Write($":{IRCd.Host} {IrcNumeric.RPL_LUSERCLIENT.Printable()} {Nick} :There are {users} users and {invisible} invisible on {servers} servers");
            // RPL_LUSEROP - IRC Operator count
            Write($":{IRCd.Host} {IrcNumeric.RPL_LUSEROP.Printable()} {Nick} {ircops} :operator(s) online");
            // RPL_LUSERCHANNELS - Number of channels formed
            Write($":{IRCd.Host} {IrcNumeric.RPL_LUSERCHANNELS.Printable()} {Nick} {channels} :channels formed");
            // RPL_LUSERME - This is all clients (including bots)
            Write($":{IRCd.Host} {IrcNumeric.RPL_LUSERME.Printable()} {Nick} :I have {users} clients and {linkedServers} servers");
            // RPL_LOCALUSERS - Local clients and max local clients
            Write($":{IRCd.Host} {IrcNumeric.RPL_LOCALUSERS.Printable()} {Nick} :Current Local Users: {users} Max: {IRCd.MaxSeen}");
            // RPL_GLOBALUSERS - Global clients and max global clients
            // TODO: Adjust this with linking
            Write($":{IRCd.Host} {IrcNumeric.RPL_GLOBALUSERS.Printable()} {Nick} :Current Global Users: {users} Max: {IRCd.MaxSeen}");
        }
        public async Task SendMotd() {
            try {
                using(Stream motd = await IRCd.MOTD.GetStreamAsync()) {
                    Write(String.Format(":{0} {1} {2} :- {3} Message of the Day -", IRCd.Host, IrcNumeric.RPL_MOTDSTART.Printable(), Nick, IRCd.Host));
                    using(StreamReader reader = new StreamReader(motd)) {
                        string line;
                        while(!reader.EndOfStream) {
                            line = reader.ReadLine();
                            if(!(String.IsNullOrEmpty(line) && reader.EndOfStream))
                                Write(String.Format(":{0} {1} {2} :- {3}", IRCd.Host, IrcNumeric.RPL_MOTD.Printable(), Nick, line));
                        }
                    }
                    Write(String.Format(":{0} {1} {2} :End of /MOTD command.", IRCd.Host, IrcNumeric.RPL_ENDOFMOTD.Printable(), Nick));
                }
            } catch(IOException e) {
                IRCd.Log.Error(e.Message);
            }
        }

        public async Task SendRules() {
            try {
                using(Stream rules = await IRCd.Rules.GetStreamAsync()) {
                    Write(String.Format(":{0} {1} {2} :- {3} Server Rules -", IRCd.Host, IrcNumeric.RPL_MOTDSTART.Printable(), Nick, IRCd.Host));
                    using(StreamReader reader = new StreamReader(rules)) {
                        string line;
                        while(!reader.EndOfStream) {
                            line = reader.ReadLine();
                            if(!(String.IsNullOrEmpty(line) && reader.EndOfStream))
                                Write(String.Format(":{0} {1} {2} :- {3}", IRCd.Host, IrcNumeric.RPL_MOTD.Printable(), Nick, line));
                        }
                    }
                    Write(String.Format(":{0} {1} {2} :End of /RULES command.", IRCd.Host, IrcNumeric.RPL_ENDOFMOTD.Printable(), Nick));
                }
            } catch(IOException e) {
                IRCd.Log.Error(e.Message);
            }
        }

        public Boolean SetNick(String nick) {
            // Return if nick is the same
            String oldNick = this.Nick;
            String newNick = nick;

            if (String.Compare(newNick, oldNick, false) == 0)
                return true;

            // Is the nick safe?
            Regex safeNicks = new Regex(@"[A-Za-z{}\[\]_\\^|`][A-Za-z{}\[\]_\-\\^|`0-9]*", RegexOptions.IgnoreCase);
            Boolean safeNick = safeNicks.Match(newNick).Success;
            if (!safeNick) {
                throw new IrcErrErroneusNicknameException(this, newNick);
            }


            // Does a user with this nick already exist?
            try {
                IRCd.GetClientByNick(newNick);
                // Allow a nick change if old nick is the same as the new new (ignoring casing)
                // e.g. Sam -> sam
                if(String.Compare(oldNick, newNick, true) != 0) {
                    throw new IrcErrNicknameInUseException(this, newNick);
                }
            } catch(InvalidOperationException) {}

            foreach(var channel in IRCd.ChannelManager.Channels) {
                if(!channel.Value.Inhabits(this)) continue;
                channel.Value.SendToRoom(this, String.Format(":{0} NICK :{1}", Mask, newNick), false);
                channel.Value.Remove(oldNick, false, false);
                channel.Value.Add(this, newNick);
            }

            if(!String.IsNullOrEmpty(oldNick)) {
                Write(String.Format(":{0} NICK {1}", Mask, nick));
            }
            this.Nick = newNick;

            IRCd.WriteToAllServers($":{UUID} NICK {newNick} {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

            SendWelcome();
            return true;
        }

        public Boolean SetUser(String ident, String real_name) {
            // TOOD: validation
            this.Ident = ident;
            this.RealName = real_name;

            SendWelcome();
            return true;
        }

        // Returns the user's mask
        public String Mask
        {
            get
            {
                String nick = this.Nick;
                String user = this.Ident;
                String real_name = this.RealName;
                String host = this.GetHost();
                return String.Format("{0}!{1}@{2}", nick, user, host);
            }
        }

        public string GetHost(bool cloak = true) {
            var hosts = GetHosts(cloak);

            // We prefer a cloak
            if (cloak && hosts.Cloak != null)
                return hosts.Cloak;

            if (hosts.Dns != null)
                return hosts.Dns;

            return hosts.Ip.ToString();
        }

        public HostInfo GetHosts(bool cloak = true) {
            var hosts = new HostInfo();

            if (cloak && !String.IsNullOrEmpty(Cloak))
                hosts.Cloak = Cloak;

            if (!String.IsNullOrEmpty(DNSHost))
                hosts.Dns = DNSHost;

            hosts.Ip = IP;

            return hosts;
        }

        public string[] GetModeStrings(string characters) {
            string provides = "";
            string value = "";
            string args = "";

            foreach (var mode in Modes) {
                provides = mode.Value.Character;
                try {
                    if (mode.Value.Enabled) {
                        characters += provides;
                        value = mode.Value.GetValue();
                        if (mode.Value.HasParameters) {
                            args += $"{value} ";
                        }
                    }
                } catch (IrcModeNotEnabledException) {
                    // Skip this mode and get another
                    continue;
                }
            }
            return new string[] { characters, args };

        }

        public async Task Write(String packet, bool transformIfServer = true) {
            if(State.Equals(ClientState.Disconnected)) return;

            try {
                if(RemoteClient && transformIfServer) {
                    // Need to translate any nicks into UIDs
                    packet = IRCd.ReplaceNickWithUUID(packet);
                    await base.Write(packet, OriginServer.Stream);
                } else {
                    await base.Write(packet);
                }
            } catch(Exception) {
                // XXX: Was {ObjectDisposed, IO}Exception but got InvalidOperation from SslStream.Write()
                // XXX: Not clear why given we check .CanWrite, etc
                // XXX: See http://bugs.cmpct.info/show_bug.cgi?id=253
                Disconnect("Connection reset by host", true, false);
            }
        }

        public new void Disconnect(bool graceful) => Disconnect("", graceful, graceful);
        public override void Disconnect(string quitReason = "", bool graceful = true, bool sendToSelf = true) {
            if(State.Equals(ClientState.Disconnected)) return;
            try {
                if(graceful) {
                    // Inform all of the channels we're a member of that we are leaving
                    var destroyChannels = new List<Channel> ();

                    foreach(var channel in IRCd.ChannelManager.Channels.Values) {
                        if(channel.Inhabits(this)) {
                            channel.Quit(this, quitReason, false);
                            destroyChannels.Add(channel);
                        }
                    }

                    foreach(var channel in destroyChannels) {
                        // Attempt to destroy any channels we were in
                        // Only kills channel if it is now empty
                        channel.Destroy();
                    }

                }

                IRCd.WriteToAllServers($":{UUID} QUIT :{quitReason}", new List<Server>() { OriginServer } );

                if(sendToSelf) {
                    // Need this flag to prevent infinite loop of calls to Disconnect() upon IOException
                    // No need to guard the Channel quit because they do not send to the user leaving
                    Write($":{Mask} QUIT :{quitReason}");
                }
            } catch(Exception e) when(e is ObjectDisposedException || e is SocketException) {
                // The user has been disconnected but the server is still trying to call Disconnect on it
                IRCd.Log.Debug($"Tried to disconnect a removed user: {e.ToString()}");
            }

            State = ClientState.Disconnected;

            if (!RemoteClient) {
                Stream?.Close();
                TcpClient.Close();
            }

            Listener.Remove(this);
         }
    }
}
