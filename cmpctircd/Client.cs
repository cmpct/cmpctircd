﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;

using static cmpctircd.Errors;

namespace cmpctircd
{
    class Client {
        // Internals
        // TODO: Many of these look like they shouldn't be public. Review?
        public IRCd IRCd { get; set; }
        public TcpClient TcpClient { get; set; }
        public SocketListener Listener { get; set; }
        public byte[] Buffer { get; set; }

        // Metadata
        // TODO: Make these read-only apart from via setNick()?
        public String Nick { get; set; }
        public String Ident { get; set; }
        public String RealName { get; set; }
        public ClientState State { get; set; }

        // Ping information
        public Boolean WaitingForPong { get; set; } = false;
        public int LastPong { get; set; } = 0;
        public String PingCookie { get; set; } = "";


        public void send_version()
        {
            write(String.Format(":{0} {1} {2} :cmpctircd-{3}", IRCd.host, IrcNumeric.RPL_VERSION.Printable(), Nick, IRCd.version));
        }

        public Client() {
            State = ClientState.PreAuth;
        }

        public void begin_tasks() {
            // Check for PING/PONG events due (TODO: and DNS)
            check_timeout();
        }

        public void send_welcome() {
            // Refuse if the user hasn't yet authenticated (or is already)
            if(String.IsNullOrWhiteSpace(Nick) || String.IsNullOrWhiteSpace(Ident)) return;
            if(State.CompareTo(ClientState.PreAuth) > 0) return;

            // Henceforth, we assume user can become Authenticated!
            State = ClientState.Auth;

            write(String.Format(":{0} {1} {2} :Welcome to the {3} IRC Network {4}", IRCd.host, IrcNumeric.RPL_WELCOME.Printable(), Nick, IRCd.network, mask()));
            write(String.Format(":{0} {1} {2} :Your host is {3}, running version cmpctircd-{4}", IRCd.host, IrcNumeric.RPL_YOURHOST.Printable(), Nick, IRCd.host, IRCd.version));
            write(String.Format(":{0} {1} {2} :This server was created {3}", IRCd.host, IrcNumeric.RPL_CREATED.Printable(), Nick, IRCd.host, 0));
            // TODO: This was commented out in the Perl version, probably not something to use for now.
            // TODO: I don't think MYINFO is very popular?
            //write(String.Format(":{0} {1} {2} {3} {4} x ntlo", ircd.host, IrcNumeric.RPL_MYINFO.Printable(), nick, ircd.host, ircd.version));
            write(String.Format(":{0} {1} {2} :CASEMAPPING=rfc1459 PREFIX=(ov)@+ STATUSMSG=@+ NETWORK={3} MAXTARGETS={4} :are supported by this server", IRCd.host, IrcNumeric.RPL_ISUPPORT.Printable(), Nick, IRCd.network, IRCd.maxTargets));
            write(String.Format(":{0} {1} {2} :CHANTYPES=# CHANMODES=b,,l,ntm :are supported by this server", IRCd.host, IrcNumeric.RPL_ISUPPORT.Printable(), Nick));

            // Send MOTD
            send_motd();
        }

        public void send_motd() {
            try {
                string[] motd = System.IO.File.ReadAllLines("ircd.motd");
                write(String.Format(":{0} {1} {2} : - {3} Message of the Day -", IRCd.host, IrcNumeric.RPL_MOTDSTART.Printable(), Nick, IRCd.host));
                for(int i = 0; i < motd.Length; i++) {
                    if((i == motd.Length) && String.IsNullOrEmpty(motd[i])) {
                        // If end of the file and a new line, don't print.
                        break;
                    }
                    write(String.Format(":{0} {1} {2} : - {3}", IRCd.host, IrcNumeric.RPL_MOTD.Printable(), Nick, motd[i]));
                }
                write(String.Format(":{0} {1} {2} :End of /MOTD command.", IRCd.host, IrcNumeric.RPL_ENDOFMOTD.Printable(), Nick));
            } catch(System.IO.FileNotFoundException e) {
                Console.WriteLine("ircd.motd doesn't exist!");
            }
        }


