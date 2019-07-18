using System;
using System.Collections.Generic;
using System.Linq;

using cmpctircd.Modes;

namespace cmpctircd.Packets {
    public static class Channels {
        [Handler("INVITE", ListenerType.Client)]
        public static bool InviteHandler(HandlerArgs args) {
            Channel channel;
            Client targetClient;

            if (args.SpacedArgs.Count <= 2)
                throw new IrcErrNotEnoughParametersException(args.Client, "INVITE");

            if(args.IRCd.ChannelManager.Exists(args.SpacedArgs[2])) {
                channel = args.IRCd.ChannelManager[args.SpacedArgs[2]];
            } else {
                throw new IrcErrNoSuchTargetNickException(args.Client, args.SpacedArgs[2]);
            }

            try {
                targetClient = args.IRCd.GetClientByNick(args.SpacedArgs[1]);
            } catch(InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(args.Client, args.SpacedArgs[1]);
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

            channel.SendToRoom(args.Client, $":{args.IRCd.Host} {IrcNumeric.RPL_INVITING.Printable()} {args.Client.Nick} {targetClient.Nick} :{channel.Name}", true);
            targetClient.Write($":{args.Client.Mask} INVITE {targetClient.Nick} :{channel.Name}");
            targetClient.Invites.Add(channel);
            return true;
        }

        [Handler("KICK", ListenerType.Client)]
        public static bool KickHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Client targetClient;
            Channel channel;
            String target;
            String message;


            if(args.SpacedArgs.Count <= 2) {
                throw new IrcErrNotEnoughParametersException(client, "KICK");
            }

            if(args.SpacedArgs.Count >= 4) {
                message = args.Trailer;
            } else {
                message = client.Nick;
            }

            target  = args.SpacedArgs[2];

            if (ircd.ChannelManager.Exists(args.SpacedArgs[1])) {
                channel = ircd.ChannelManager[args.SpacedArgs[1]];
            } else {
                throw new IrcErrNoSuchTargetChannelException(client, args.SpacedArgs[1]);
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

        [Handler("TOPIC", ListenerType.Client)]
        public static bool TopicHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Topic topic;
            if(args.SpacedArgs.Count == 0) {
                throw new IrcErrNotEnoughParametersException(client, "TOPIC");
            } else {
                string target = args.SpacedArgs[1];
                if(!ircd.ChannelManager.Channels.ContainsKey(target))
                    throw new IrcErrNoSuchTargetNickException(client, target);
                topic = ircd.ChannelManager[target].Topic;
                if(args.Trailer != null)
                    topic.SetTopic(client, target, args.Trailer);
                else
                    topic.GetTopic(client, target);
                return true;
            }
        }

        [Handler("JOIN", ListenerType.Client)]
        public static bool JoinHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;

            String[] splitCommaLine;
            Channel channel;
            Topic topic;


            if(args.SpacedArgs.Count == 0 && String.IsNullOrWhiteSpace(args.Trailer)) {
                throw new IrcErrNotEnoughParametersException(client, "JOIN");
            }

            try {
                // Message of format: JOIN #x,#y,#z
                splitCommaLine = args.SpacedArgs[1].Split(new char[] { ','});
            } catch(ArgumentOutOfRangeException) {
                // Message of format: JOIN :#x,#y,#z
                splitCommaLine = args.Trailer.Split(new char[] { ',' });
            }

            for(int i = 0; i < splitCommaLine.Length; i++) {
                if((i + 1) > ircd.MaxTargets) break;

                string channel_name = splitCommaLine[i];
                // Some bots will try to send ':' with the channel, remove this
                channel_name = channel_name.StartsWith(":") ? channel_name.Substring(1) : channel_name;
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

                try {
                    channel.Modes["z"].GetValue();
                    if (!client.Modes["z"].Enabled) {
                        throw new IrcErrSecureOnlyChanException(args.Client, channel.Name);
                    }
                } catch(IrcModeNotEnabledException) {}

                try {
                    channel.Modes["O"].GetValue();
                    if (!client.Modes["o"].Enabled) {
                        throw new IrcErrOperOnlyException(client, channel.Name);
                    }
                } catch(IrcModeNotEnabledException) {}

                channel.AddClient(client);
                client.Invites.Remove(channel);
                topic = channel.Topic;
                topic.GetTopic(client, channel_name, true);
            }

            return true;
        }

