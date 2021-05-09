using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cmpctircd.Configuration
{
    public class LoggerElement : ConfigurationElement
    {
        [JsonExtensionData] private Dictionary<string, JsonElement> _attributes { get; set; }

        public LoggerType Type { get; set; }
        public LogType Level { get; set; }
        public string Channel { get; set; }
        public string Modes { get; set; }

        public Dictionary<string, string> Attributes
        {
            get { return _attributes.ToDictionary(x => x.Key, x => x.Value.ToString()); }
        }
    }
}