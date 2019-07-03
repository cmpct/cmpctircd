using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace cmpctircd.Configuration {
    public class ServerElement : ConfigurationElement {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host {
            get { return (string) this["host"]; }
            set { this["host"] = value; }
        }

        [TypeConverter(typeof(ListConverter))]
        [ConfigurationProperty("masks", IsRequired = true)]
        public List<string> Masks {
            get { return ((List<string>) this["masks"]); }
            set { this["masks"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
        public int Port {
            get { return (int) this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password {
            get { return (string) this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = false)]
        public bool IsTls {
            get { return (bool) this["tls"]; }
            set { this["tls"] = value; }
        }
    }
}
