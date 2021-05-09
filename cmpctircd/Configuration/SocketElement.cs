using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace cmpctircd.Configuration
{
    public class SocketElement
    {
        public ListenerType Type { get; set; }
        public IPAddress Host { get; set; }
        public int Port { get; set; }

        public bool Tls { get; set; }
        public ServerType Protocol { get; set; }
        public IPEndPoint EndPoint => new IPEndPoint(Host, Port);

        public static implicit operator SocketElement(ServerElement serverElement)
        {
            var se = new SocketElement();

            // Check if this is a DNS name
            // If it is, resolve it
            if (!IPAddress.TryParse(serverElement.Destination, out var IP))
                IP = Dns.GetHostEntry(serverElement.Destination).AddressList[0];

            se.Host = IP;
            se.Port = serverElement.Port;
            se.Tls = serverElement.Tls;
            se.Type = ListenerType.Server;

            return se;
        }
    }

    [TypeConverter(typeof(IPAddressConverter))]
    internal class IPAddressConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return IPAddress.Parse((string) value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            return ((IPAddress) value).ToString();
        }
    }
}