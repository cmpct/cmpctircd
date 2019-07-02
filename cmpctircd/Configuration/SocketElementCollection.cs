using System.Configuration;

namespace cmpctircd.Configuration {
    public class SocketElementCollection : ConfigurationElementCollection {
        public SocketElement this[int i] {
            get {
                return (SocketElement) BaseGet(i);
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new SocketElement();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return ((SocketElement) element).EndPoint;
        }
    }
}
