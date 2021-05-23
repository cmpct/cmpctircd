using cmpctircd.Configuration;

namespace cmpctircd {
    public interface ISocketConnectorFactory {
        SocketConnector CreateSocketConnector(IRCd ircd, ServerElement config);
    }
}