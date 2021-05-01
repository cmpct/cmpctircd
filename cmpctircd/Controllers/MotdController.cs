using System;

namespace cmpctircd.Controllers {
    public class MotdController : ControllerBase {
        [Handler("MOTD", ListenerType.Client)]
        public bool MOTDHandler(HandlerArgs args) {
            args.Client.SendMotd();
            return true;
        }

        [Handler("RULES", ListenerType.Client)]
        public bool RulesHandler(HandlerArgs args) {
            args.Client.SendRules();
            return true;
        }

    }
}
