using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class PacketManager {
        private IRCd ircd;
        // XXX: Instead of Array, this could be a bundle which we send with each packet - args baked in, ircd, etc?
        public Dictionary<String, Func<Array, Boolean>> handlers = new Dictionary<string, Func<Array, Boolean>>();
           
        public PacketManager(IRCd ircd) {
            this.ircd = ircd;
        }

        public bool register(String packet, Func<Array, Boolean> handler) {
            Console.WriteLine("Registering packet: " + packet);
            handlers.Add(packet, handler);
            return true;
        }

        public bool findHandler(String packet, Array args) {
            try {
                handlers[packet.ToUpper()].Invoke(args);
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
