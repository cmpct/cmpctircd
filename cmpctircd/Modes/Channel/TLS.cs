namespace cmpctircd.Modes {
    public class TlsMode : ChannelMode {

        public TlsMode(Channel channel) : base(channel) {
            Name = "TLS";
            Description = "Provides the +z (TLS users only) mode.";
            Character = "z";
            Symbol = "";
            Type = ChannelModeType.D;
            Level = ChannelPrivilege.Op;
            HasParameters = false;
            ChannelWide = true;
        }
        private bool Enabled { get; set; }

        override public string GetValue() {
            if (Enabled) {
                return Enabled.ToString();
            }
            throw new IrcModeNotEnabledException(Character);
        }
        override public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {

            if (!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (Enabled && !forceSet) {
                return false;
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

            if (!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (!Enabled && !forceSet) {
                return false;
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