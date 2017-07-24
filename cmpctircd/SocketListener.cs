using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private List<Client> _clients = new List<Client>();

        public bool TLS { get; set; } = false;

        public SocketListener(IRCd ircd, IPAddress IP, int port, bool TLS) {
            this._ircd = ircd;
            this.TLS = TLS;
            _listener = new TcpListener(IP, port);
            lock(_ircd.ClientLists) {
                _ircd.ClientLists.Add(_clients);
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
            _listener.Stop();
            _started = false;
        }

        // Accept + read from clients.
        // This function should be called in a loop, and waited on
        public async Task ListenToClients() {
            if (!_started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            // TODO: Loop (should be done by caller instead)
            while(true) {
                TcpClient tc = await _listener.AcceptTcpClientAsync();
                HandleClient(tc); // this should split off execution
            }
        }

        async Task HandleClient(TcpClient tc) {
            var client = new Client(_ircd, tc, this);
            lock (_clients)
                _clients.Add(client);
            
            // Handshake with TLS if they're from a TLS port
            SslStream sslStream = null;
            if(TLS) {
                try {
                    sslStream = new SslStream(client.TcpClient.GetStream(), true);
                    client.ClientTlsStream = sslStream;

                    // TODO: must use carefully constructed cert in PKCS12 format (.pfx)
                    X509Certificate serverCertificate = new X509Certificate2("server.pfx", "pass");
                    sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls, true);

                    sslStream.ReadTimeout = 5000;
                    sslStream.WriteTimeout = 5000;
                } catch(Exception e) {
                    Console.WriteLine(e.ToString());
                }
            }

            client.BeginTasks();

            while (true) {
                try {
                    int bytesRead;
                    if(TLS) {
                        bytesRead = await sslStream.ReadAsync(client.Buffer, 0, client.Buffer.Length);
                    } else {
                        bytesRead = await client.ClientStream.ReadAsync(client.Buffer, 0, client.Buffer.Length);
                    }
                    if (bytesRead > 0) {
                        // Would a TcpClient have ReadLine for us?
                        string data = Encoding.UTF8.GetString(client.Buffer);
                        string[] lines = Regex.Split(data, "\r\n");
                        foreach (string line in lines) {
                            // Split each line into bits
                            string[] parts = Regex.Split(line, " ");
                            var args = new HandlerArgs(_ircd, client, line);
                            if (parts[0].Contains("\0")) continue;
                            _ircd.PacketManager.FindHandler(parts[0], args);
                        }
                        client.Buffer = new Byte[1024];
                    } else {
                        Console.WriteLine("No data, killing client");
                        // Close the connection
                        client.Disconnect(false);
                        break;
                    }
                } catch(ObjectDisposedException) {
                    client.Disconnect(false);
                    break;
                };
            }
        }

        public void Remove(Client client) {
            lock (_clients) {
                _clients.Remove(client);
            }
        }
    }
}
