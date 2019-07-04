using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class ChannelManager {
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
        public Channel Create(String channel_name) {
            Channel channel = new Channel(this, IRCd);
            channel.Name = channel_name;
            Channels.Add(channel_name, channel);

            IRCd.ServerLists.ForEach(serverList => serverList.ForEach(
                server => server.SyncChannel(channel)
            ));
            return channel;
        }

        public void Remove(String channel_name) {
            Channels.Remove(channel_name);
        }

        public Channel this[String channel] => Channels[channel];

        public bool Exists(String channel) => Channels.ContainsKey(channel);

        public int Size => Channels.Count();

        public static bool IsValid(string channel_name) {
            if(
                !(channel_name.StartsWith("#") || channel_name.StartsWith("&"))
                ||
                (channel_name.Contains(" ") || channel_name.Contains("\a"))) {
                return false;
            }
            return true;
        }
    }
}
