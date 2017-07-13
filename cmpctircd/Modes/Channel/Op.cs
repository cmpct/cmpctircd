namespace cmpctircd.Modes {
    public class OpMode : Mode {

        public OpMode(Channel channel) : base(channel) {
            Name = "op";
            Description = "Provides the +o (op) mode for moderating a channel";
            Character = "o";
            Level = ChannelPrivilege.Op;
            HasParameters = true;
            ChannelWide = false;
        }

        override public bool Grant(Client client, string args) => Grant(client, args, false, true);
        override public bool Grant(Client client, string args, bool forceSet, bool announce) {
            string targetNick = args;
            Client targetClient;

            try {
                targetClient = client.IRCd.GetClientByNick(targetNick);
            } catch(System.InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(client, targetNick);
            }

            // Check if both the source and the target are on the channel
            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            // TODO: this should be ERR_NOSUCHNICK when we have +i to avoid leaking user's presence on a channel?
            if(!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // User already has the mode
            if(Has(targetClient))
                return true;

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            // Set the subject's privilege to the new status
            // But first check if they already have such a privilege...
            ChannelPrivilege targetPrivs = channel.Privileges.GetOrAdd(targetClient, ChannelPrivilege.Normal);
            if(targetPrivs.CompareTo(Level) < 0) {
                // Set the user to op because they were previously less privileged
                channel.Privileges.TryUpdate(targetClient, ChannelPrivilege.Op, targetPrivs);
            }

            // Announce the change to the room
            lock(Affects) {
                Affects.Add(targetClient);
            }
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +o {targetClient.Nick}", announce);
            return true;
        }


        override public bool Revoke(Client client, string args) => Revoke(client, args, false, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce) {
            string targetNick = args;
            Client targetClient;

            try {
                targetClient = client.IRCd.GetClientByNick(targetNick);
            } catch(System.InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(client, targetNick);
            }

            // Check if both the source and the target are on the channel
            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            // TODO: this should be ERR_NOSUCHNICK when we have +i to avoid leaking user's presence on a channel?
            if(!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // User doesn't already have the mode
            if(!Has(targetClient))
                return false;

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            // Announce the change to the room
            lock(Affects) {
                Affects.Remove(targetClient);
            }

            channel.Privileges.TryUpdate(client, channel.Status(client), ChannelPrivilege.Op);
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -o {targetClient.Nick}", announce);
            return true;
        }

    }
}