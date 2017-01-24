using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets {
    class Login {
        //private IRCd ircd;

        public Login(IRCd ircd) {
            ircd.packetManager.register("USER", userHandler);
            ircd.packetManager.register("NICK", nickHandler);
        }

        public Boolean userHandler(Array args) {
            //Console.WriteLine("Running user handler...");
            return true;
        }

        public Boolean nickHandler(Array args) {
            IRCd ircd = (IRCd) args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
            String newNick = rawLine.Split(' ')[1];

            Console.WriteLine("Changing nick to {0}", newNick);
            client.setNick(newNick);
            return true;
        }

    }
}
