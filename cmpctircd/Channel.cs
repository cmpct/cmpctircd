using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cmpctircd.Modes;

namespace cmpctircd {
    public class Channel {
        public String Name { get; set; }
        public ChannelManager Manager { get; private set;}
        // A dictionary of clients in the room (nick => client)
        public ConcurrentDictionary<string, Client> Clients
        {
            get; private set;
        } = new ConcurrentDictionary<string, Client>();

        public Topic Topic { get; set; } = new Topic();

        public ConcurrentDictionary<string, ChannelMode> Modes {
            get; set;
        } = new ConcurrentDictionary<string, ChannelMode>();

        public ConcurrentDictionary<Client, ChannelPrivilege> Privileges = new ConcurrentDictionary<Client, ChannelPrivilege>();
        public int CreationTime { get; set; }

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
                    Console.WriteLine($"{modeInstance.Name} has the same character ({modeChar}) as another channel mode! Skipping.");
                    continue;
                }
                Modes.TryAdd(modeChar, modeInstance);
                // TODO: debug level with logging
                //Console.WriteLine($"Creating instance of {modeChar} - {modeInstance.Description}");
            }


            DateTime date = DateTime.UtcNow;
            CreationTime = (Int32)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public void AddClient(Client client) {
            if(Inhabits(client)) {
                // TODO: Send ERR_USERONCHANNEL? Not clear if other ircds do this.
                throw new InvalidOperationException("User is already in the room!");
            }
            if(Modes["b"].Has(client)) {
                throw new IrcErrBannedFromChanException(client, Name);
            }
            if(!Clients.TryAdd(client.Nick, client)) { return; }
            Console.WriteLine("Added {0} to {1}", client.Nick, Name);

            // Tell everyone we've joined
            SendToRoom(client, String.Format(":{0} JOIN :{1}", client.Mask, this.Name));

            if(Size == 1) {
                Modes["o"].Grant(client, client.Nick, true, true);
                foreach(var mode in client.IRCd.AutoModes) {
                    Modes[mode.Key].Grant(client, "", true, false);
                }
            }
            foreach(var room_client in Clients) {
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

            if(Modes.ContainsKey("Z")) {
                // Attempt to set +Z (only works if applicable)
                Modes["Z"].Grant(client, client.Nick, false, true, true);
            }
        }


        public void Part(Client client, String reason, bool strip = true) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }
            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            SendToRoom(client, String.Format(":{0} PART {1} :{2}", client.Mask, Name, reason), true);
            Remove(client, strip, strip);
        }

        public void Quit(Client client, String reason) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            SendToRoom(client, String.Format(":{0} QUIT {1}", client.Mask, reason), false);
            Remove(client, true);
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
        public void SendToRoom(Client client, String message, Boolean sendSelf) {
            Parallel.ForEach(Clients, (iClient) => {
                if (!sendSelf && iClient.Value.Equals(client)) {
                    return;
                }
                iClient.Value.Write(message);
            });
        }

        public bool Inhabits(Client client) {
            return Clients.Values.Contains(client);
        }
        public bool Inhabits(String nick) {
            return Clients.ContainsKey(nick);
        }
        public void Add(Client client, String nick) {
            Clients.TryAdd(nick, client);
        }

        // For both Remove() functions, consider carefully whether or not the channel object may need to be destroyed once completed
        // e.g. nick changes will NOT require this (so graceful = false), but leaving the room WOULD (so graceful = true)
        public void Remove(Client client, Boolean graceful = true, Boolean stripModes = true) {
            // Strip all modes
            // TODO: may need modification for cloaking
            if(stripModes) {
                foreach(var mode in Modes) {
                    try {
                        if(mode.Value.Has(client))
                            mode.Value.Revoke(client, client.Nick, true, false);
                    }
                    catch {}
                }
                Privileges.TryRemove(client, out _);
            }

            Clients.TryRemove(client.Nick, out _);

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
            if(Size == 0)
                Manager.Remove(Name);
        }
        public int Size => Clients.Count();
    }
}
