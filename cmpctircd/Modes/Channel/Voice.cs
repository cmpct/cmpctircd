namespace cmpctircd.Modes {
    public class VoiceMode : ChannelMode {

        override public string Name { get; } = "voice";
        override public string Description { get; } = "Provides the +v (voice) mode for voicing a user";
        override public string Character { get; } = "v";
        override public string Symbol { get; } = "+";
        override public ChannelModeType Type { get; } = ChannelModeType.PerUser;
        override public ChannelPrivilege MinimumUseLevel { get; } = ChannelPrivilege.Op;
        override public ChannelPrivilege ProvidedLevel { get; } = ChannelPrivilege.Voice;
        override public bool HasParameters { get; } = true;
        override public bool ChannelWide { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool AllowAutoSet { get; } = false;

        public VoiceMode(Channel channel) : base(channel) {}

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

            // TODO: this should be ERR_NOSUCHNICK when we have +i to avoid leaking user's presence on a channel?
            if (!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // Set the subject's privilege to the new status
            // But first check if they already have such a privilege...
            ChannelPrivilege targetPrivs = channel.Privileges.GetOrAdd(targetClient, ChannelPrivilege.Normal);
            if(targetPrivs.CompareTo(ProvidedLevel) < 0) {
                channel.Privileges.TryUpdate(targetClient, ProvidedLevel, targetPrivs);
            }

            // Announce the change to the room
            lock(Affects) {
                Affects.Add(targetClient);
            }

            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +v {targetClient.Nick}", sendSelf);
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

            // TODO: this should be ERR_NOSUCHNICK when we have +i to avoid leaking user's presence on a channel?
            if (!channel.Inhabits(targetClient))
                throw new IrcErrNotOnChannelException(targetClient, channel.Name);

            // Announce the change to the room
            lock (Affects) {
                Affects.Remove(targetClient);
            }

            channel.Privileges.TryUpdate(targetClient, channel.Status(targetClient), ProvidedLevel);
            if (announce) {
                channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -v {targetClient.Nick}", sendSelf);
            }
            return true;
        }

    }
}