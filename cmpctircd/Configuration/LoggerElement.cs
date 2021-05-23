using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cmpctircd.Configuration
{
    public class LoggerElement
    {
        public LoggerType Type { get; set; }
        public LogType Level { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}