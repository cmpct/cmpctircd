using System;
using System.Net;

namespace cmpctircd.Modes {
    public class CloakMode : UserMode {

        override public string Name { get; } = "cloak";
        override public string Description { get; } = "Provides the +x (cloak) mode to hide user hostnames/IPs";
        override public string Character { get; } = "x";
        override public bool HasParameters {get; } = false;
        override public bool Stackable { get; } = true;
        override public bool Enabled { get; set; } = false;
        override public bool AllowAutoSet { get; } = true;

        public CloakMode(Client subject) : base(subject) {}


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled)
                return false;

            if (Subject.DNSHost == null) {
               switch(Subject.IP.AddressFamily) {
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        // IPv6
                        Subject.Cloak = Cloak.CmpctCloakIPv6(Subject.IP, Subject.IRCd.CloakKey);
                        break;

                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        // IPv4
                        Subject.Cloak = Cloak.CmpctCloakIPv4(Subject.IP, Subject.IRCd.CloakKey);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown IP address type: {Subject.IP.AddressFamily}!");

                }
            } else {
                // DNS cloaking
                Subject.Cloak = Cloak.CmpctCloakHost(Subject.DNSHost, Subject.IRCd.CloakKey);
            }

            Enabled = true;
            Subject.Write($":{Subject.IRCd.Host} {IrcNumeric.RPL_HOSTHIDDEN.Printable()} {Subject.Nick} {Subject.Cloak} :is now your displayed host");
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} +x");
            }

            foreach(var chan in Subject.IRCd.ChannelManager.Channels) {
                if(!chan.Value.Inhabits(Subject)) continue;
                chan.Value.Part(Subject, "Changing host", false);
                chan.Value.AddClient(Subject);

                // Force set all the modes they had
                foreach(var mode in chan.Value.Modes) {
                    if(!mode.Value.ChannelWide && mode.Value.Has(Subject)) {
                        mode.Value.Grant(Subject, Subject.Nick, true, true, true);
                    }
                }
            }
            return true;
        }

        override public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (!Enabled)
                return false;

            Enabled = false;
            Subject.Cloak = "";
            Subject.Write($":{Subject.IRCd.Host} {IrcNumeric.RPL_HOSTHIDDEN.Printable()} {Subject.Nick} {Subject.GetHost()} :is now your displayed host");
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} -x");
            }
            
            foreach(var chan in Subject.IRCd.ChannelManager.Channels) {
                if(!chan.Value.Inhabits(Subject)) continue;
                chan.Value.Part(Subject, "Changing host", false);
                chan.Value.AddClient(Subject);
            }
            return true;
        }

    }
}