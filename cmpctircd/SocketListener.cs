﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Text.RegularExpressions;

namespace cmpctircd {
    class SocketListener {
        private IRCd _ircd;
        private Boolean _started = false;
        private TcpListener _listener = null;
        private Dictionary<Client, TcpClient> _clients = new Dictionary<Client, TcpClient>();

        public SocketListener(IRCd ircd, String IP, int port) {
            this._ircd = ircd;
            _listener = new TcpListener(IPAddress.Any, port);
            _ircd.clientLists.Add(_clients);
        }
        ~SocketListener() {
            stop();
        }

        // Bind to the port and start listening
        public void bind() {
            _listener.Start();
            _started = true;
        }
        public void stop() {
            _listener.Stop();
            _started = false;
        }

        // Accept + read from clients.
        // This function should be called in a loop, and waited on
        public async Task listenToClients() {
            if (!_started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            // TODO: Loop (should be done by caller instead)
            TcpClient tc = await _listener.AcceptTcpClientAsync();
            HandleClient(tc); // this should split off execution
        }

        async Task HandleClient(TcpClient tc) {
            var client = new Client();
            client.IRCd = _ircd;
            client.TcpClient = tc;
            client.Buffer = new byte[1024];
            client.Listener = this;

            lock (_clients)
                _clients.Add(client, client.TcpClient);
            
            client.begin_tasks();

            using (var s = client.TcpClient.GetStream()) {
                while (true) {
                    try {
                        int bytesRead = await s.ReadAsync(client.Buffer, 0, client.Buffer.Length);
                        if (bytesRead > 0) {
                            // Would a TcpClient have ReadLine for us?
                            string data = Encoding.UTF8.GetString(client.Buffer);
                            string[] lines = Regex.Split(data, "\r\n");
                            foreach (string line in lines) {
                                // Split each line into bits
                                string[] parts = Regex.Split(line, " ");
                                object[] args = new object[] { _ircd, client, line };
                                if (parts[0].Contains("\0")) continue;
                                _ircd.packetManager.findHandler(parts[0], args);
                            }
                            client.Buffer = new Byte[1024];
                        } else {
                            Console.WriteLine("No data, killing client");
                            // Close the connection
                            client.disconnect(false);
                        }
                    } catch(ObjectDisposedException) {
                        client.disconnect(false);
                        break;
                    };
                }
            }
        }

        public void remove(Client client) {
            client.TcpClient.Close();
            lock (_clients) {
                _clients.Remove(client);
            }
        }
    }
}
