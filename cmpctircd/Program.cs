using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using cmpctircd.Threading;

namespace cmpctircd {
     class Program {
         static void Main(string[] args) {
            var log = new Log();
            var config = new cmpctircd.Config(log);
            var configData = config.Parse();
            using(var sc = new QueuedSynchronizationContext()) {
                SynchronizationContext.SetSynchronizationContext(sc);
                IRCd ircd = new cmpctircd.IRCd(log, configData);
                log.Initialise(ircd, configData.Loggers);
                ircd.Run();
                sc.Run();
            }
         }
     }
 }
