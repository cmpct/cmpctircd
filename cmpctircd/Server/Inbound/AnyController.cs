using System;

namespace cmpctircd.Controllers {
    [Controller(ListenerType.Server, ServerType.Any)]
    public class AnyController : ControllerBase {
        private readonly IRCd ircd;
        private readonly Server server;

        public AnyController(IRCd ircd, Server server) {
            this.ircd = ircd ?? throw new ArgumentNullException(nameof(ircd));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
        }

        [Handles("ERROR")]
        public bool ErrorHandler(HandlerArgs args) {
            ircd.Log.Error($"[SERVER] Received an error (from: {server?.Name}), disconnecting: {args.Trailer}");
            server.Disconnect("ERROR: Received an error", false, false);
            return true;
        }


        [Handles("PING")]
        public bool PingHandler(HandlerArgs args) {
            // TODO: implement for hops > 1
            // TODO: could use server.SID instead of SpacedArgs?
            var pingCookie = "";
            if (args.SpacedArgs.Count == 1) {
                pingCookie = args.SpacedArgs[0];
            } else {
                pingCookie = args.SpacedArgs[1];
            }

            server.Write($":{ircd.SID} PONG {args.IRCd.SID} {pingCookie}");
            return true;
        }

        [Handles("PONG")]
        public bool PongHandler(HandlerArgs args) {
            server.WaitingForPong = false;
            server.LastPong       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }
    }
}