namespace cmpctircd.Cloak.Service
{
    /// <summary>
    /// A service to return DNS Cloaks compatible with InspIRCd.
    /// </summary>
    public class InspDnsService : CloakService
    {
        /// <summary>
        /// Gets an InspDNSCloak.
        /// </summary>
        /// <returns>InspDNSCloak.</returns>
        public override Cloak GetCloak()
        {
            return new InspDNSCloak();
        }
    }
}
