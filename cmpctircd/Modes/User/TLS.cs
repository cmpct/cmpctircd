using System;
using System.Net;

namespace cmpctircd.Modes {
    public class TLSMode : UserMode {

        override public string Name { get; } = "TLS";
        override public string Description { get; } = "Provides the +z (TLS) mode for users connecting via TLS";
        override public string Character { get; }  = "z";
        override public bool HasParameters { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool Enabled { get; set; } = false;
        override public bool AllowAutoSet { get; } = false;

        public TLSMode(Client subject) : base(subject) {}

        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled && !forceSet)
                return false;
            if (Subject.TlsStream != null) {
                Enabled = true;
                if (announce) {
                    Subject.Write($":{Subject.Nick} MODE {Subject.Nick} :+z");
                }

                // TODO: Careful, is this Insp-compatible?
                Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} +{Character}");

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

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} -{Character}");
            return true;
        }
    }
}