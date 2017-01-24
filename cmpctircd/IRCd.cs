using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cmpctircd.Packets;

namespace cmpctircd {
    class IRCd {
        private Dictionary<String, SocketListener> listeners;
        public PacketManager packetManager;

        public void run() {
            Console.WriteLine("Starting cmpctircd");
            Console.WriteLine("==> Host: irc.cmpct.info");
            Console.WriteLine("==> Listening on: 127.0.0.1:6669");
            SocketListener sl = new SocketListener(this, "127.0.0.1", 6669);
            packetManager = new PacketManager(this);

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
