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
        public string TopicText { get; set; }
        public Client User { get; set; }
        public Channel Channel { get; set; }
        public Int32 Date { get; set; }

        // TODO: Revise function definition
        internal void get_topic(Client client, string target)
        {
            if (TopicText == null)
            {
                client.write(String.Format(":{0} {1} {2} {3} :No topic is set.", client.IRCd.host, IrcNumeric.RPL_NOTOPIC.Printable(), client.Nick, target));
            } else
            {
                client.write(String.Format(":{0} {1} {2} {3} :{4}", client.IRCd.host, IrcNumeric.RPL_TOPIC.Printable(), client.Nick, target, TopicText));
                client.write(String.Format(":{0} {1} {2} {3} {4} {5}", client.IRCd.host, IrcNumeric.RPL_TOPICWHOTIME.Printable(), client.Nick, target, User.mask(), Date));
            }
        }

        internal void set_topic(Client client, string target, string rawLine)
        {
            TopicText = rawLine.Split(':')[1];
            Channel = client.IRCd.ChannelManager[target];
            if (Channel.inhabits(client))
            {
                // TO DO: Change how we get the unix timestamp
                Date = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                User = client;
                client.write(String.Format(":{0} TOPIC {1} :{2}", client.mask(), Channel.Name, TopicText));
            }
        }
    }
}