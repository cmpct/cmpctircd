using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Packets
{
    public class Queries
    {
        // This class is for the server query group of commands
        // TODO: Stats & Links?
        public Queries(IRCd ircd)
        {
            ircd.PacketManager.Register("VERSION", versionHandler);
            ircd.PacketManager.Register("WHOIS", WhoisHandler);
        }

        public Boolean versionHandler(HandlerArgs args)
        {
            args.Client.SendVersion();
            return true;
        }

        public Boolean WhoisHandler(HandlerArgs args) {
            String[] splitLine = args.Line.Split(' ');
            String target;
            Client targetClient;

            try {
                target = splitLine[1];
            } catch(IndexOutOfRangeException) {
                throw new IrcErrNotEnoughParametersException(args.Client, "WHOIS");
            }

            // Need the client object of the target...
            try {
                targetClient = args.IRCd.getClientByNick(target);
            } catch(InvalidOperationException) {
                throw new IrcErrNoSuchTargetException(args.Client, target);
            }

            args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_WHOISUSER.Printable()} {args.Client.Nick} {targetClient.Nick} {targetClient.Ident} {targetClient.Host} * :{targetClient.RealName}");

            // Generate a list of all the channels inhabited by the target
            // XXX: no LINQ for now because of strange bug where LINQ in Packet/dynamic classes causes an exception
            //Predicate<Channel> channelFinder = (Channel chan) => { return chan.Inhabits(targetClient); };
            //List<Channel> inhabitedChannels = args.IRCd.ChannelManager.Channels.Values.ToList().FindAll(channelFinder);

            var inhabitedChannels = new List<String>();
            foreach(var channel in args.IRCd.ChannelManager.Channels.Values) {
                if(channel.Inhabits(targetClient)) {
                    inhabitedChannels.Add(channel.Name);
                }
            }

            if(targetClient == args.Client) {
                // Only allow the target client's sensitive connection info if WHOISing themselves
                // TODO: modify to allow ircops to see this too (when we have ircops)
                args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_WHOISHOST.Printable()} {args.Client.Nick} {targetClient.Nick} :is connecting from {targetClient.Ident}@{targetClient.Host} {targetClient.Host}");
            }

            if(inhabitedChannels.Count() > 0) {
                // Only show if the target client resides in at least one channel
                // TODO: needs modification for DNS (the last 'Host' should become 'IP', but there's no distinction between these yet)
                args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_WHOISCHANNELS.Printable()} {args.Client.Nick} {targetClient.Nick} :{string.Join(" ", inhabitedChannels)}");
            }

            args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_WHOISSERVER.Printable()} {args.Client.Nick} {targetClient.Nick} {args.IRCd.host} :{args.IRCd.desc}");
            args.Client.Write($":{args.IRCd.host} {IrcNumeric.RPL_ENDOFWHOIS.Printable()} {args.Client.Nick} {targetClient.Nick} :End of /WHOIS list");
            return true;
        }
    }
}
