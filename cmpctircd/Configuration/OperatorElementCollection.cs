using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

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

        [TypeConverter(typeof(ListConverter))]
        [ConfigurationProperty("channels", IsRequired = false)]
        public IList<string> Channels {
            get { return (List<string>) this["channels"]; }
            set { this["channels"] = value; }
        }
    }
}
