using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cmpctircd.Modes;

namespace cmpctircd {
    public class Channel {
        public String Name { get; set; }
        public ChannelManager Manager { get; private set;}
        // A dictionary of clients in the room (nick => client)
        public Dictionary<string, Client> Clients
        {
            get; private set;
        } = new Dictionary<string, Client>();

        public Topic Topic { get; set; } = new Topic();

        public Dictionary<string, ChannelMode> Modes {
            get; set;
        } = new Dictionary<string, ChannelMode>();

        public Dictionary<Client, ChannelPrivilege> Privileges = new Dictionary<Client, ChannelPrivilege>();
        public long CreationTime { get; set; }

        // Used to prevent logging channels from being destroyed
        public bool CanDestroy = true;

        public Channel(ChannelManager manager, IRCd ircd) {
            this.Manager = manager;

            string[] badClasses = { "ChannelMode", "ChannelModeType" };
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(
                                       t => t.IsClass &&
                                       t.Namespace == "cmpctircd.Modes" &&
                                       t.BaseType.Equals(typeof(ChannelMode)) &&
                                       !badClasses.Contains(t.Name)
                                    );
            foreach(Type className in classes) {
                ChannelMode modeInstance = (ChannelMode) Activator.CreateInstance(Type.GetType(className.ToString()), this);
                string modeChar = modeInstance.Character;

                if(Modes.Values.Any(m => m.Character == modeChar)) {
                    ircd.Log.Error($"{modeInstance.Name} has the same character ({modeChar}) as another channel mode! Skipping.");
                    continue;
                }
                Modes.Add(modeChar, modeInstance);
                ircd.Log.Debug($"Creating instance of {modeChar} - {modeInstance.Description}");
            }


            DateTime date = DateTime.UtcNow;
            CreationTime  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        public void AddClient(Client client) {
            if(Inhabits(client)) {
                // TODO: Send ERR_USERONCHANNEL? Not clear if other ircds do this.
                throw new InvalidOperationException("User is already in the room!");
            }
            if(Modes["b"].Has(client)) {
                throw new IrcErrBannedFromChanException(client, Name);
            }
            Clients.Add(client.Nick, client);
            client.IRCd.Log.Debug($"Added {client.Nick} to {Name}");

            // Tell everyone local we've joined
            SendToRoom(client, String.Format(":{0} JOIN :{1}", client.Mask, this.Name), true, false);

            // Newly created channel (this client is the founding user)
            if(Size == 1) {
                Modes["o"].Grant(client, client.Nick, true, true);
                foreach(var mode in client.IRCd.AutoModes) {
                    if(Modes.ContainsKey(mode.Key)) {
                        var modeObject = Modes[mode.Key];
                        if(!modeObject.AllowAutoSet) {
                            client.IRCd.Log.Warn($"Attempting to set non-auto-settable channel mode: {mode.Key}!");
                            client.IRCd.Log.Warn($"You may wish to remove {mode.Key} from <cmodes> in config file.");
                            continue;
                        }
                        modeObject.Grant(client, mode.Value, true, false);
                    } else {
                        client.IRCd.Log.Warn($"Attempting to autoset non-existent channel mode: {mode.Key}!");
                        client.IRCd.Log.Warn($"You may wish to remove {mode.Key} from <cmodes> in config file.");
                    }
                }
            }

            if (!client.RemoteClient) {
                foreach (var room_client in Clients) {
                    var userPriv = Status(room_client.Value);
                    var userSymbol = GetUserSymbol(userPriv);
                    client.Write($":{client.IRCd.Host} {IrcNumeric.RPL_NAMREPLY.Printable()} {client.Nick} = {Name} :{userSymbol}{room_client.Value.Nick}");
                }
                client.Write(String.Format(":{0} {1} {2} {3} :End of /NAMES list.",
                        client.IRCd.Host,
                        IrcNumeric.RPL_ENDOFNAMES.Printable(),
                        client.Nick,
                        Name
                ));

                client.Write($":{client.IRCd.Host} {IrcNumeric.RPL_CREATIONTIME.Printable()} {client.Nick} {Name} {CreationTime}");
            }

            // TODO: Check if this works if a RemoteClient is non TLS
            if(Modes.ContainsKey("Z")) {
                // Attempt to set +Z (only works if applicable)
                Modes["Z"].Grant(client, client.Nick, false, true, true);
            }

            // TODO: Don't send to the originating server
            client.IRCd.ServerLists.ForEach(serverList => serverList.ForEach(
                server => server.SyncChannel(this)
            ));
        }


        public void Part(Client client, String reason, bool strip = true) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }
            client.IRCd.Log.Debug($"Removing {client.Nick} from {Name}");
            SendToRoom(client, String.Format(":{0} PART {1} :{2}", client.Mask, Name, reason), true);
            Remove(client, strip, strip);

            client.IRCd.ServerLists.ForEach(serverList => serverList.ForEach(
                server => server.Write($":{client.UUID} PART {Name} :{reason}")
            ));
        }

