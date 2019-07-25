using System;

namespace cmpctircd {
    public class LogUser {
        public string Nick;
        public long   FirstJoinedTime;
        public long   LastSeenTime;
        public long   LastQuitTime;

        // TODO: validation? ^^^

        public LogUser(string nick) {
            Nick            = nick;
            FirstJoinedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

    }
}