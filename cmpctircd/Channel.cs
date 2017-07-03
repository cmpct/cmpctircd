using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class Channel {
        public String Name { get; set; }
        // A dictionary of clients in the room (nick => client)
        private Dictionary<String, Client> Clients
        {
            get; set;
        } = new Dictionary<string, Client>();
        public Topic Topic = new Topic();

        public void addClient(Client client) {
            if(inhabits(client)) {
                throw new InvalidOperationException("User is already in the room!");
            }
            Clients.Add(client.Nick, client);
            Console.WriteLine("Added {0} to {1}", client.Nick, Name);

            // Tell everyone we've joined
            send_to_room(client, String.Format(":{0} JOIN :{1}", client.mask(), this.Name));
            foreach(var room_client in Clients) {
                client.write(String.Format(":{0} {1} {2} = {3} :{4}",
                        client.IRCd.host,
                        IrcNumeric.RPL_NAMREPLY.Printable(),
                        client.Nick,
                        Name,
                        room_client.Value.Nick
                ));
            }
            client.write(String.Format(":{0} {1} {2} {3} :End of /NAMES list.",
                    client.IRCd.host,
                    IrcNumeric.RPL_ENDOFNAMES.Printable(),
                    client.Nick,
                    Name
            ));

            // TODO: op if size == 1

        }


        public void part(Client client, String reason) {
            if(!inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }
            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            send_to_room(client, String.Format(":{0} PART {1} :{2}", client.mask(), Name, reason), true);
            Clients.Remove(client.Nick);
        }

        public void quit(Client client, String reason) {
            if(!inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            Console.WriteLine("Removing {0} from {1}", client.Nick, Name);
            send_to_room(client, String.Format(":{0} QUIT {1}", client.mask(), reason), false);
            Clients.Remove(client.Nick);
        }


        /*
         * Useful internals (public methods) 
        */
        public void send_to_room(Client client, String message) {
            // Default: assume send to everyone including the client
            send_to_room(client, message, true);
        }
        public void send_to_room(Client client, String message, Boolean sendSelf) {
            Parallel.ForEach(Clients, (iClient) => {
                if (!sendSelf && iClient.Value.Equals(client)) {
                    return;
                }
                iClient.Value.write(message);
            });
        }

        public bool inhabits(Client client) {
            return Clients.ContainsValue(client);
        }
        public bool inhabits(String nick) {
            return Clients.ContainsKey(nick);
        }
        public void add(Client client, String nick) {
            Clients.Add(nick, client);
        }
        public void remove(Client client) {
            Clients.Remove(client.Nick);
        }
        public void remove(String nick) {
            Clients.Remove(nick);
        }
        public int size() {
            return Clients.Count();
        }

    }
}
