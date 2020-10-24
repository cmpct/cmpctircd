namespace cmpctircd.Cloak.Service
{
    /// <summary>
    /// A service to return IPv6 Cloaks compatible with InspIRCd.
    /// </summary>
    public class InspIPv6Service : CloakService
    {
        /// <summary>
        /// Gets an InspIPv6Cloak.
        /// </summary>
        /// <returns>InspIPv6Cloak.</returns>
        public override Cloak GetCloak()
        {
            return new InspIPv6Cloak();
        }
    }
}
