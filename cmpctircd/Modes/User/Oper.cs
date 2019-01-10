namespace cmpctircd.Modes {
    public class OperMode : UserMode {
        override public string Name { get; }  = "oper";
        override public string Description { get; } = "Provides the +o (Oper) mode grant irc operator status";
        override public string Character { get; }  = "o";
        override public bool HasParameters { get; } = false;
        override public bool Stackable { get; } = true;
        override public bool Enabled { get; set; } = false;
        override public bool AllowAutoSet { get; } = false;

        public OperMode(Client subject) : base(subject) {}


        override public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (Enabled && !forceSet)
                return false;

            Enabled = true;
            if (announce) {
                Subject.Write($":{Subject.Nick} MODE {Subject.Nick} :+o");
            }

            // Let the servers know too
            // NOTE: InspIRCd only
            // NOTE: We don't need to send MODE +o to servers here, just unset it if deoper
            var type_name = "IRCop"; // TODO: Aesthetics only
            Subject.IRCd.WriteToAllServers($":{Subject.UUID} OPERTYPE {type_name}");
            return true;
        }

        override public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true) {
            if (!Enabled && !forceSet)
                return false;

            Enabled = false;
            if (announce) {
                Subject.Write($":{Subject.Nick} MODE {Subject.Nick} :-o");
            }

            Subject.IRCd.WriteToAllServers($":{Subject.UUID} MODE {Subject.UUID} -{Character}");
            return true;
        }
    }
}