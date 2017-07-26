namespace cmpctircd.Modes {
    public class TopicMode : Mode {

        public TopicMode(Channel channel) : base(channel) {
            Name = "topic";
            Description = "Provides +t/-t for locking or unlocking topic set";
            Character = "t";
            Symbol = "";
            Type = ModeType.D;
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
        override public bool Grant(Client client, string args, bool forceSet, bool announce) => Grant(client, args, forceSet, announce, true);
        override public bool Grant(Client client, string args, bool forceSet, bool announce, bool sendSelf) {

            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (Enabled) {
                return false;
            }

            // Announce the change to the room
            Enabled = true;
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +t", sendSelf);
            }
            return true;
        }
        
        override public bool Revoke(Client client, string args) => Revoke(client, args, false, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce) => Revoke(client, args, forceSet, announce, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce, bool sendSelf) {

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

            if (!Enabled) {
                return false;
            }

            Enabled = false;
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -t", sendSelf);
            }
            return true;
        }

    }
}