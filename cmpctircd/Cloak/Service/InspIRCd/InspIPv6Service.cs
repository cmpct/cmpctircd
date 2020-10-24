namespace cmpctircd.Cloak.Service
{
    public class InspIPv6Service : CloakService
    {
        public override Cloak GetCloak()
        {
            return new InspIPv6Cloak();
        }
    }
}
