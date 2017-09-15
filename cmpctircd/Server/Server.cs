using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class Server : SocketBase {
        // Internals
        // TODO: Many of these look like they shouldn't be public or should be private set. Review?
        public ServerState State { get; set; }
 
        public Server(IRCd ircd, TcpClient tc, SocketListener sl) : base(ircd, tc, sl) {
            State = ServerState.PreAuth;
        }

        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
