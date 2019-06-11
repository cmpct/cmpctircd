using System;
using System.Configuration;
using System.IO;

namespace cmpctircd.Configuration {
    public class LoggerElement : ConfigurationElement {
        [ConfigurationProperty("type", IsRequired = true)]
        public LoggerType Type {
            get { return (LoggerType) Enum.Parse(typeof(LoggerType), (string)this["type"]); }
            set { this["type"] = value.ToString(); }
        }

        [ConfigurationProperty("level", IsRequired = true)]
        public LogType Level {
            get { return (LogType)Enum.Parse(typeof(LogType), (string)this["level"]); }
            set { this["level"] = value.ToString(); }
        }

        [ConfigurationProperty("path", IsRequired = false)]
        public FileInfo Path {
            get { return new FileInfo((string)this["path"]); }
            set { this["path"] = value.FullName; }
        }
    }
}
