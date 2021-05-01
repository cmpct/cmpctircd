using System;

namespace cmpctircd.Controllers {
    [Controller(ListenerType.Client)]
    public class LoginController : ControllerBase {
        private readonly IRCd ircd;
        private readonly Client client;

        public LoginController(IRCd ircd, Client client) {
            this.ircd = ircd ?? throw new ArgumentNullException(nameof(ircd));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Handles("USER")]
        public bool UserHandler(HandlerArgs args) {
            string username;
            string real_name;

            // Format:
            // USER username hostname server_name (:)real_name
            // hostname, server_name ignored
            if(args.SpacedArgs.Count >= 3) {
                username  = args.SpacedArgs[1];
                real_name = args.Trailer != null ? args.Trailer : args.SpacedArgs[4];
            } else {
                throw new IrcErrNotEnoughParametersException(client, "");
            }

            // Only allow one registration
            if(client.State.CompareTo(ClientState.PreAuth) > 0) {
                throw new IrcErrAlreadyRegisteredException(client);
            }

            client.SetUser(username, real_name);
            return true;
        }

        [Handles("NICK")]
        public bool NickHandler(HandlerArgs args) {
            var newNick = args.SpacedArgs.Count > 1 ? args.SpacedArgs[1] : args.Trailer;
            ircd.Log.Debug($"Changing nick to {newNick}");
            client.SetNick(newNick);
            return true;
        }

        [Handles("QUIT")]
        public bool quitHandler(HandlerArgs args) {
            string reason;
            try {
                reason = args.SpacedArgs[1];
            } catch(ArgumentOutOfRangeException) {
                reason = "Leaving";
            }

            client.Disconnect(reason, true);
            return true;
        }

        [Handles("PONG")]
        public bool PongHandler(HandlerArgs args) {
            string rawLine = args.Line;
            string[] splitLine = rawLine.Split(new char[] { ':' }, 2);
            string cookie;

            try {
                cookie = splitLine[1];
            } catch(IndexOutOfRangeException) {
                // We've assumed the message is: PONG :cookie (or PONG server :cookie)
                // But some clients seem to send PONG cookie, so look for that if we've not found a cookie
                splitLine = rawLine.Split(new char[] { ' '}, 2);
                cookie    = splitLine[1];
            }

            if(client.PingCookie == cookie) {
                // Keep track of this so we know if this was first ever PONG
                // Needed because SendWelcome needs to know if waiting for a pong
                var prevLastPong = client.LastPong;

                client.WaitingForPong = false;
                client.LastPong = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (ircd.RequirePong && prevLastPong == 0) {
                    // Got a correct PONG and not had one yet
                    // So call handshake
                    client.SendWelcome();
                }
            }

            return true;
        }

    }
}
