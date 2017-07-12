using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class ChannelManager {
        private IRCd IRCd { get; set; }
        public ConcurrentDictionary<String, Channel> Channels
        {
            get; private set;
        } = new ConcurrentDictionary<string, Channel>();

        public ChannelManager(IRCd ircd) {
            this.IRCd = ircd;
        }

        /*
         * Useful methods for managing channels 
        */
        public Channel Create(String channel_name) {
            Channel channel = new Channel();
            channel.Name = channel_name;
            Channels.TryAdd(channel_name, channel);
            return channel;
        }

        public Channel this[String channel] => Channels[channel];

        public bool Exists(String channel) => Channels.ContainsKey(channel);

        public int Size => Channels.Count();
    }
}
