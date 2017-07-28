using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cmpctircd {
    public class Ban {
        private string _nick;
        private string _user;
        private string _host;
        private string _setter;
        private int   _date;

        public Ban(string Nick, string User, string Host, string Setter, int Date) {
            _nick   = Nick ?? "*";
            _user   = User ?? "*";
            _host   = Host ?? "*";
            _setter = Setter;
            _date   = Date;
        }

        public bool Match(Client Client) {

            return Regex.IsMatch(Client.Nick, @_nick.Replace("*", ".*"))
                && Regex.IsMatch(Client.Ident, @_user.Replace("*", ".*"))
                && Regex.IsMatch(Client.Cloak, @_host.Replace("*", ".*"))
                && Regex.IsMatch(Client.DNSHost, @_host.Replace("*", ".*"))
                && Regex.IsMatch(Client.IP, @_host.Replace("*", ".*"));
        }

        public string GetBan() {
            return $"{_nick}!{_user}@{_host} {_setter} {_date}";
        }

        public static Dictionary<string, string> CreateMask(string input) {
            Dictionary<string, string> ClientData = new Dictionary<string, string>();
            string nick = "*";
            string user = "*";
            string host = "*";
            string mask = "*!*@*";
            if(Regex.IsMatch(input, "!") && Regex.IsMatch(input, "@")) {
                // Packet was nick!user@host
                nick = input.Split('!')[0];
                user = input.Split('!')[1];
                user = user.Split('@')[0];
                host = input.Split('@')[1];
            } 
            else if(Regex.IsMatch(input, "!")) {
                // Packet was nick!user or nick!
                nick = input.Split('!')[0];
                user = input.Split('!')[1];
            } else if(Regex.IsMatch(input, "@")) {
                // Packet was user@host or @host
                host = input.Split('@')[1];
            } else {
                // Packet was nick
                nick = input;
            }
            nick = string.IsNullOrEmpty(nick) ? "*" : nick;
            user = string.IsNullOrEmpty(user) ? "*" : user;
            host = string.IsNullOrEmpty(host) ? "*" : host;
            mask = $"{nick}!{user}@{host}";
            ClientData.Add("nick", nick);
            ClientData.Add("user", user);
            ClientData.Add("host", host);
            ClientData.Add("mask", mask);
            return ClientData;
        }
    }
}
