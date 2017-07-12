using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public class Channel {
        public String Name { get; set; }
        // A dictionary of clients in the room (nick => client)
        public ConcurrentDictionary<String, Client> Clients
        {
            get; private set;
        } = new ConcurrentDictionary<string, Client>();
        public Topic Topic { get; set; } = new Topic();

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
        }

        public void Quit(Client client, String reason) {
            if(!Inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            SendToRoom(client, String.Format(":{0} QUIT {1}", client.Mask, reason), false);
            Clients.TryRemove(client.Nick, out _);
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
