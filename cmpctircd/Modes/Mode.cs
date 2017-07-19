using System.Collections.Generic;

using cmpctircd.Modes;

namespace cmpctircd.Modes {
    public abstract class Mode {

        public Channel channel { get; protected set; }
        public string Name { get; protected set; }
        public string Character { get; protected set; }
        public string Symbol { get; protected set; }
        public string Description { get; protected set; }
        public ModeType Type { get; protected set; }
        public bool HasParameters { get; protected set; }
        public bool ChannelWide { get; protected set; }
        public List<Client> Affects = new List<Client>();

        // Minimum level to run the command
        public ChannelPrivilege Level {
            get; protected set;
        } = ChannelPrivilege.Normal;


        public Mode(Channel channel) {
            this.channel = channel;
        }


        abstract public bool Grant(Client client, string args);
        abstract public bool Grant(Client client, string args, bool forceSet, bool announce);
        abstract public bool Grant(Client client, string args, bool forceSet, bool announce, bool sendSelf);

        abstract public bool Revoke(Client client, string args);
        abstract public bool Revoke(Client client, string args, bool forceSet, bool announce);
        abstract public bool Revoke(Client client, string args, bool forceSet, bool announce, bool sendSelf);


        public bool Has(Client client) {
            lock(Affects) {
                return Affects.Contains(client);
            }
        }

        public List<Client> Get() {
            lock(Affects) {
                return Affects;
            }
        }

        virtual public string GetValue() {
            return "";
        }

    }
}
