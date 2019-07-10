using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace cmpctircd {
    public class Ban {
        private string _nick;
        private string _user;
        private string _host;
        private string _setter;
        private long   _date;

        public Ban(string Nick, string User, string Host, string Setter, long Date) {
            _nick   = Nick ?? "*";
            _user   = User ?? "*";
            _host   = Host ?? "*";
            _setter = Setter;
            _date   = Date;
        }

        public bool Match(Client client) {
            var masks = new Dictionary<string, string>();
            masks["nick"] = _nick;
            masks["user"] = _user;
            masks["host"] = _host;
            return Match(client, masks);
        }

        public static bool Match(Client client, Dictionary<string, string> masks, HostInfo hosts = null) {
            string mask;

            var identifiers = new List<Tuple<string, string>>() {
                // Key:   mask
                // Value: value to compare with
                Tuple.Create(masks["nick"], client.Nick),
                Tuple.Create(masks["user"], client.Ident)
            };

            if(hosts == null)
                hosts = client.GetHosts(true);

            // Check nick, user/ident
            foreach(var identifier in identifiers) {
                mask = @identifier.Item1.Replace("*", ".*");
                if(!Regex.IsMatch(identifier.Item2, mask))
                    return false;
            }

            // Check all the hosts
            var matchMask = CheckHost(masks, hosts);

            return matchMask;
        }

        public static bool CheckHost(Dictionary<string, string> masks, HostInfo hosts) {
            var match = false;
            var mask  = masks["host"].Replace("*", ".*");

            foreach(var host in hosts) {
                if(Regex.IsMatch(host, mask))
                    match = true;
            }

            return match;
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
