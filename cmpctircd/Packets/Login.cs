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
        }

        public Boolean userHandler() {
            //Console.WriteLine("Running user handler...");
            return true;
        }

    }
}
