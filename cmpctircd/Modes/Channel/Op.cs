namespace cmpctircd.Modes {
    public class OpMode : ChannelMode {

        override public string Name { get; } = "op";
        override public string Description { get; } = "Provides the +o (op) mode for moderating a channel";
        override public string Character { get; } = "o";
        override public string Symbol { get; } = "@";
        override public ChannelModeType Type { get; } = ChannelModeType.PerUser;
        override public ChannelPrivilege MinimumUseLevel { get; } = ChannelPrivilege.Op;
        override public ChannelPrivilege ProvidedLevel { get; } = ChannelPrivilege.Op;
        override public bool HasParameters { get; } = true;
        override public bool ChannelWide { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool AllowAutoSet { get; } = false;

        public OpMode(Channel channel) : base(channel) {}

        override public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            string targetNick = args;
            Client targetClient;

            try {
                targetClient = client.IRCd.GetClientByNick(targetNick);
            } catch(System.InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(client, targetNick);
            }

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                // Check if both the source and the target are on the channel
                if (!channel.Inhabits(client))
                    throw new IrcErrNotOnChannelException(client, channel.Name);

                // User already has the mode
                if (Has(targetClient))
                    return false;

                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // Set the subject's privilege to the new status
            // But first check if they already have such a privilege...
            ChannelPrivilege targetPrivs = channel.Privileges.GetOrAdd(targetClient, ChannelPrivilege.Normal);
            if(targetPrivs.CompareTo(ProvidedLevel) < 0) {
                // Set the user to op because they were previously less privileged
                channel.Privileges[targetClient] = ChannelPrivilege.Op;
            }

            // Announce the change to the room
            Affects.Add(targetClient);

            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +o {targetClient.Nick}", sendSelf);
            }
            return true;
        }


        override public bool Revoke(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            string targetNick = args;
            Client targetClient;

            try {
                targetClient = client.IRCd.GetClientByNick(targetNick);
            } catch(System.InvalidOperationException) {
                throw new IrcErrNoSuchTargetNickException(client, targetNick);
            }

            // Check user has right to set the mode
            // Get the setters's privilege if not forcing the mode change
            if(!forceSet) {
                // Check if both the source and the target are on the channel
                if (!channel.Inhabits(client))
                    throw new IrcErrNotOnChannelException(client, channel.Name);

                // User doesn't already have the mode
                if (!Has(targetClient))
                    return false;

                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }

            if (!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // Announce the change to the room
            Affects.Remove(targetClient);

            channel.Privileges[targetClient] = channel.Status(targetClient);
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -o {targetClient.Nick}", sendSelf);
            }
            return true;
        }

    }
}