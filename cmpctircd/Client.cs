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
        public TcpClient TcpClient { get; set; }
        public byte[] Buffer { get; set; }

        public Client() {}
    }
}
