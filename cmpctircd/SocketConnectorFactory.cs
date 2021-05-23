using cmpctircd.Configuration;
using System;

namespace cmpctircd {
    public class SocketConnectorFactory : ISocketConnectorFactory {
        private readonly Log log;

        public SocketConnectorFactory(Log log) {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SocketConnector CreateSocketConnector(IRCd ircd, ServerElement config) {
            return new SocketConnector(log, ircd, config);
        }
    }
}
