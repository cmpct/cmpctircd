using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Net;

namespace cmpctircd.Configuration {
    public class SocketElement : ConfigurationElement {
        [ConfigurationProperty("type", IsRequired = true)]
        public ListenerType Type {
            get { return (ListenerType) this["type"]; }
            set { this["type"] = value; }
        }

        [TypeConverter(typeof(IPAddressConverter))]
        [ConfigurationProperty("host", IsRequired = true)]
        public IPAddress Host {
            get { return (IPAddress) this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
        public int Port {
            get { return (int) this["port"]; }
            set { this["port"] = value; }
        }

        public IPEndPoint EndPoint {
            get { return new IPEndPoint(Host, Port); }
        }

        [ConfigurationProperty("tls", IsRequired = false, DefaultValue = false)]
        public bool IsTls {
            get { return (bool) this["tls"]; }
            set { this["tls"] = value; }
        }

        public static implicit operator SocketElement(ServerElement serverElement) {
            var se = new SocketElement();

            // Check if this is a DNS name
            // If it is, resolve it
            if (!IPAddress.TryParse(serverElement.Destination, out var IP)) {
                IP = Dns.GetHostEntry(serverElement.Destination).AddressList[0];
            }

            se.Host = IP;
            se.Port = serverElement.Port;
            se.IsTls = serverElement.IsTls;
            se.Type = ListenerType.Server;

            return se;
        }
    }

    [TypeConverter(typeof(IPAddressConverter))]
    class IPAddressConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            return IPAddress.Parse((string) value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return ((IPAddress) value).ToString();
        }
    }
}
