using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

namespace cmpctircd.Configuration {
    public class LoggerElement : ConfigurationElement {
        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();

        // Guid, due to a lack of other unique properties for this element type.
        public Guid InstanceGuid {
            get;
        } = Guid.NewGuid();

        public IReadOnlyDictionary<string, string> Attributes {
            get { return new ReadOnlyDictionary<string, string>(_attributes); }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public LoggerType Type {
            get { return (LoggerType) this["type"]; }
            set { this["type"] = value; }
        }

        [ConfigurationProperty("level", IsRequired = true)]
        public LogType Level {
            get { return (LogType) this["level"]; }
            set { this["level"] = value; }
        }

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value) {
            _attributes.Add(name, value);
            return true;
        }
    }
}
