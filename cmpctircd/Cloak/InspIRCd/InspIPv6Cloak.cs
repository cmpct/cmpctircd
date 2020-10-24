using System.Net;
using System.Text;

namespace cmpctircd.Cloak
{
    public class InspIPv6Cloak : Cloak
    {

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
        public override string GetCloakString(CloakOptions cloakOptions)
        {
            var cloak = new StringBuilder();
            var addrParts = cloakOptions.Ip.ToString().Split(':');
            var stringIp = EncodeIpAddressBytes(cloakOptions.Ip);
            var suffix = $".{addrParts[1]}.{addrParts[0]}.IP";


            cloak.Append($"{IRCd.CloakPrefix}-");

            CloakFirstSegment(cloakOptions, cloak, stringIp);

            CloakSecondSegment(cloakOptions, cloak, stringIp);

            cloak.Append(".");
            CloakThirdSegment(cloakOptions, cloak, stringIp);

            // If full mode is selected, cloak entire IP
            if (cloakOptions.Full)
            {
                CloakFullIp(cloakOptions, cloak, stringIp);
                suffix = ".IP";
            }

            cloak.Append(suffix);

            return cloak.ToString();
        }

        private static string EncodeIpAddressBytes(IPAddress ip)
        {
            var utf8 = new UTF8Encoding();
            var addressBytes = ip.GetAddressBytes();

            return utf8.GetString(addressBytes);
        }

        private static void CloakFullIp(CloakOptions cloakOptions, StringBuilder cloak, string string_ip)
        {
            cloak.Append(".");
            string_ip = string_ip.Substring(0, 4);
            cloak.Append(Cloaking.InspCloakHost((char) 13, cloakOptions.Key, string_ip, 6));
        }

        private static void CloakThirdSegment(CloakOptions cloakOptions, StringBuilder cloak, string string_ip)
        {
            // Segment 3 (First 3 chars of IP hashed)
            string_ip = string_ip.Substring(0, 6);
            cloak.Append(Cloaking.InspCloakHost((char) 12, cloakOptions.Key, string_ip, 4));
        }

        private static void CloakSecondSegment(CloakOptions cloakOptions, StringBuilder cloak, string string_ip)
        {
            // Segment 2 (First 5 chars of IP hashed)
            string_ip = string_ip.Substring(0, 8);
            cloak.Append(Cloaking.InspCloakHost((char) 11, cloakOptions.Key, string_ip, 4));
        }

        private static void CloakFirstSegment(CloakOptions cloakOptions, StringBuilder cloak, string string_ip)
        {
            // Segment 1 (Full IP hashed)
            cloak.Append(Cloaking.InspCloakHost((char) 10, cloakOptions.Key, string_ip, 6));
            cloak.Append(".");
        }
    }
}
