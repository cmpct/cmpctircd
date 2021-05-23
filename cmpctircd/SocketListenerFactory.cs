using cmpctircd.Configuration;
using System;

namespace cmpctircd {
    public class SocketListenerFactory : ISocketListenerFactory {
        private readonly Log log;

        public SocketListenerFactory(Log log) {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SocketListener CreateSocketListener(IRCd ircd, SocketElement config) {
            return new SocketListener(log, ircd, config);
        }
    }
}
