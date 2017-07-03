using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static cmpctircd.Errors;

namespace cmpctircd.Packets {
    class Login {
        //private IRCd ircd;

        public Login(IRCd ircd) {
            ircd.packetManager.register("USER", userHandler);
            ircd.packetManager.register("NICK", nickHandler);
            ircd.packetManager.register("QUIT", quitHandler);
            ircd.packetManager.register("PONG", pongHandler);
        }

        public Boolean userHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();

            String[] splitLine = rawLine.Split(' ');
            String[] splitColonLine = rawLine.Split(new char[] { ':' }, 2);
            String username;
            String real_name;

            try {
                username = splitLine[1];
                real_name = splitColonLine[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "");
            }

            // Only allow one registration
            if(client.State.CompareTo(ClientState.PreAuth) > 0) {
                throw new IrcErrAlreadyRegisteredException(client);
            }

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

        private Boolean pongHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
            String[] splitLine = rawLine.Split(new char[] { ':' }, 2);
            String cookie = splitLine[1];

            if(client.PingCookie == cookie) {
                client.WaitingForPong = false;
                client.LastPong = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }

            return true;
        }

    }
}
