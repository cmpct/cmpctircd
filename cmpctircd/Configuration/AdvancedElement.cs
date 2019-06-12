using System.Configuration;
using System.Xml;

namespace cmpctircd.Configuration {
    public class AdvancedElement : ConfigurationElement {
        [ConfigurationProperty("resolveHostnames", IsRequired = true)]
        public bool ResolveHostnames {
            get { return bool.Parse((string) this["resolveHostnames"]); }
            set { this["resolveHostnames"] = XmlConvert.ToString(value);  }
        }

        [ConfigurationProperty("requirePongCookie", IsRequired = true)]
        public bool RequirePongCookie {
            get { return bool.Parse((string) this["requirePongCookie"]); }
            set { this["requirePongCookie"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("pingTimeout", IsRequired = true)]
        public int PingTimeout {
            get { return int.Parse((string) this["pingTimeout"]); }
            set { this["pingTimeout"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("maxTargets", IsRequired = true)]
        public int MaxTargets {
            get { return int.Parse((string) this["maxTargets"]); }
            set { this["maxTargets"] = XmlConvert.ToString(value); }
        }

        [ConfigurationProperty("cloak", IsRequired = true)]
        public CloakElement Cloak {
            get {
                return this["cloak"] as CloakElement;
            }
        }
    }
}
