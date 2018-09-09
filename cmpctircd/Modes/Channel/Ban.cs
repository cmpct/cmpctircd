using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace cmpctircd.Modes {
    public class BanMode : ChannelMode {

        override public string Name { get; } = "ban";
        override public string Description { get; } = "Provides the +b (ban) mode for banning users from channels";
        override public string Character { get; } = "b";
        override public string Symbol { get; } = "";
        override public ChannelModeType Type { get; } = ChannelModeType.A;
        override public ChannelPrivilege MinimumUseLevel { get; } = ChannelPrivilege.Op;
        override public ChannelPrivilege ProvidedLevel { get; } = ChannelPrivilege.Normal;
        override public bool HasParameters { get; } = true;
        override public bool ChannelWide { get; } = false;
        override public bool Stackable { get; } = false;
        override public bool AllowAutoSet { get; } = false;

        public BanMode(Channel channel) : base(channel) {}

        // Dictionary of bans. mask => Ban
        public Dictionary<string, Ban> Bans = new Dictionary<string, Ban>();

        override public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if(String.IsNullOrEmpty(args)) {
                return false;
            }

            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(MinimumUseLevel) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }
            // Create the ban
            Dictionary<string, string> mask = Ban.CreateMask(args);
            // Get the unix timestamp
            var date = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Add the ban to the dict
            Bans.Add(mask["mask"], new Ban(mask["nick"], mask["user"], mask["host"], client.Nick, date));

            // Announce the change to the room
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +b {mask["mask"]}", announce);
            return true;

        }

        override public bool Revoke(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if(String.IsNullOrEmpty(args)) {
                return false;
            }

            if(!channel.Inhabits(client))
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

            // Get the ban details
            Dictionary<string, string> mask = Ban.CreateMask(args);
            //Check the ban exists
            if(!Bans.ContainsKey(mask["mask"])) {
                return false;
            }


            // It exists. Kill it.
            Bans.Remove(mask["mask"]);

            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} -b {mask["mask"]}", announce);
            return true;
        }

        override public bool Has(Client client) {
            if(Bans.ContainsKey(client.Mask)) {
                return true;
            }

            foreach(Ban ban in Bans.Values) {
                if(ban.Match(client)) {
                    return true;
                }
            }
            return false;
        }
    }
}