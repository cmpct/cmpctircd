using System.Net;
using System.Collections;
using System.Collections.Generic;

namespace cmpctircd {

    public class HostInfo : IEnumerable<string> {
        public string    Cloak { get; set; }
        public string    Dns   { get; set; }
        public IPAddress Ip    { get; set; }

        public IEnumerator<string> GetEnumerator() {
            var hosts = new string[] { Cloak, Dns, Ip.ToString() };

            foreach(var host in hosts) {
                if(host != null)
                    yield return host;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

}
