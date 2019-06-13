using System.Configuration;

namespace cmpctircd.Configuration {
    public class AdvancedElement : ConfigurationElement {
        [ConfigurationProperty("resolveHostnames", IsRequired = true)]
        public bool ResolveHostnames {
            get { return (bool) this["resolveHostnames"]; }
            set { this["resolveHostnames"] = value;  }
        }

        [ConfigurationProperty("requirePongCookie", IsRequired = true)]
        public bool RequirePongCookie {
            get { return (bool) this["requirePongCookie"]; }
            set { this["requirePongCookie"] = value; }
        }

        [ConfigurationProperty("pingTimeout", IsRequired = true)]
        public int PingTimeout {
            get { return (int) this["pingTimeout"]; }
            set { this["pingTimeout"] = value; }
        }

        [ConfigurationProperty("maxTargets", IsRequired = true)]
        public int MaxTargets {
            get { return (int) this["maxTargets"]; }
            set { this["maxTargets"] = value; }
        }

        [ConfigurationProperty("cloak", IsRequired = true)]
        public CloakElement Cloak {
            get {
                return this["cloak"] as CloakElement;
            }
        }
    }
}
