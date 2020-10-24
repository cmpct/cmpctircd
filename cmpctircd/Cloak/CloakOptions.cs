using System.Net;

namespace cmpctircd.Cloak
{
    /// <summary>
    /// A class containing cloaking parameters from the client.
    /// Use this instead of excess method arguments.
    /// </summary>
    public class CloakOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloakOptions"/> class.
        /// </summary>
        /// <param name="host">The client's DNS host.</param>
        /// <param name="ip">The client's IP Address.</param>
        /// <param name="key">The server's cloak key.</param>
        /// <param name="full">Full cloaking config option.</param>
        public CloakOptions(string host, IPAddress ip, string key, bool full)
        {
            Host = host;
            Ip = ip;
            Key = key;
            Full = full;
        }

        /// <summary>
        /// Gets client's DNS host.
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Gets client's IP Address.
        /// </summary>
        public IPAddress Ip { get; private set; }

        /// <summary>
        /// Gets server's cloak key.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets a value indicating whether full cloaking should occur.
        /// </summary>
        public bool Full { get; private set; }
    }
}