namespace cmpctircd.Cloak
{
    using System.Text;

    public class InspIPv4Cloak : Cloak
    {

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
        /// <returns>A fully formatted cloak string</returns>
        public override string GetCloakString(CloakOptions cloakOptions)
        {
            var cloak = new StringBuilder();
            var utf8 = new UTF8Encoding();
            var addrBytes = cloakOptions.Ip.GetAddressBytes();
            var stringIp = utf8.GetString(addrBytes);
            var suffix = $".{addrBytes[1]}.{addrBytes[0]}.IP";

            // Prefix
            cloak.Append($"{IRCd.CloakPrefix}-");

            CloakFirstSegment(cloakOptions, cloak, stringIp);
            cloak.Append(".");

            CloakSecondSegment(cloakOptions, stringIp, cloak);

            // If full mode is selected, cloak entire IP
            if (cloakOptions.Full)
            {
                CloakFullIp(cloakOptions, cloak, stringIp);
                suffix = ".IP";
            }

            cloak.Append(suffix);

            return cloak.ToString();
        }

        private static void CloakFullIp(CloakOptions cloakOptions, StringBuilder cloak, string stringIp)
        {
            cloak.Append(".");
            stringIp = stringIp.Substring(0, 2);
            cloak.Append(Cloaking.InspCloakHost((char) 13, cloakOptions.Key, stringIp, 6));
        }

        private static void CloakSecondSegment(CloakOptions cloakOptions, string stringIp, StringBuilder cloak)
        {
            // Segment 2 (First 3 bytes of IP hashed)
            stringIp = stringIp.Substring(0, 3);
            cloak.Append(Cloaking.InspCloakHost((char) 11, cloakOptions.Key, stringIp, 3));
        }

        private static void CloakFirstSegment(CloakOptions cloakOptions, StringBuilder cloak, string stringIp)
        {
            // Segment 1 (Full IP hashed)
            cloak.Append(Cloaking.InspCloakHost((char) 10, cloakOptions.Key, stringIp, 3));
        }
    }
}
