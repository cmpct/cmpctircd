using System;
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
        // maybe this should be tcpclients
        private List<Socket> _sockets = new List<Socket>();

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
            HandleClient(tc);
        }

        async Task HandleClient(TcpClient tc) {
            var client = new Client();
            client.socket = tc.Client;
            client.buffer = new byte[1024];

            lock (_sockets)
                _sockets.Add(client.socket);

            // use Stream - socket draft shown here
            //var rEvent = new SocketAsyncEventArgs();
            //rEvent.SetBuffer(client.buffer, 0, client.buffer.Length);
            //tc.Client.ReceiveAsync(rEvent);

            // TODO: Loops

            using (var s = tc.GetStream()) {
                var bytesRead = await s.ReadAsync(client.buffer, 0, client.buffer.Length);
                if (bytesRead > 0) {
                    // Would a TcpClient have ReadLine for us?
                    string data = Encoding.UTF8.GetString(client.buffer);
                    Console.WriteLine("Got data:");
                    Console.WriteLine(data);
                } else {
                    Console.WriteLine("No data, killing client");
                    // Close the connectiono
                    client.socket.Close();
                    lock (_sockets) {
                        _sockets.Remove(client.socket);
                    }
                }
            }
        }

        // dead code starts here

        public void acceptClient(IAsyncResult ar) {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient newTcpClient = listener.EndAcceptTcpClient(ar);
            Socket newSocket = newTcpClient.Client;

            Client client = new Client();
            client.socket = newTcpClient.Client;
            client.buffer = new byte[1024];

            Console.WriteLine("Accepting a client");
            lock (_sockets) {
                _sockets.Add(client.socket);
            }

            client.socket.BeginReceive(client.buffer, 0, client.buffer.Length, SocketFlags.None, new AsyncCallback(readClient), client);
            // shouldn't the caller be the one looping?
            _listener.BeginAcceptTcpClient(new AsyncCallback(acceptClient), _listener);
        }

        public void readClient(IAsyncResult ar) {
            Client client = (Client)ar.AsyncState;
            Socket socket = client.socket;
            int bytesRead = client.socket.EndReceive(ar);

            if (bytesRead > 0) {
                // Would a TcpClient have ReadLine for us?
                String data = System.Text.Encoding.UTF8.GetString(client.buffer);
                Console.WriteLine("Got data:");
                Console.WriteLine(data);
                client.socket.BeginReceive(client.buffer, 0, client.buffer.Length, SocketFlags.None, new AsyncCallback(readClient), client);
            } else {
                Console.WriteLine("No data, killing client");
                // Close the connectiono
                client.socket.Close();
                lock (_sockets) {
                    _sockets.Remove(client.socket);
                }
            }
        }
    }
}
