using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static cmpctircd.Errors;

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
            handlers.Add(packet.ToUpper(), handler);
            return true;
        }

        public bool findHandler(String packet, Array args)
        {
            List<String> registrationCommands = new List<String>();
            registrationCommands.Add("USER");
            registrationCommands.Add("NICK");
            registrationCommands.Add("PONG");

            Client client = (Client)args.GetValue(1);
            try
            {

                // Restrict the commands which non-registered (i.e. pre PONG, pre USER/NICK) users can execute
                if(client.state.Equals(ClientState.PreAuth) && !registrationCommands.Contains(packet)) {
                    throw new IrcErrNotRegisteredException(client);
                }
                handlers[packet.ToUpper()].Invoke(args);
                Console.WriteLine("Handler for " + packet.ToUpper() + " executed");
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("No handler for " + packet.ToUpper());
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
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
