using System.Collections.Generic;

using cmpctircd.Modes;

namespace cmpctircd.Modes {
    public abstract class ChannelMode {

        public Channel channel { get; protected set; }
        abstract public string Name { get; }
        abstract public string Character { get; }
        abstract public string Symbol { get; }
        abstract public string Description { get; }
        abstract public ChannelModeType Type { get; }
        abstract public bool HasParameters { get; }
        abstract public bool ChannelWide { get; }
        abstract public bool Stackable { get; }
        public List<Client> Affects = new List<Client>();

        // Minimum level to use the command
        abstract public ChannelPrivilege MinimumUseLevel {
            get;
        }

        // Level given to those who have the mode set
        abstract public ChannelPrivilege ProvidedLevel {
            get;
        }

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
