using System.Net;

namespace cmpctircd.Cloak
{
    public class CloakOptions
    {
        public CloakOptions(string host, IPAddress ip, string key, bool full)
        {
            Host = host;
            Ip = ip;
            Key = key;
            Full = full;
        }

        public string Host { get; private set; }

        public IPAddress Ip { get; private set; }

        public string Key { get; private set; }

        public bool Full { get; private set; }
    }
}