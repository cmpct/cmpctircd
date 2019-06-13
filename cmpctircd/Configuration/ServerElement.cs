using System.Collections.Generic;
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

        [ConfigurationProperty("masks", IsRequired = true)]
        public List<string> Masks {
            get { return ((string)this["masks"]).Split(' ').ToList(); }
            set { this["masks"] = string.Join(" ", value); }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
        public int Port {
            get { return int.Parse((string)this["port"]); }
            set { this["port"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = "false")]
        public bool IsTls {
            get { return bool.Parse((string)this["tls"]); }
            set { this["tls"] = XmlConvert.ToString(value); }
        }
    }
}
