using System.Collections.Generic;

namespace cmpctircd.Configuration
{
    public class ServerElement
    {
        public ServerType Type { get; set; }
        public string Host { get; set; }
        public List<string> Masks { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public bool Tls { get; set; }
        public bool Outbound { get; set; }
        public string Destination { get; set; }
        public bool VerifyTlsCert { get; set; }
    }
}