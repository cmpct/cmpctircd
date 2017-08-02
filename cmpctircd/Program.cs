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
            var log = new Log();
            var config = new cmpctircd.Config(log);
            var configData = config.Parse();

            IRCd ircd = new cmpctircd.IRCd(log, configData);
            log.Initialise(ircd, configData.Loggers);
            ircd.Run();
         }
     }
 }
