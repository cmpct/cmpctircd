using cmpctircd.Configuration;

namespace cmpctircd {
    public class SocketListenerFactory : ISocketListenerFactory {
        public SocketListener CreateSocketListener(IRCd ircd, SocketElement config) {
            return new SocketListener(ircd, config);
        }
    }
}
