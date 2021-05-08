using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace cmpctircd.Controllers {
    [Controller(ListenerType.Client)]
    public class QueryController : ControllerBase {
        private readonly IRCd ircd;
        private readonly Client client;

        public QueryController(IRCd ircd, Client client) {
            this.ircd = ircd ?? throw new ArgumentNullException(nameof(ircd));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Handles("VERSION")]
        public bool VersionHandler(HandlerArgs args) {
            client.SendVersion();
            return true;
        }

        [Handles("WHOIS")]
        public bool WhoisHandler(HandlerArgs args) {
            String[] splitLineComma;
            Client targetClient;
            long idleTime;

            try {
                splitLineComma = args.SpacedArgs[1].Split(new char[] { ','});
            } catch(ArgumentOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "WHOIS");
            }

            // Need the client object of the target...
            for(int i = 0; i < splitLineComma.Count(); i++) {
                if((i + 1) > ircd.MaxTargets) break;

                var target = splitLineComma[i];
                try {
                    targetClient = ircd.GetClientByNick(target);
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetNickException(client, target);
                }

                idleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - targetClient.IdleTime;
                client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISUSER.Printable()} {client.Nick} {targetClient.Nick} {targetClient.Ident} {targetClient.GetHost()} * :{targetClient.RealName}");

                // Generate a list of all the channels inhabited by the target
                // XXX: no LINQ for now because of strange bug where LINQ in Packet/dynamic classes causes an exception
                //Predicate<Channel> channelFinder = (Channel chan) => { return chan.Inhabits(targetClient); };
                //List<Channel> inhabitedChannels = ircd.ChannelManager.Channels.Values.ToList().FindAll(channelFinder);

                var inhabitedChannels = new List<string>();
                var channelList       = ircd.ChannelManager.Channels.Values;

                foreach(var channel in channelList) {
                    if(channel.Inhabits(targetClient)) {
                        if(targetClient.Modes["i"].Enabled) {
                            // If the user has +i, we can only tell the originator of the query about common channels
                            // Grab all of the inhabited channels, check we're both in them, and make a list of the names of those channels
                            if(!channel.Inhabits(client)) continue;
                        }

                        var userPriv = channel.Status(targetClient);
                        var userSymbol = channel.GetUserSymbol(userPriv);
                        inhabitedChannels.Add($"{userSymbol}{channel.Name}");
                    }
                }

                if(targetClient == client || client.Modes["o"].Enabled) {
                    // Only allow the target client's sensitive connection info if WHOISing themselves
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISHOST.Printable()} {client.Nick} {targetClient.Nick} :is connecting from {targetClient.Ident}@{targetClient.GetHost(false)} {targetClient.IP}");
                }

                if(inhabitedChannels.Any()) {
                    // Only show if the target client resides in at least one channel
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISCHANNELS.Printable()} {client.Nick} {targetClient.Nick} :{string.Join(" ", inhabitedChannels)}");
                }

                // Deal with the remote case
                var targetServerDesc = ircd.Desc;
                if (targetClient.RemoteClient) {
                    targetServerDesc = targetClient.OriginServer.Desc;
                }

                client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISSERVER.Printable()} {client.Nick} {targetClient.Nick} {targetClient.OriginServerName()} :{targetServerDesc}");

                if (targetClient.Modes["o"].Enabled) {
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISOPERATOR.Printable()} {client.Nick} {targetClient.Nick} :is an IRC Operator on {ircd.Network}");
                }


                if (targetClient.Modes["z"].Enabled) {
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISSECURE.Printable()} {client.Nick} {targetClient.Nick} :is using a secure connection");
                }

                if(!String.IsNullOrWhiteSpace(targetClient.AwayMessage)) {
                    // Only show if the user is away
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_AWAY.Printable()} {client.Nick} {targetClient.Nick} :{targetClient.AwayMessage}");
                }

                if (targetClient.Modes["B"].Enabled) {
                    // TODO: Unreal bolds the 'Bot', not sure about that for us
                    client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISBOT.Printable()} {client.Nick} {targetClient.Nick} :is a Bot on {ircd.Network}");
                }
                client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOISIDLE.Printable()} {client.Nick} {targetClient.Nick} {idleTime} {targetClient.SignonTime} :seconds idle, signon time");
                client.Write($":{ircd.Host} {IrcNumeric.RPL_ENDOFWHOIS.Printable()} {client.Nick} {targetClient.Nick} :End of /WHOIS list");
            }
            return true;
        }

        [Handles("WHO")]
        public bool WhoHandler(HandlerArgs args) {
            string mask = args.SpacedArgs[1];

            // TODO: may be another change as with channel WHO?
            // TODO: change when linking
            var hopCount = 0;

            if(mask.StartsWith("#")) {
                return false;
            }
            // Iterate through all of our clients if no mask
            foreach (var client in ircd.Clients) {
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
                if(client.Modes["i"].Enabled && !ircd.ChannelManager.Channels.Any(
                        channel => channel.Value.Inhabits(client) &&
                        channel.Value.Inhabits(client))) {
                    // Skip if none
                    continue;
                }

                var away = String.IsNullOrEmpty(client.AwayMessage) ? "H" : "G";
                var ircopSymbol = client.Modes["o"].Enabled ? "*" : "";
                client.Write($":{ircd.Host} {IrcNumeric.RPL_WHOREPLY.Printable()} {client.Nick} {client.Nick} {client.Ident} {client.GetHost()} {client.OriginServerName()} {client.Nick} {away}{ircopSymbol} :{hopCount} {client.RealName}");
            }

            client.Write($":{ircd.Host} {IrcNumeric.RPL_ENDOFWHO.Printable()} {client.Nick} {mask} :End of /WHO list.");
            return true;
        }

        [Handles("AWAY")]
        public bool AwayHandler(HandlerArgs args) {
            String[] splitColonLine = args.Line.Split(new char[] { ':' }, 2);
            String message;

            try {
                message = splitColonLine[1];
            } catch(IndexOutOfRangeException) {
                message = "";
            }

            client.AwayMessage = message;
            if(client.AwayMessage != "") {
                // Now away
                client.Write($":{ircd.Host} {IrcNumeric.RPL_NOWAWAY.Printable()} {client.Nick} :You have been marked as being away");
            } else {
                // No longer away
                client.Write($":{ircd.Host} {IrcNumeric.RPL_UNAWAY.Printable()} {client.Nick} :You are no longer marked as being away");
            }

            // CAP: away-notify
            foreach (var client in ircd.Clients.Where(c => c != client && c.CapManager.HasCap("away-notify"))) {
                // Ensure we share a channel
                var cohabit = ircd.ChannelManager.Channels.Values.Any(
                    channel => channel.Clients.ContainsValue(client) && channel.Clients.ContainsValue(client)
                );


                if (cohabit) {
                    // Let everyone know who has subscribed to away notifications if we share a channel
                    client.Write($":{client.Mask} AWAY :{message}");
                }
            }

            return true;
        }

        [Handles("LUSERS")]
        public bool LUsersHandler(HandlerArgs args) {
            // TODO: Lusers takes a server parameter
            // add this when we have linking
            client.SendLusers();
            return true;
        }

        [Handles("USERHOST")]
        public bool UserHostHandler(HandlerArgs args) {
            // the format is USERHOST nick1 nick2; so skip the command name
            var items = args.SpacedArgs.Skip(1).ToArray();
            if (items.Length == 0 || items.Length > 5) {
                throw new IrcErrNotEnoughParametersException(client, "USERHOST");
            }

            var replyBase = $":{ircd.Host} {IrcNumeric.RPL_USERHOST.Printable()} {client.Nick} :";
            var replyBuilder = new StringBuilder(replyBase);
            for (int i = 0; i < items.Length; i++) {
                // <nick>['*'] '=' <'+'|'-'><hostname>
                try {
                    var userClient = ircd.GetClientByNick(items[i]);

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

            client.Write(replyBuilder.ToString());

            return true;
        }

        [Handles("PING")]
        public bool PingHandler(HandlerArgs args) {
            // TODO: Modification for multiple servers
            var cookie = "";
            if(args.SpacedArgs.Count > 1) {
                cookie = args.SpacedArgs[1];
            }
            client.Write($":{ircd.Host} PONG {ircd.Host} :{cookie}");
            return true;
        }

        [Handles("MODE")]
        public bool ModeHandler(HandlerArgs args) {
            string target;

            try {
                target = args.SpacedArgs[1];
            } catch (ArgumentOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "MODE");
            }

            if (target.StartsWith("#") || target.StartsWith("&")) {
                return false;
            }

            // Only allow the user to set their own modes
            Client targetClient = client;

            if (args.SpacedArgs.Count == 1) {
                // This is a MODE request of the form: MODE <nick>
                if (targetClient != client) {
                    // Can only request own modes
                    return false;
                }

                var userModes = targetClient.GetModeStrings("+");
                client.Write($":{ircd.Host} {IrcNumeric.RPL_UMODEIS.Printable()} {targetClient.Nick} {userModes[0]} {userModes[1]}");
            } else if(args.SpacedArgs.Count >= 2) {
                // Process
                string modes = args.SpacedArgs[2];
                string[] param;

                if (args.SpacedArgs.Count == 2) {
                    args.SpacedArgs.Add("");
                }

                param = args.SpacedArgs[2].Split(new string[] { " " }, StringSplitOptions.None);

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
                    targetClient.Write($":{client.Mask} MODE {targetClient.Nick} {modeString}");

                    ircd.Servers.Where(server => server != client?.OriginServer).ForEach(
                        server => server.Write($":{client.Mask} MODE {targetClient.Nick} {modeString}")
                    );
                }
            }
            return true;
        }
    }
}
