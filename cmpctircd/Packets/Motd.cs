using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets {
    public class Motd {

        // This class is for the MOTD and RULES commands
        // TODO: Attach to user logon event and fire client.send_motd() there?
        public Motd(IRCd ircd) {
            ircd.PacketManager.Register("MOTD", motdHandler);
            ircd.PacketManager.Register("RULES", rulesHandler);
        }

        public Boolean motdHandler(HandlerArgs args) {
            args.Client.SendMotd();
            return true;
        }

        public Boolean rulesHandler(HandlerArgs args) {
            args.Client.SendRules();
            return true;
        }

    }
}
