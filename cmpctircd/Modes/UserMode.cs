using System.Collections.Generic;
using cmpctircd.Modes;

namespace cmpctircd.Modes {
    public abstract class UserMode {

        abstract public string Name { get; }
        abstract public string Character { get; }
        abstract public string Description { get; }
        abstract public bool HasParameters { get; }
        abstract public bool Stackable { get; }
        abstract public bool AllowAutoSet { get; }
        abstract public bool Enabled { get; set; }
        public Client Subject;

        public UserMode(Client subject) {
            this.Subject = subject;
        }


        abstract public bool Grant(string args, bool forceSet = false, bool announce = false, bool sendSelf = true);
        abstract public bool Revoke(string args, bool forceSet = false, bool announce = false, bool sendSelf = true);


        virtual public bool Has(Client client) {
            return Enabled;
        }
        virtual public string GetValue() {
            return "";
        }

    }
}
