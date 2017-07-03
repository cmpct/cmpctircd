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
                        return true;
                    }
                }
            } else {
                // Check the user exists
                foreach (var clientList in ircd.ClientLists) {
                    foreach (var clientDict in clientList) {
                        if (clientDict.Key.Nick.ToLower() == target.ToLower()) {
                            clientDict.Key.Write(String.Format(":{0} PRIVMSG {1} :{2}", client.Mask, target, message));
                            return true;
                        }
                    }
                }
            }
            throw new IrcErrNoSuchTargetException(client, target);
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
