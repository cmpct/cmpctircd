using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class Topic {
        // Hold our topic metadata: The topic itself, user who set it, date & time set, channel set on
        public string TopicText { get; set; }
        public Client User { get; set; }
        public Channel Channel { get; set; }
        public Int32 Date { get; set; }

        // TODO: Revise function definition (could be properties?)
        public void GetTopic(Client client, string target, bool onJoin = false) {
            if (String.IsNullOrWhiteSpace(TopicText) && !onJoin) {
                client.Write(String.Format(":{0} {1} {2} {3} :No topic is set.", client.IRCd.host, IrcNumeric.RPL_NOTOPIC.Printable(), client.Nick, target));
            }
            else if (!String.IsNullOrWhiteSpace(TopicText)) {
                client.Write(String.Format(":{0} {1} {2} {3} :{4}", client.IRCd.host, IrcNumeric.RPL_TOPIC.Printable(), client.Nick, target, TopicText));
                client.Write(String.Format(":{0} {1} {2} {3} {4} {5}", client.IRCd.host, IrcNumeric.RPL_TOPICWHOTIME.Printable(), client.Nick, target, User.Mask, Date));
            }
        }

        public void SetTopic(Client client, string target, string rawLine) {
            TopicText = rawLine.Split(':')[1];
            Channel = client.IRCd.ChannelManager[target];
            if (Channel.Inhabits(client)) {
                // TO DO: Change how we get the unix timestamp
                Date = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                User = client;
                Channel.SendToRoom(client, String.Format(":{0} TOPIC {1} :{2}", client.Mask, Channel.Name, TopicText), true);
            }
        }
    }
}