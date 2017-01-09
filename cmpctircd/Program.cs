using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting cmpctircd");
            Console.WriteLine("==> Host: irc.cmpct.info");
            Console.WriteLine("==> Listening on: 127.0.0.1:6669");
            //Console.ReadKey();
            SocketListener sl = new SocketListener("127.0.0.1", 6669);
            sl.bind();
            try {
                while (true) {
                    sl.listenToClients();
                }
            } finally {
                sl.stop();
            }
        }
    }
}
