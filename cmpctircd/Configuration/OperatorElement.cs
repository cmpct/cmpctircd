using System;
using System.Configuration;
using System.Xml;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace cmpctircd.Configuration {
    public class OperatorElement : ConfigurationElement {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public byte[] Password {
            get { return SoapHexBinary.Parse((string) this["password"]).Value; }
            set { this["password"] = new SoapHexBinary(value).ToString(); }
        }

        [ConfigurationProperty("provider", IsRequired = true)]
        public Type Provider {
            get { return Type.GetType((string)this["provider"], true, false); }
            set { this["provider"] = value.FullName; }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = "false")]
        public bool Tls {
            get { return bool.Parse((string)this["tls"]); }
            set { this["tls"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("host", IsRequired = true)]
        public string Host {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }
    }
}
