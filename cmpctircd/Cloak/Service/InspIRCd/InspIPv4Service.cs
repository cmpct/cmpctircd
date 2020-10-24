namespace cmpctircd.Cloak.Service
{
    public class InspIPv4Service : CloakService
    {
        public override Cloak GetCloak() {
            return new InspIPv4Cloak();
        }
    }
}
