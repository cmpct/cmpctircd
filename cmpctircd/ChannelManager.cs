using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class ChannelManager {
        private IRCd ircd;
        private Dictionary<String, Channel> channels = new Dictionary<string, Channel>();

        public ChannelManager(IRCd ircd) {
            this.ircd = ircd;
        }

        /*
         * Useful methods for managing channels 
        */
        public Channel create(String channel_name) {
            Channel channel = new Channel();
            channel.name = channel_name;
            channels.Add(channel_name, channel);
            return channel;
        }
        public Channel get(String channel) {
            return channels[channel];
        }
        public bool exists(String channel) {
            return channels.ContainsKey(channel);
        }
        public int size() {
            return channels.Count();
        }


    }
}
