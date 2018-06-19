using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Security;
using System.Net;

namespace cmpctircd {
    public class SocketBase {
        // A shared base class for the Client and Server classes

        // Internals
        // TODO: Many of these look like they shouldn't be public or should be private set. Review?
        public IRCd IRCd { get; set; }
        public TcpClient TcpClient { get; set; }
        public SslStream TlsStream { get; set; }
        // TODO: make these protected set?
        public NetworkStream Stream { get; set; }
        public SocketListener Listener { get; set; }

        // TODO: do constructor too

        public SocketBase(IRCd ircd, TcpClient tc, SocketListener sl) {
            IRCd = ircd;
            TcpClient = tc;
            Listener = sl;
            Stream = TcpClient?.GetStream();
        }

        public void BeginTasks() {
            // Base tasks such as DNS or connections
            // noop in base
        }

        // Returns the socket's raw IP
        public IPAddress IP {
            get {
                var EndPoint = (System.Net.IPEndPoint) TcpClient.Client.RemoteEndPoint;
                if(EndPoint != null) {
                    // Live socket
                    return EndPoint.Address;
                } else {
                    // Fake local client with no remote host
                    return IPAddress.Loopback;
                }
            }
        }

        // TODO rework these? (for TLS links especially?)
        public void Write(string packet) {
            Write(packet, Stream);
        }

        public void Write(string packet, NetworkStream Stream) {
            byte[] packetBytes = Encoding.UTF8.GetBytes(packet + "\r\n");
            try {
                if(TlsStream != null && TlsStream.CanWrite) {
                    TlsStream.Write(packetBytes, 0, packetBytes.Length);
                } else if(Stream != null && Stream.CanWrite) {
                    Stream.Write(packetBytes, 0, packetBytes.Length);
                }
            } catch(Exception) {
                // XXX: Was {ObjectDisposed, Socket}Exception but got InvalidOperation from SslStream.Write()
                // XXX: Not clear why given we check .CanWrite, etc
                // XXX: See http://bugs.cmpct.info/show_bug.cgi?id=253
                Disconnect("Connection reset by host", true, false);
            }
        }

        public void Disconnect(bool graceful = false) => Disconnect("", graceful, graceful);
        public void Disconnect(string reason = "", bool graceful = false, bool sendToSelf = false) {
            if(sendToSelf) {
                Write(reason);
            }
            if(TlsStream != null) {
                TlsStream.Close();
            }
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
