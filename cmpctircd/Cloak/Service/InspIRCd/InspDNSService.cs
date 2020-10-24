using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
