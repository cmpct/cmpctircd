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
        private ArrayList _sockets = new ArrayList();

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
        public void listenToClients() {
            if (!_started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            _listener.BeginAcceptTcpClient(new AsyncCallback(acceptClient), _listener);
        }

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
