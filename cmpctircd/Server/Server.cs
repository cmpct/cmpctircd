using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class Server : SocketBase {
        // Internals
        // TODO: Many of these look like they shouldn't be public or should be private set. Review?
        public string SID { get; set; }
        public ServerState State { get; set; }
 
        public Server(IRCd ircd, TcpClient tc, SocketListener sl) : base(ircd, tc, sl) {
            State = ServerState.PreAuth;
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
            var sUmodes = "+i"; // TODO make this dynamic
            var sVirtHost = client.IP;
            var sCloakHost = client.GetHost(true);
            var sIP = client.IP;
            var sGECOS = client.RealName;
            Write($":{IRCd.SID} UID {sUUID} {sServiceStamp} {sNick} {sHost} {sCloakHost} {sUser} {sIP} {sTime} {sUmodes} :{sGECOS}");
        }

        public void SyncChannel(Channel channel) {
            var nicks = "";
            foreach (var client in channel.Clients) {
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

        public new void Disconnect(bool graceful = false) => Disconnect("", graceful, graceful);
        public new void Disconnect(string reason = "", bool graceful = false, bool sendToSelf = false) {
            // TODO: ServerState.Disconnected?
            // TODO: Graceful, SQUIT-like?
            if(sendToSelf) {
                Write(reason);
            }

            if(TlsStream != null) {
                TlsStream.Close();
            }
            TcpClient.Close();
            Listener.Remove(this);
            // graceful means inform clients of departure
            // !graceful means kill the connection
        }


        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
