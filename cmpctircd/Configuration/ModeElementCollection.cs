using System.Configuration;

namespace cmpctircd.Configuration {
    public class ModeElementCollection : ConfigurationElementCollection {
        public ModeElement this[int i] {
            get {
                return (ModeElement) BaseGet(i);
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new ModeElement();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return ((ModeElement) element).Name;
        }
    }
}
