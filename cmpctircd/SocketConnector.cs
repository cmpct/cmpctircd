using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Net;
using System.IO;
using System.Net.Sockets;
using cmpctircd.Configuration;

namespace cmpctircd {
    public class SocketConnector : SocketListener {

        public ServerElement ServerInfo;
        public bool Connected;
        private TcpClient tc;
        private NetworkStream stream;


        public SocketConnector(IRCd ircd, ServerElement info) : base(ircd, info) {
            ServerInfo = info;
        }

        // TODO: How to sanely noop this?
        // TODO: Rename both to 'Start'?
        public override void Bind() {}
        public override void Stop() {}

        public async Task Connect() {
            if (Connected) {
                throw new InvalidOperationException("Called SocketConnector.Connect() on connected SocketConnector");
            }

            StreamReader reader = null;
            Stream stream;

            tc = new TcpClient();
            try {
                await tc.ConnectAsync(Info.Host, Info.Port);
                stream = tc.GetStream();
            } catch (SocketException) {
                _ircd.Log.Warn($"Unable to connect to server {Info.Host}:{Info.Port}");
                return;
            }

            if (ServerInfo.Tls) {
                // If we're TLS, we need to handshake immediately
                stream = await HandshakeTlsAsClient(tc, ServerInfo.Host, ServerInfo.VerifyTlsCert);
            }

            var server = new Server(_ircd, tc, this, stream, ServerInfo);
            _servers.Add(server);

            try {
                Connected = true;

                // Call the appropriate BeginTasks
                // Must be AFTER TLS handshake because could send text
                if (stream.CanRead) {
                    server.BeginTasks();
                } else {
                    throw new InvalidOperationException("Can't read on this socket");
                }

                // Send handshake
                server.SendHandshake();
                server.SendCapab();

                // Once we get a socket, loop indefinitely reading
                reader = new StreamReader(stream);

                // Loop until socket disconnects
                await ReadLoopAsync(server, reader);
            } catch (Exception) {
                Connected = false;
                server?.Disconnect("Connection reset by host", true, false);
                reader?.Dispose();
            }
        }




    }

}