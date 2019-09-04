using System;
using System.Collections.Generic;
using System.Linq;

namespace cmpctircd.Packets {
    public static class Server {
        // TODO: three CAPAB packets
        // TODO: Handle BURST, don't process until we get them all? Group the FJOINs

        [Handler("PING", ListenerType.Server)]
        public static bool PingHandler(HandlerArgs args) {
            // TODO: implement for hops > 1
            // TODO: could use args.Server.SID instead of SpacedArgs?
            args.Server.Write($":{args.IRCd.SID} PONG {args.IRCd.SID} {args.SpacedArgs[1]}");
            return true;
        }

        [Handler("PONG", ListenerType.Server)]
        public static bool PongHandler(HandlerArgs args) {
            args.Server.WaitingForPong = false;
            args.Server.LastPong       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        [Handler("CAPAB", ListenerType.Server)]
        public static bool CapabHandler(HandlerArgs args) {
            // TODO: checked if already sent capab?
            if (args.SpacedArgs.Count > 0 && args.SpacedArgs[1] == "START") {
                args.Server.SendCapab();
            }
            return true;
        }

        [Handler("SERVER", ListenerType.Server)]
        public static bool ServerHandler(HandlerArgs args) {
            // TODO: introduce some ServerState magic
            var parts = args.Line.Split(new char[] { ' ' }, 7).ToList();

            // Drop :SID at the beginning (Anope does this, InspIRCd doesn't)
            if(parts[0].StartsWith(":")) {
                parts.RemoveAt(0);
            }

            // Check there are enough parameters
            if (parts.Count() != 6) {
                // TODO: send an error
                return false;
            }

            // Parse out some values
            var hostname = parts[1];
            var password = parts[2];
            var sid      = parts[4];
            var desc     = parts[5].Substring(1);

            // Compare with config
            bool foundMatch = true && args.IRCd.Config.Servers.Count > 0;
            for(int i = 0; i < args.IRCd.Config.Servers.Count; i++) {
                var link = args.IRCd.Config.Servers[i];

                // TODO: Add error messages
                if(link.Host     != hostname) foundMatch = false;
                if(link.Port     != args.Server.Listener.Info.Port) foundMatch = false;
                if(link.IsTls    != args.Server.Listener.Info.IsTls) foundMatch = false;
                if(link.Password != password) foundMatch = false;

                var foundHostMatch = false;
                foreach(var mask in link.Masks) {
                    var maskObject = Ban.CreateMask(mask);
                    // TODO: Allow DNS in Masks (for Servers)
                    var hostInfo   = new HostInfo {
                        Ip = args.Server.IP
                    };

                    if (Ban.CheckHost(maskObject, hostInfo)) {
                        foundHostMatch = true;
                        break;
                    }
                }
                foundMatch = foundMatch && foundHostMatch;

                // Check if the server is already connected
                // [Important that this is after all authentication checks because it has a conditional on whether server is authed]
                try {
                    var foundServer = args.IRCd.GetServerBySID(sid);

                    if(foundMatch) {
                        // If the incoming server is authenticated, disconnect the old one (saves time waiting for ping timeouts, etc)
                        args.IRCd.Log.Warn($"[SERVER] Ejecting server (SID: {sid}, name: {hostname}) for new connection");
                        foundServer.Disconnect("ERROR: Replaced by a new connection", true);
                    }
                } catch (InvalidOperationException) {}

                if(foundMatch) {
                    // If we're got a match after all of the checks, stop looking
                    break;
                } else {
                    // Reset for next iteration unless we're at the end
                    if(i != args.IRCd.Config.Servers.Count - 1)
                        foundMatch = true;
                }
            }

            if(foundMatch) {
                args.Server.State = ServerState.Auth;
                args.IRCd.Log.Warn("[SERVER] Got an authed server"); // TODO: Change to Info?

                // Introduce ourselves
                args.Server.SendHandshake();

                // Set the ping cookie to be the SID, so we start pinging them (see CheckTimeout)
                args.Server.PingCookie = args.Server.SID;
            } else {
                args.IRCd.Log.Warn("[SERVER] Got an unauthed server");
                args.Server.Disconnect("ERROR: Invalid credentials", true);
                return false;
            }
            return true;
        }

        [Handler("UID", ListenerType.Server)]
        public static bool UidHandler(HandlerArgs args) {
            var parts = args.Line.Split(new char[] { ' ' }, 12);
            
            // TODO: check parts count
            var last_index = parts.Length - 1;
            var sid_from     = parts[0];
            var user_uuid    = parts[2];
            var timestamp    = parts[3];
            var nick         = parts[4];
            var hostname     = parts[5];
            var display_host = parts[6];
            var ident        = parts[7];
            var ip           = parts[8];
            var signon_time  = parts[9];
            var modes        = parts[10];
            var mode_params  = "";
            var realname     = parts[last_index].Substring(1);
            if(last_index != 11) {
                // If the last index != 11, realname is pushed back by one for an additional mode parameter
                mode_params = parts[11];
            }

            // TODO: needed for FJOIN (???)
            var uid = user_uuid.Substring(3); // Drop the first 2 characters of UUID to make it a UID
            var client = new Client(args.IRCd, args.Server.TcpClient, null, args.Server.Stream, uid, args.Server, true) {
                Nick  = nick,
                Ident = ident,
                SignonTime = Int32.Parse(signon_time),
                RealName = realname,
                OriginServer = args.Server,
                State = ClientState.Auth,
                ResolvingHost = false,
                Listener = args.Server.Listener
                // TODO IP
                // client.IP = System.Net.IPAddress.Parse(ip)
            };

            // TODO modes
            args.Server.Listener.Clients.Add(client);

            ++args.Server.Listener.ClientCount;
            ++args.Server.Listener.AuthClientCount;

            args.IRCd.Log.Debug($"[SERVER] got new client {nick}");
            return true;
        }

        [Handler("FJOIN", ListenerType.Server)]
        public static bool FjoinHandler(HandlerArgs args) {
            // TODO check if the SID is one we know, maybe specify in config? (???)
            var parts     = args.Line.Split(new char[] { ' ' }, 6);
            var channel   = parts[2];
            var userList  = parts[5].Split(new char[] { ' ' });
            foreach(var user in userList) {
                var userParts = user.Split(new char[] { ',' });
                var modes     = userParts[0];
                var UID       = userParts[1];

                try {
                    Channel chan = null;
                    var client = args.IRCd.GetClientByUUID(UID);
                    try {
                        // Use the channel if it already exists (very unlikely)
                        chan = args.IRCd.ChannelManager.Channels[channel];
                    } catch(KeyNotFoundException) {
                        chan = args.IRCd.ChannelManager.Create(channel);
                    } finally {
                        chan.AddClient(client);
                    }
                    
                } catch(Exception e) {
                    args.IRCd.Log.Debug($"[SERVER] exception in FJOIN handler! {e.ToString()}");
                }

                
            }
            return true;
        }

        [Handler("QUIT", ListenerType.Server)]
        public static bool QuitHandler(HandlerArgs args) {
            return args.IRCd.PacketManager.FindHandler("QUIT", args, ListenerType.Client, true);
        }

        [Handler("SQUIT", ListenerType.Server)]
        public static bool SQuitHandler(HandlerArgs args) {
            // TODO: reason?
            args.Server.IRCd.Log.Info($"Server {args.Server.Name} sent SQUIT; disconnecting");
            args.Server.Disconnect(true);

            return true;
        }

        [Handler("PRIVMSG", ListenerType.Server)]
        public static bool PrivMsgHandler(HandlerArgs args) {
            return args.IRCd.PacketManager.FindHandler("PRIVMSG", args, ListenerType.Client, true);
        }

        [Handler("NOTICE", ListenerType.Server)]
        public static bool NoticeHandler(HandlerArgs args) {
            return args.IRCd.PacketManager.FindHandler("NOTICE", args, ListenerType.Client, true);
        }

        [Handler("FMODE", ListenerType.Server)]
        public static bool FModeHandler(HandlerArgs args) {
            // Trust the server
            args.Force = true;

            // Hack to drop the TS for now
            // TODO: Implement TS
            args.SpacedArgs.RemoveAt(2);

            // Convert all of the UUID args to nicks
            // TODO: May be moved to individual modes
            for(int i = 0; i < args.SpacedArgs.Count(); i++) {
                var arg = args.SpacedArgs[i];

                try {
                    if(args.IRCd.IsUUID(arg)) {
                        args.SpacedArgs[i] = args.IRCd.GetClientByUUID(arg).Nick;
                    }
                } catch(InvalidOperationException) {
                    // We normally check if the user exists in the normal handler
                    // But because MODE could have nicks at any point, the alternative is to check for UUIDs in individual MODEs
                    throw new IrcErrNoSuchTargetNickException(args.Client, args.SpacedArgs[3]);
                }
            }

            // Call the normal mode with the modified args
            return args.IRCd.PacketManager.FindHandler("MODE", args, ListenerType.Client, true);
        }

        [Handler("SVSNICK", ListenerType.Server)]
        public static bool SVSNickHandler(HandlerArgs args) {
            // SVSMODE format: :SID SVSMODE TARGET_UUID NEW_NICK TS
            // NICK    format: NICK NEW_NICK

            var target   = args.Server.IRCd.GetClientByUUID(args.SpacedArgs[1]);
            var new_nick = args.SpacedArgs[2];

            args.Client = target;
            args.Line   = $"NICK {new_nick}";

            return args.IRCd.PacketManager.FindHandler("NICK", args, ListenerType.Client, false);
        }
    }
}
