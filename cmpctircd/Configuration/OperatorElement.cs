using System;
using System.Configuration;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace cmpctircd.Configuration {
    public class OperatorElement : ConfigurationElement {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [TypeConverter(typeof(HexadecimalConverter))]
        [ConfigurationProperty("password", IsRequired = true)]
        public byte[] Password {
            get { return (byte[]) this["password"]; }
            set { this["password"] = value; }
        }

        [TypeConverter(typeof(TypeNameConverter))]
        [ConfigurationProperty("algorithm", IsRequired = true)]
        public Type Algorithm {
            get { return this["algorithm"] as Type; }
            set { this["algorithm"] = value; }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = false)]
        public bool Tls {
            get { return (bool) this["tls"]; }
            set { this["tls"] = value; }
        }

        [TypeConverter(typeof(ListConverter))]
        [ConfigurationProperty("hosts", IsRequired = true)]
        public List<string> Hosts {
            get { return (List<string>) this["hosts"]; }
            set { this["hosts"] = value; }
        }
    }

    [TypeConverter(typeof(HexadecimalConverter))]
    class HexadecimalConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            return SoapHexBinary.Parse((string) value).Value;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return SoapHexBinary.Parse((string) value).Value;
        }
    }
}
