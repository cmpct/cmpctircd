using System.Configuration;

namespace cmpctircd.Configuration {
    public class ServerElementCollection : ConfigurationElementCollection {
        public ServerElement this[int i] {
            get { return (ServerElement) BaseGet(i); }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new ServerElement();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            ServerElement se = (ServerElement) element;
            return se.Host + ":" + se.Port;
        }
    }
}
