using System;

namespace cmpctircd.Controllers {
    public static class MotdController {
        [Handler("MOTD", ListenerType.Client)]
        public static bool MOTDHandler(HandlerArgs args) {
            args.Client.SendMotd();
            return true;
        }

        [Handler("RULES", ListenerType.Client)]
        public static bool RulesHandler(HandlerArgs args) {
            args.Client.SendRules();
            return true;
        }

    }
}
