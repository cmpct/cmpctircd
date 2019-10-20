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
                    case ServerType.InspIRCd20:
                        LinkProtocol = new InspIRCd20(this);
                        break;

                    default:
                        // TODO: Nicer way to do this?
                        break;
                }
            }
        }
        public ILinkProtocol LinkProtocol { get; private set; }

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
            foreach(var client in IRCd.Clients) {
                // Tell the server about all of the clients
                SyncClient(client);
            }

            foreach(var channel in IRCd.ChannelManager.Channels) {
                SyncChannel(channel.Value);
            }
        }

        // TODO need to SyncClient, SyncChannel when they connect/are created
        public void SyncClient(Client client) {
            LinkProtocol.SyncClient(client);
        }

        public void SyncChannel(Channel channel) {
            LinkProtocol.SyncChannel(channel);
        }

        public void SendHandshake() {
            // Introduce ourselves
            LinkProtocol.Handshake();
        }

        public void SendCapab() {
            LinkProtocol.SendCapab();
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

            IRCd.Log.Debug($"Disconnecting server: {Name} (reason: {reason})");

            if (sendToSelf) {
                Write(reason);
            }

            Stream?.Close();
            TcpClient.Close();

            // TODO: Graceful? Tell everyone we're going if they didn't send QUITs for all clients like they should?
            foreach (var client in IRCd.Clients) {
                if (client.OriginServer == this) {
                    // These are clients which were on our server
                    client.Disconnect("Server gone", true, false);
                }
            }

            State = ServerState.Disconnected;
            Listener.Remove(this);
        }

        public bool FindServerConfig(string hostname, string password) {
            ServerElement link;
            bool foundMatch = false;

            if (ServerInfo != null) {
                // We are connecting outbound so have a specific server to compare against
                link = ServerInfo;

                if (link.Host == hostname && link.Port == Listener.Info.Port
                    && link.IsTls == Listener.Info.IsTls && link.Password == password) {
                    foundMatch = true;
                }
            } else {
                // Find matching <server> tag in config (or null)
                link = IRCd.Config.Servers.Cast<ServerElement>().Where(s => s.Host == hostname
                        && s.Port == Listener.Info.Port
                        && s.IsTls == Listener.Info.IsTls
                        && s.Password == password).FirstOrDefault();

                // IP address needed for the block
                var compare = new HostInfo { Ip = IP };

                // Check that any found link mask matches the IP
                if (link != null && link.Masks.Select(m => Ban.CreateMask(m)).Any(m => Ban.CheckHost(m, compare))) {
                    foundMatch = true;
                }
            }

            if (foundMatch) {
                ServerInfo = link;
                Type = link.Type;
            }

            return foundMatch;
        }

        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
