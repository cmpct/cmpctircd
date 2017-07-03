using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Text.RegularExpressions;

namespace cmpctircd {
    public class SocketListener {
        private IRCd _ircd;
        private Boolean _started = false;
        private TcpListener _listener = null;
        private List<Client> _clients = new List<Client>();

        public SocketListener(IRCd ircd, String IP, int port) {
            this._ircd = ircd;
            _listener = new TcpListener(IPAddress.Any, port);
            _ircd.ClientLists.Add(_clients);
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
            TcpClient tc = await _listener.AcceptTcpClientAsync();
            HandleClient(tc); // this should split off execution
        }

        async Task HandleClient(TcpClient tc) {
            var client = new Client(_ircd, tc, this);
            lock (_clients)
                _clients.Add(client);
            
            client.BeginTasks();

            while (true) {
                try {
                    int bytesRead = await client.ClientStream.ReadAsync(client.Buffer, 0, client.Buffer.Length);
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
