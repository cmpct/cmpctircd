using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    /// <summary>
    /// Represents function arguments for a packet handler.
    /// </summary>
    public class HandlerArgs
    {
        public IRCd IRCd { get; set; }
        public Client Client { get; set; }
        public Server Server { get; set; }
        public string Line { get; set; }
        public bool Force { get; set; }

        public HandlerArgs(IRCd ircd, Client client, string line, bool force) {
            IRCd = ircd;
            Client = client;
            Line = line;
            Force = force;
        }

        public HandlerArgs(IRCd ircd, Server server, string line, bool force) {
            IRCd = ircd;
            Server = server;
            Line = line;
            Force = force;
        }

    }
}
