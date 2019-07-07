using System;
using System.Configuration;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
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
            // From hex string to byte[]
            // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
            var hex = (string) value;
            return Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            // From byte[] to hex string
            return String.Concat(Array.ConvertAll((byte[]) value, x => x.ToString("X2")));
        }
    }
}
