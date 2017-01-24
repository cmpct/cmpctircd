using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace cmpctircd
{
    class Client
    {
        // Internals
        public TcpClient TcpClient { get; set; }
        public byte[] Buffer { get; set; }

        // Metadata
        // TODO: Make these read-only apart from via setNick()?
        public String nick;
        public String ident;
        public String real_name;
        public ClientState state {get; set; }

        public Client() {
            state = ClientState.PreAuth;
        }
        public void send_welcome() {
            // Refuse if the user hasn't yet authenticated (or is already)
            if(String.IsNullOrWhiteSpace(nick) || String.IsNullOrWhiteSpace(ident)) return;
            if(state.CompareTo(ClientState.PreAuth) > 0) return;

            // Henceforth, we assume user can become Authenticated!
            state = ClientState.Auth;
            write(String.Format(":{0} {1} {2} :Welcome to the {3} IRC Network {4}", "irc.cmpct.info", "001", nick, "cmpct", mask()));
        }


        public Boolean setNick(String nick) {
            // Return if nick is the same
            if (String.Compare(nick, this.nick, false) == 0)
                return true;

            // TODO: Verify the nickname is safe
            this.nick = nick;
            write(String.Format(":{0} NICK {1}", get_host(), nick));

            send_welcome();
            return true;
        }
        public Boolean setUser(String ident, String real_name) {
            // TOOD: validation
            this.ident = ident;
            this.real_name = real_name;

            send_welcome();
            // this.sendWelcome() a lá cmpctircd?
            return true;
        }


        // Returns the user's host (raw IP)
        // TODO: DNS?
        public String get_host() {
            return ((System.Net.IPEndPoint)TcpClient.Client.RemoteEndPoint).Address.ToString();
        }

        // Returns the user's mask
        // TODO: cloaking
        public String mask() {
            String nick = this.nick;
            String user = this.ident;
            String real_name = this.real_name;
            String host = this.get_host();
            return String.Format("{0}!{1}@{2}", nick, user, host);
        }

        public void write(String packet) {
            byte[] packetBytes = Encoding.UTF8.GetBytes(packet + "\r\n");
            TcpClient.GetStream().Write(packetBytes, 0, packetBytes.Length);
        }
    }
}
