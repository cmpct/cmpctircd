namespace cmpctircd.Modes {
    public class InviteMode : ChannelMode {

        public InviteMode(Channel channel) : base(channel) {
            Name = "invite";
            Description = "Provides the +i (invite) mode for invite-only channels";
            Character = "i";
            Symbol = "";
            Type = ChannelModeType.D;
            MinimumUseLevel = ChannelPrivilege.Op;
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
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
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
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +i", sendSelf);
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

            Enabled = false;
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -i", sendSelf);
            }
            return true;
        }

    }
}