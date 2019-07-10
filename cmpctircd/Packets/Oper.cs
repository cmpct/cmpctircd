using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace cmpctircd.Packets {
    public static class Oper {
        /// <summary>
        /// HashAlgorithm instance cache, to reduce reflection overheads.
        /// </summary>
        private static readonly Dictionary<Type, HashAlgorithm> _algorithms = new Dictionary<Type, HashAlgorithm>();

        [Handler("OPER", ListenerType.Client)]
        public static bool OperHandler(HandlerArgs args) {
            bool hostMatch = false;
            if (args.SpacedArgs.Count <= 2) {
                throw new IrcErrNotEnoughParametersException(args.Client, "OPER");
            }
            try {
                var ircop = args.IRCd.Opers.Single(oper => oper.Name == args.SpacedArgs[1]);
                // Check for TLS
                if (ircop.Tls) {
                    if(!args.Client.Modes["z"].Enabled) {
                        return false;
                    }
                }
                // Check the hosts match
                foreach (var hostList in ircop.Hosts) {
                    var mask = Ban.CreateMask(hostList);

                    if(Ban.Match(args.Client, mask)) {
                        hostMatch = true;
                    }
                }
                if (!hostMatch) {
                    throw new IrcErrNoOperHostException(args.Client);
                }
                // Instantiate the algorithm through reflection, if not already instantiated.
                HashAlgorithm algorithm;
                if(!_algorithms.TryGetValue(ircop.Algorithm, out algorithm))
                    algorithm = _algorithms[ircop.Algorithm] = (ircop.Algorithm.GetConstructor(Type.EmptyTypes)?.Invoke(new object[0]) as HashAlgorithm) ?? throw new IrcErrNoOperHostException(args.Client);
                // Hash the user's input to match the stored hash password in the config
                byte[] password = Encoding.UTF8.GetBytes(args.SpacedArgs[2]);
                byte[] builtHash = algorithm.ComputeHash(password);
                if(builtHash.SequenceEqual(ircop.Password)) {
                    Channel channel;
                    Topic topic;
                    args.Client.Modes["o"].Grant("", true, true);
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_YOUREOPER.Printable()} {args.Client.Nick} :You are now an IRC Operator");
                    if(args.IRCd.OperChan.Count() == 0) {
                        return true;
                    }
                    // Create and join the oper-only chan
                    foreach(string chan in args.IRCd.OperChan) {
                        if(args.IRCd.ChannelManager.Exists(chan)) {
                            channel = args.IRCd.ChannelManager[chan];
                            args.Client.Invites.Remove(channel);
                        } else {
                            channel = args.IRCd.ChannelManager.Create(chan);
                        }
                        channel.AddClient(args.Client);
                        topic = channel.Topic;
                        topic.GetTopic(args.Client, chan, true);
                    }
                    return true;
                } else {
                    throw new IrcErrNoOperHostException(args.Client);
                }
            } catch (InvalidOperationException) {
                throw new IrcErrNoOperHostException(args.Client);
            }
        }

        [Handler("SAMODE", ListenerType.Client)]
        public static bool SamodeHandler(HandlerArgs args) {
            if (args.SpacedArgs.Count == 1)
                throw new IrcErrNotEnoughParametersException(args.Client, "SAMODE");
            if(args.Client.Modes["o"].Enabled) {
                args.Force = true;
                args.IRCd.PacketManager.FindHandler("MODE", args, ListenerType.Client);
                return true;
            }
            throw new IrcErrNoPrivileges(args.Client);
        }
    }
}
