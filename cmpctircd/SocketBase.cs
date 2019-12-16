using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Security;
using System.Net;
using System.IO;

namespace cmpctircd {
    public class SocketBase {
        // A shared base class for the Client and Server classes

        // Internals
        public IRCd IRCd { get; }
        public TcpClient TcpClient { get; }
        public Stream Stream { get; }
        public bool IsTlsEnabled { get; }
        // TODO: make these protected set?
        public SocketListener Listener { get; set; }

        // Ping information
        public long SignonTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public bool WaitingForPong { get; set; } = false;
        public long LastPong { get; set; } = 0;
        public string PingCookie { get; set; } = "";

        // TODO: do constructor too

        public SocketBase(IRCd ircd, TcpClient tc, SocketListener sl, Stream stream) {
            IRCd = ircd;
            TcpClient = tc;
            Listener = sl;
            Stream = stream;
            IsTlsEnabled = stream is SslStream;
        }

        public virtual void BeginTasks() {}

        public void CheckTimeout(bool server = false) {
            // By default, no pong cookie is required
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var requirePong = false;
            var SendPing = false;
            var PingString = "";
            long period = 0;

            if (LastPong == 0) {
                period = SignonTime + IRCd.PingTimeout;
            } else {
                period = LastPong + IRCd.PingTimeout;
            }

            // This is a flag to check whether an initial pong cookie is needed
            requirePong = !server && IRCd.RequirePong && (LastPong == 0);

            // Is the socket due a PING?
            if (!WaitingForPong) {
                if ((requirePong) || (time > period && !WaitingForPong)) {
                    if (server && !string.IsNullOrEmpty(PingCookie)) {
                        // Here, PingCookie is the SID of the server being pinged
                        // The server has authenticated (SID is empty if they haven't yet)
                        PingString = $":{IRCd.SID} PING {IRCd.SID} {PingCookie}";
                        SendPing = true;
                    } else {
                        PingCookie = CreatePingCookie();
                        PingString = $"PING :{PingCookie}";
                        SendPing   = true;
                    }

                    if (SendPing) {
                        WaitingForPong = true;
                        Write(PingString);
                        return;
                    }
                }
            }

            // Has the user taken too long to reply with a PONG?
            if (WaitingForPong && (time > (LastPong + (IRCd.PingTimeout * 2)))) {
                if (server) {
                    // For servers, PingCookie is the SID of the server being pinged
                    try {
                        var name = IRCd.GetServerBySID(PingCookie).Name;
                        IRCd.Log.Info($"Disconnecting server due to ping timeout: {name}");
                    } catch(InvalidOperationException) {
                        // If the server is gone, give a generic message
                        IRCd.Log.Info($"Disconnecting server due to ping timeout: {PingCookie}");
                    }
                }
                Disconnect("Ping timeout", true, true);
                return;
            }

            Task.Delay((int)TimeSpan.FromMinutes(1).TotalMilliseconds).ContinueWith(t => CheckTimeout(server));
        }

        public static String CreatePingCookie() => System.IO.Path.GetRandomFileName().Substring(0, 7);

        // Returns the socket's raw IP
        public IPAddress IP {
            get {
                var EndPoint = (System.Net.IPEndPoint) TcpClient?.Client?.RemoteEndPoint;
                if(EndPoint != null) {
                    // Live socket
                    return EndPoint.Address;
                } else {
                    // Fake local client with no remote host
                    return IPAddress.Loopback;
                }
            }
        }

        public Task Write(string packet) {
            return Write(packet, Stream);
        }

        public async Task Write(string packet, Stream stream) {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            byte[] packetBytes = Encoding.UTF8.GetBytes(packet + "\r\n");
            if(stream.CanWrite)
                await stream.WriteAsync(packetBytes, 0, packetBytes.Length);
        }

        public void Disconnect(bool graceful = false) => Disconnect("", graceful, graceful);
        public virtual void Disconnect(string reason = "", bool graceful = false, bool sendToSelf = false) {
            if(sendToSelf)
                Write(reason);
            Stream?.Close();
            TcpClient.Close();
            // graceful means inform clients of departure
            // !graceful means kill the connection
        }

        ~SocketBase() {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}
