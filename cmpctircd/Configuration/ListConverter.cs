using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd.Configuration {
    [TypeConverter(typeof(ListConverter))]
    class ListConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            return value == null ? new List<string>() : ((string) value).Split(' ').Where(s => !string.IsNullOrEmpty(s)).ToList<string>();
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return string.Join(" ", (IEnumerable<string>) value);
        }
    }
}
