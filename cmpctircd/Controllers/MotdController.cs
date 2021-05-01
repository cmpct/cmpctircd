using System;

namespace cmpctircd.Controllers {
    public class MotdController : ControllerBase {
        private readonly Client client;

        public MotdController(IRCd ircd, Client client) {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Handler("MOTD", ListenerType.Client)]
        public bool MOTDHandler(HandlerArgs args) {
            client.SendMotd();
            return true;
        }

        [Handler("RULES", ListenerType.Client)]
        public bool RulesHandler(HandlerArgs args) {
            client.SendRules();
            return true;
        }

    }
}
