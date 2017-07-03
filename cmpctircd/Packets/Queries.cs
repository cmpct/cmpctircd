using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets
{
    public class Queries
    {

        // This class is for the server query group of commands
        // TODO: Stats & Links?
        public Queries(IRCd ircd)
        {
            ircd.PacketManager.Register("VERSION", versionHandler);
        }

        public Boolean versionHandler(Array args)
        {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);

            client.SendVersion();
            return true;
        }
    }
}
