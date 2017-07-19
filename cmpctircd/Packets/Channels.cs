using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using cmpctircd.Modes;

namespace cmpctircd.Packets {
    public class Channels {
        //private IRCd ircd;

        public Channels(IRCd ircd) {
            ircd.PacketManager.Register("JOIN", joinHandler);
            ircd.PacketManager.Register("PRIVMSG", privmsgHandler);
            ircd.PacketManager.Register("PART", partHandler);
            ircd.PacketManager.Register("TOPIC", topicHandler);
            ircd.PacketManager.Register("NOTICE", noticeHandler);
            ircd.PacketManager.Register("WHO", WhoHandler);
            ircd.PacketManager.Register("NAMES", NamesHandler);
            ircd.PacketManager.Register("MODE", ModeHandler);
            ircd.PacketManager.Register("KICK", KickHandler);
            ircd.PacketManager.Register("INVITE", InviteHandler);
        }

        private bool InviteHandler(HandlerArgs args) {
            string[] rawSplit = args.Line.Split(' ');
            Channel channel;
            Client targetClient;

            if (rawSplit.Count() <= 2) {
                throw new IrcErrNotEnoughParametersException(args.Client, "INVITE");
            }

            if(args.IRCd.ChannelManager.Exists(rawSplit[2])) {
                channel = args.IRCd.ChannelManager[rawSplit[2]];
            } else {
                throw new IrcErrNoSuchTargetNickException(args.Client, rawSplit[2]);
            }

            try {
                targetClient = args.IRCd.GetClientByNick(rawSplit[1]);
            } catch(InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(args.Client, rawSplit[1]);
            }

            if(!channel.Clients.ContainsKey(args.Client.Nick)) {
                throw new IrcErrNotOnChannelException(args.Client, channel.Name);
            }

            if (channel.Clients.ContainsKey(targetClient.Nick)) {
                throw new IrcErrUserOnChannelException(args.Client, targetClient.Nick, channel.Name);
            }

            ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(args.Client, ChannelPrivilege.Normal);
            if(sourcePrivs.CompareTo(ChannelPrivilege.Op) < 0) {
                throw new IrcErrChanOpPrivsNeededException(args.Client, channel.Name);
            }

            channel.SendToRoom(args.Client, $":{args.IRCd.host} {IrcNumeric.RPL_INVITING.Printable()} {args.Client.Nick} {targetClient.Nick} :{channel.Name}", true);
            targetClient.Write($":{args.Client.Mask} INVITE {targetClient.Nick} :{channel.Name}");
            targetClient.Invites.Add(channel);
            return true;
        }

        public bool KickHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            Client targetClient;
            String[] rawSplit;
            Channel channel;
            String target;
            String message;

            rawSplit = rawLine.Split(' ');
            
            if(rawSplit.Count() <= 2) {
                throw new IrcErrNotEnoughParametersException(client, "KICK");
            }

            if(rawSplit.Count() >= 4) {
                message = rawLine.Split(':')[1];
            } else {
                message = client.Nick;
            }

            target  = rawSplit[2];
            
            if (ircd.ChannelManager.Exists(rawSplit[1])) {
                channel = ircd.ChannelManager[rawSplit[1]];
            } else {
                throw new IrcErrNoSuchTargetChannelException(client, rawSplit[1]);
            }

            try {
                targetClient = ircd.GetClientByNick(target);
            } catch (InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(client, target);
            }

            ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
            if(sourcePrivs.CompareTo(ChannelPrivilege.Op) < 0) {
                throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
            }

            channel.SendToRoom(client, $":{client.Mask} KICK {channel.Name} {targetClient.Nick} :{message}", true);
            channel.Remove(targetClient, true);
            return true;
        }

        public Boolean topicHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Topic topic;
            String rawLine = args.Line;
            String[] rawSplit;
            String target;
            String command;

