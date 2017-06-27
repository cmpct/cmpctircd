using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    class Topic
    {
        // Hold our topic metadata: The topic itself, user who set it, date & time set, channel set on
        public string topic;
        public Client user;
        public Channel channel;
        public Int32 date;

        internal void get_topic(Client client, string target)
        {
            if (topic == null)
            {
                client.write(String.Format(":{0} {1} {2} {3} :No topic is set.", client.ircd.host, IrcNumeric.RPL_NOTOPIC.Printable(), client.nick, target));
            } else
            {
                client.write(String.Format(":{0} {1} {2} {3} :{4}", client.ircd.host, IrcNumeric.RPL_TOPIC.Printable(), client.nick, target, topic));
                client.write(String.Format(":{0} {1} {2} {3} {4} {5}", client.ircd.host, IrcNumeric.RPL_TOPICWHOTIME.Printable(), client.nick, target, user.mask(), date));
            }
        }

        internal void set_topic(Client client, string target, string rawLine)
        {
            topic = rawLine.Split(':')[1];
            channel = client.ircd.channelManager.get(target);
            if (channel.inhabits(client))
            {
                // TO DO: Change how we get the unix timestamp
                date = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                user = client;
                client.write(String.Format(":{0} TOPIC {1} :{2}", client.mask(), channel.name, topic));
            }
        }
    }
}