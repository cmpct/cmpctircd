using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Net;
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

        public async void Connect(ServerElement info) {
            // TODO: started logic?
            // TODO: TLS?
            StreamReader reader = null;

            tc = new TcpClient(); 
            try {
                await tc.ConnectAsync(Info.Host.ToString(), Info.Port);
            } catch(SocketException) {
                _ircd.Log.Warn($"Unable to connect to server {Info.Host.ToString()}:{Info.Port}");
                return;
            }

            var server = new Server(_ircd, tc, this, tc.GetStream(), info);
            _servers.Add(server);

            try {
                // Call the appropriate BeginTasks
                // Must be AFTER TLS handshake because could send text
                if(server.Stream.CanRead) {
                    server.BeginTasks();
                } else {
                    throw new InvalidOperationException("Can't read on this socket");
                }

                // Send handshake
                server.SendHandshake();
                server.SendCapab();

                // Once we get a socket, loop indefinitely reading
                reader = new StreamReader(server.Stream);

                // Loop until socket disconnects
                await ReadLoop(server, reader);
            } catch(Exception) {
                server?.Disconnect("Connection reset by host", true, false);
                reader?.Dispose();
            }
        }




    }

}