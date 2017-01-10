using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace cmpctircd
{
    class Client
    {
        // make this tcpclient?
        public Socket socket;
        public byte[] buffer;

        public Client() {}
    }
}