            rawSplit = rawLine.Split(' ');
            command = rawSplit[0].ToUpper();
            switch (rawSplit.Length) {
                case 1:
                    throw new IrcErrNotEnoughParametersException(client, command);
                case 2:
                    target = rawSplit[1];
                    if (!ircd.ChannelManager.Channels.ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                    topic = ircd.ChannelManager[target].Topic;
                    topic.GetTopic(client, target);
                    return true;

                default:
                    target = rawSplit[1];
                    if (!ircd.ChannelManager.Channels.ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                    topic = ircd.ChannelManager[target].Topic;
                    topic.SetTopic(client, target, rawLine);
                    return true;
            }
        }

        public Boolean joinHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;

            String[] splitLine = rawLine.Split(' ');
            String[] splitColonLine = rawLine.Split(new char[] { ':' }, 2);
            String[] splitCommaLine;
            Channel channel;
            Topic topic;

            try {
                splitCommaLine = splitLine[1].Split(new char[] { ','});
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "JOIN");
            }

            foreach(String channel_name in splitCommaLine) {
                // We don't need to check for commas because the split handled that.
                // TODO: Do check for proper initializing char, and check for BEL and space.
                if (!ChannelManager.IsValid(channel_name)) {
                    throw new IrcErrNoSuchTargetChannelException(client, channel_name);
                }
                // Get the channel object, creating it if it doesn't already exist
                // TODO: only applicable error is ERR_NEEDMOREPARAMS for now, more for limits/bans/invites
                if (ircd.ChannelManager.Exists(channel_name)) {
                    channel = ircd.ChannelManager[channel_name];
                } else {
                    channel = ircd.ChannelManager.Create(channel_name);
                }
                try {
                    channel.Modes["i"].GetValue();
                    if (!client.Invites.Contains(channel)) {
                        throw new IrcErrInviteOnlyChanException(args.Client, channel.Name);
                    }
                } catch (IrcModeNotEnabledException) {}
                channel.AddClient(client);
                client.Invites.Remove(channel);
                topic = channel.Topic;
                topic.GetTopic(client, channel_name, true);
            }

