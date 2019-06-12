using System.Configuration;
using System.Xml;

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
            get { return int.Parse((string) this["domainParts"]); }
            set { this["domainParts"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("full", IsRequired = true)]
        public bool Full {
            get { return bool.Parse((string) this["full"]); }
            set { this["full"] = XmlConvert.ToString(value); }
        }
    }
}
