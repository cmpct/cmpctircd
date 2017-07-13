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

        public ConcurrentDictionary<string, Mode> Modes {
            get; set;
        } = new ConcurrentDictionary<string, Mode>();

        public ConcurrentDictionary<Client, ChannelPrivilege> Privileges = new ConcurrentDictionary<Client, ChannelPrivilege>();

        public Channel(ChannelManager manager, IRCd ircd) {
            this.Manager = manager;

            string[] badClasses = { "Mode" };
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                                   .SelectMany(t => t.GetTypes())
                                   .Where(
                                       t => t.IsClass &&
                                       t.Namespace == "cmpctircd.Modes" &&
                                       !badClasses.Contains(t.Name) &&
                                       t.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Count() == 0
                                    );
            foreach(Type className in classes) {
                Mode modeInstance = (Mode) Activator.CreateInstance(Type.GetType(className.ToString()), this);
                string modeChar = modeInstance.Character;

                Modes.TryAdd(modeChar, modeInstance);
                Console.WriteLine($"Creating instance of {modeChar} - {modeInstance.Description}");
            }
        }
        public void AddClient(Client client) {
            if(Inhabits(client)) {
                // TODO: Send ERR_USERONCHANNEL? Not clear if other ircds do this.
                throw new InvalidOperationException("User is already in the room!");
            }
            if(!Clients.TryAdd(client.Nick, client)) { return; }
            Console.WriteLine("Added {0} to {1}", client.Nick, Name);

            // Tell everyone we've joined
            SendToRoom(client, String.Format(":{0} JOIN :{1}", client.Mask, this.Name));
            foreach(var room_client in Clients) {
                client.Write(String.Format(":{0} {1} {2} = {3} :{4}",
                        client.IRCd.host,
                        IrcNumeric.RPL_NAMREPLY.Printable(),
                        client.Nick,
                        Name,
                        room_client.Value.Nick
                ));
            }
            client.Write(String.Format(":{0} {1} {2} {3} :End of /NAMES list.",
                    client.IRCd.host,
                    IrcNumeric.RPL_ENDOFNAMES.Printable(),
                    client.Nick,
                    Name
            ));

            // TODO: op if size == 1

        }


        public void Part(Client client, String reason) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }
            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            SendToRoom(client, String.Format(":{0} PART {1} :{2}", client.Mask, Name, reason), true);
            Clients.TryRemove(client.Nick, out _);

            // Destroy if last user to leave room (TODO: will need modification for cloaking)
            if(Size == 0) {
                Manager.Remove(this.Name);
            }
        }

        public void Quit(Client client, String reason) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            SendToRoom(client, String.Format(":{0} QUIT {1}", client.Mask, reason), false);
            Clients.TryRemove(client.Nick, out _);

            // Destroy if last user to leave room (TODO: will need modification for cloaking)
            if(Size == 0) {
                Manager.Remove(this.Name);
            }
        }

        public ChannelPrivilege Status(Client client) {
            ChannelPrivilege privilege = ChannelPrivilege.Normal;

            // Iterate through all of the modes, finding the highest rank the user holds
            foreach(var mode in Modes) {
                Mode modeObject = mode.Value;
                if(modeObject.Has(client) && modeObject.Level > privilege) {
                    privilege = modeObject.Level;
                }
            }
            return privilege;
        }

        public string[] GetModeStrings(string characters) {
            string provides = "";
            string value = "";
            string args = "";

            foreach(var mode in Modes) {
                bool channelWide = mode.Value.ChannelWide;
                if(channelWide) {
                    provides = mode.Value.Character;
                    value = mode.Value.GetValue();
                    if(!String.IsNullOrWhiteSpace(value) && int.Parse(value) > 0) {
                        characters += provides;
                        if(mode.Value.HasParameters) {
                            args += $"{value} ";
                        }
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
        public void Remove(Client client) {
            Clients.TryRemove(client.Nick, out _);
        }
        public void Remove(String nick) {
            Clients.TryRemove(nick, out _);
        }
        public int Size => Clients.Count();
    }
}
