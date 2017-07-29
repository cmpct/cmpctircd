using System;
using System.Net;

namespace cmpctircd.Modes {
    public class CloakMode : UserMode {

        public CloakMode(Client subject) : base(subject) {
            Name = "cloak";
            Description = "Provides the +x (cloak) mode to hide user hostnames/IPs";
            Character = "x";
            HasParameters = false;
        }


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled)
                return false;

            if (string.Compare(Subject.IP.ToString(), Subject.DNSHost) == 0) {
                // ip cloaking where host == ip because they clearly don't have a dns host
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