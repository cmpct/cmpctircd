using System.Collections.Generic;
using cmpctircd.Modes;

namespace cmpctircd.Modes {
    public abstract class UserMode {

        public string Name { get; protected set; }
        public string Character { get; protected set; }
        public string Description { get; protected set; }
        public bool HasParameters { get; protected set; }
        public bool Stackable = true;
        public bool Enabled = false;
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
