using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using cmpctircd.Packets;
using cmpctircd.Modes;

namespace cmpctircd {
    public class IRCd {
        private List<SocketListener> Listeners;
        public PacketManager PacketManager { get; set; }
        public ChannelManager ChannelManager { get; set; }
        public List<List<Client>> ClientLists { get; set; }
        public Dictionary<string, List<string>> ModeTypes { get; set; }

        public Config.ConfigData Config;
        public string Host;
        public string Desc;
        public string Network;
        public const string Version = "0.2.0-dev";
        public int MaxTargets;

        public bool RequirePong { get; set; } = true;
        public int PingTimeout { get; set; } = 120;

        public IRCd(Config.ConfigData config) {
            this.Config = config;

            // Interpret the ConfigData
            Host    = config.Host;
            Desc    = config.Description;
            Network = config.Network;

            PingTimeout = config.PingTimeout;
            RequirePong = config.RequirePongCookie;

            MaxTargets = config.MaxTargets;
        }

        public void Run() {
            Console.WriteLine("Starting cmpctircd");
            Console.WriteLine($"==> Host: {Host}");

            Listeners = new List<SocketListener>();
            ClientLists = new List<List<Client>>();

            foreach(var listener in Config.Listeners) {
                // TODO: tls
                SocketListener sl = new SocketListener(this, listener.IP, listener.Port, listener.TLS);
                Console.WriteLine($"==> Listening on: {listener.IP}:{listener.Port}");
                Listeners.Add(sl);
            }

            PacketManager = new PacketManager(this);
            ChannelManager = new ChannelManager(this);

            Listeners.ForEach(listener => listener.Bind());
            PacketManager.Load();

            try {
                // HACK: You can't use await in async
                Listeners.ForEach(listener => listener.ListenToClients());
            } catch {
                Listeners.ForEach(listener => listener.Stop());
            }

            while (true) {
                System.Threading.Thread.Sleep(10);
            }
        }

        public Client GetClientByNick(String nick) {
            foreach (var clientList in ClientLists) {
                foreach (var clientItem in clientList) {
                    // User may not have a nick yet
                    if (String.IsNullOrEmpty(clientItem.Nick)) continue;

                    // Check if user has the nick we're looking for
                    if (clientItem.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase)) {
                        return clientItem;
                    }
                }
            }
            throw new InvalidOperationException("No such user exists");
        }

        public Dictionary<string, List<string>> GetSupportedModes() {
            if(ModeTypes != null && ModeTypes.Count() > 0) {
                // Caching to only generate this list once - reflection is expensive
                return ModeTypes;
            }

            ModeTypes = new Dictionary<string, List<string>>();

            // http://www.irc.org/tech_docs/005.html
            List<string> typeA = new List<string>();
            List<string> typeB = new List<string>();
            List<string> typeC = new List<string>();
            List<string> typeD = new List<string>();

            string[] badClasses = { "Mode", "ModeType" };
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(
                                       t => t.IsClass &&
                                       t.Namespace == "cmpctircd.Modes" &&
                                       t.BaseType.Equals(typeof(Mode)) &&
                                       !badClasses.Contains(t.Name)
            );

            foreach(Type className in classes) {
                Mode modeInstance = (Mode) Activator.CreateInstance(Type.GetType(className.ToString()), new Channel(ChannelManager, this));
                ModeType type = modeInstance.Type;
                string modeChar = modeInstance.Character;

                switch(type) {
                    case ModeType.A:
                        typeA.Add(modeChar);
                        break;
                    case ModeType.B:
                        typeB.Add(modeChar);
                        break;
                    case ModeType.C:
                        typeC.Add(modeChar);
                        break;
                    case ModeType.D:
                        typeD.Add(modeChar);
                        break;
                }
            }

            ModeTypes.Add("A", typeA);
            ModeTypes.Add("B", typeB);
            ModeTypes.Add("C", typeC);
            ModeTypes.Add("D", typeD);
            return ModeTypes;
        }


    }
}
