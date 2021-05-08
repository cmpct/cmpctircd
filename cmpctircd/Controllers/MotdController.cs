using System;

namespace cmpctircd.Controllers {
    [Controller(ListenerType.Client)]
    public class MotdController : ControllerBase {
        private readonly Client client;

        public MotdController(IRCd ircd, Client client) {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Handles("MOTD")]
        public bool MOTDHandler(HandlerArgs args) {
            client.SendMotd();
            return true;
        }

        [Handles("RULES")]
        public bool RulesHandler(HandlerArgs args) {
            client.SendRules();
            return true;
        }

    }
}
