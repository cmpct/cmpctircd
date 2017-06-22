using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets {
    class Channels {
        //private IRCd ircd;

        public Channels(IRCd ircd) {
            ircd.packetManager.register("JOIN", joinHandler);
            ircd.packetManager.register("PRIVMSG", privmsgHandler);
        }

        public Boolean joinHandler(Array args) {
            IRCd ircd = (IRCd)args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();

            String[] splitLine = rawLine.Split(' ');
            String[] splitColonLine = rawLine.Split(new char[] { ':' }, 2);

            String channel_name = splitLine[1];
            Channel channel;

            // Get the channel object, creating it if it doesn't already exist
            // TODO: validation
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
            IRCd ircd = (IRCd) args.GetValue(0);
            Client client = (Client)args.GetValue(1);
            String rawLine = args.GetValue(2).ToString();
            String target;
            String message;

            try {
                target = rawLine.Split(' ')[1];
                message = rawLine.Split(new string[] { ":" }, 2, StringSplitOptions.None)[1];
            } catch(IndexOutOfRangeException) {
                Console.WriteLine("Invalid data sent in PRIVMSG");
                return false;
            }


            Console.WriteLine("Got a PRIVMSG");
            if (target.StartsWith("#")) {
                // PRIVMSG a channel
                // TODO: We don't have +n yet so just check if they're in the room...
                if(ircd.channelManager.exists(target)) {
                    Channel channel = ircd.channelManager.get(target);
                    if(channel.inhabits(client)) {
                        channel.send_to_room(client, String.Format(":{0} PRIVMSG {1} :{2}", client.mask(), channel.name, message), false);
                    }
                }
            } else {
                // PRIVMSG a user
            }

            return true;
        }

    }
}
