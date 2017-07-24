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
            var config = new cmpctircd.Config();
            var configData = config.Parse();

            IRCd ircd = new cmpctircd.IRCd(configData);
            ircd.Run();
        }
    }
}
