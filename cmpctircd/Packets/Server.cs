using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets {
    class Server {

        public Server(IRCd ircd) {
            /*ircd.PacketManager.Register(new PacketManager.HandlerInfo() {
                Packet = "HELLO",
                Handler = Test,
                Type = ListenerType.Server
            });*/
        }

        public bool Test(HandlerArgs args) {
            args.Sender.Write("hello");
            return true;
        }
    }
}
