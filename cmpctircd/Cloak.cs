using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography;

namespace cmpctircd
{
    static class Cloak {

        /// <summary>
        /// This function will return a suitable cloak for the user
        /// depending on if they have a DNS host or IP. Will throw
        /// an exception if it can't deduce the user's IP address family.
        /// </summary>
        /// <param name="host">The user's DNS host string</param>
        /// <param name="ip">The user's IPAddress IP</param>
        /// <param name="key">The config cloak key</param>
        /// <returns>A cloak string</returns>
        static public string GetCloak(String host, IPAddress ip, string key, bool full) {
            bool hostIsIp = host == null ? true : false;
            string cloak;

            if(!hostIsIp && !full) {
                // DNS Host
                cloak = CmpctCloakDNS(host, key);
            } else {
                switch(ip.AddressFamily) {
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        // IPv6
                        cloak = InspCloakIPv6(ip, key, full);
                        break;

                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        // IPv4
                        cloak = InspCloakIPv4(ip, key, full);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown IP address type: {ip.AddressFamily}!");
                }
            }

            return cloak;
        }

        /// <summary>
        /// This returns the the last x domain parts of a host string (x set in config)
        /// or a set amount if the domain parts are lower than x.
        /// This is to retrieve a suffix for the cloak and ensure important parts are hidden.
        /// </summary>
        /// <param name="host">DNS host string to truncate</param>
        /// <returns>A truncated DNS host string</returns>
        static public string GetTruncatedHost(string host) {
            string truncHost;
            List<int> dotPositions = new List<int>();
            var dotCount           = host.Count(s => s == '.');
            var dotSplit           = host.Split('.');

            // Count dot positions
            for(int i = 0; i < host.Length; i++) {
                if(host[i] == '.') {
                    dotPositions.Add(i);
                }
            }

            switch(dotCount) {
                case 0:
                    // No dots in the host string, return no suffix
                    truncHost = "";
                    break;

                case 1:
                    //One dot in the host string, only return the last part.
                    truncHost = $".{dotSplit[1]}";
                    break;

                case 2:
                    // Two dots - check that ClockDomainParts is low enough to not reveal
                    // the entire host string, or else return the last two parts.
                    if(IRCd.CloakDomainParts < 2) {
                        truncHost = host.Substring(dotPositions[dotCount - IRCd.CloakDomainParts]);
                    } else {
                        truncHost = host.Substring(dotPositions[dotCount - 2]);
                    }
                    break;

                default:
                    // Return the domain parts given in the config
                    truncHost = host.Substring(dotPositions[dotCount - IRCd.CloakDomainParts]);
                    break;
            }

            return truncHost;
        }

        /// <summary>
        /// Returns a cloak string from a DNS host string
        /// </summary>
        /// <param name="host">DNS host string</param>
        /// <param name="key">Config cloak key</param>
        /// <returns>Cloak string</returns>
        static public string CmpctCloakDNS(string host, string key) {
            var prefix = IRCd.CloakPrefix;
            var suffix = GetTruncatedHost(host);
            var hash   = InspCloakHost((char)1, key, host, 6);

            return $"{prefix}-{hash}{suffix}";
        }

