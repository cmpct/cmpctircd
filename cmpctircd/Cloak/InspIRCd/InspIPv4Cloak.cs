namespace cmpctircd.Cloak
{
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
            var string_ip = utf8.GetString(addrBytes);
            var suffix = $".{addrBytes[1]}.{addrBytes[0]}.IP";

            // The cloak string
            // Prefix
            cloak.Append($"{IRCd.CloakPrefix}-");

            // Segment 1 (Full IP hashed)
            cloak.Append(Cloaking.InspCloakHost((char)10, cloakOptions.Key, string_ip, 3));
            cloak.Append(".");

            // Remove the last byte
            string_ip = string_ip.Substring(0, 3);

            // Segment 2 (First 3 bytes of IP hashed)
            cloak.Append(Cloaking.InspCloakHost((char)11, cloakOptions.Key, string_ip, 3));

            // If full mode is selected, remove further bytes and cloak entire IP
            if (cloakOptions.Full) {
                cloak.Append(".");
                string_ip = string_ip.Substring(0, 2);
                cloak.Append(Cloaking.InspCloakHost((char)13, cloakOptions.Key, string_ip, 6));
                suffix = ".IP";
            }

            // Suffix
            cloak.Append(suffix);

            return cloak.ToString();
        }
    }
}
