namespace cmpctircd.Cloak.Service
{
    public class InspDnsService : CloakService
    {
        public override Cloak GetCloak()
        {
            return new InspDNSCloak();
        }
    }
}
