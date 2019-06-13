using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

namespace cmpctircd.Configuration {
    public class LoggerElement : ConfigurationElement {
        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Attributes {
            get { return new ReadOnlyDictionary<string, string>(_attributes); }
        }

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

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value) {
            _attributes.Add(name, value);
            return true;
        }
    }
}
