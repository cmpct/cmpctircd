using System;

namespace cmpctircd.Packets {
    public class Login {
        //private IRCd ircd;

        public Login(IRCd ircd) {
            ircd.PacketManager.Register("USER", userHandler);
            ircd.PacketManager.Register("NICK", nickHandler);
            ircd.PacketManager.Register("QUIT", quitHandler);
            ircd.PacketManager.Register("PONG", pongHandler);
        }

        public Boolean userHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;

            String[] splitLine = rawLine.Split(new char[] { ' ' }, 5);
            String username;
            String real_name;

            try {
                username = splitLine[1];
                real_name = splitLine[4].StartsWith(":") ? splitLine[4].Substring(1) : splitLine[4];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "");
            }

            // Only allow one registration
            if(client.State.CompareTo(ClientState.PreAuth) > 0) {
                throw new IrcErrAlreadyRegisteredException(client);
            }

            client.SetUser(username, real_name);
            return true;
        }

        public Boolean nickHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            String newNick = rawLine.Split(' ')[1];
            // Some bots will try to send ':' with the channel, remove this
            newNick = newNick.StartsWith(":") ? newNick.Substring(1) : newNick;
            ircd.Log.Debug($"Changing nick to {newNick}");
            client.SetNick(newNick);
            return true;
        }

        private Boolean quitHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            String[] splitLine = rawLine.Split(new char[] { ':' }, 2);
            string reason;
            try {
                reason = splitLine[1];
            } catch(IndexOutOfRangeException) {
                reason = "Leaving";
            }

            client.Disconnect(reason, true);
            return true;
        }

        private Boolean pongHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            String[] splitLine = rawLine.Split(new char[] { ':' }, 2);
            String cookie;

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
