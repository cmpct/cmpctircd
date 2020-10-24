namespace cmpctircd.Cloak
{
    using System.Collections.Generic;
    using System.Linq;

    public class InspDNSCloak : Cloak
    {
        /// <summary>
        /// This returns the the last x domain parts of a host string (x set in config)
        /// or a set amount if the domain parts are lower than x.
        /// This is to retrieve a suffix for the cloak and ensure important parts are hidden.
        /// </summary>
        /// <param name="host">DNS host string to truncate</param>
        /// <returns>A truncated DNS host string</returns>
        public static string GetTruncatedHost(string host) {
            string truncHost;
            var dotCount = CountDotsInHost(host);
            var hostSplit = host.Split('.');
            List<int> dotPositions = CountDotPositionsInHost(host);

            switch (dotCount) {
                case 0:
                    // No dots in the host string, return no suffix
                    truncHost = "";
                    break;

                case 1:
                    // One dot in the host string, only return the last part.
                    truncHost = $".{hostSplit[1]}";
                    break;

                case 2:
                    // Two dots - check that ClockDomainParts is low enough to not reveal
                    // the entire host string, or else return the last two parts.
                    truncHost = IRCd.CloakDomainParts < 2
                        ? host.Substring(dotPositions[dotCount - IRCd.CloakDomainParts])
                        : host.Substring(dotPositions[dotCount - 2]);

                    break;

                default:
                    // Return the domain parts given in the config
                    truncHost = host.Substring(dotPositions[dotCount - IRCd.CloakDomainParts]);
                    break;
            }

            return truncHost;
        }

        private static List<int> CountDotPositionsInHost(string host)
        {
            List<int> dotPositions = new List<int>();
            int i;
            // Count dot positions
            for (i = 0; i < host.Length; i++)
            {
                if (host[i] == '.')
                {
                    dotPositions.Add(i);
                }
            }

            return dotPositions;
        }

        private static int CountDotsInHost(string host)
        {
            var dotCount = host.Count(s => s == '.');
            return dotCount;
        }

        public override string GetCloakString(CloakOptions cloakOptions)
        {
            var prefix = IRCd.CloakPrefix;
            var suffix = GetTruncatedHost(cloakOptions.Host);
            var hash = Cloaking.InspCloakHost((char)1, cloakOptions.Key, cloakOptions.Host, 6);

            return $"{prefix}-{hash}{suffix}";
        }
    }
}
