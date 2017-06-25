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
        public IRCd ircd;
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

            write(String.Format(":{0} {1} {2} :Welcome to the {3} IRC Network {4}", ircd.host, IrcNumeric.RPL_WELCOME.Printable(), nick, ircd.network, mask()));
            write(String.Format(":{0} {1} {2} :Your host is {3}, running version cmpctircd-{4}", ircd.host, IrcNumeric.RPL_YOURHOST.Printable(), nick, ircd.host, ircd.version));
            write(String.Format(":{0} {1} {2} :This server was created {3}", ircd.host, IrcNumeric.RPL_CREATED.Printable(), nick, ircd.host, 0));
            // TODO: This was commented out in the Perl version, probably not something to use for now.
            // TODO: I don't think MYINFO is very popular?
            //write(String.Format(":{0} {1} {2} {3} {4} x ntlo", ircd.host, IrcNumeric.RPL_MYINFO.Printable(), nick, ircd.host, ircd.version));
            write(String.Format(":{0} {1} {2} :CASEMAPPING=rfc1459 PREFIX=(ov)@+ STATUSMSG=@+ NETWORK={3} MAXTARGETS={4} :are supported by this server", ircd.host, IrcNumeric.RPL_ISUPPORT.Printable(), nick, ircd.network, ircd.maxTargets));
            write(String.Format(":{0} {1} {2} :CHANTYPES=# CHANMODES=b,,l,ntm :are supported by this server", ircd.host, IrcNumeric.RPL_ISUPPORT.Printable(), nick));

            // Send MOTD
            send_motd();
        }

        public void send_motd() {
            try {
                string[] motd = System.IO.File.ReadAllLines("ircd.motd");
                write(String.Format(":{0} {1} {2} : - {3} Message of the Day -", ircd.host, IrcNumeric.RPL_MOTDSTART.Printable(), nick, ircd.host));
                for(int i = 0; i < motd.Length; i++) {
                    if((i == motd.Length) && String.IsNullOrEmpty(motd[i])) {
                        // If end of the file and a new line, don't print.
                        break;
                    }
                    write(String.Format(":{0} {1} {2} : - {3}", ircd.host, IrcNumeric.RPL_MOTD.Printable(), nick, motd[i]));
                }
                write(String.Format(":{0} {1} {2} :End of /MOTD command.", ircd.host, IrcNumeric.RPL_ENDOFMOTD.Printable(), nick));
            } catch(System.IO.FileNotFoundException e) {
                Console.WriteLine("ircd.motd doesn't exist!");
            }
        }


        public void send_rules() {
            try {
                string[] rules = System.IO.File.ReadAllLines("ircd.rules");
                write(String.Format(":{0} {1} {2} :- {3} server rules -", ircd.host, IrcNumeric.RPL_MOTDSTART.Printable(), nick, ircd.host));
                for(int i = 0; i < rules.Length; i++) {
                    if((i == rules.Length) && String.IsNullOrEmpty(rules[i])) {
                        // If end of the file and a new line, don't print.
                        break;
                    }
                    write(String.Format(":{0} {1} {2} : - {3}", ircd.host, IrcNumeric.RPL_MOTD.Printable(), nick, rules[i]));
                }
                write(String.Format(":{0} {1} {2} :End of RULES command.", ircd.host, IrcNumeric.RPL_ENDOFMOTD.Printable(), nick));
            } catch(System.IO.FileNotFoundException e) {
                Console.WriteLine("ircd.rules doesn't exist!");
            }
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

        public void disconnect() {
            // Inform all of the channels we're a member of that we are leaving
            foreach (var channel in ircd.channelManager.list()) {
                if(channel.Value.inhabits(this)) {
                    channel.Value.remove(this);
                }
            }
        }

    }
}
