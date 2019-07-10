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
        private IRCd _ircd;
        private Boolean _started = false;
        private TcpListener _listener = null;
        private IList<Server> _servers = new List<Server>();

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
        public void Bind() {
            _listener.Start();
            _started = true;
        }
        public void Stop() {
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

        async Task HandleClient(TcpClient tc) {
            Client client = null;
            Server server = null;
            Stream stream = tc.GetStream();

            // Handshake with TLS if they're from a TLS port
            if (Info.IsTls) {
                try {
                    stream = await HandshakeTls(tc);
                } catch (Exception e) {
                    _ircd.Log.Debug($"Exception in HandshakeTls: {e.ToString()}");
                    tc.Close();
                }
            }

            if (Info.Type == ListenerType.Client) {
                client = new Client(_ircd, tc, this, stream);
                Clients.Add(client);

                // Increment the client count
                ++ClientCount;
            } else {
                server = new Server(_ircd, tc, this, stream);
                _servers.Add(server);

                // Increment the server count
                ++ServerCount;
            }

            StreamReader reader = null;

            try {
                // Call the appropriate BeginTasks
                // Must be AFTER TLS handshake because could send text

                // XXX: Regarding the CanRead checks:
                // XXX: Temporary fix for http://bugs.cmpct.info/show_bug.cgi?id=253
                // XXX: May need deeper changes(?)
                if(Info.Type == ListenerType.Client) {
                    if(!client.Stream.CanRead)
                        throw new InvalidOperationException("Can't read on this socket");
                    client.BeginTasks();
                } else {
                    if(!server.Stream.CanRead)
                        throw new InvalidOperationException("Can't read on this socket");
                    server.BeginTasks();
                }

                string line;
                    
                reader = new StreamReader(Info.Type == ListenerType.Client ? client.Stream : server.Stream);

                line = await reader.ReadLineAsync();

                while(line != null) {
                    if (!string.IsNullOrWhiteSpace(line)) {
                        // Read until there's no more left
                        var parts = Regex.Split(line, " ");

                        HandlerArgs args;
                        switch (Info.Type) {
                            case ListenerType.Server:
                                args = new HandlerArgs(_ircd, server, line, false);
                                break;

                            case ListenerType.Client:
                            default:
                                args = new HandlerArgs(_ircd, client, line, false);
                                break;
                        }

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

                        _ircd.PacketManager.FindHandler(search_prefix, args, Info.Type);
                    }
                    // Grab another line
                    line = await reader.ReadLineAsync();
                }
            } catch(Exception) {
                if(client != null) {
                    client.Disconnect("Connection reset by host", true, false);
                } else if (server != null) {
                    server.Disconnect("Connection reset by host", true, false);
                }

                if(reader != null) {
                    reader.Dispose();
                }
            }
        }

        public async Task<SslStream> HandshakeTls(TcpClient tc) {
            SslStream stream = new SslStream(tc.GetStream(), true);
            // NOTE: Must use carefully constructed cert in PKCS12 format (.pfx)
            // NOTE: https://security.stackexchange.com/a/29428
            // NOTE: You will get a NotSupportedException otherwise
            // NOTE: Create a TLS certificate using openssl (or $TOOL), then:
            // NOTE:    openssl pkcs12 -export -in tls_cert.pem -inkey tls_key.pem -out server.pfx
            stream.AuthenticateAsServer(await _ircd.Certificate.GetCertificateAsync(), false, SslProtocols.Tls12, true);
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
