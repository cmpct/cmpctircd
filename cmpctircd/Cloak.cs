using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;

namespace cmpctircd
{
    static class Cloak {
        static public string CmpctCloakIPv4(IPAddress address, string keys) {
            var string_ip   = address.ToString();
            var trunc_ip    = "";
            var cloak_s     = "";
            var dot_count   = string_ip.Count(s => s == '.');
            var dot_positions = new List<int>();


            // Need to know positions of each of the dots
            for (int i = 0; i < string_ip.Length; i++) {
                if(string_ip[i].Equals('.')) {
                    dot_positions.Add(i);
                }
            }

            // Go up to last but one
            for(int i = 0; i < string_ip.Length; i++) {
                // Getting closer to the number of desired dots...
                if(dot_positions.Contains(i))
                    dot_count -= 1;

                // Last two dots, stop 
                if (dot_count == 2) {
                    trunc_ip = string_ip.Substring(0, i + 1);
                    break;
                }
                
                // We keep everything up to the last two dots
            }
            
            using(MD5 hasher = MD5.Create()) {
                var cloak = hasher.ComputeHash(Encoding.UTF8.GetBytes(trunc_ip + keys));
                cloak_s = string.Concat(cloak.Select(x => x.ToString("X2")));
            }

            cloak_s = trunc_ip + cloak_s;
            return cloak_s;
        }

        static public string CmpctCloakIPv6(IPAddress address, string key) {
            // TODO: Check that this works properly for IPv6
            // TODO: And provides appropriate protection
            var string_ip   = address.ToString();
            var trunc_ip    = "";
            var cloak_s     = "";
            var colon_count     = string_ip.Count(s => s == ':');
            var colon_positions = new List<int>();


            // Need to know positions of each of the colons
            for (int i = 0; i < string_ip.Length; i++) {
                if(string_ip[i].Equals('.')) {
                    colon_positions.Add(i);
                }
            }

            // Go up to last but one
            for(int i = 0; i < string_ip.Length; i++) {
                // Getting closer to the number of desired colons...
                if(colon_positions.Contains(i))
                    colon_count -= 1;

                // Last two dots, stop 
                if (colon_count == 2) {
                    trunc_ip = string_ip.Substring(0, i + 1);
                    break;
                }
                
                // We keep everything up to the last two dots
            }
            
            using(MD5 hasher = MD5.Create()) {
                var cloak = hasher.ComputeHash(Encoding.UTF8.GetBytes(trunc_ip + key));
                cloak_s = string.Concat(cloak.Select(x => x.ToString("X2")));
            }

            cloak_s = trunc_ip + cloak_s;
            return cloak_s;
        }

        static public string CmpctCloakHost(String host, string key) {
            var trunc_host    = "";
            var cloak_s       = "";
            var dot_count     = host.Count(s => s == '.');
            var dot_positions = new List<int>();


            // Need to know positions of each of the dots
            for (int i = 0; i < host.Length; i++) {
                if(host[i].Equals('.')) {
                    dot_positions.Add(i);
                }
            }

            // Go up to last but one
            for(int i = 0; i < host.Length; i++) {
                // Getting closer to the number of desired dots...
                if(dot_positions.Contains(i))
                    dot_count -= 1;

                // Last two dots, stop 
                if (dot_count == 2) {
                    trunc_host = host.Substring(i);
                    break;
                }
                
                // We keep everything up to the last two dots
            }
            
            using(MD5 hasher = MD5.Create()) {
                var cloak = hasher.ComputeHash(Encoding.UTF8.GetBytes(trunc_host + key));
                cloak_s = string.Concat(cloak.Select(x => x.ToString("X2")));
            }

            cloak_s += trunc_host;
            return cloak_s;
        }

    }
}
