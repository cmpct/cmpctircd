namespace cmpctircd.Cloak
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;
    using cmpctircd.Cloak.Service;

    /// <summary>
    /// Generates cloak strings using factories.
    /// </summary>
    public static class Cloaking
    {
        /// <summary>
        /// Generate a cloak compatible with InspIRCd.
        /// </summary>
        /// <param name="cloakOptions">The client's cloaking parameters.</param>
        /// <returns>A cloak string.</returns>
        public static string GenerateInspCloak(CloakOptions cloakOptions)
        {
            Cloak cloak = GetInspCloakFromService(cloakOptions);
            return cloak.GetCloakString(cloakOptions);
        }


        /// <summary>
        ///  This code is a copy of InspIRCd's undocumented host cloaking algorithm.
        ///  The hash is given an input which consists of:
        ///  a (char) ID, a key, a null character, and the full host.
        ///
        ///  After hashing, it is cut down to the given length and each byte is cut down to fit base32 (0..31)
        ///  in order to cut off any overflow & invalid characters
        /// </summary>
        /// <param name="id">A unique ID for the host</param>
        /// <param name="key">A key set in the configuration</param>
        /// <param name="host">The host itself</param>
        /// <param name="length">The length for the output, max = 16</param>
        /// <returns>Returns a hashed string</returns>
        public static string InspCloakHost(char id, string key, string host, int length) {
            string base32 = "0123456789abcdefghijklmnopqrstuv";
            string cloakString = "";

            using (MD5 hasher = MD5.Create()) {
                string input = id + key + "\0" + host;
                StringBuilder str = new StringBuilder();
                var cloak = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

                for (int i = 0; i < length; i++) {
                    // In order to avoid character overflow, AND with 0x1F (31) will ensure each character is only ever <= 31
                    str.Append(base32[cloak[i] & 0x1F]);
                }

                cloakString = str.ToString();
            }

            return cloakString;
        }

        /// <summary>
        /// Generate a `Cloak` based on the client's IP version.
        /// </summary>
        /// <param name="cloakOptions">The client's cloaking parameters.</param>
        /// <returns>A Cloak object.</returns>
        private static Cloak GetInspCloakFromService(CloakOptions cloakOptions)
        {
            CloakService service = null;
            string cloakVersion = GetCloakVersion(cloakOptions);

            switch (cloakVersion)
            {
                case "DNS":
                    service = new InspDnsService();
                    break;
                case "IPv4":
                    service = new InspIPv4Service();
                    break;
                case "IPv6":
                    service = new InspIPv6Service();
                    break;
            }

            return service.GetCloak();
        }

        /// <summary>
        /// Get the client's cloak version - either DNS, IPv4 or IPv6.
        /// </summary>
        /// <param name="cloakOptions">The client's cloaking parameters.</param>
        /// <returns>A cloak version string</returns>
        private static string GetCloakVersion(CloakOptions cloakOptions)
        {
            bool hostIsIp = cloakOptions.Host == null;
            string cloakVersion;

            if (!hostIsIp && !cloakOptions.Full) {
                // DNS Host
                cloakVersion = "DNS";
            } else {
                cloakVersion = GetIpVersion(cloakOptions.Ip, cloakOptions.Key, cloakOptions.Full);
            }

            return cloakVersion;
        }

        /// <summary>
        /// Get the client's IP version - IPv4 or IPv6.
        /// </summary>
        /// <param name="ip">Client's IP Address.</param>
        /// <param name="key">The server cloaking key.</param>
        /// <param name="full">Full cloaking config option.</param>
        /// <returns>A IP version string</returns>
        private static string GetIpVersion(IPAddress ip, string key, bool full) {
            string cloak = ip.AddressFamily switch
            {
                AddressFamily.InterNetworkV6 => "IPv6",
                AddressFamily.InterNetwork => "IPv4",
                _ => throw new NotSupportedException($"Unknown IP address type: {ip.AddressFamily}!"),
            };

            return cloak;
        }
    }
}
