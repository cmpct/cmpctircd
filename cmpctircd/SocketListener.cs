using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using cmpctircd.Configuration;

namespace cmpctircd {
    public class SocketListener {
        protected IRCd _ircd;
        private Boolean _started = false;
        private TcpListener _listener = null;
        protected IList<Server> _servers = new List<Server>();

        public SocketElement Info { get; private set; }
        public IList<Client> Clients { get; } = new List<Client>();
        public int ClientCount = 0;
        public int AuthClientCount = 0;

        public int ServerCount = 0;
        public int AuthServerCount = 0;

        public SocketListener(IRCd ircd, SocketElement info) {
            this._ircd = ircd;
            this.Info = info;
            _listener = new TcpListener(info.Host, info.Port);
            _ircd.ClientLists.Add(Clients);
            _ircd.ServerLists.Add(_servers);
        }
        ~SocketListener() {
            Stop();
        }

        // Bind to the port and start listening
        public virtual void Bind() {
            _listener.Start();
            _started = true;
        }
        public virtual void Stop() {
            if (_started) {
                _ircd.Log.Debug($"Shutting down listener [IP: {Info.Host}, Port: {Info.Port}, TLS: {Info.IsTls}]");
                _listener.Stop();
                _started = false;
            }
        }

        // Accept + read from clients.
        public async Task ListenToClients() {
            if (!_started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            // TODO: Loop (should be done by caller instead)
            while(_started) {
                try {
                    TcpClient tc = await _listener.AcceptTcpClientAsync();
                    HandleClient(tc); // this should split off execution
                } catch(Exception e) {
                    _ircd.Log.Error($"Exception in ListenToClients(): {e.ToString()}");
                }
            }
        }

        protected async Task<Stream> HandshakeIfNeeded(TcpClient tc, Stream stream) {
            // Handshake with TLS if they're from a TLS port
            if (Info.IsTls) {
                try {
                    stream = await HandshakeTlsAsServer(tc);
                } catch (Exception e) {
                    _ircd.Log.Debug($"Exception in HandshakeTls: {e.ToString()}");
                    tc.Close();
                }
            }

            return stream;
        }

        protected SocketBase CreateClientObject(TcpClient tc, Stream stream) {
            // Check whether this listener port is for clients or servers
            if (Info.Type == ListenerType.Client) {
                var client = new Client(_ircd, tc, this, stream);
                Clients.Add(client);

                // Increment the client count
                ++ClientCount;
                return client;
            }

            var server = new Server(_ircd, tc, this, stream);
            _servers.Add(server);

            // Increment the server count
            ++ServerCount;
            return server;
        }

        private async void HandleClient(TcpClient tc) {
            StreamReader reader = null;
            var stream = (Stream) tc.GetStream();
            var socketBase = CreateClientObject(tc, stream);

            // Sends the TLS handshake if we're a TLS listener
            // Swaps out the stream for an SslStream if that's the case
            socketBase.Stream = await HandshakeIfNeeded(tc, stream);

            try {
                // Call the appropriate BeginTasks
                // Must be AFTER TLS handshake because could send text

                // XXX: Regarding the CanRead checks:
                // XXX: Temporary fix for http://bugs.cmpct.info/show_bug.cgi?id=253
                // XXX: May need deeper changes(?)
                if(socketBase.Stream.CanRead) {
                    socketBase.BeginTasks();
                } else {
                    throw new InvalidOperationException("Can't read on this socket");
                }

                reader = new StreamReader(socketBase.Stream);

                // Loop until socket disconnects
                await ReadLoop(socketBase, reader);
            } catch(Exception) {
                socketBase?.Disconnect("Connection reset by host", true, false);
                reader?.Dispose();
            }
        }

        public async Task ReadLoop(SocketBase socketBase, StreamReader reader) {
            var line = await reader.ReadLineAsync();

            while(line != null) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    // Read until there's no more left
                    var parts = Regex.Split(line, " ");
                    var args  = GetHandlerArgs(socketBase, line);
                    var search_prefix = GetPacketPrefix(parts);

                    _ircd.PacketManager.FindHandler(search_prefix, args, Info.Type);
                }
                // Grab another line
                line = await reader.ReadLineAsync();
            }
        }

        public string GetPacketPrefix(string[] parts) {
            // For ListenerType.Client, search for parts[0]
            // But for ListenerType.Server, packets post-authentication are prefixed with :SID
            var search_prefix = parts[0];
            if (Info.Type == ListenerType.Server) {
                // Did check for ServerState.Auth before but no need
                if (parts[0].StartsWith(":") && parts.Count() > 1) {
                    // (typically) authenticated server or they start their packets with their SID
                    search_prefix = parts[1];
                } else {
                    // (typically) unauthenticated server
                    // (e.g. CAPAB ...)
                    search_prefix = parts[0];
                }
            }

            return search_prefix;
        }

        public HandlerArgs GetHandlerArgs(SocketBase socketBase, string line) {
            HandlerArgs args;
            switch (Info.Type) {
                case ListenerType.Server:
                    args = new HandlerArgs(_ircd, socketBase as Server, line, false);
                    break;

                case ListenerType.Client:
                default:
                    args = new HandlerArgs(_ircd, socketBase as Client, line, false);
                    break;
            }

            return args;
        }

        public async Task<SslStream> HandshakeTlsAsServer(TcpClient tc) {
            SslStream stream = new SslStream(tc.GetStream(), true);
            // NOTE: Must use carefully constructed cert in PKCS12 format (.pfx)
            // NOTE: https://security.stackexchange.com/a/29428
            // NOTE: You will get a NotSupportedException otherwise
            // NOTE: Create a TLS certificate using openssl (or $TOOL), then:
            // NOTE:    openssl pkcs12 -export -in tls_cert.pem -inkey tls_key.pem -out server.pfx
            stream.AuthenticateAsServer(await _ircd.Certificate.GetCertificateAsync(), false, SslProtocols.Tls12, true);
            return stream;
        }

        public async Task<SslStream> HandshakeTlsAsClient(TcpClient tc, string host, bool verifyCert = true) {
            SslStream stream;

            if (verifyCert) {
                stream = new SslStream(tc.GetStream(), true);
            } else {
                _ircd.Log.Warn($"[SERVER] Connecting out to server {host} with TLS verification disabled: this is dangerous!");
                stream = new SslStream(tc.GetStream(), true, (sender, certificate, chain, sslPolicyErrors) => true);
            }

            await stream.AuthenticateAsClientAsync(host);
            return stream;
        }

        public void Remove(Client client) {
            Clients.Remove(client);
        }

        public void Remove(Server server) {
            _servers.Remove(server);
        }
    }
}
