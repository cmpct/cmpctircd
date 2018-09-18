using System;
using System.Net;

namespace cmpctircd.Modes {
    public class BotMode : UserMode {

        override public string Name { get; } = "bot";
        override public string Description { get; } = "Provides the +B (bot) mode for bot owners to mark bots";
        override public string Character { get; } = "B";
        override public bool HasParameters { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool Enabled { get; set; } = false;
        override public bool AllowAutoSet { get; } = false;

        public BotMode(Client subject) : base(subject) {}


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled)
                return false;

            Enabled = true;
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} +B");
            }

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} +{Character}");

            return true;
        }

        override public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (!Enabled)
                return false;

            Enabled = false;
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} -B");
            }

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} -{Character}");

            return true;
        }

    }
}