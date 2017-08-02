using System;
using System.Net;

namespace cmpctircd.Modes {
    public class TLSMode : UserMode {

        // XXX: DO NOT USE THIS CONSTRUCTOR
        public TLSMode(IRCd ircd) : base(ircd) {}

        public TLSMode(Client subject) : base(subject) {
            Name = "TLS";
            Description = "Provides the +z (TLS) mode for users connecting via TLS";
            Character = "z";
            HasParameters = false;
        }


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled && !forceSet)
                return false;
            if (Subject.ClientTlsStream != null) {
                Enabled = true;
                if (announce) {
                    Subject.Write($":{Subject.Nick} MODE {Subject.Nick} :+z");
                }
                return true;
            }
            return false;
        }

        override public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (!Enabled && !forceSet)
                return false;

            Enabled = false;
            if (announce) {
                Subject.Write($":{Subject.Nick} MODE {Subject.Nick} :-z");
            }
            return true;
        }
    }
}