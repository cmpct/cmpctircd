using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            ircd.PacketManager.Register("AWAY", AwayHandler);
            ircd.PacketManager.Register("LUSERS", LusersHandler);
            ircd.PacketManager.Register("PING", PingHandler);
            ircd.PacketManager.Register("MODE", ModeHandler);
        }

        public Boolean versionHandler(HandlerArgs args)
        {
            args.Client.SendVersion();
            return true;
        }

        public Boolean WhoisHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(' ');
            String target;
            Client targetClient;
            int idleTime = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds - args.Client.IdleTime;

            try {
                target = splitLine[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "WHOIS");
            }

            // Need the client object of the target...
            try {
                targetClient = args.IRCd.GetClientByNick(target);
            } catch(InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(args.Client, target);
            }

            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISUSER.Printable()} {args.Client.Nick} {targetClient.Nick} {targetClient.Ident} {targetClient.GetHost()} * :{targetClient.RealName}");

            // Generate a list of all the channels inhabited by the target
            // XXX: no LINQ for now because of strange bug where LINQ in Packet/dynamic classes causes an exception
            //Predicate<Channel> channelFinder = (Channel chan) => { return chan.Inhabits(targetClient); };
            //List<Channel> inhabitedChannels = args.IRCd.ChannelManager.Channels.Values.ToList().FindAll(channelFinder);

            var inhabitedChannels = new List<String>();
            foreach(var channel in args.IRCd.ChannelManager.Channels.Values) {
                if(channel.Inhabits(targetClient)) {
                    var userPriv = channel.Status(targetClient);
                    var userSymbol = channel.GetUserSymbol(userPriv);
                    inhabitedChannels.Add($"{userSymbol}{channel.Name}");
                }
            }

            if(targetClient == args.Client) {
                // Only allow the target client's sensitive connection info if WHOISing themselves
                // TODO: modify to allow ircops to see this too (when we have ircops)
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISHOST.Printable()} {args.Client.Nick} {targetClient.Nick} :is connecting from {targetClient.Ident}@{targetClient.GetHost(false)} {targetClient.GetHost(false)}");
            }

            if(inhabitedChannels.Count() > 0) {
                // Only show if the target client resides in at least one channel
                // TODO: needs modification for DNS (the last 'Host' should become 'IP', but there's no distinction between these yet)
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISCHANNELS.Printable()} {args.Client.Nick} {targetClient.Nick} :{string.Join(" ", inhabitedChannels)}");
            }

            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISSERVER.Printable()} {args.Client.Nick} {targetClient.Nick} {args.IRCd.Host} :{args.IRCd.Desc}");

            if(!String.IsNullOrWhiteSpace(targetClient.AwayMessage)) {
                // Only show if the user is away
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_AWAY.Printable()} {args.Client.Nick} {targetClient.Nick} :{targetClient.AwayMessage}");
            }

            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOISIDLE.Printable()} {args.Client.Nick} {targetClient.Nick} {idleTime} {targetClient.SignonTime} :seconds idle, signon time");
            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFWHOIS.Printable()} {args.Client.Nick} {targetClient.Nick} :End of /WHOIS list");
            return true;
        }
        public Boolean AwayHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(' ');
            String[] splitColonLine = args.Line.Split(':');
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

                    if (targetClient.Modes.ContainsKey(noOperatorMode)) {
                        targetClient.Modes.TryGetValue(noOperatorMode, out var modeObject);

                        if (!modeObject.Stackable) {
                            announce = true;
                        }
                        if (currentModifier == "+") {
                            // Attempt to add the mode
                            bool success = modeObject.Grant(param[position], false, announce, announce);

                            if (success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if (modeObject.HasParameters) {
                                    modeArgs += param[position];
                                }
                            }
                        } else if (currentModifier == "-") {
                            // Attempt to revoke the mode
                            bool success = modeObject.Revoke(param[position], false, announce, announce);

                            if (success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if (modeObject.HasParameters) {
                                    modeArgs += param[position];
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
                    targetClient.Write($":{args.Client.Mask} MODE {modeString}");
                }
            }
            return true;
        }
    }
}
