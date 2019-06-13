using System.Configuration;

namespace cmpctircd.Configuration {
    public class CmpctConfigurationSection : ConfigurationSection {
        public static CmpctConfigurationSection GetConfiguration() {
            return (CmpctConfigurationSection) ConfigurationManager.GetSection("ircd") ?? new CmpctConfigurationSection();
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

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = null)]
        public TlsElement Tls {
            get {
                return this["tls"] as TlsElement;
            }
        }

        [ConfigurationProperty("log", IsRequired = true)]
        [ConfigurationCollection(typeof(LoggerElement), AddItemName = "logger")]
        public LoggerElementCollection Loggers {
            get {
                return this["log"] as LoggerElementCollection;
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
        public ModeElementCollection AutomaticModes {
            get {
                return this["cmodes"] as ModeElementCollection;
            }
        }

        [ConfigurationProperty("umodes")]
        [ConfigurationCollection(typeof(ModeElement), AddItemName = "mode")]
        public ModeElementCollection AutomaticUserModes {
            get {
                return this["umodes"] as ModeElementCollection;
            }
        }

        [ConfigurationProperty("servers")]
        [ConfigurationCollection(typeof(ModeElement), AddItemName = "server")]
        public ServerElementCollection Servers {
            get {
                return this["servers"] as ServerElementCollection;
            }
        }

        [ConfigurationProperty("opers")]
        [ConfigurationCollection(typeof(ModeElement), AddItemName = "oper")]
        public OperatorElementCollection Operators {
            get {
                return this["opers"] as OperatorElementCollection;
            }
        }
    }
}
