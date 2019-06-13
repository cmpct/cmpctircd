using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace cmpctircd.Configuration {
    public class OperatorElementCollection : ConfigurationElementCollection {
        public OperatorElement this[int i] {
            get {
                return (OperatorElement) BaseGet(i);
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new OperatorElement();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return ((OperatorElement) element).Name;
        }

        [ConfigurationProperty("channels", IsRequired = false, DefaultValue = "")]
        public List<string> Channels {
            get { return ((string) this["channels"]).Split(' ').Where(s => !string.IsNullOrEmpty(s)).ToList(); }
            set { this["channels"] = string.Join(" ", value); }
        }
    }
}
