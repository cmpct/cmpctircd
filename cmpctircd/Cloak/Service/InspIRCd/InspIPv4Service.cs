using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Cloak.Service
{
    public class InspIPv4Service : CloakService
    {
        public override Cloak GetCloak() {
            return new InspIPv4Cloak();
        }
    }
}
