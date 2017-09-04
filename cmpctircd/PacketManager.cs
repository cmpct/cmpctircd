using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class PacketManager {
        private IRCd ircd;
        // XXX: Instead of Array, this could be a bundle which we send with each packet - args baked in, ircd, etc?
        public Dictionary<String, List<Func<HandlerArgs, Boolean>>> handlers = new Dictionary<string, List<Func<HandlerArgs, Boolean>>>();

        public PacketManager(IRCd ircd) {
            this.ircd = ircd;
        }

        public bool Register(String packet, Func<HandlerArgs, Boolean> handler) {
            ircd.Log.Debug("Registering packet: " + packet);
            if (handlers.ContainsKey(packet.ToUpper())) {
                // Already a handler for this packet so add it to the list
                handlers[packet].Add(handler);
            } else {
                // No handlers for this packet yet, so create the List
                var list = new List<Func<HandlerArgs, bool>>();
                list.Add(handler);
                handlers.Add(packet.ToUpper(), list);
            }
            return true;
        }

        public bool FindHandler(String packet, HandlerArgs args)
        {
            List<String> registrationCommands = new List<String>();
            registrationCommands.Add("USER");
            registrationCommands.Add("NICK");
            registrationCommands.Add("CAP"); // TODO: NOT YET IMPLEMENTED
            registrationCommands.Add("PONG");

            List<String> idleCommands = new List<String>();
            idleCommands.Add("PING");
            idleCommands.Add("PONG");
            idleCommands.Add("WHOIS");
            idleCommands.Add("WHO");
            idleCommands.Add("NAMES");
            idleCommands.Add("AWAY");

            var client = args.Client;
            try
            {

                // Restrict the commands which non-registered (i.e. pre PONG, pre USER/NICK) users can execute
                if(client.State.Equals(ClientState.PreAuth) && !registrationCommands.Contains(packet.ToUpper())) {
                    throw new IrcErrNotRegisteredException(client);
                }

                // Only certain commands should reset the idle clock
                if(!idleCommands.Contains(packet.ToUpper())) {
                    client.IdleTime = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                }

                if(handlers.ContainsKey(packet.ToUpper())) {
                    foreach(var record in handlers[packet.ToUpper()]) {
                        // Invoke all of the handlers for the command
                        record.Invoke(args);
                    }
                } else {
                    ircd.Log.Debug("No handler for " + packet.ToUpper());
                    throw new IrcErrUnknownCommandException(client, packet.ToUpper());
                }
                ircd.Log.Debug("Handler for " + packet.ToUpper() + " executed");
            } catch (Exception e) {
                ircd.Log.Debug("Exception: " + e.ToString());
            }
            return true;
        }

        public bool Load() {
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(
                                       t => t.IsClass &&
                                            t.Namespace == "cmpctircd.Packets" &&
                                            t.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Count() == 0
                                    );
            foreach(Type className in classes) {
                Activator.CreateInstance(Type.GetType(className.ToString()), ircd);
            }
            return true;
        }
    }
}
