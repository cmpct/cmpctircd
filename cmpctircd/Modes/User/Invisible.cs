using System;
using System.Net;

namespace cmpctircd.Modes {
    public class InvisibleMode : UserMode {

        override public string Name { get; } = "invisible";
        override public string Description { get; } = "Provides the +i (invisible) mode for users to be hidden from general queries";
        override public string Character { get; } = "i";
        override public bool HasParameters { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool Enabled { get; set; } = false;
        override public bool AllowAutoSet { get; } = true;

        public InvisibleMode(Client subject) : base(subject) {}


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled)
                return false;

            Enabled = true;
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} +{Character}");
            }
            return true;
        }

        override public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (!Enabled)
                return false;

            Enabled = false;
            if(announce) {
                Subject.Write($":{Subject.Mask} MODE {Subject.Nick} -{Character}");
            }
            return true;
        }

    }
}