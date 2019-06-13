using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using cmpctircd.Threading;
using cmpctircd.Configuration;

namespace cmpctircd {
     class Program {
         static void Main(string[] args) {
            var log = new Log();
            var config = CmpctConfigurationSection.GetConfiguration();
            using(var sc = new QueuedSynchronizationContext()) {
                SynchronizationContext.SetSynchronizationContext(sc);
                IRCd ircd = new cmpctircd.IRCd(log, config);
                log.Initialise(ircd, config.Loggers.OfType<LoggerElement>().ToList());
                ircd.Run();
                sc.Run();
            }
         }
     }
 }
