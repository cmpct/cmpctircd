using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using cmpctircd.Packets;

namespace cmpctircd {
    class IRCd {
        private Dictionary<String, SocketListener> listeners;
        public PacketManager PacketManager { get; set; }
        public ChannelManager ChannelManager { get; set; }
        public List<Dictionary<Client, TcpClient>> ClientLists { get; set; }

        // TODO: constants which will go into the config (not changing until then)
        public String host = "irc.cmpct.info";
        public String network = "cmpct";
        public String version = "0.1-dev";
        public int maxTargets = 200;

        public Boolean RequirePong { get; set; } = true;
        public int PingTimeout { get; set; } = 120;

        public void Run() {
            Console.WriteLine("Starting cmpctircd");
            Console.WriteLine("==> Host: irc.cmpct.info");
            Console.WriteLine("==> Listening on: 127.0.0.1:6669");
            ClientLists = new List<Dictionary<Client, TcpClient>>();
            SocketListener sl = new SocketListener(this, "127.0.0.1", 6669);
            PacketManager = new PacketManager(this);
            ChannelManager = new ChannelManager(this);

            sl.Bind();
            PacketManager.Load();

            // TODO: need to listen to everybody
            while (true) {
                try {
                    Console.WriteLine("Listening to one more");
                    // HACK: You can't use await in async
                    sl.ListenToClients().Wait();
                } catch {
                    sl.Stop();
                }
            }
        }


    }
}
