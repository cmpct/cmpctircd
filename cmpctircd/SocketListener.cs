using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Text.RegularExpressions;

namespace cmpctircd {
    public class SocketListener {
        private IRCd _ircd;
        private Boolean _started = false;
        private TcpListener _listener = null;
        private List<Server> _servers = new List<Server>();

        public Config.ListenerInfo Info { get; private set; }
        public List<Client> Clients = new List<Client>();
        public int ClientCount = 0;
        public int AuthClientCount = 0;

        public int ServerCount = 0;
        public int AuthServerCount = 0;

        public SocketListener(IRCd ircd, Config.ListenerInfo info) {
            this._ircd = ircd;
            this.Info = info;
            _listener = new TcpListener(info.IP, info.Port);
            lock(_ircd.ClientLists) {
                _ircd.ClientLists.Add(Clients);
            }
            lock(_ircd.ServerLists) {
                _ircd.ServerLists.Add(_servers);
            }
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
                _ircd.Log.Debug($"Shutting down listener [IP: {Info.IP}, Port: {Info.Port}, TLS: {Info.TLS}]");
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

            if (Info.Type == ListenerType.Client) {
                client = new Client(_ircd, tc, this);
                lock (Clients)
                    Clients.Add(client);

                // Increment the client count
                System.Threading.Interlocked.Increment(ref ClientCount);
            } else {
                server = new Server(_ircd, tc, this);
                lock (_servers)
                    _servers.Add(server);

                // Increment the server count
                System.Threading.Interlocked.Increment(ref ServerCount);
            }

            // Handshake with TLS if they're from a TLS port
            if(Info.TLS) {
                HandshakeTls(Info.Type == ListenerType.Client ? (SocketBase) client : (SocketBase) server);
            }

            StreamReader reader = null;

            try {
                // Call the appropriate BeginTasks
                // Must be AFTER TLS handshake because could send text

                // XXX: Regarding the CanRead checks:
                // XXX: Temporary fix for http://bugs.cmpct.info/show_bug.cgi?id=253
                // XXX: May need deeper changes(?)
                if(Info.Type == ListenerType.Client) {
                    if((Info.TLS && !client.TlsStream.CanRead) || (!Info.TLS && !client.Stream.CanRead))
                        throw new InvalidOperationException("Can't read on this socket");

                    client.BeginTasks();
                } else {
                    if((Info.TLS && !server.TlsStream.CanRead) || (!Info.TLS && !server.Stream.CanRead))
                        throw new InvalidOperationException("Can't read on this socket");

                    server.BeginTasks();
                }

                string line;
                    
                // Need to supply a different stream for TLS
                if(Info.TLS) {
                    reader = new StreamReader(Info.Type == ListenerType.Client ? client.TlsStream : server.TlsStream);
                } else {
                    // TODO: need outbound server support
                    reader = new StreamReader(Info.Type == ListenerType.Client ? client.Stream : server.Stream);
                }

                line = await reader.ReadLineAsync();

                while(line != null) {
                    // Read until there's no more left
                    var parts = Regex.Split(line, " ");

                    HandlerArgs args;
                    switch(Info.Type) {
                        case ListenerType.Server:
                            args = new HandlerArgs(_ircd, server, line, false);
                            break;
 
                        case ListenerType.Client:
                        default:
                            args = new HandlerArgs(_ircd, client, line, false);
                            break;
                    }

                    // For ListenerType.Client, search for parts[0]
                    // But for ListenerType.Server, packets are prefixed with :SID
                    _ircd.PacketManager.FindHandler(Info.Type == ListenerType.Client ? parts[0] : parts[1], args, Info.Type);

                    // Grab another line
                    line = await reader.ReadLineAsync();
                }
            } catch(Exception) {
                if(client != null) {
                    client.Disconnect("Connection reset by host", true, false);
                }

                if(reader != null) {
                    reader.Dispose();
                }
            }
        }

        public void HandshakeTls(SocketBase s) {
            try {
                s.TlsStream = new SslStream(s.TcpClient.GetStream(), true);

                // NOTE: Must use carefully constructed cert in PKCS12 format (.pfx)
                // NOTE: https://security.stackexchange.com/a/29428
                // NOTE: You will get a NotSupportedException otherwise
                // NOTE: Create a TLS certificate using openssl (or $TOOL), then:
                // NOTE:    openssl pkcs12 -export -in tls_cert.pem -inkey tls_key.pem -out server.pfx
                X509Certificate serverCertificate = new X509Certificate2(_ircd.Config.TLS_PfxLocation, _ircd.Config.TLS_PfxPassword);
                s.TlsStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls12, true);
            } catch(Exception e) {
                _ircd.Log.Debug($"Exception in HandshakeTls: {e.ToString()}");
                s.Disconnect(false);
            }
        }

        public void Remove(Client client) {
            lock (Clients) {
                Clients.Remove(client);
            }
        }

        public void Remove(Server server) {
            lock (_servers) {
                _servers.Remove(server);
            }
        }
    }
}
