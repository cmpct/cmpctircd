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

            IRCd.Log.Debug($"Disconnecting server: {Name} (reason: {reason})");

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

        public bool FindServerConfig(string hostname, string password) {
            bool foundMatch = true;
            if (ServerInfo != null) {
                // We are connecting outbound so have a specific server to compare against
                var link = ServerInfo;
                if(link.Host != hostname) foundMatch = false;
                if(link.Port != Listener.Info.Port) foundMatch = false;
                if(link.IsTls != Listener.Info.IsTls) foundMatch = false;
                if(link.Password != password) foundMatch = false;

                Type = link.Type;
                ServerInfo = link;
            } else {
                // Check that any <server> blocks exist
                foundMatch = IRCd.Config.Servers.Count > 0;

                // Check against all <server> blocks in the config
                for(int i = 0; i < IRCd.Config.Servers.Count; i++) {
                    var link = IRCd.Config.Servers[i];

                    // TODO: Add error messages
                    if(link.Host     != hostname) foundMatch = false;
                    if(link.Port     != Listener.Info.Port) foundMatch = false;
                    if(link.IsTls    != Listener.Info.IsTls) foundMatch = false;
                    if(link.Password != password) foundMatch = false;

                    var foundHostMatch = false;
                    foreach(var mask in link.Masks) {
                        var maskObject = Ban.CreateMask(mask);
                        // TODO: Allow DNS in Masks (for Servers)
                        var hostInfo   = new HostInfo {
                            Ip = IP
                        };

                        if (Ban.CheckHost(maskObject, hostInfo)) {
                            foundHostMatch = true;
                            break;
                        }
                    }
                    foundMatch = foundMatch && foundHostMatch;

                    if(foundMatch) {
                        // If we're got a match after all of the checks, stop looking
                        Type = link.Type;
                        ServerInfo = link;
                        break;
                    } else {
                        // Reset for next iteration unless we're at the end
                        if(i != IRCd.Config.Servers.Count - 1)
                            foundMatch = true;
                    }
                }
            }

            return foundMatch;
        }

        ~Server() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
