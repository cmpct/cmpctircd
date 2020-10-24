using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace cmpctircd.Cloak
{
    public abstract class CloakService
    {
        public abstract Cloak GetCloak();
    }
}