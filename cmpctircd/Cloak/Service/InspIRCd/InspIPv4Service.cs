namespace cmpctircd.Cloak.Service
{
    /// <summary>
    /// A service to return IPv4 Cloaks compatible with InspIRCd.
    /// </summary>
    public class InspIPv4Service : CloakService
    {
        /// <summary>
        /// Gets an InspIPv4Cloak.
        /// </summary>
        /// <returns>InspIPv4Cloak.</returns>
        public override Cloak GetCloak() {
            return new InspIPv4Cloak();
        }
    }
}
