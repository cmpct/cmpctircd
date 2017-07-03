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
        public string Line { get; set; }

        public HandlerArgs(IRCd ircd, Client client, string line)
        {
            IRCd = ircd;
            Client = client;
            Line = line;
        }
    }
}
