using cmpctircd.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cmpctircd {
    public class PacketManager {
        private IRCd _ircd;
        private IDictionary<String, IList<HandlerInfo>> _handlers = new Dictionary<string, IList<HandlerInfo>>();

        public struct HandlerInfo {
            public string Packet;
            public Func<HandlerArgs, Boolean> Handler;
            public ListenerType Type;
            public ServerType ServerType;
        }

        public PacketManager(IRCd ircd) {
            _ircd = ircd;
        }

        public bool Register(HandlerInfo info) {
            _ircd.Log.Debug("Registering packet: " + info.Packet);
            if (_handlers.ContainsKey(info.Packet.ToUpper())) {
                // Already a handler for this packet so add it to the list
                _handlers[info.Packet].Add(info);
            } else {
                // No handlers for this packet yet, so create the List
                var list = new List<HandlerInfo>();
                list.Add(info);
                _handlers.Add(info.Packet.ToUpper(), list);
            }
            return true;
        }

        public bool Register(string packet, Func<HandlerArgs, Boolean> handler, ListenerType type = ListenerType.Client) {
            // Legacy function, defaults to registering ListenerType.Client packets
            return Register(new HandlerInfo {
                Packet  = packet,
                Handler = handler,
                Type    = type
            });
        }

        public bool FindHandler(String packet, HandlerArgs args, ListenerType type, bool convertUids = false)
        {
            List<String> registrationCommands = new List<String>();
            List<String> idleCommands = new List<String>();

            if(convertUids) {
                args.Line   = _ircd.ReplaceUUIDWithNick(args.Line);
                args.Client = _ircd.GetClientByNick(_ircd.ExtractIdentifierFromMessage(args.Line, true));
            }

            var client = args.Client;
            if (client != null) {
                registrationCommands.Add("USER");
                registrationCommands.Add("NICK");
                registrationCommands.Add("CAP"); // TODO: NOT YET IMPLEMENTED
                registrationCommands.Add("PONG");
                registrationCommands.Add("PASS"); // TODO: NOT YET IMPLEMENTED
                idleCommands.Add("PING");
                idleCommands.Add("PONG");
                idleCommands.Add("WHOIS");
                idleCommands.Add("WHO");
                idleCommands.Add("NAMES");
                idleCommands.Add("AWAY");

                try {
                    // Restrict the commands which non-registered (i.e. pre PONG, pre USER/NICK) users can execute
                    if((client.State.Equals(ClientState.PreAuth) || (args.IRCd.Config.Advanced.ResolveHostnames && args.Client.ResolvingHost)) && !registrationCommands.Contains(packet.ToUpper())) {
                        throw new IrcErrNotRegisteredException(client);
                    }

                    // Only certain commands should reset the idle clock
                    if(!idleCommands.Contains(packet.ToUpper())) {
                        client.IdleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                } catch(Exception e) {
                    _ircd.Log.Debug($"Exception (client): {e.ToString()}");
                    return false;
                }
            } else {
                // Server
                var server = args.Server;
                try {
                    // TODO: Per-protocol?
                    registrationCommands.Add("CAPAB");
                    registrationCommands.Add("SERVER");
                    registrationCommands.Add("PING");
                    registrationCommands.Add("PONG");

                    _ircd.Log.Debug($"Got a server line: {args.Line}");

                    if(server.State.Equals(ServerState.PreAuth) && !registrationCommands.Contains(packet.ToUpper())) {
                        _ircd.Log.Error($"Server just tried to use command pre-auth: {packet.ToUpper()}");
                        server.Disconnect("ERROR: Sent command before auth (send SERVER packet!)", true);
                        return false;
                    }
                } catch(Exception e) {
                    _ircd.Log.Debug($"Exception (server): ${e.ToString()}");
                    return false;
                }
            }

            try {
                List<HandlerInfo> functions;

                if (client != null) {
                    // Client
                    // Call handlers with no type defined (i.e. ServerType.Dummy)
                    functions = FindHandlers(packet, type);
                } else {
                    // Server
                    // Only call handlers matching our exact server type, or ServerType.Any
                    functions = FindHandlers(packet, type, args.Server.Type);
                }

                if(functions.Count() > 0) {
                    foreach(var record in functions) {
                        // Invoke all of the handlers for the command
                        record.Handler.Invoke(args);
                    }
                } else {
                    _ircd.Log.Debug("No handler for " + packet.ToUpper());
                    if(client != null)
                        throw new IrcErrUnknownCommandException(client, packet.ToUpper());
                }
            } catch(Exception e) {
                _ircd.Log.Debug("Exception: " + e.ToString());
                return false;
            }
            return true;
        }


        private List<HandlerInfo> FindHandlers(string name, ListenerType type, ServerType serverType = ServerType.Dummy) {
            var functions = new List<HandlerInfo>();
            name = name.ToUpper();
            if (_handlers.ContainsKey(name)) {
                functions.AddRange(_handlers[name].Where(
                    record => record.Type == type &&
                    (record.ServerType == serverType || record.ServerType == ServerType.Any)
                ));
            }
            return functions;
        }

        public bool Load() {
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .SelectMany(t => t.GetMethods())
                .ForEach(
                    m => m.GetCustomAttributes(typeof(Handler), false).ForEach(a => {
                        Handler attr = (Handler) a;
                        if(!m.IsStatic)
                            _ircd.Log.Warn($"'{m.DeclaringType.FullName}.{m.Name}' is not static. Handler methods loaded through reflection must be static.");
                        else {
                            var handler = new HandlerInfo {
                                Packet  = attr.Command,
                                Handler = (Func<HandlerArgs, bool>) Delegate.CreateDelegate(typeof(Func<HandlerArgs, bool>), m),
                                Type    = attr.Type,
                                ServerType = attr.ServerType,
                            };

                            Register(handler);
                        }
                    })
                );
            return true;
        }
    }
}
