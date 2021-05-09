using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace cmpctircd.Configuration
{
    [TypeConverter(typeof(IpAddressConverter))]
    public class IpAddressConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context,
            Type sourceType)
        {
            if (sourceType == typeof(IPAddress)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            CultureInfo culture, object value)
        {
            if (value is string) return IPAddress.Parse((string) value);
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string)) value.ToString();
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}