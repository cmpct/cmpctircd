namespace cmpctircd.Modes {
    public class InviteMode : Mode {

        public InviteMode(Channel channel) : base(channel) {
            Name = "invite";
            Description = "Provides the +i (invite) mode for invite-only channels";
            Character = "i";
            Symbol = "";
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
        override public bool Grant(Client client, string args) => Grant(client, args, false, true);
        override public bool Grant(Client client, string args, bool forceSet, bool announce) {

            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            // Announce the change to the room
            Enabled = true;
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +i", announce);
            return true;
        }
        
        override public bool Revoke(Client client, string args) => Revoke(client, args, false, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce) {

            if(!channel.Inhabits(client))
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

            Enabled = false;
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -i", announce);
            return true;
        }

    }
}