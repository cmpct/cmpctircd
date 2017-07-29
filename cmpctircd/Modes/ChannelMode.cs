using System.Collections.Generic;

using cmpctircd.Modes;

namespace cmpctircd.Modes {
    public abstract class ChannelMode {

        public Channel channel { get; protected set; }
        public string Name { get; protected set; }
        public string Character { get; protected set; }
        public string Symbol { get; protected set; }
        public string Description { get; protected set; }
        public ChannelModeType Type { get; protected set; }
        public bool HasParameters { get; protected set; }
        public bool ChannelWide { get; protected set; }
        public bool Stackable = true;
        public List<Client> Affects = new List<Client>();

        // Minimum level to use the command
        public ChannelPrivilege MinimumUseLevel {
            get; protected set;
        } = ChannelPrivilege.Normal;

        // Level given to those who have the mode set
        public ChannelPrivilege ProvidedLevel {
            get; protected set;
        } = ChannelPrivilege.Normal;

        public ChannelMode(Channel channel) {
            this.channel = channel;
        }


        abstract public bool Grant(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true);
        abstract public bool Revoke(Client client, string args, bool forceSet = false, bool announce = false, bool sendSelf = true);


        virtual public bool Has(Client client) {
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
