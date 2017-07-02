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
            ircd.packetManager.register("JOIN", joinHandler);
            ircd.packetManager.register("PRIVMSG", privmsgHandler);
            ircd.packetManager.register("PART", partHandler);
            ircd.packetManager.register("TOPIC", topicHandler);
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
                    if (!ircd.channelManager.list().ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                    topic = ircd.channelManager.get(target).topic;
                    topic.get_topic(client, target);
                    return true;

                default:
                    target = rawSplit[1];
                    if (!ircd.channelManager.list().ContainsKey(target)) {
                        throw new IrcErrNoSuchTargetException(client, target);
                    }
                    topic = ircd.channelManager.get(target).topic;
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
            String channel_name;
            Channel channel;

            try {
                channel_name = splitLine[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(client, "JOIN");
            }

            // Get the channel object, creating it if it doesn't already exist
            // TODO: only applicable error is ERR_NEEDMOREPARAMS for now, more for limits/bans/invites
            if (ircd.channelManager.exists(channel_name)) {
                channel = ircd.channelManager.get(channel_name);
            } else {
                channel = ircd.channelManager.create(channel_name);
            }
            channel.addClient(client);
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
                if(ircd.channelManager.exists(target)) {
                    Channel channel = ircd.channelManager.get(target);
                    if(channel.inhabits(client)) {
                        channel.send_to_room(client, String.Format(":{0} PRIVMSG {1} :{2}", client.mask(), channel.name, message), false);
                        return true;
                    }
                }
            } else {
                // Check the user exists
                foreach (var clientList in ircd.clientLists) {
                    foreach (var clientDict in clientList) {
                        if (clientDict.Key.nick.ToLower() == target.ToLower()) {
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
            String channel = splitLineSpace[1];
            String reason = splitLine[1];

            ircd.channelManager.get(channel).part(client, reason);
            return true;
        }

    }
}