        public void Quit(Client client, String reason) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            client.IRCd.Log.Debug($"Removing {client.Nick} from {Name}");
            // Need to remove them from the room, then SendToRoom (not the other way around)
            // This is because if two clients have both disconnected, SendToRoom will have a failed Write on the other client
            // And try to disconnect them - resulting in a SendToRoom from their perspective to tell the clients in this channel they are gone
            // (which includes the user who was disconnecting first!)

            // This way, we call Remove to remove them from the room
            // Then we call Destroy() afterwards which will only destroy the room if conditions are met
            Remove(client, false);
            SendToRoom(client, String.Format(":{0} QUIT {1}", client.Mask, reason), false);
            Destroy(); // if needed

            // TODO: Do we need to tell servers about us quitting?
        }

        public ChannelPrivilege Status(Client client) {
            ChannelPrivilege privilege = ChannelPrivilege.Normal;

            // Iterate through all of the modes, finding the highest rank the user holds
            foreach(var mode in Modes) {
                ChannelMode modeObject = mode.Value;
                if(modeObject.Has(client) && modeObject.ProvidedLevel > privilege && !String.IsNullOrEmpty(modeObject.Symbol)) {
                    privilege = modeObject.ProvidedLevel;
                }
            }
            return privilege;
        }

        public string GetUserSymbol(ChannelPrivilege privilege) {
            foreach(var mode in Modes) {
                if(privilege.CompareTo(mode.Value.ProvidedLevel) == 0 && !String.IsNullOrEmpty(mode.Value.Symbol)) {
                    return mode.Value.Symbol;
                }
            }
            return "";
        }

        public string[] GetModeStrings(string characters) {
            string provides = "";
            string value = "";
            string args = "";

            foreach(var mode in Modes) {
                bool channelWide = mode.Value.ChannelWide;
                if(channelWide) {
                    provides = mode.Value.Character;
                    try {
                        value = mode.Value.GetValue();
                        if(!String.IsNullOrWhiteSpace(value)) {
                            characters += provides;
                            if(mode.Value.HasParameters) {
                                args += $"{value} ";
                            }
                        }
                    } catch(IrcModeNotEnabledException) {
                        // Skip this mode and get another
                        continue;
                    }
                }
            }
            return new string[] { characters, args };

        }


        /*
         * Useful internals (public methods)
        */
        public void SendToRoom(Client client, String message) {
            // Default: assume send to everyone including the client
            SendToRoom(client, message, true);
        }

        public void SendToRoom(Client client, String message, Boolean sendSelf, Boolean sendRemote = true) {
            var RecipientServers = new HashSet<string>();

            foreach (var recipientClient in Clients.Values) {
                if(client != null) {
                    if (!sendSelf && recipientClient.Equals(client)) {
                        continue;
                    }
                }

                if(recipientClient.RemoteClient) {
                    // Don't send this message to the same server twice
                    if (RecipientServers.Contains(recipientClient.OriginServer.Name)) {
                        continue;
                    }

                    // Don't send to users on the same server
                    // TODO: Logic may need updating for hops > 1
                    if (client.RemoteClient && client.OriginServer == recipientClient.OriginServer) {
                        continue;
                    }

                }

                // Don't send to remote clients
                // NOTE: You can't sendSelf if remote client and this is false
                if (!sendRemote && recipientClient.RemoteClient) {
                    continue;
                }

                try {
                    recipientClient.Write(message);
                } catch(System.IO.IOException) { continue; }

                // Note: we do this after the write, so server doesn't miss out next time if there was an exception this time around
                if (recipientClient.RemoteClient) {
                    // Keep a list of servers who have been sent this message, so we don't redundantly send
                    RecipientServers.Add(recipientClient.OriginServer.Name);
                }
            };
        }

        public bool Inhabits(Client client) {
            return Clients.Values.Contains(client);
        }
        public bool Inhabits(String nick) {
            return Clients.ContainsKey(nick);
        }
        public void Add(Client client, String nick) {
            Clients.Add(nick, client);
        }

        // For both Remove() functions, consider carefully whether or not the channel object may need to be destroyed once completed
        // e.g. nick changes will NOT require this (so graceful = false), but leaving the room WOULD (so graceful = true)
        public void Remove(Client client, Boolean graceful = true, Boolean stripModes = true) {
            // Strip all modes
            if(stripModes) {
                foreach(var mode in Modes) {
                    try {
                        if(mode.Value.Has(client))
                            mode.Value.Revoke(client, client.Nick, true, false);
                    }
                    catch {}
                }
                Privileges.Remove(client);
            }

            Clients.Remove(client.Nick);

            if(Modes.ContainsKey("Z")) {
                // Attempt to set +Z (only works if applicable)
                // If we were the last person without TLS to leave, +Z can be set.
                Modes["Z"].Grant(client, client.Nick, false, true, true);
            }

            if (graceful)
                Destroy();
        }

        public void Remove(String nick, Boolean graceful, Boolean stripModes) {
            Remove(Clients[nick], graceful, stripModes);
        }

        public void Destroy() {
            // Destroy if last user to leave room (TODO: may need modification for cloaking)
            if(CanDestroy && Size == 0)
                Manager.Remove(Name);
        }
        public int Size => Clients.Count();
    }
}
