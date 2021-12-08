using cmpctircd.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace cmpctircd {
    public class PacketManager {
        private readonly IRCd _ircd;
        private readonly IDictionary<string, IList<HandlerInfo>> _handlers = new Dictionary<string, IList<HandlerInfo>>();
        private readonly IServiceProvider _services;

        public struct HandlerInfo {
            public string Packet;
            public Func<object, HandlerArgs, bool> Delegate;
            public Type ControllerType;
            public ListenerType ListenerType;
            public ServerType ServerType;
        }

        public PacketManager(IRCd ircd, IServiceProvider services) {
            _ircd = ircd;
            _services = services;
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

        public bool Handle(String packet, IRCd ircd, HandlerArgs args, ListenerType type) {
            args.Line = _ircd.ReplaceUUIDWithNick(args.Line);
            var sender = _ircd.GetClientByNick(_ircd.ExtractIdentifierFromMessage(args.Line, true));
            return Handle(packet, ircd, sender, args, type);
        }

        public bool Handle(String packet, IRCd ircd, object sender, HandlerArgs args, ListenerType type)
        {
            List<String> registrationCommands = new List<String>();
            List<String> idleCommands = new List<String>();

            if (sender is Client client) {
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
                    if ((client.State.Equals(ClientState.PreAuth) || (ircd.Config.Value.Advanced.ResolveHostnames && client.ResolvingHost)) && !registrationCommands.Contains(packet.ToUpper())) {
                        throw new IrcErrNotRegisteredException(client);
                    }

                    // Only certain commands should reset the idle clock
                    if (!idleCommands.Contains(packet.ToUpper())) {
                        client.IdleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                } catch(Exception e) {
                    _ircd.Log.Debug($"Exception (client): {e.ToString()}");
                    return false;
                }
            } else if (sender is Server server) {
                try {
                    // TODO: Per-protocol?
                    registrationCommands.Add("CAPAB");
                    registrationCommands.Add("SERVER");
                    registrationCommands.Add("PING");
                    registrationCommands.Add("PONG");

                    _ircd.Log.Debug($"Got a server line: {args.Line}");

                    if (server.State.Equals(ServerState.PreAuth) && !registrationCommands.Contains(packet.ToUpper())) {
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
                List<HandlerInfo> functions = new List<HandlerInfo>();

                if (sender is Client) {
                    // Client
                    // Call handlers with no type defined (i.e. ServerType.Dummy)
                    functions = FindHandlers(packet, type);
                } else if(sender is Server server) {
                    // Server
                    // Only call handlers matching our exact server type, or ServerType.Any
                    functions = FindHandlers(packet, type, server.Type);
                }

                if (functions.Any()) {
                    // Invoke all of the handlers for the command
                    foreach (var handler in functions) {
                        using (var scope = _services.CreateScope()) {
                            var context = scope.ServiceProvider.GetRequiredService<IrcContext>();
                            context.Sender = sender;
                            context.Daemon = ircd;
                            context.Args = args;

                            var controller = scope.ServiceProvider.GetRequiredService(handler.ControllerType);
                            handler.Delegate.Invoke(controller, args);
                        }
                    }
                } else {
                    _ircd.Log.Debug("No handler for " + packet.ToUpper());
                    if(sender is Client)
                        throw new IrcErrUnknownCommandException((Client)sender, packet.ToUpper());
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
                    record => record.ListenerType == type &&
                    (record.ServerType == serverType || record.ServerType == ServerType.Any)
                ));
            }
            return functions;
        }

        public bool Load() {
            var instance = Expression.Parameter(typeof(object));
            var args = Expression.Parameter(typeof(HandlerArgs));

            foreach (var controllerType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))) {
                var controllerAttribute = (ControllerAttribute)controllerType.GetCustomAttributes(typeof(ControllerAttribute), false).FirstOrDefault();
                if (controllerAttribute != null) {
                    foreach (var method in controllerType.GetMethods().Where(m => m.ReturnType == typeof(bool))) {
                        foreach (HandlesAttribute handlerAttribute in method.GetCustomAttributes(typeof(HandlesAttribute), false).Cast<HandlesAttribute>())
                        {
                            Func<object, HandlerArgs, bool> d = Expression.Lambda<Func<object, HandlerArgs, bool>>(
                                Expression.Call(Expression.Convert(instance, controllerType), method, args),
                                instance,
                                args).Compile();

                            Register(new HandlerInfo {
                                Packet = handlerAttribute.Command,
                                Delegate = d,
                                ListenerType = controllerAttribute.Type,
                                ServerType = controllerAttribute.ServerType,
                                ControllerType = controllerType,
                            });
                        }
                    }
                } else {
                    _ircd.Log.Warn($"'{controllerType.Name}' does not have a controller attribute and will be skipped.");
                }
            }

            return true;
        }
    }
}
