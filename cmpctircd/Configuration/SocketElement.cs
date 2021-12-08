using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

namespace cmpctircd.Configuration
{
    public class SocketElement
    {
        public ListenerType Type { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Tls { get; set; }

        public ServerType Protocol { get; set; }

        public IPEndPoint EndPoint => new IPEndPoint(IPAddress.Parse(Host), Port);

        public static implicit operator SocketElement(ServerElement serverElement)
        {
            var se = new SocketElement();

            // Check if this is a DNS name
            // If it is, resolve it
            if (!IPAddress.TryParse(serverElement.Destination, out var IP))
                IP = Dns.GetHostEntry(serverElement.Destination).AddressList[0];

            se.Host = IP.ToString();
            se.Port = serverElement.Port;
            se.Tls = serverElement.Tls;
            se.Type = ListenerType.Server;

            return se;
        }
    }
}