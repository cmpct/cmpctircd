namespace cmpctircd.Modes {
    public class TlsMode : ChannelMode {

        override public string Name { get; } = "TLS";
        override public string Description { get; } = "Provides the +z (TLS users only) mode";
        override public string Character { get; } = "z";
        override public string Symbol { get; } = "";
        override public ChannelModeType Type { get; } = ChannelModeType.D;
        override public ChannelPrivilege MinimumUseLevel { get; } = ChannelPrivilege.Op;
        override public ChannelPrivilege ProvidedLevel { get; } = ChannelPrivilege.Normal;
        override public bool HasParameters { get; } = false;
        override public bool ChannelWide { get; } = true;
        override public bool Stackable { get; } = true;
        override public bool AllowAutoSet { get; } = true;
        override public bool InspircdCompatible { get; } = false;

        public TlsMode(Channel channel) : base(channel) {}
        private bool Enabled { get; set; }

        override public string GetValue() {
            if (Enabled) {
                return Enabled.ToString();
            }
            throw new IrcModeNotEnabledException(Character);
        }
        override public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if(!forceSet) {
                if (!channel.Inhabits(client))
                    throw new IrcErrNotOnChannelException(client, channel.Name);

                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }

                if (Enabled) {
                    return false;
                }
            }

            // Announce the change to the room
            Enabled = true;
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +z", sendSelf);
            }

            // Attempt to set +Z (only works if applicable)
            if(channel.Modes.ContainsKey("Z")) {
                channel.Modes["Z"].Grant(client, client.Nick, false, true, true);
            }
            return true;
        }

        override public bool Revoke(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                if (!channel.Inhabits(client))
                    throw new IrcErrNotOnChannelException(client, channel.Name);

                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }

                if (!Enabled) {
                    return false;
                }
            }

            Enabled = false;
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -z", sendSelf);
            }

            // Attempt to unset +Z
            channel.Modes["Z"].Revoke(client, client.Nick, false, true, true);
            return true;
        }

    }
}