        /// <summary>
        /// This is a copy of InspIRCd's undocumented IPv4 cloaking code.
        /// Due to how InspIRCd take the user's IP address as an integer
        /// casted to an array of chars, this code needs to take the
        /// address bytes and encode them to retrieve a comparable string.
        /// The suffix consists of the first two bytes of the IP address
        /// reversed, followed by ".IP".
        ///
        /// The cloak is formatted as "prefix-seg1.seg2.suffix"
        /// </summary>
        /// <param name="address">The user's IPv4 address (An IPAddress)</param>
        /// <param name="key">The cloaking key, set in the config</param>
        /// <returns>A fully formated cloak string</returns>
        static public string InspCloakIPv4(IPAddress address, string key, bool full) {
            var cloak     = new StringBuilder();
            var utf8      = new UTF8Encoding();
            var addrBytes = address.GetAddressBytes();
            var string_ip = utf8.GetString(addrBytes);
            var suffix    = $".{addrBytes[1]}.{addrBytes[0]}.IP";

            // The cloak string
            // Prefix
            cloak.Append($"{IRCd.CloakPrefix}-");
            // Segment 1 (Full IP hashed)
            cloak.Append(InspCloakHost((char)10, key, string_ip, 3));
            cloak.Append(".");
            // Remove the last byte
            string_ip = string_ip.Substring(0, 3);
            // Segment 2 (First 3 bytes of IP hashed)
            cloak.Append(InspCloakHost((char)11, key, string_ip, 3));

            // If full mode is selected, remove further bytes and cloak entire IP
            if(full) {
                cloak.Append(".");
                string_ip = string_ip.Substring(0, 2);
                cloak.Append(InspCloakHost((char)13, key, string_ip, 6));
                suffix = ".IP";
            }

            // Suffix
            cloak.Append(suffix);

            return cloak.ToString();
        }


        /// <summary>
        /// This is a copy of InspIRCd's undocumented IPv6 cloaking code.
        /// Due to how InspIRCd take the user's IP address as an integer
        /// casted to an array of chars, this code needs to take the
        /// address bytes and encode them to retrieve a comparable string.
        /// The suffix consists of the first two bytes of the IP address
        /// reversed, followed by ".IP".
        ///
        /// The cloak is formatted as "prefix-seg1.seg2.seg3.suffix"
        /// </summary>
        /// <param name="address">The user's IPv4 address (An IPAddress)</param>
        /// <param name="key">The cloaking key, set in the config</param>
        /// <returns>A fully formated cloak string</returns>
        static public string InspCloakIPv6(IPAddress address, string key, bool full) {
            var cloak     = new StringBuilder();
            var utf8      = new UTF8Encoding();
            var addrBytes = address.GetAddressBytes();
            var addrParts = address.ToString().Split(':');
            var string_ip = utf8.GetString(addrBytes);
            var suffix    = $".{addrParts[1]}.{addrParts[0]}.IP";

            // The cloak string
            // Prefix
            cloak.Append($"{IRCd.CloakPrefix}-");
            // Segment 1 (Full IP hashed)
            cloak.Append(InspCloakHost((char)10, key, string_ip, 6));
            cloak.Append(".");
            // Remove the last 6 chars
            string_ip = string_ip.Substring(0, 8);
            // Segment 2 (First 5 chars of IP hashed)
            cloak.Append(InspCloakHost((char)11, key, string_ip, 4));
            // Remove further 2 chars
            string_ip = string_ip.Substring(0, 6);
            cloak.Append(".");
            // Segment 3 (First 3 chars of IP hashed)
            cloak.Append(InspCloakHost((char)12, key, string_ip, 4));

            // If full mode is selected, remove further bytes and cloak entire IP
            if(full) {
                cloak.Append(".");
                string_ip = string_ip.Substring(0, 4);
                cloak.Append(InspCloakHost((char)13, key, string_ip, 6));
                suffix = ".IP";
            }

            // Suffix
            cloak.Append(suffix);

            return cloak.ToString();
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
        static public string InspCloakHost(char id, string key, string host, int length) {
            string base32 = "0123456789abcdefghijklmnopqrstuv";
            string cloak_s = "";

            using(MD5 hasher = MD5.Create()) {
                string input = id + key + "\0" + host;
                StringBuilder str = new StringBuilder();
                var cloak = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

                for(int i = 0; i < length; i++) {
                    // In order to avoid character overflow, AND with 0x1F (31) will ensure each character is only ever <= 31
                    str.Append(base32[cloak[i] & 0x1F]);
                }

                cloak_s = str.ToString();
            }

            return cloak_s;
        }
    }
}