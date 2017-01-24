using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace cmpctircd
{
    class Client
    {
        // Internals
        public TcpClient TcpClient { get; set; }
        public byte[] Buffer { get; set; }

        // Metadata
        // TODO: Make these read-only apart from via setNick()?
        public String nick;
        public String ident;
        public String real_name;

        public Boolean setNick(String nick) {
            // Return if nick is the same
            if (String.Compare(nick, this.nick, false) == 0)
                return true;

            // TODO: Verify the nickname is safe
            this.nick = nick;

            return true;
        }



        public Client() {}
    }
}
