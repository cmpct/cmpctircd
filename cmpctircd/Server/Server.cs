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
            Write($":{SID} UID {sUUID} {sServiceStamp} {sNick} {sHost} {sCloakHost} {sUser} {sIP} {sTime} {sUmodes} :{sGECOS}");
        }

        public void SyncChannel(Channel channel) {
            // TODO
            return;
        }

        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