        [Handler("PRIVMSG", ListenerType.Client)]
        public static bool PrivMsgHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Client targetClient = null;

            String target;
            String message;

            // Catch both no parameter and whitespace
            if (args.SpacedArgs.Count < 2 || String.IsNullOrWhiteSpace(args.SpacedArgs[1])) {
                throw new IrcErrNoRecipientException(client, "PRIVMSG");
            }

            target = args.SpacedArgs[1];

            if (!target.StartsWith("#")) {
                try {
                    targetClient = ircd.IsUUID(target) ? ircd.GetClientByUUID(target) : ircd.GetClientByNick(target);
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetNickException(client, target);
                }
            }

            if (String.IsNullOrEmpty(args.Trailer)) {
                throw new IrcErrNoTextToSendException(client);
            }

            message = args.Trailer;

            if (target.StartsWith("#")) {
                // PRIVMSG a channel
                bool NoExternal = false;
                bool moderated = false;
                if(ircd.ChannelManager.Exists(target)) {
                    Channel channel = ircd.ChannelManager[target];
                    try {
                        channel.Modes["n"].GetValue();
                        NoExternal = true;
                    } catch (IrcModeNotEnabledException) {}
                    if(!channel.Inhabits(client) && NoExternal) {
                        throw new IrcErrNotOnChannelException(client, channel.Name);
                    } else {
                        // Check the bans
                        var userRank = channel.Status(client);
                        if (channel.Modes["b"].Has(client) && userRank.CompareTo(ChannelPrivilege.Op) < 0) {
                            throw new IrcErrCannotSendToChanException(client, channel.Name, "Cannot send to channel (You're banned)");
                        }
                        // Don't send PRIVMSG if it's a moderated channel and the client isn't at least voice
                        try {
                            channel.Modes["m"].GetValue();
                            moderated = true;
                        } catch (IrcModeNotEnabledException) {}

                        if (moderated) {
                            if (!channel.Inhabits(client)) {
                                return false;
                            } else if (channel.Inhabits(client)) {
                                // If the user isn't voiced, send ERR_CANNOTSENDTOCHAN
                                if (userRank.CompareTo(ChannelPrivilege.Voice) < 0) {
                                    throw new IrcErrCannotSendToChanException(client, channel.Name, "You need voice (+v)");
                                }
                            }
                        }
                        channel.SendToRoom(client, String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, channel.Name, message), false);
                    }
                } else {
                    throw new IrcErrNoSuchTargetNickException(client, target);
                }
            } else if(targetClient != null) {
                if(!String.IsNullOrWhiteSpace(targetClient.AwayMessage)) {
                    // If the target client (recipient) is away, warn the person (source) sending the message to them.
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_AWAY.Printable()} {args.Client.Nick} {target} :{targetClient.AwayMessage}");
                }
                targetClient.Write(String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, target, message));
            }
            return true;
        }

        [Handler("NOTICE", ListenerType.Client)]
        public static bool NoticeHandler(HandlerArgs args) {
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            Client targetClient = null;

            String target;
            String message;
            String fmtMessage;

            // Check the target has been sent
            if (args.SpacedArgs.Count >= 1) {
                // Check the target exists
                target = args.SpacedArgs[1];
                if(target.StartsWith("#")) {
                    // The target is a channel
                    if(!ircd.ChannelManager.Exists(target)) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                } else {
                    // The target is a user
                    try {
                        targetClient = ircd.IsUUID(target) ? ircd.GetClientByUUID(target) : ircd.GetClientByNick(target);
                    } catch (InvalidOperationException) {
                        throw new IrcErrNoSuchTargetNickException(client, target);
                    }
                }

                if (String.IsNullOrEmpty(args.Trailer)) {
                    // Client has provided a target but no message
                    throw new IrcErrNoTextToSendException(client);
                }

                message    = args.Trailer;
                fmtMessage = String.Format(":{0} NOTICE {1} {2}", client.Mask, target, message);
                if (target.StartsWith("#")) {
                    bool NoExternal = false;
                    bool moderated = false;
                    Channel channel = ircd.ChannelManager[target];
                    try {
                        channel.Modes["n"].GetValue();
                        NoExternal = true;
                    } catch (IrcModeNotEnabledException) {}
                    if(!channel.Inhabits(client) && NoExternal) {
                        throw new IrcErrNotOnChannelException(client, channel.Name);
                    }
                    // Don't send NOTICE or reply if it's a moderated channel
                    try {
                        channel.Modes["m"].GetValue();
                        moderated = true;
                    } catch (IrcModeNotEnabledException) {}

                    if (moderated) {
                        if (!channel.Inhabits(client)) {
                            return false;
                        } else if (channel.Inhabits(client)) {
                            var userRank = channel.Status(client);
                            // If the user isn't voiced, do nothing
                            if (userRank.CompareTo(ChannelPrivilege.Voice) < 0) {
                                return false;
                            }
                        }
                    }
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

        [Handler("PART", ListenerType.Client)]
        public static bool PartHandler(HandlerArgs args) {
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
            // Some bots will try to send ':' with the channel, remove this
            channel = channel.StartsWith(":") ? channel.Substring(1) : channel;
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

        [Handler("WHO", ListenerType.Client)]
        public static Boolean WhoHandler(HandlerArgs args) {
            String target;
            Channel targetChannel;

            try {
                target = args.SpacedArgs[1];
            } catch(ArgumentOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "WHO");
            }

            if(target.StartsWith("#")) {
                // The target is a channel
                try {
                    targetChannel = args.IRCd.ChannelManager.Channels[target];
                } catch(KeyNotFoundException) {
                    throw new IrcErrNoSuchTargetNickException(args.Client, target);
                }

                foreach(var client in targetChannel.Clients) {
                    // TODO: Also for when we have links (:0 is hopcount)
                    var userPriv = targetChannel.Status(client.Value);
                    var userSymbol = targetChannel.GetUserSymbol(userPriv);
                    var ircopSymbol = client.Value.Modes["o"].Enabled ? "*" : "";
                    var hopCount = 0;

                    var away = "";
                    if(String.IsNullOrEmpty(client.Value.AwayMessage)) {
                        away = "H"; // "Here"
                    } else {
                        away = "G"; // "Gone"
                    }
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_WHOREPLY.Printable()} {args.Client.Nick} {target} {client.Value.Ident} {client.Value.GetHost()} {args.IRCd.Host} {client.Value.Nick} {away}{ircopSymbol}{userSymbol} :{hopCount} {client.Value.RealName}");
                }
                args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFWHO.Printable()} {args.Client.Nick} {target} :End of /WHO list.");
            } else {
                // The target is a user
                // See Queries.cs for the user-WHO implementation
            };
            return true;
        }

        [Handler("NAMES", ListenerType.Client)]
        public static Boolean NamesHandler(HandlerArgs args) {
            String[] splitLineSpace = args.Line.Split(new string[] { " " }, 3, StringSplitOptions.None);
            String [] splitCommaLine;

            try {
                splitCommaLine = splitLineSpace[1].Split(new char[] { ','});
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "NAMES");
            }

            for(int i = 0; i < splitCommaLine.Length; i++) {
                if((i + 1) > args.IRCd.MaxTargets) break;

                var channel_name = splitCommaLine[i];
                if (!ChannelManager.IsValid(channel_name)) {
                    throw new IrcErrNoSuchTargetChannelException(args.Client, channel_name);
                }

                if(args.IRCd.ChannelManager.Exists(channel_name)) {
                    Channel targetChannel = args.IRCd.ChannelManager[channel_name];
                    foreach(var client in targetChannel.Clients) {
                        var userPriv = targetChannel.Status(client.Value);
                        var userSymbol = targetChannel.GetUserSymbol(userPriv);
                        args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_NAMREPLY.Printable()} {args.Client.Nick} = {channel_name} :{userSymbol}{client.Value.Nick}");
                    }
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFNAMES.Printable()} {args.Client.Nick} {channel_name} :End of /NAMES list.");
                }
            }

            return true;
        }

        [Handler("MODE", ListenerType.Client)]
        public static bool ModeHandler(HandlerArgs args) {
            // This handler is for Channel requests (i.e. where the target begins with a # or &)
            // TODO: update with channel validation logic (in ChannelManager?)
            string target;
            Channel channel;

            try {
                target = args.SpacedArgs[1];
            } catch(ArgumentOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "MODE");
            }

            if(!target.StartsWith("#") && !target.StartsWith("&")) {
                return false;
            }

            if(args.IRCd.ChannelManager.Exists(target)) {
                channel = args.IRCd.ChannelManager[target];
            } else {
                throw new IrcErrNoSuchTargetChannelException(args.Client, target);
            }

            if(args.SpacedArgs.Count == 2) {
                // This is a request for MODE (e.g. MODE #cmpctircd)
                string[] channelModes = channel.GetModeStrings("+");
                string characters = channelModes[0];
                string argsSet = channelModes[1];
                if(!String.IsNullOrWhiteSpace(argsSet)) {
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_CHANNELMODEIS.Printable()} {args.Client.Nick} {channel.Name} {characters} {argsSet}");
                } else {
                    args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_CHANNELMODEIS.Printable()} {args.Client.Nick} {channel.Name} {characters}");
                }
                // TODO: creation time?

            } else if(args.SpacedArgs.Count > 2) {
                // Process
                string modes = args.SpacedArgs[2];
                string[] param;

                if(args.SpacedArgs.Count == 3) {
                    args.SpacedArgs.Add("");
                }

                param = args.SpacedArgs[3].Split(new string[] { " " }, StringSplitOptions.None);

                string currentModifier = "";
                string modeChars = "";
                string modeArgs = "";
                string modeString = "";
                bool announce = false;
                int position = 0;
                ChannelMode modeObject;
                var modesNoOperator = modes.Replace("+", "").Replace("-", "");
                if(args.IRCd.GetSupportedModesByType()["A"].Contains(modesNoOperator)) {
                    // Is this mode of Type A (and listable)? See ModeType
                    // TODO: should we put this in the foreach?

                    // TODO: Some ircds make the ban list op only?
                    if(modesNoOperator == "b" && String.IsNullOrEmpty(args.SpacedArgs[3])) {
                        var banMode = (BanMode)channel.Modes["b"];
                        foreach(Ban ban in banMode.Bans.Values) {
                            args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_BANLIST.Printable()} {args.Client.Nick} {channel.Name} {ban.GetBan()}");
                        }
                        args.Client.Write($":{args.IRCd.Host} {IrcNumeric.RPL_ENDOFBANLIST.Printable()} {args.Client.Nick} {channel.Name} :End of channel ban list");
                        return true;
                    }
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
                        if (!modeObject.Stackable) {
                            announce = true;
                        }
                        if(currentModifier == "+") {
                            // Attempt to add the mode
                            bool success = modeObject.Grant(args.Client, param[position], args.Force, announce, announce);

                            if(success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if(modeObject.HasParameters) {
                                    modeArgs += param[position] + " ";
                                }
                            }
                        } else if(currentModifier == "-") {
                            // Attempt to revoke the mode
                            bool success = modeObject.Revoke(args.Client, param[position], args.Force, announce, announce);

                            if(success && modeObject.Stackable) {
                                modeChars += modeStr;
                                if(modeObject.HasParameters) {
                                    modeArgs += param[position] + " ";
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
                    if(!modeString.Contains("+") && !modeString.Contains("-")) {
                        // Return if the mode string doesn't contain an operator
                        return true;
                    }
                    // Need to send UUID to room and not the nick if stacking
                    if(args.Client.RemoteClient) {
                        if (modeString.Contains(" ")) {
                            var splitBySpaces = modeString.Split(' ');
                            foreach(var chunk in splitBySpaces) {
                                if(args.IRCd.IsUUID(chunk)) {
                                    modeString = modeString.Replace(chunk, args.IRCd.GetClientByUUID(chunk).Nick);
                                }
                            }
                        }
                    }
                    channel.SendToRoom(args.Client, $":{args.Client.Mask} MODE {channel.Name} {modeString}");
                }
            }

            return true;
        }


    }
}
