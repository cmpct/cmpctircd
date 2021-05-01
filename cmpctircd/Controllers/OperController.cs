using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using cmpctircd.Configuration;

namespace cmpctircd.Controllers {
    public class OperController : ControllerBase {
        private readonly IRCd ircd;
        private readonly Client client;

        /// <summary>
        /// HashAlgorithm instance cache, to reduce reflection overheads.
        /// </summary>
        private readonly Dictionary<Type, HashAlgorithm> _algorithms = new Dictionary<Type, HashAlgorithm>();

        public OperController(IRCd ircd, Client client) {
            this.ircd = ircd ?? throw new ArgumentNullException(nameof(ircd));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Handler("OPER", ListenerType.Client)]
        public bool OperHandler(HandlerArgs args) {
            bool hostMatch = false;
            if (args.SpacedArgs.Count <= 2) {
                throw new IrcErrNotEnoughParametersException(client, "OPER");
            }
            try {
                var ircop = ircd.Opers.Single(oper => oper.Name == args.SpacedArgs[1]);
                // Check for TLS
                if (ircop.Tls) {
                    if(!client.Modes["z"].Enabled) {
                        return false;
                    }
                }
                // Check the hosts match
                foreach (var hostList in ircop.Hosts) {
                    var mask = Ban.CreateMask(hostList);

                    if(Ban.Match(client, mask)) {
                        hostMatch = true;
                    }
                }
                if (!hostMatch) {
                    throw new IrcErrNoOperHostException(client);
                }
                // Instantiate the algorithm through reflection, if not already instantiated.
                HashAlgorithm algorithm;
                if(!_algorithms.TryGetValue(ircop.Algorithm, out algorithm))
                    algorithm = _algorithms[ircop.Algorithm] = (ircop.Algorithm.GetConstructor(Type.EmptyTypes)?.Invoke(new object[0]) as HashAlgorithm) ?? throw new IrcErrNoOperHostException(client);
                // Hash the user's input to match the stored hash password in the config
                byte[] password = Encoding.UTF8.GetBytes(args.SpacedArgs[2]);
                byte[] builtHash = algorithm.ComputeHash(password);
                if(builtHash.SequenceEqual(ircop.Password)) {
                    Channel channel;
                    Topic topic;
                    client.Modes["o"].Grant("", true, true);
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_YOUREOPER.Printable()} {client.Nick} :You are now an IRC Operator");
                    if (!ircd.OperChan.Any()) {
                        return true;
                    }
                    // Create and join the oper-only chan
                    foreach(string chan in ircd.OperChan) {
                        if(ircd.ChannelManager.Exists(chan)) {
                            channel = ircd.ChannelManager[chan];
                            client.Invites.Remove(channel);
                        } else {
                            channel = ircd.ChannelManager.Create(chan);
                        }
                        channel.AddClient(client);
                        topic = channel.Topic;
                        topic.GetTopic(client, chan, true);
                    }
                    return true;
                } else {
                    throw new IrcErrNoOperHostException(client);
                }
            } catch (InvalidOperationException) {
                throw new IrcErrNoOperHostException(client);
            }
        }

        [Handler("SAMODE", ListenerType.Client)]
        public bool SamodeHandler(HandlerArgs args) {
            if (args.SpacedArgs.Count == 1)
                throw new IrcErrNotEnoughParametersException(client, "SAMODE");
            if(client.Modes["o"].Enabled) {
                args.Force = true;
                ircd.PacketManager.FindHandler("MODE", args, ListenerType.Client);
                return true;
            }
            throw new IrcErrNoPrivileges(client);
        }

        [Handler("CONNECT", ListenerType.Client)]
        public bool ConnectHandler(HandlerArgs args) {
            if (args.SpacedArgs.Count == 1) {
                throw new IrcErrNotEnoughParametersException(client, "CONNECT");
            }

            if (!client.Modes["o"].Enabled) {
                throw new IrcErrNoPrivileges(client);
            }

            var host = args.SpacedArgs[1];
            SocketConnector connector = null;

            try {
                connector = ircd.Connectors.First(iterConnector => iterConnector.ServerInfo.Host == host);

                if (connector.Connected) {
                    client.Write($":{ircd.Host} NOTICE {client.Nick} :Already connected to: {host}");
                    return true;
                }
            } catch (InvalidOperationException) {
                // No such server was found
                ircd.Log.Warn($"Oper {client.Nick} tried to connect to non-existent server: {host}");
                client.Write($":{ircd.Host} NOTICE {client.Nick} :Such a server does not exist in config: {host}");
            }

            client.Write($":{ircd.Host} NOTICE {client.Nick} :Attempting to connect to: {host}");
            try {
                connector.Connect();
            } catch (InvalidOperationException) {
                // Couldn't connect
                client.Write($":{ircd.Host} NOTICE {client.Nick} :Unable to connect to server: {host}");
            }

            client.Write($":{ircd.Host} NOTICE {client.Nick} :Successfully connected to: {host}");

            return true;
        }
    }
}
