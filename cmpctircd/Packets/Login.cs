using System;

namespace cmpctircd.Packets {
    public static class Login {
        [Handler("USER", ListenerType.Client)]
        public static bool UserHandler(HandlerArgs args) {
            Client client = args.Client;

            string username;
            string real_name;

            if(args.SpacedArgs.Count >= 3 && args.Trailer != null) {
                username = args.SpacedArgs[1];
                real_name = args.Trailer;
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

        [Handler("NICK", ListenerType.Client)]
        public static bool NickHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;

            var newNick = args.SpacedArgs.Count > 1 ? args.SpacedArgs[1] : args.Trailer;
            ircd.Log.Debug($"Changing nick to {newNick}");
            client.SetNick(newNick);
            return true;
        }

        [Handler("QUIT", ListenerType.Client)]
        public static bool quitHandler(HandlerArgs args) {
            Client client = args.Client;
            string reason;
            try {
                reason = args.SpacedArgs[1];
            } catch(ArgumentOutOfRangeException) {
                reason = "Leaving";
            }

            client.Disconnect(reason, true);
            return true;
        }

        [Handler("PONG", ListenerType.Client)]
        public static bool PongHandler(HandlerArgs args) {
            Client client = args.Client;
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
                client.WaitingForPong = false;
                client.LastPong = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            return true;
        }

    }
}
