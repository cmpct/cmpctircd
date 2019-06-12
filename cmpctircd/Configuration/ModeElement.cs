using System.Configuration;

namespace cmpctircd.Configuration {
    public class ModeElement : ConfigurationElement {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name {
            get { return (string) this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("param", IsRequired = false, DefaultValue = "")]
        public string Param {
            get { return (string) this["param"]; }
            set { this["param"] = value; }
        }
    }
}
