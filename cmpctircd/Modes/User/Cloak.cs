using cmpctircd.Cloak;

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

            Subject.Cloak = Cloaking.GenerateInspCloak(new CloakOptions(Subject.DNSHost, Subject.IP, Subject.IRCd.CloakKey, Subject.IRCd.CloakFull));

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

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} +{Character}");

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

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} -{Character}");
            return true;
        }

    }
}