namespace cmpctircd.Modes {
    public class AllTlsMode : ChannelMode {

        override public string Name { get; } = "AllTls";
        override public string Character { get; } = "Z";
        override public string Symbol { get; } = "";
        override public string Description { get; } = "Provides the +Z (TLS users only) mode; all users in channel are on TLS.";
        override public ChannelModeType Type { get; } = ChannelModeType.D;
        override public ChannelPrivilege ProvidedLevel { get; } = ChannelPrivilege.Normal;
        override public ChannelPrivilege MinimumUseLevel { get; } = ChannelPrivilege.Op;
        override public bool HasParameters { get; } = false;
        override public bool ChannelWide { get; } = true;
        override public bool Stackable { get; } = true;

        public AllTlsMode(Channel channel) : base(channel) {}
        private bool Enabled { get; set; }

        override public string GetValue() {
            if (Enabled) {
                return Enabled.ToString();
            }
            throw new IrcModeNotEnabledException(Character);
        }
        override public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {            
            try {
                channel.Modes["z"].GetValue();
            } catch(IrcModeNotEnabledException) {
                return false;
            }

            if (Enabled && !forceSet) {
                return false;
            }

            lock(channel.Clients) {
                // We can't have people join which aren't on TLS, so lock the dict
                foreach(var pair in channel.Clients) {
                    // Return false if any user doesn't have TLS
                    if(!pair.Value.Modes["z"].Enabled) return false;
                }
            }

            // Announce the change to the room
            Enabled = true;
            if (announce) {
                channel.SendToRoom(client, $":{client.IRCd.Host} MODE {channel.Name} +Z", sendSelf);
            }
            return true;
        }

        override public bool Revoke(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {

            if (!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (!Enabled && !forceSet) {
                return false;
            }
            
            bool foundNonTlsUser = false;
            lock(channel.Clients) {
                // We can't have people join which aren't on TLS, so lock the dict
                foreach(var pair in channel.Clients) {
                    // Return false if any user doesn't have TLS
                    if(!pair.Value.Modes["z"].Enabled) foundNonTlsUser = true;
                }
            }

            if(!foundNonTlsUser && !forceSet) {
                return false;
            }

            Enabled = false;
            if (announce) {
                channel.SendToRoom(client, $":{client.IRCd.Host} MODE {channel.Name} -Z", sendSelf);
            }
            return true;
        }

    }
}