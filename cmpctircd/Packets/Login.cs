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
            ircd.packetManager.register("QUIT", quitHandler);
        }

        public Boolean userHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();

            String[] splitLine = rawLine.Split(' ');
            String[] splitColonLine = rawLine.Split(new char[] { ':' }, 2);

            String username = splitLine[1];
            String real_name = splitColonLine[1];

            client.setUser(username, real_name);
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

        private Boolean quitHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
            String[] splitLine = rawLine.Split(new char[] { ':' }, 2);
            String reason = splitLine[1];

            client.disconnect(reason, true);
            return true;
        }

    }
}
