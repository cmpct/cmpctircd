using System.Configuration;

namespace cmpctircd.Configuration {
    public class LoggerElementCollection : ConfigurationElementCollection {
        public LoggerElement this[int i] {
            get {
                return (LoggerElement)BaseGet(i);
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new LoggerElement();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return element.GetHashCode();
        }
    }
}
