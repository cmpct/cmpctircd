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
        public PacketManager packetManager;
        public ChannelManager channelManager;
        public List<Dictionary<Client, TcpClient>> clientLists;

        // TODO: constants which will go into the config
        public String host = "irc.cmpct.info";
        public String network = "cmpct";
        public String version = "0.1-dev";
        public int maxTargets = 200;

        public Boolean requirePong = true;
        public int pingTimeout = 120;

        public void run() {
            Console.WriteLine("Starting cmpctircd");
            Console.WriteLine("==> Host: irc.cmpct.info");
            Console.WriteLine("==> Listening on: 127.0.0.1:6669");
            clientLists = new List<Dictionary<Client, TcpClient>>();
            SocketListener sl = new SocketListener(this, "127.0.0.1", 6669);
            packetManager = new PacketManager(this);
            channelManager = new ChannelManager(this);

            sl.bind();
            packetManager.load();

            // TODO: need to listen to everybody
            while (true) {
                try {
                    Console.WriteLine("Listening to one more");
                    // HACK: You can't use await in async
                    sl.listenToClients().Wait();
                } catch {
                    sl.stop();
                }
            }
        }


    }
}
