﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace cmpctircd {
    class SocketListener {
        private Boolean _started = false;
        private TcpListener _listener = null;
        // port
        //private Dictionary<TcpClient, Client> client_mapping = new Dictionary<Socket, Client>();
        // maybe this should be Client?
        private List<TcpClient> _clients = new List<TcpClient>();

        public SocketListener(String IP, int port) {
            _listener = new TcpListener(IPAddress.Any, port);
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
        // This function should be called in a loop
        public async void listenToClients() {
            if (!_started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            // TODO: Loop (should be done by caller instead)
            TcpClient tc = await _listener.AcceptTcpClientAsync();
            HandleClient(tc); // this should split off execution
        }

        async Task HandleClient(TcpClient tc) {
            var client = new Client();
            client.TcpClient = tc;
            client.Buffer = new byte[1024];

            lock (_clients)
                _clients.Add(client.TcpClient);

            // TODO: Loop this

            using (var s = client.TcpClient.GetStream()) {
                var bytesRead = await s.ReadAsync(client.Buffer, 0, client.Buffer.Length);
                if (bytesRead > 0) {
                    // Would a TcpClient have ReadLine for us?
                    string data = Encoding.UTF8.GetString(client.Buffer);
                    Console.WriteLine("Got data:");
                    Console.WriteLine(data);
                } else {
                    Console.WriteLine("No data, killing client");
                    // Close the connection
                    client.TcpClient.Close();
                    lock (_clients) {
                        _clients.Remove(client.TcpClient);
                    }
                }
            }
        }
    }
}
