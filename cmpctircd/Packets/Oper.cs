using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace cmpctircd.Packets {
    public class Oper {

        // Class for all IRC Operator commands such as OPER, KILL, SAMODE, etc.
        public Oper(IRCd ircd) {
            ircd.PacketManager.Register("OPER", OperHandler);
            ircd.PacketManager.Register("SAMODE", SamodeHandler);
        }

        public Boolean OperHandler(HandlerArgs args) {
            string[] userInput = args.Line.Split(' ');
            bool hostMatch = false;
            if (userInput.Count() <= 2) {
                throw new IrcErrNotEnoughParametersException(args.Client, "OPER");
            }
            try {
                var ircop = args.IRCd.Opers.Single(oper => oper.Name == userInput[1]);
                // Check for TLS
                if (ircop.TLS) {
                    if(!args.Client.Modes["z"].Enabled) {
                        return false;
                    }
                }
                // Check the hosts match
                foreach (var hostList in ircop.Host) {
                    var mask = Ban.CreateMask(hostList);

                    if(Ban.Match(args.Client, mask)) {
                        hostMatch = true;
                    }
                }
                if (!hostMatch) {
                    throw new IrcErrNoOperHostException(args.Client);
                }
                // Hash the user's input to match the stored hash password in the config
                SHA256 sha256 = SHA256Managed.Create();
                byte[] password = Encoding.UTF8.GetBytes(userInput[2]);
                string builtHash = string.Concat(sha256.ComputeHash(password).Select(x => x.ToString("x2")));
                if (builtHash == ircop.Password) {
                    Channel channel;
                    Topic topic;
                    args.Client.Modes["o"].Grant("", true, true);
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_YOUREOPER.Printable()} {args.Client.Nick} :You are now an IRC Operator");
                    if (args.IRCd.OperChan.Count() == 0) {
                        return true;
                    }
                    // Create and join the oper-only chan
                    foreach (string chan in args.IRCd.OperChan) {
                        if (args.IRCd.ChannelManager.Exists(chan)) {
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

        public Boolean SamodeHandler(HandlerArgs args) {
            if (args.Line.Split(' ').Count() == 1) {
                throw new IrcErrNotEnoughParametersException(args.Client, "SAMODE");
            }
            if(args.Client.Modes["o"].Enabled) {
                args.Force = true;
                args.IRCd.PacketManager.FindHandler("MODE", args, ListenerType.Client);
                return true;
            }
            throw new IrcErrNoPrivileges(args.Client);
        }
    }
}
