using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.IO;
using System.Net.Sockets;
using cmpctircd.Configuration;

namespace cmpctircd {
    public class SocketConnector : SocketListener {

        private TcpClient tc;
        private NetworkStream stream;

        public SocketConnector(IRCd ircd, SocketElement info) : base(ircd, info) {
            // TODO
            _ircd = ircd;
        }

        // TODO: How to sanely noop this?
        // TODO: Rename both to 'Start'?
        public override void Bind() {}
        public override void Stop() {}

        public async Task Connect(string password) {
            // TODO: started logic?
            // TODO: TLS?
            StreamReader reader;

            tc = new TcpClient(); 
            await tc.ConnectAsync(Info.Host.ToString(), Info.Port);

            reader = new StreamReader(tc.GetStream());

            var server = new Server(_ircd, tc, this, tc.GetStream());
            
            var list  = new List<Server>();
            list.Add(server);

            _ircd.ServerLists.Add(list);
            // Once we get a socket, loop indefinitely reading
            ReadLoop(server, reader);
        }




    }

}