using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cmpctircd.Packets
{
    public class Queries
    {
        // This class is for the server query group of commands
        // TODO: Stats & Links?
        public Queries(IRCd ircd)
        {
            ircd.PacketManager.Register("VERSION", versionHandler);
            ircd.PacketManager.Register("WHOIS", WhoisHandler);
            ircd.PacketManager.Register("WHO", WhoHandler);
            ircd.PacketManager.Register("AWAY", AwayHandler);
            ircd.PacketManager.Register("LUSERS", LusersHandler);
            ircd.PacketManager.Register("USERHOST", UserhostHandler);
            ircd.PacketManager.Register("PING", PingHandler);
            ircd.PacketManager.Register("MODE", ModeHandler);
        }

        public Boolean versionHandler(HandlerArgs args)
        {
            args.Client.SendVersion();
            return true;
        }

        public Boolean WhoisHandler(HandlerArgs args) {
            String[] splitLineSpace = args.Line.Split(' ');
            String[] splitLineComma = args.Line.Split(',');
            Client targetClient;
            long idleTime;

            try {
                splitLineComma = splitLineSpace[1].Split(new char[] { ','});
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "WHOIS");
            }

            // Need the client object of the target...
            for(int i = 0; i < splitLineComma.Count(); i++) {
                if((i + 1) > args.IRCd.MaxTargets) break;

                var target = splitLineComma[i];
                try {
                    targetClient = args.IRCd.GetClientByNick(target);
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetNickException(args.Client, target);
                }

                idleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - targetClient.IdleTime;
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISUSER.Printable()} {args.Client.Nick} {targetClient.Nick} {targetClient.Ident} {targetClient.GetHost()} * :{targetClient.RealName}");

                // Generate a list of all the channels inhabited by the target
                // XXX: no LINQ for now because of strange bug where LINQ in Packet/dynamic classes causes an exception
                //Predicate<Channel> channelFinder = (Channel chan) => { return chan.Inhabits(targetClient); };
                //List<Channel> inhabitedChannels = args.IRCd.ChannelManager.Channels.Values.ToList().FindAll(channelFinder);

                var inhabitedChannels = new List<string>();
                var channelList       = args.IRCd.ChannelManager.Channels.Values;

                foreach(var channel in channelList) {
                    if(channel.Inhabits(targetClient)) {
                        if(targetClient.Modes["i"].Enabled) {
                            // If the user has +i, we can only tell the originator of the query about common channels
                            // Grab all of the inhabited channels, check we're both in them, and make a list of the names of those channels
                            if(!channel.Inhabits(args.Client)) continue;
                        }

                        var userPriv = channel.Status(targetClient);
                        var userSymbol = channel.GetUserSymbol(userPriv);
                        inhabitedChannels.Add($"{userSymbol}{channel.Name}");
                    }
                }

                if(targetClient == args.Client || args.Client.Modes["o"].Enabled) {
                    // Only allow the target client's sensitive connection info if WHOISing themselves
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISHOST.Printable()} {args.Client.Nick} {targetClient.Nick} :is connecting from {targetClient.Ident}@{targetClient.GetHost(false)} {targetClient.IP}");
                }

                if(inhabitedChannels.Count() > 0) {
                    // Only show if the target client resides in at least one channel
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISCHANNELS.Printable()} {args.Client.Nick} {targetClient.Nick} :{string.Join(" ", inhabitedChannels)}");
                }

                // Deal with the remote case
                var targetServerDesc = args.IRCd.Desc;
                if (targetClient.RemoteClient) {
                    targetServerDesc = targetClient.OriginServer.Desc;
                }

                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISSERVER.Printable()} {args.Client.Nick} {targetClient.Nick} {targetClient.OriginServerName()} :{targetServerDesc}");

                if (targetClient.Modes["o"].Enabled) {
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISOPERATOR.Printable()} {args.Client.Nick} {targetClient.Nick} :is an IRC Operator on {args.IRCd.Network}");
                }


                if (targetClient.Modes["z"].Enabled) {
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISSECURE.Printable()} {args.Client.Nick} {targetClient.Nick} :is using a secure connection");
                }

                if(!String.IsNullOrWhiteSpace(targetClient.AwayMessage)) {
                    // Only show if the user is away
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_AWAY.Printable()} {args.Client.Nick} {targetClient.Nick} :{targetClient.AwayMessage}");
                }

                if (targetClient.Modes["B"].Enabled) {
                    // TODO: Unreal bolds the 'Bot', not sure about that for us
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISBOT.Printable()} {args.Client.Nick} {targetClient.Nick} :is a Bot on {args.IRCd.Network}");
                }
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISIDLE.Printable()} {args.Client.Nick} {targetClient.Nick} {idleTime} {targetClient.SignonTime} :seconds idle, signon time");
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFWHOIS.Printable()} {args.Client.Nick} {targetClient.Nick} :End of /WHOIS list");
            }
            return true;
        }

        public bool WhoHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(' ');
            string mask = splitLine[1];

            // TODO: may be another change as with channel WHO?
            // TODO: change when linking
            var hopCount = 0;

            if(mask.StartsWith("#")) {
                return false;
            }
            // Iterate through all of our clients if no mask
            foreach(var list in args.IRCd.ClientLists) {
                foreach(var client in list) {
                    if (!(String.IsNullOrEmpty(mask) || mask.Equals("0"))) {
                        // If mask isn't blank or 0, then we need to check if the user matches criteria.
                        var metCriteria = false;
                        mask = mask.Replace("*", ".*?");

                        // host, server, real name, nickname
                        var criteria = new string[] { client.GetHost(), client.OriginServerName(), client.RealName, client.Nick };
                        foreach (var criterion in criteria) {
                            if (Regex.IsMatch(criterion, mask)) {
                                metCriteria = true;
                                break;
                            }
                        }

                        if(!metCriteria) {
                            // Failed to match on any basis, skip
                            continue;
                        }
                    }

                    // Always check if invisible users are being included in the mix
                    if(client.Modes["i"].Enabled && !args.IRCd.ChannelManager.Channels.Any(
                            channel => channel.Value.Inhabits(args.Client) &&
                            channel.Value.Inhabits(client))) {
                        // Skip if none
                        continue;
                    }

                    var away = String.IsNullOrEmpty(client.AwayMessage) ? "H" : "G";
                    var ircopSymbol = client.Modes["o"].Enabled ? "*" : "";
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOREPLY.Printable()} {args.Client.Nick} {client.Nick} {client.Ident} {client.GetHost()} {client.OriginServerName()} {client.Nick} {away}{ircopSymbol} :{hopCount} {client.RealName}");
                }
            }

            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFWHO.Printable()} {args.Client.Nick} {mask} :End of /WHO list.");
            return true;
        }

        public Boolean AwayHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(' ');
            String[] splitColonLine = args.Line.Split(new char[] { ':' }, 2);
            String message;

            try {
                message = splitColonLine[1];
            } catch(IndexOutOfRangeException) {
                message = "";
            }

            args.Client.AwayMessage = message;
            if(args.Client.AwayMessage != "") {
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_NOWAWAY.Printable()} {args.Client.Nick} :You have been marked as being away");
            } else {
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_UNAWAY.Printable()} {args.Client.Nick} :You are no longer marked as being away");
            }

            return true;
        }

        public Boolean LusersHandler(HandlerArgs args) {
            // TODO: Lusers takes a server parameter
            // add this when we have linking
            args.Client.SendLusers();
            return true;
        }

        public bool UserhostHandler(HandlerArgs args) {
            // the format is USERHOST nick1 nick2; so skip the command name
            var items = args.Line.Split(' ').Skip(1).ToArray();
            if (items.Length == 0 || items.Length > 5) {
                throw new IrcErrNotEnoughParametersException(args.Client, "USERHOST");
            }

            var replyBase = $":{args.IRCd.Host} {IrcNumeric.RPL_USERHOST.Printable()} {args.Client.Nick} :";
            var replyBuilder = new StringBuilder(replyBase);
            for (int i = 0; i < items.Length; i++) {
                // <nick>['*'] '=' <'+'|'-'><hostname>
                try {
                    var userClient = args.IRCd.GetClientByNick(items[i]);

                    var isOp = userClient.Modes["o"].Enabled ? "*" : "";
                    var isAway = !string.IsNullOrEmpty(userClient.AwayMessage) ? "-" : "+";

                    var replyItem = $"{userClient.Nick}{isOp}={isAway}{userClient.Ident}@{userClient.GetHost()}";
                    replyBuilder.Append(replyItem);
                    if (i != items.Length - 1) // if not last item
                        replyBuilder.Append(" ");
                } catch (InvalidOperationException) { // no user
                    continue;
                } 
            }

            args.Client.Write(replyBuilder.ToString());

            return true;
        }

        public Boolean PingHandler(HandlerArgs args) {
            // TODO: Modification for multiple servers
            string cookie = args.Line.Split(' ')[1];
            args.Client.Write($":{args.IRCd.Host} PONG {args.IRCd.Host} :{cookie}");
            return true;
        }

        public bool ModeHandler(HandlerArgs args) {
            string[] splitLine = args.Line.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            List<string> splitLineSpace = args.Line.Split(new string[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
            string target;

            try {
                target = splitLineSpace[1];
            } catch (IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "MODE");
            }

            if (target.StartsWith("#") || target.StartsWith("&")) {
                return false;
            }

            // Only allow the user to set their own modes
            Client targetClient = args.Client;

            if (splitLineSpace.Count == 1) {
                // This is a MODE request of the form: MODE <nick>
                if (targetClient != args.Client) {
                    // Can only request own modes
                    return false;
                }

                var userModes = targetClient.GetModeStrings("+");
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_UMODEIS.Printable()} {targetClient.Nick} {userModes[0]} {userModes[1]}");
            } else if(splitLineSpace.Count() >= 2) {
                // Process
                string modes = splitLineSpace[1];
                string[] param;

                if (splitLineSpace.Count() == 2) {
                    splitLineSpace.Add("");
                }

                param = splitLineSpace[2].Split(new string[] { " " }, StringSplitOptions.None);

                string currentModifier = "";
                string modeChars = "";
                string modeArgs = "";
                string modeString = "";
                bool announce = false;
                int position = 0;
                var modesNoOperator = modes.Replace("+", "").Replace("-", "");

                foreach (var mode in modes) {
                    var modeStr = mode.ToString();
                    var noOperatorMode = modeStr.Replace("+", "").Replace("-", "");
                    if (modeStr.Equals("+") || modeStr.Equals("-")) {
                        currentModifier = modeStr;
                        modeChars += modeStr;
                    }

                    Modes.UserMode modeObject; // HACK for C# 6 without discards
                    if (targetClient.Modes.ContainsKey(noOperatorMode)) {
                        targetClient.Modes.TryGetValue(noOperatorMode, out modeObject);

                        if (!modeObject.Stackable) {
                            announce = true;
                        }
                        if (currentModifier == "+") {
                            // Attempt to add the mode
                            bool success = modeObject.Grant(param[position], false, announce, announce);

                            if (success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if (modeObject.HasParameters) {
                                    modeArgs += param[position] + " ";
                                }
                            }
                        } else if (currentModifier == "-") {
                            // Attempt to revoke the mode
                            bool success = modeObject.Revoke(param[position], false, announce, announce);

                            if (success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if (modeObject.HasParameters) {
                                    modeArgs += param[position] + " ";
                                }
                            }
                        }
                    }
                }


                if (!modeChars.Equals("+") && !modeChars.Equals("-")) {
                    modeString = $"{modeChars} {modeArgs}";
                    if (!modeString.Contains("+") && !modeString.Contains("-")) {
                        // Return if the mode string doesn't contain an operator
                        return true;
                    }
                    targetClient.Write($":{args.Client.Mask} MODE {targetClient.Nick} {modeString}");
                }
            }
            return true;
        }
    }
}
