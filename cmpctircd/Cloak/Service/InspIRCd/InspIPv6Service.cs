using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
