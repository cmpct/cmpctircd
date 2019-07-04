using System.Configuration;

namespace cmpctircd.Configuration {
    public class TlsElement : ConfigurationElement {
        [ConfigurationProperty("file", IsRequired = true)]
        public string File {
            get { return (string) this["file"]; }
            set { this["file"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = false, DefaultValue = "")]
        public string Password {
            get { return (string) this["password"]; }
            set { this["password"] = value; }
        }
    }
}
