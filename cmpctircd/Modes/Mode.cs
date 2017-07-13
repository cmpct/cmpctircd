using System.Collections.Generic;

namespace cmpctircd {
    public abstract class Mode {

        public Channel channel { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool HasParameters { get; protected set; }
        public List<Client> Affects = new List<Client>();

        // Minimum level to run the command
        public ChannelPrivilege Level {
            get; protected set;
        } = ChannelPrivilege.Normal;


        public Mode(Channel channel) {
            this.channel = channel;
        }


        public bool Grant(Client client, string args) => Grant(client, args, false, true);
        public bool Grant(Client client, string args, bool forceSet, bool announce) { return true; }

        public bool Revoke(Client client, string args) => Revoke(client, args, false, true);
        public bool Revoke(Client client, string args, bool forceSet, bool announce) { return true; }


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

    }
}
