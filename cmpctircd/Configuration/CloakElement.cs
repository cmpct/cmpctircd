using System.Configuration;

namespace cmpctircd.Configuration {
    public class CloakElement : ConfigurationElement {
        [ConfigurationProperty("key", IsRequired = true)]
        public string Key {
            get { return (string) this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("prefix", IsRequired = true)]
        public string Prefix {
            get { return (string)this["prefix"]; }
            set { this["prefix"] = value; }
        }

        [ConfigurationProperty("domainParts", IsRequired = true)]
        public int DomainParts {
            get { return (int) this["domainParts"]; }
            set { this["domainParts"] = value; }
        }

        [ConfigurationProperty("full", IsRequired = true)]
        public bool Full {
            get { return (bool) this["full"]; }
            set { this["full"] = value; }
        }
    }
}
