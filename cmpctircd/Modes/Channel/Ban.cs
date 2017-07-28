using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace cmpctircd.Modes {
    public class BanMode : ChannelMode {

        public BanMode(Channel channel) : base(channel) {
            Name = "ban";
            Description = "Provides the +b (ban) mode for banning users from channels";
            Character = "b";
            Symbol = "";
            Type = ChannelModeType.A;
            Level = ChannelPrivilege.Op;
            HasParameters = true;
            ChannelWide = false;
            Stackable = false;
        }
        // Dictionary of bans. mask => Ban
        public Dictionary<string, Ban> Bans = new Dictionary<string, Ban>();

        override public bool Grant(Client client, string args) => Grant(client, args, false, true);
        override public bool Grant(Client client, string args, bool forceSet, bool announce) => Grant(client, args, forceSet, announce, true);
        override public bool Grant(Client client, string args, bool forceSet, bool announce, bool sendSelf) {
            if(String.IsNullOrEmpty(args)) {
                return false;
            }

            if(!channel.Inhabits(client))
                throw new IrcErrNotOnChannelException(client, channel.Name);

            if(!forceSet) {
                ChannelPrivilege sourcePrivs = channel.Privileges.GetOrAdd(client, ChannelPrivilege.Normal);
                if(sourcePrivs.CompareTo(Level) < 0) {
                    // Insufficient setter privileges
                    throw new IrcErrChanOpPrivsNeededException(client, channel.Name);
                }
            }
            // Create the ban
            Dictionary<string, string> mask = Ban.CreateMask(args);
            // Get the unix timestamp - there is a better way to do this,
            // my environment won't allow it - josh
            int date = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            // Add the ban to the dict
            Bans.Add(mask["mask"], new Ban(mask["nick"], mask["user"], mask["host"], client.Nick, date));

            // Announce the change to the room
            channel.SendToRoom(client, $":{client.Mask} MODE {channel.Name} +b {mask["mask"]}", announce);
            return true;

        }

        override public bool Revoke(Client client, string args) => Revoke(client, args, false, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce) => Revoke(client, args, forceSet, announce, true);
        override public bool Revoke(Client client, string args, bool forceSet, bool announce, bool sendSelf) {
            if(String.IsNullOrEmpty(args)) {
                return false;
            }

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