            return true;
        }

        public Boolean privmsgHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Client targetClient = null;
            String rawLine = args.Line;
            String[] rawSplit;
            String target;
            String message;
            String command;

            rawSplit = rawLine.Split(' ');
            command = rawSplit[0].ToUpper();

            if(rawSplit.Count() >= 2) {
                target = rawSplit[1];
                // Check the user exists
                try {
                    targetClient = ircd.GetClientByNick(target);
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetNickException(client, target);
                }
            }
            switch(rawSplit.Count()) {
                case 1:
                    throw new IrcErrNoRecipientException(client, "PRIVMSG");
                case 2:
                    throw new IrcErrNoTextToSendException(client);
            }
            target = rawSplit[1];
            message = rawLine.Split(new string[] { ":" }, 2, StringSplitOptions.None)[1];

            if (target.StartsWith("#")) {
                // PRIVMSG a channel
                // TODO: We don't have +n yet so just check if they're in the room...
                if(ircd.ChannelManager.Exists(target)) {
                    Channel channel = ircd.ChannelManager[target];
                    if(channel.Inhabits(client)) {
                        channel.SendToRoom(client, String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, channel.Name, message), false);
                    }
                } else {
                    throw new IrcErrNoSuchTargetNickException(client, target);
                }
            } else if(targetClient != null) {
                if(!String.IsNullOrWhiteSpace(targetClient.AwayMessage)) {
                    // If the target client (recipient) is away, warn the person (source) sending the message to them.
                    args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_AWAY.Printable()} {args.Client.Nick} {target} :{targetClient.AwayMessage}");
                }
                targetClient.Write(String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, target, message));
            }
            return true;
        }

        public bool noticeHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;

            Client targetClient = null;
            String target;
            String message;
            String fmtMessage;
            String[] rawSplit;
            bool userExists = false;

            rawSplit = rawLine.Split(' ');
            target = rawSplit[1];

            // Check the client has sent the expected amount of params (3)
            if (rawSplit.Count() >= 2) {
                // Check the target exists
                if(target.StartsWith("#")) {
                    // The target is a channel
                    if(!ircd.ChannelManager.Exists(target)) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                } else {
                    // The target is a user
                    foreach (var clientList in ircd.ClientLists) {
                        foreach (var clientSearch in clientList) {
                            if (clientSearch.Nick.Equals(target, StringComparison.OrdinalIgnoreCase)) {
                                targetClient = clientSearch;
                                userExists = true;
                            }
                        }
                    }

                    if (!userExists) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                }


                if (rawSplit.Count() < 3) {
                    switch (rawSplit.Count()) {
                        case 1:
                            // Client has only sent "NOTICE", nothing to respond
                            return false;
                        case 2:
                            // Client has provided a target but no message
                            throw new IrcErrNoTextToSendException(client);
                    }
                }

                message = rawSplit[2];
                fmtMessage = String.Format(":{0} NOTICE {1} {2}", client.Mask, target, message);
                if (target.StartsWith("#")) {
                    Channel channel = ircd.ChannelManager[target];
                    channel.SendToRoom(client, fmtMessage, false);
                    return true;
                } else {
                    targetClient.Write(fmtMessage);
                    return true;
                }

            }
            // XXX: RFC states there should never be a response to NOTICE, but Unreal and others send a No Such Target error message. We will follow them for now.
            return true;
        }

        public Boolean partHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            String[] splitLine = rawLine.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            String[] splitLineSpace = rawLine.Split(new string[] { " " }, 3, StringSplitOptions.None);

            String channel;
            String reason;

            try {
                channel = splitLineSpace[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "PART");
            }

            try {
                reason = splitLine[1];
            } catch(IndexOutOfRangeException) {
                reason = "Leaving";
            }

            // Does the channel exist?
            if(!ircd.ChannelManager.Exists(channel)) {
                throw new IrcErrNoSuchTargetChannelException(client, channel);
            }

            // Are we in the channel?
            Channel channelObj = ircd.ChannelManager[channel];
            if(!channelObj.Inhabits(client)) {
                throw new IrcErrNotOnChannelException(client, channel);
            }

            channelObj.Part(client, reason);
            return true;
        }

        public Boolean WhoHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            String[] splitLineSpace = args.Line.Split(new string[] { " " }, 3, StringSplitOptions.None);
            String target;
            Channel targetChannel;

            try {
                target = splitLineSpace[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "WHO");
            }

            if(target.StartsWith("#")) {
                // The target is a channel
                try {
                    targetChannel = args.IRCd.ChannelManager.Channels[target];
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetNickException(args.Client, target);
                }

                foreach(var client in targetChannel.Clients) {
                    // TODO: Needs updating for when we have ircop, ops
                    // TODO: Also for when we have links (:0 is hopcount)
                    var userSymbol = "";
                    var hopCount = 0;

                    var away = "";
                    if(String.IsNullOrEmpty(client.Value.AwayMessage)) {
                        away = "H"; // "Here"
                    } else {
                        away = "G"; // "Gone"
                    }
                    args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_WHOREPLY.Printable()} {args.Client.Nick} {target} {client.Value.Ident} {client.Value.Host} {args.IRCd.host} {client.Value.Nick} {away}{userSymbol} :{hopCount} {client.Value.RealName}");
                }
            } else {
                // The target is a user
                // TODO: implement this once we have +i (invisible mode)
            }

            args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_ENDOFWHO.Printable()} {args.Client.Nick} {target} :End of /WHO list.");
            return true;
        }
        public Boolean NamesHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            String[] splitLineSpace = args.Line.Split(new string[] { " " }, 3, StringSplitOptions.None);
            String [] splitCommaLine;

            try {
                splitCommaLine = splitLineSpace[1].Split(new char[] { ','});
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "NAMES");
            }

            foreach(var channel_name in splitCommaLine) {
                if (!ChannelManager.IsValid(channel_name)) {
                    throw new IrcErrNoSuchTargetChannelException(args.Client, channel_name);
                }

                if(args.IRCd.ChannelManager.Exists(channel_name)) {
                    Channel targetChannel = args.IRCd.ChannelManager[channel_name];
                    foreach(var client in targetChannel.Clients) {
                        var userPriv = targetChannel.Status(client.Value);
                        var userSymbol = targetChannel.GetUserSymbol(userPriv);
                        args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_NAMREPLY.Printable()} {args.Client.Nick} = {channel_name} :{userSymbol}{client.Value.Nick}");
                    }
                    args.Client.Write($"{args.IRCd.host} {IrcNumeric.RPL_ENDOFNAMES.Printable()} {args.Client.Nick} {channel_name} :End of /NAMES list.");
                }
            }

            return true;
        }

        public bool ModeHandler(HandlerArgs args) {
            // This handler is for Channel requests (i.e. where the target begins with a # or &)
            // TODO: update with channel validation logic (in ChannelManager?)
            string[] splitLine = args.Line.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            List<string> splitLineSpace = args.Line.Split(new string[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
            string target;
            Channel channel;

            try {
                target = splitLineSpace[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "MODE");
            }

            // TODO: no user mode support yet
            if(!target.StartsWith("#") && !target.StartsWith("&")) {
                return false;
            }

            if(args.IRCd.ChannelManager.Exists(target)) {
                channel = args.IRCd.ChannelManager[target];
            } else {
                throw new IrcErrNoSuchTargetChannelException(args.Client, target);
            }

            if(splitLineSpace.Count() == 2) {
                // This is a request for MODE (e.g. MODE #cmpctircd)
                string[] channelModes = channel.GetModeStrings("+");
                string characters = channelModes[0];
                string argsSet = channelModes[1];
                if(!String.IsNullOrWhiteSpace(argsSet)) {
                    args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_CHANNELMODEIS.Printable()} {args.Client.Nick} {channel.Name} {characters} {argsSet}");
                } else {
                    args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_CHANNELMODEIS.Printable()} {args.Client.Nick} {channel.Name} {characters}");
                }
                // TODO: creation time?

            } else if(splitLineSpace.Count() > 2) {
                // Process
                string modes = splitLineSpace[2];
                string[] param;

                if(splitLineSpace.Count() == 3) {
                    splitLineSpace.Add("");
                }

                param = splitLineSpace[3].Split(new string[] { " " }, StringSplitOptions.None);

                string currentModifier = "";
                string modeChars = "";
                string modeArgs = "";
                string modeString = "";
                int position = 0;
                Mode modeObject;

                if(args.IRCd.GetSupportedModes()["A"].Contains(modes)) {
                    // Is this mode of Type A (and listable)? See ModeType
                    // TODO: should we put this in the foreach?

                    // TODO: for bans, check if this is +b...
                    // TODO: and so on
                    // TODO: if so, return the list of bans
                    // TODO: https://git.cmpct.info/cmpctircd.git/blob/a98635da09650310bffe2c98b11ac8fa7cb67445:/lib/IRCd/Client/Packets.pm#l487
                }

                foreach(var mode in modes) {
                    var modeStr = mode.ToString();
                    var noOperatorMode = modeStr.Replace("+", "").Replace("-", "");
                    if(modeStr.Equals("+") || modeStr.Equals("-")) {
                        currentModifier = modeStr;
                        modeChars += modeStr;
                    }

                    if(channel.Modes.ContainsKey(noOperatorMode)) {
                        channel.Modes.TryGetValue(noOperatorMode, out modeObject);
                        if(currentModifier == "+") {
                            // Attempt to add the mode
                            bool success = modeObject.Grant(args.Client, param[position], false, false);

                            if(success) {
                                modeChars += modeStr;
                                if(modeObject.HasParameters) {
                                    modeArgs += param[position];
                                }
                            }
                        } else if(currentModifier == "-") {
                            // Attempt to revoke the mode
                            bool success = modeObject.Revoke(args.Client, param[position], false, false);

                            if(success) {
                                modeChars += modeStr;
                                if(modeObject.HasParameters) {
                                    modeArgs += param[position];
                                }
                            }
                        }

                        if(modeObject.HasParameters) {
                            position += 1;
                        }
                    }
                }

                if(!modeChars.Equals("+") && !modeChars.Equals("-")) {
                    modeString = $"{modeChars} {modeArgs}";
                    channel.SendToRoom(args.Client, $":{args.Client.Mask} MODE {channel.Name} {modeString}");
                }
            }

            return true;
        }


    }
}
