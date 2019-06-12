using System.Configuration;

namespace cmpctircd.Configuration {
    public class CmpctConfigurationSection : ConfigurationSection {
        public static CmpctConfigurationSection GetConfiguration() {
            return (CmpctConfigurationSection) ConfigurationManager.GetSection("cmpctircd") ?? new CmpctConfigurationSection();
        }

        [ConfigurationProperty("sid", DefaultValue = "auto", IsRequired = false)]
        public string SID {
            get { return (string) this["sid"]; }
            set { this["sid"] = value; }
        }

        [ConfigurationProperty("host", IsRequired = true)]
        public string Host {
            get { return (string) this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("network", IsRequired = true)]
        public string Network {
            get { return (string) this["network"]; }
            set { this["network"] = value; }
        }

        [ConfigurationProperty("description", DefaultValue = "", IsRequired = false)]
        public string Description {
            get { return (string) this["description"]; }
            set { this["description"] = value; }
        }

        [ConfigurationProperty("sockets", IsRequired = true)]
        [ConfigurationCollection(typeof(SocketElement), AddItemName = "socket")]
        public SocketElementCollection Sockets {
            get {
                return this["sockets"] as SocketElementCollection;
            }
        }

        [ConfigurationProperty("advanced", IsRequired = true)]
        public AdvancedElement Advanced {
            get {
                return this["advanced"] as AdvancedElement;
            }
        }

        [ConfigurationProperty("cmodes")]
        [ConfigurationCollection(typeof(ModeElement), AddItemName = "mode")]
        public ModeElementCollection ChannelModes {
            get {
                return this["cmodes"] as ModeElementCollection;
            }
        }

        [ConfigurationProperty("umodes")]
        [ConfigurationCollection(typeof(ModeElement), AddItemName = "mode")]
        public ModeElementCollection UserModes {
            get {
                return this["umodes"] as ModeElementCollection;
            }
        }


    }
}
