using System.Configuration;
using System.IO;

namespace cmpctircd.Configuration {
    public class TlsElement : ConfigurationElement {
        [ConfigurationProperty("file", IsRequired = true)]
        public FileInfo File {
            get { return new FileInfo((string) this["file"]); }
            set { this["file"] = value.FullName; }
        }

        [ConfigurationProperty("password", IsRequired = false, DefaultValue = "")]
        public string Password {
            get { return (string) this["password"]; }
            set { this["password"] = value; }
        }
    }
}
