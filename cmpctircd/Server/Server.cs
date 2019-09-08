using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using cmpctircd.Configuration;

namespace cmpctircd {
    public class Server : SocketBase {
        // Internals
        // TODO: Many of these look like they shouldn't be public or should be private set. Review?
        public string Name { get; set; }
        public string SID { get; set; }
        public string Desc { get; set; }
        public ServerState State { get; set; }

        private ServerType _type;

        public ServerType Type {
            get { return _type; }
            set {
                // Set the type correctly first
                _type = value;

                // Create an appropriate translator based on the type given
                switch (Type) {
                    case ServerType.InspIRCd21:
                        Translator = new InspIRCd21(this);
                        break;

                    default:
                        // TODO: Nicer way to do this?
                        break;
                }
            }
        }
        public ITranslator Translator { get; private set; }

        public ServerElement ServerInfo { get; set; }

        public Server(IRCd ircd, TcpClient tc, SocketListener sl, Stream stream) : base(ircd, tc, sl, stream) {
            State = ServerState.PreAuth;
            Type  = sl.Info.Protocol;
        }

        public Server(IRCd ircd, TcpClient tc, SocketListener sl, Stream stream, ServerElement serverInfo) : this(ircd, tc, sl, stream) {
            ServerInfo = serverInfo;
            Type = ServerInfo.Type;
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
            Translator.SyncClient(client);
        }

        public void SyncChannel(Channel channel) {
            Translator.SyncChannel(channel);
        }

        public void SendHandshake() {
            // Introduce ourselves
            Translator.Handshake();
        }

        public void SendCapab() {
            Translator.SendCapab();
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
