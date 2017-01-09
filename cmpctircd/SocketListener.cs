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
        private Boolean started = false;
        private TcpListener listener = null;
        private int port;
        private Dictionary<Socket, Client> socket_mapping = new Dictionary<Socket, Client>();

        public SocketListener(String IP, int port) {
            listener = new TcpListener(IPAddress.Any, port);
        }
        ~SocketListener() {
            stop();
        }

        // Bind to the port and start listening
        public void bind() {
            listener.Start();
            started = true;
        }
        public void stop() {
            listener.Stop();
            started = false;
        }

        // Accept + read from clients.
        // This function should be called in a loop
        public void listenToClients() {
            if(!started) {
                throw new InvalidOperationException("Bind() not called or has been stopped.");
            }

            ArrayList listenList = new ArrayList();
            ArrayList tempList = new ArrayList();
            while (true) {
                listenList.Add(listener.Server);
                Socket.Select(listenList, null, null, 1000);
                foreach(Socket socket in listenList.ToArray()) {
                    if (socket.Equals(listener.Server)) {
                        // This is a new socket
                        Socket newSocket = listener.AcceptSocket();
                        Client client = new Client();

                        socket_mapping.Add(newSocket, client);
                        if (!listenList.Contains(newSocket)) {
                            listenList.Add(newSocket);
                        }

                        client.UUID = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8);
                        Console.WriteLine("created client: " + client.UUID);
                    } else {
                        // Existing socket
                        Client client = socket_mapping[socket];
                        Console.WriteLine(client.UUID);
                        if (!listenList.Contains(socket)) {
                            listenList.Add(socket);
                        }
                        byte[] buffer = new byte[5];
                        socket.Receive(buffer);
                        Console.WriteLine(buffer.ToString());
                    }
                }
            }
        }

    }
}
