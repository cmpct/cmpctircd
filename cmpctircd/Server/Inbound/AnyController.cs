using System;

namespace cmpctircd.Controllers {
    public static class AnyController {
        [Handler("ERROR", ListenerType.Server, ServerType.Any)]
        public static bool ErrorHandler(HandlerArgs args) {
            args.IRCd.Log.Error($"[SERVER] Received an error (from: {args.Server?.Name}), disconnecting: {args.Trailer}");
            args.Server.Disconnect("ERROR: Received an error", false, false);
            return true;
        }


        [Handler("PING", ListenerType.Server, ServerType.Any)]
        public static bool PingHandler(HandlerArgs args) {
            // TODO: implement for hops > 1
            // TODO: could use args.Server.SID instead of SpacedArgs?
            var pingCookie = "";
            if (args.SpacedArgs.Count == 1) {
                pingCookie = args.SpacedArgs[0];
            } else {
                pingCookie = args.SpacedArgs[1];
            }

            args.Server.Write($":{args.IRCd.SID} PONG {args.IRCd.SID} {pingCookie}");
            return true;
        }

        [Handler("PONG", ListenerType.Server, ServerType.Any)]
        public static bool PongHandler(HandlerArgs args) {
            args.Server.WaitingForPong = false;
            args.Server.LastPong       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }
    }
}