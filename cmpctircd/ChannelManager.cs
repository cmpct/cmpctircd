using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class ChannelManager {
        private IRCd IRCd { get; set; }
        public Dictionary<String, Channel> Channels
        {
            get; private set;
        } = new Dictionary<string, Channel>();

        public ChannelManager(IRCd ircd) {
            this.IRCd = ircd;
        }

        /*
         * Useful methods for managing channels 
        */
        public Channel create(String channel_name) {
            Channel channel = new Channel();
            channel.Name = channel_name;
            Channels.Add(channel_name, channel);
            return channel;
        }
        public Channel get(String channel) {
            return Channels[channel];
        }
        public bool exists(String channel) {
            return Channels.ContainsKey(channel);
        }
        public int size() {
            return Channels.Count();
        }
    }
}
