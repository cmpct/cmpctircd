using cmpctircd.Configuration;

namespace cmpctircd {
    public interface ISocketListenerFactory {
        SocketListener CreateSocketListener(IRCd ircd, SocketElement config);
    }
}