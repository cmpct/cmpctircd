using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace cmpctircd.Configuration {
    public class OperatorElement : ConfigurationElement {
        public string Name { get; set; }

        public string Password { get; set; }

        public string Algorithm { get; set; }

        public bool Tls { get; set; }
        public List<string> Hosts { get; set; }
        public Type AlgorithmType => Type.GetType(Algorithm);
        public byte[] PasswordArray => ConvertPassword(Password);

        public byte[] ConvertPassword(string value) {
            // From hex string to byte[]
            // https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
            var hex = value;
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}