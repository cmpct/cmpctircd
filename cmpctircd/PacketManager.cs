using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class PacketManager {
        private IRCd ircd;
        public Dictionary<String, Func<Boolean>> handlers = new Dictionary<string, Func<Boolean>>();
           
        public PacketManager(IRCd ircd) {
            this.ircd = ircd;
        }

        public bool register(String packet, Func<Boolean> handler) {
            handlers.Add(packet, handler);
            return true;
        }

        public bool findHandler(String packet) {
            try {
                handlers[packet].Invoke();
                Console.WriteLine("Handler for " + packet + " executed");
            } catch(KeyNotFoundException) {
                Console.WriteLine("No handler for " + packet);
            }
            return true;
        }

        public bool load() {
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(t => t.IsClass && t.Namespace == "cmpctircd.Packets");
            foreach(Type className in classes) {
                Activator.CreateInstance(Type.GetType(className.ToString()), ircd);
            }
            return true;
        }
    }
}
