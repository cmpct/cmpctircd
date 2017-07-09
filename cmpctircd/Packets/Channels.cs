using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets {
    public class Channels {
        //private IRCd ircd;

        public Channels(IRCd ircd) {
            ircd.PacketManager.Register("JOIN", joinHandler);
            ircd.PacketManager.Register("PRIVMSG", privmsgHandler);
            ircd.PacketManager.Register("PART", partHandler);
            ircd.PacketManager.Register("TOPIC", topicHandler);
            ircd.PacketManager.Register("NOTICE", noticeHandler);
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
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                    topic = ircd.ChannelManager[target].Topic;
                    topic.GetTopic(client, target);
                    return true;

                default:
                    target = rawSplit[1];
                    if (!ircd.ChannelManager.Channels.ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetException(client, target);
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

            try {
                splitCommaLine = splitLine[1].Split(new char[] { ','});
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "JOIN");
            }

            foreach(String channel_name in splitCommaLine) {
                // TODO: Regex, error handling
                // We don't need to check for commas because the split handled that.
                // Do check for proper initializing char, and check for BEL and space.
                if (!(channel_name.StartsWith("#") || channel_name.StartsWith("&")) &&
                    (channel_name.Contains(" ") || channel_name.Contains("\a"))) {
                    continue;
                }
                // Get the channel object, creating it if it doesn't already exist
                // TODO: only applicable error is ERR_NEEDMOREPARAMS for now, more for limits/bans/invites
                if (ircd.ChannelManager.Exists(channel_name)) {
                    channel = ircd.ChannelManager[channel_name];
                } else {
                    channel = ircd.ChannelManager.Create(channel_name);
                }
                channel.AddClient(client);
            }

            return true;
        }

        public Boolean privmsgHandler(HandlerArgs args) {
            // Only for channel PRIVMSGs (PRIVMSG #channel ...)
            IRCd ircd = args.IRCd;
            Client client = args.Client;
            String rawLine = args.Line;
            String[] rawSplit;
            String target;
            String message;
            String command;

            rawSplit = rawLine.Split(' ');
            command = rawSplit[0].ToUpper();

            try {
                target = rawSplit[1];
                message = rawLine.Split(new string[] { ":" }, 2, StringSplitOptions.None)[1];
            } catch (IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, command);
            }


            Console.WriteLine("Got a PRIVMSG");
            if (target.StartsWith("#")) {
                // PRIVMSG a channel
                // TODO: We don't have +n yet so just check if they're in the room...
                if(ircd.ChannelManager.Exists(target)) {
                    Channel channel = ircd.ChannelManager[target];
                    if(channel.Inhabits(client)) {
                        channel.SendToRoom(client, String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, channel.Name, message), false);
                    }
                } else {
                    throw new IrcErrNoSuchTargetException(client, target);
                }
            } else {
                Client targetClient;
                // Check the user exists
                try {
                    targetClient = ircd.GetClientByNick(target);
                } catch(InvalidOperationException) {
                    throw new IrcErrNoSuchTargetException(client, target);
                }

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
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                } else {
                    // The target is a user
                    foreach (var clientList in ircd.ClientLists) {
                        foreach (var clientSearch in clientList) {
                            if (clientSearch.Nick.ToLower() == target.ToLower()) {
                                targetClient = clientSearch;
                                userExists = true;
                            }
                        }
                    }

                    if (!userExists) {
                        throw new IrcErrNoSuchTargetException(client, target);
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
                throw new IrcErrNoSuchTargetException(client, channel);
            }

            // Are we in the channel?
            Channel channelObj = ircd.ChannelManager[channel];
            if(!channelObj.Inhabits(client)) {
                throw new IrcErrNotOnChannelException(client, channel);
            }

            channelObj.Part(client, reason);
            return true;
        }

    }
}
