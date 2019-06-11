using System.Configuration;
using System.Net;

namespace cmpctircd.Configuration {
    public class SocketElement : ConfigurationElement {
        [ConfigurationProperty("listener", IsRequired = false, DefaultValue = "true")]
        public bool IsListener {
            get { return bool.Parse((string) this["listener"]); }
            set { this["listener"] = value ? "true" : "false"; }
        }

        [ConfigurationProperty("host", IsRequired = true)]
        public IPAddress Host {
            get { return IPAddress.Parse((string) this["host"]); }
            set { this["host"] = value.ToString(); }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
        public int Port {
            get { return int.Parse((string) this["port"]); }
            set { this["port"] = value.ToString(); }
        }

        public IPEndPoint EndPoint {
            get { return new IPEndPoint(Host, Port); }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = "false")]
        public bool IsTls {
            get { return bool.Parse((string) this["tls"]); }
            set { this["tls"] = value ? "true" : "false"; }
        }
    }
}
