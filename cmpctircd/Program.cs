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
            IRCd ircd = new cmpctircd.IRCd();
            ircd.Run();
        }
    }
}
