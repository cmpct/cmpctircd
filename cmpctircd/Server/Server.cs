using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace cmpctircd {
    public class Server : SocketBase {
        // Internals
        // TODO: Many of these look like they shouldn't be public or should be private set. Review?
        public string Name { get; set; }
        public string SID { get; set; }
        public string Desc { get; set; }
        public ServerState State { get; set; }

        public Server(IRCd ircd, TcpClient tc, SocketListener sl, Stream stream) : base(ircd, tc, sl, stream) {
            State = ServerState.PreAuth;
        }

        public new void BeginTasks() {
            try {
                // Check for PING/PONG events due
                CheckTimeout(true);
            } catch (Exception e) {
                IRCd.Log.Debug($"Failed to access server: {e.ToString()}");
                Disconnect("Server gone", true);
                return;
            }
        }

        public void Sync() {
            // This sends the server a copy of all of the clients and channels known to the server
            // 'Bursts'
            foreach(var clientList in IRCd.ClientLists) {
                foreach(var client in clientList) {
                    // Tell the server about all of the clients
                    SyncClient(client);
                }
            }

            foreach(var channel in IRCd.ChannelManager.Channels) {
                SyncChannel(channel.Value);
            }
        }

        // TODO need to SyncClient, SyncChannel when they connect/are created
        public void SyncClient(Client client) {
            var sNick = client.Nick;
            var sHop  = 0; // TODO change when we have more than one hop links
            var sTime = client.SignonTime;
            var sUser = client.Ident;
            var sHost = client.GetHost(false);
            var sUUID = client.UUID;
            var sServiceStamp = client.SignonTime; // TODO ?

            var modeString      = client.GetModeStrings("+");
            var modeString_chrs = modeString[0];
            var modeString_args = modeString[1];
            var sUmodes         = modeString_chrs;
            if (!string.IsNullOrEmpty(modeString_args)) {
                sUmodes += $" {modeString_args}";
            }

            var sVirtHost = client.IP;
            var sCloakHost = client.GetHost(true);
            var sIP = client.IP;
            var sGECOS = client.RealName;

            Write($":{IRCd.SID} UID {sUUID} {sServiceStamp} {sNick} {sHost} {sCloakHost} {sUser} {sIP} {sTime} {sUmodes} :{sGECOS}");
        }

        public void SyncChannel(Channel channel) {
            // TODO: Don't tell the same server about a client repeatedly (check OriginServer somewhere? In Channels? obviously may need changes for hops > 0)
            // TODO: Same for PRIVMSG? We want to send it remote possibly, just not to the server it came from
            var nicks = "";
            foreach (var client in channel.Clients) {
                if (client.Value.OriginServer == this) {
                    // Don't tell them about a server about their own clients
                    // TODO: Needs adjustment for hops > 0
                    continue;
                }

                nicks += String.Join("", channel.Modes.Values.Where(mode => !mode.ChannelWide && mode.Has(client.Value)));
                nicks += ",";
                nicks += client.Value.UUID;
                nicks += " ";
            }
            nicks = nicks.TrimEnd(new char[] { ',' });

            // TODO: Check but if necessary note that in insp, FJOIN is for both creation and joins?
            var modeStrings = channel.GetModeStrings("+");
            var modeString  = (modeStrings[0] + modeStrings[1]).TrimEnd();

            Write($":{IRCd.SID} FJOIN {channel.Name} {channel.CreationTime} {modeString} :{nicks}");
        }

        public void SendHandshake() {
            // Introduce ourselves

            // TODO: send password?
            var Password = "mypassword";
            Write($"SERVER {IRCd.Host} {Password} 0 {IRCd.SID} :{IRCd.Desc}");

            // Burst
            // TODO: Implement INBOUND burst
            // TODO: And queue up things during the BURST? (e.g. FJOINs)
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Write($":{IRCd.SID} BURST {time}");
            Sync();
            Write($":{IRCd.SID} ENDBURST");
        }

        public void SendCapab() {
            // CAPAB BEGIN
            var version = 1202;
            Write($"CAPAB START {version}");

            // TODO: Can this be de-duplicated with Client.SendWelcome()?
            var UModeTypes = IRCd.GetSupportedUModes(new Client(IRCd, null, Listener, null));
            var ModeTypes = IRCd.GetSupportedModesByType();
            var modes = IRCd.GetSupportedModes(true);

            // TODO: Lie a bit for now
            // TODO: Dynamic!
            // https://pypkg.com/pypi/pylinkirc/f/protocols/inspircd.py
            // https://github.com/jlu5/PyLink/blob/master/protocols/inspircd.py
            // https://www.anope.org/doxy/2.0/d6/dd0/inspircd20_8cpp_source.html
            // TODO: Make sure we actually do m_services_account, m_hidechans
            // TODO: Readd m_services_account.so below!
            Write($"CAPAB MODULES :m_cloaking.so={IRCd.CloakPrefix}-{Cloak.InspCloakHost((char) 3, IRCd.CloakKey, "*", 8)}.IP");
            Write($"CAPAB MODSUPPORT m_services_account.so"); // TODO: this is a requirement for services, not insp (obviously)
            Write($"CAPAB USERMODES hidechans");

            // Create a list of chanmodes based on the available modes locally
            // e.g. CAPAB CHANMODES :op=o ...
            var ChannelModeString = new StringBuilder();
            ChannelModeString.Append("CAPAB CHANMODES :");

            var chan = new Channel(IRCd.ChannelManager, IRCd);
            foreach (var modeList in ModeTypes) {
                foreach (var m in modeList.Value) {
                    var mode = chan.Modes[m];
                    var modeName = mode.Name;
                    var modeSymbol = "";
                    var modeCharacter = mode.Character;

                    // Don't tell the remote server about it for now
                    // TODO: Need to figure out how this will affect desyncs etc
                    if (!mode.InspircdCompatible)
                        continue;

                    if (mode.Symbol != "") {
                        modeSymbol = mode.Symbol;
                    }

                    ChannelModeString.Append($"{modeName}={modeSymbol}{modeCharacter} ");
                }
            }
            // TODO: Stop lying to InspIRCd
            ChannelModeString.Append("c_registered=r key=k limit=l private=p reginvite=R regmoderated=M secret=s");
            Write(ChannelModeString.ToString());

            // TODO: Need to (elsewhere) look at the capabilities of the remote server!
            var UserModeString = new StringBuilder();
            UserModeString.Append("CAPAB USERMODES :");

            var client = new Client(IRCd, null, null, null);
            foreach (var mode in client.Modes.Values) {
                // Don't tell the remote server about it for now
                // TODO: Need to figure out how this will affect desyncs etc
                if (!mode.InspircdCompatible)
                    continue;

                UserModeString.Append($"{mode.Name}={mode.Character} ");
            }

            // TODO: Stop lying to to InspIRCd
            UserModeString.Append("regdeaf=R snomask=s u_registered=r wallops=w");
            // TODO: Stop lying to Anope
            UserModeString.Append(" hidechans=I");
            Write(UserModeString.ToString());

            // TODO: Make this dynamic
            Write($"CAPAB CAPABILITIES :NICKMAX=31 CHANMAX=64 MAXMODES=20 IDENTMAX=11 MAXQUIT=255 MAXTOPIC=307 MAXKICK=255 MAXGECOS=128 MAXAWAY=200 IP6SUPPORT=1 PROTOCOL=1202 USERMODES=,, s, IRiorwx GLOBOPS=0 SVSPART=1");
            Write($"CAPAB CAPABILITIES :CASEMAPPING=rfc1459 :PREFIX=({modes["Characters"]}){modes["Symbols"]}");
            Write($"CAPAB CAPABILITIES :CHANMODES={string.Join("", ModeTypes["A"])},{string.Join("", ModeTypes["B"])},{string.Join("", ModeTypes["C"])},{string.Join("", ModeTypes["D"])}");
            Write($"CAPAB END");

        }

        public new void Write(string message) {
            if (State.Equals(ServerState.Disconnected)) return;

            try {
                base.Write(message);
            } catch {
                IRCd.Log.Info($"Disconnecting server due to write fail: {Name}");
                Disconnect("Connection reset by peer", true, false);
            }
        }

        public new void Disconnect(bool graceful = false) => Disconnect("", graceful, graceful);
        public override void Disconnect(string reason = "", bool graceful = false, bool sendToSelf = false) {
            if (State.Equals(ServerState.Disconnected)) return;

            IRCd.Log.Debug($"Disconnecting server: {Name}");

            if (sendToSelf) {
                Write(reason);
            }

            Stream?.Close();
            TcpClient.Close();

            // TODO: Graceful? Tell everyone we're going if they didn't send QUITs for all clients like they should?
            foreach (var clientList in IRCd.ClientLists) {
                foreach (var serverClient in clientList.Where(client => client.OriginServer == this)) {
                    // These are clients which were on our server
                    serverClient.Disconnect("Server gone", true, false);
                }
            }

            State = ServerState.Disconnected;
            Listener.Remove(this);
        }


        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