        public void send_rules() {
            try {
                string[] rules = System.IO.File.ReadAllLines("ircd.rules");
                write(String.Format(":{0} {1} {2} :- {3} server rules -", IRCd.host, IrcNumeric.RPL_MOTDSTART.Printable(), Nick, IRCd.host));
                for(int i = 0; i < rules.Length; i++) {
                    if((i == rules.Length) && String.IsNullOrEmpty(rules[i])) {
                        // If end of the file and a new line, don't print.
                        break;
                    }
                    write(String.Format(":{0} {1} {2} : - {3}", IRCd.host, IrcNumeric.RPL_MOTD.Printable(), Nick, rules[i]));
                }
                write(String.Format(":{0} {1} {2} :End of RULES command.", IRCd.host, IrcNumeric.RPL_ENDOFMOTD.Printable(), Nick));
            } catch(System.IO.FileNotFoundException e) {
                Console.WriteLine("ircd.rules doesn't exist!");
            }
        }

        public Boolean setNick(String nick) {
            // Return if nick is the same
            String oldNick = this.Nick;
            String newNick = nick;

            if (String.Compare(newNick, oldNick, false) == 0)
                return true;

            // Is the nick safe?
            Regex safeNicks = new Regex(@"[A-Za-z{}\[\]_\\^|`][A-Za-z{}\[\]_\-\\^|`0-9]*", RegexOptions.IgnoreCase);
            Boolean safeNick = safeNicks.Match(newNick).Success;
            if (!safeNick) {
                throw new IrcErrErroneusNicknameException(this, newNick);
            }


            // Does a user with this nick already exist?
            foreach(var clientList in IRCd.clientLists) {
                foreach(var clientDict in clientList) {
                    if(clientDict.Key.Nick == newNick) {
                        throw new IrcErrNicknameInUseException(this, newNick);
                        return false;
                   }
                }
            };

            foreach(var channel in IRCd.channelManager.list()) {
                if(!channel.Value.inhabits(this)) continue;
                channel.Value.send_to_room(this, String.Format(":{0} NICK :{1}", mask(), newNick), false);
                channel.Value.remove(oldNick);
                channel.Value.add(this, newNick);
            }

            // TODO: Verify the nickname is safe
            write(String.Format(":{0} NICK {1}", mask(), nick));
            this.Nick = newNick;

            send_welcome();
            return true;
        }
        public Boolean setUser(String ident, String real_name) {
            // TOOD: validation
            this.Ident = ident;
            this.RealName = real_name;

            send_welcome();
            // this.sendWelcome() a lá cmpctircd?
            return true;
        }


        public void check_timeout() {
            // By default, no pong cookie is required
            Int32 time = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Boolean requirePong = false;
            int period = (LastPong) + (IRCd.pingTimeout);

            requirePong = (IRCd.requirePong) && (LastPong == 0);

            // Is the user due a PING?
            if((requirePong) || (time > period && !WaitingForPong)) {
                PingCookie = create_ping_cookie();
                LastPong = time;
                WaitingForPong = true;

                write(String.Format("PING :{0}", PingCookie));
            }

            // Has the user taken too long to reply with a PONG?
            if(WaitingForPong && (time > (LastPong + (IRCd.pingTimeout * 2)))) {
                disconnect("Ping timeout", true);
            }

            Task.Delay(60000).ContinueWith(t => check_timeout());
        }

        public String create_ping_cookie() {
            return System.IO.Path.GetRandomFileName().Substring(0, 7);
        }

        // Returns the user's host (raw IP)
        // TODO: DNS?
        public String get_host() {
            return ((System.Net.IPEndPoint)TcpClient.Client.RemoteEndPoint).Address.ToString();
        }

        // Returns the user's mask
        // TODO: cloaking
        public String mask() {
            String nick = this.Nick;
            String user = this.Ident;
            String real_name = this.RealName;
            String host = this.get_host();
            return String.Format("{0}!{1}@{2}", nick, user, host);
        }

        public void write(String packet) {
            byte[] packetBytes = Encoding.UTF8.GetBytes(packet + "\r\n");
            // TODO: I'm unsure if we should be constantly using GetStream, or
            // reuse the same object
            using (var s = TcpClient.GetStream())
            {
                s.Write(packetBytes, 0, packetBytes.Length);
            }
        }

        public void disconnect(Boolean graceful) {
            disconnect("", graceful);
        }

        public void disconnect(String quitReason, Boolean graceful) {
            if (graceful) {
                // Inform all of the channels we're a member of that we are leaving
                foreach (var channel in IRCd.channelManager.list()) {
                    if (channel.Value.inhabits(this)) {
                        channel.Value.quit(this, quitReason);
                        channel.Value.remove(this);
                    }
                }
                write(String.Format(":{0} QUIT :{1}", mask(), quitReason));
            }

            Listener.remove(this);
        }

    }
}
