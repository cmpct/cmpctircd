using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static cmpctircd.Errors;

namespace cmpctircd.Packets {
    class Channels {
        //private IRCd ircd;

        public Channels(IRCd ircd) {
            ircd.PacketManager.register("JOIN", joinHandler);
            ircd.PacketManager.register("PRIVMSG", privmsgHandler);
            ircd.PacketManager.register("PART", partHandler);
            ircd.PacketManager.register("TOPIC", topicHandler);
        }

        public Boolean topicHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            Topic topic;
            String rawLine = args.GetValue(2).ToString();
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
                    if (!ircd.ChannelManager.list().ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                    topic = ircd.ChannelManager.get(target).Topic;
                    topic.get_topic(client, target);
                    return true;

                default:
                    target = rawSplit[1];
                    if (!ircd.ChannelManager.list().ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                    topic = ircd.ChannelManager.get(target).Topic;
                    topic.set_topic(client, target, rawLine);
                    return true;
            }
        }

        public Boolean joinHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();

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
                if (ircd.ChannelManager.exists(channel_name)) {
                    channel = ircd.ChannelManager.get(channel_name);
                } else {
                    channel = ircd.ChannelManager.create(channel_name);
                }
                channel.addClient(client);
            }

            return true;
        }

        public Boolean privmsgHandler(Array args) {
            // Only for channel PRIVMSGs (PRIVMSG #channel ...)
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
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
                if(ircd.ChannelManager.exists(target)) {
                    Channel channel = ircd.ChannelManager.get(target);
                    if(channel.inhabits(client)) {
                        channel.send_to_room(client, String.Format(":{0} PRIVMSG {1} :{2}", client.mask(), channel.Name, message), false);
                        return true;
                    }
                }
            } else {
                // Check the user exists
                foreach (var clientList in ircd.ClientLists) {
                    foreach (var clientDict in clientList) {
                        if (clientDict.Key.Nick.ToLower() == target.ToLower()) {
                            clientDict.Key.write(String.Format(":{0} PRIVMSG {1} :{2}", client.mask(), target, message));
                            return true;
                        }
                    }
                }
            }
            throw new IrcErrNoSuchTargetException(client, target);
        }

        public Boolean partHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
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
            if(!ircd.ChannelManager.exists(channel)) {
                throw new IrcErrNoSuchTargetException(client, channel);
            }

            // Are we in the channel?
            Channel channelObj = ircd.ChannelManager.get(channel);
            if(!channelObj.inhabits(client)) {
                throw new IrcErrNotOnChannelException(client, channel);
            }

            channelObj.part(client, reason);
            return true;
        }

    }
}
