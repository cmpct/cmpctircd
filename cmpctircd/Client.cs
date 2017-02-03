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

            // TODO: constants which will go into the config
            String host = "irc.cmpct.info";
            String network = "cmpct";
            String version = "0.1";
            int maxTargets = 200;

            write(String.Format(":{0} {1} {2} :Welcome to the {3} IRC Network {4}", host, IrcNumeric.RPL_WELCOME.Printable(), nick, network, mask()));
            write(String.Format(":{0} {1} {2} :Your host is {3}, running version cmpctircd-{4}", host, IrcNumeric.RPL_YOURHOST.Printable(), nick, host, version));
            write(String.Format(":{0} {1} {2} :This server was created {3}", host, IrcNumeric.RPL_CREATED.Printable(), nick, host, 0));
            // TODO: Should this not have a ':'? It didn't in the perl version...
            write(String.Format(":{0} {1} {2} {3} {4} x ntlo", host, IrcNumeric.RPL_MYINFO.Printable(), nick, host, version));
            write(String.Format(":{0} {1} {2} :CASEMAPPING=rfc1459 PREFIX=(ov)@+ STATUSMSG=@+ NETWORK={3} MAXTARGETS={4} :are supported by this server", host, IrcNumeric.RPL_ISUPPORT.Printable(), nick, network, maxTargets));
            write(String.Format(":{0} {1} {2} :CHANTYPES=# CHANMODES=b,,l,ntm :are supported by this server", host, IrcNumeric.RPL_ISUPPORT.Printable(), nick));
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
