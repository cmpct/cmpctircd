using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace cmpctircd.Configuration {
    public class CmpctConfigurationSection : ConfigurationSection {
        public static CmpctConfigurationSection GetConfiguration() {
            return (CmpctConfigurationSection) ConfigurationManager.GetSection("cmpct") ?? new CmpctConfigurationSection();
        }
    }
}
