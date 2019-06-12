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
    }
}
