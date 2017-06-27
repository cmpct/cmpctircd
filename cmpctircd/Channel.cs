using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    class Channel {
        public String name;
        // A dictionary of clients in the room (nick => client)
        private Dictionary<String, Client> clients = new Dictionary<string, Client>();

        public void addClient(Client client) {
            if(inhabits(client)) {
                throw new InvalidOperationException("User is already in the room!");
            }
            clients.Add(client.nick, client);
            Console.WriteLine("Added {0} to {1}", client.nick, name);

            // Tell everyone we've joined
            send_to_room(client, String.Format(":{0} JOIN :{1}", client.mask(), this.name));
            foreach(var room_client in clients) {
                client.write(String.Format(":{0} {1} {2} = {3} :{4}",
                        client.ircd.host,
                        IrcNumeric.RPL_NAMREPLY.Printable(),
                        client.nick,
                        name,
                        room_client.Value.nick
                ));
            }
            client.write(String.Format(":{0} {1} {2} {3} :End of /NAMES list.",
                    client.ircd.host,
                    IrcNumeric.RPL_ENDOFNAMES.Printable(),
                    client.nick,
                    name
            ));

            // TODO: op if size == 1

        }


        public void part(Client client, String reason) {
            if(!inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }
            Console.WriteLine("Removing {0} from {1}", client.nick, name);
            send_to_room(client, String.Format(":{0} PART {1} :{2}", client.mask(), name, reason), true);
            clients.Remove(client.nick);
        }

        public void quit(Client client, String reason) {
            if(!inhabits(client)) {
                throw new InvalidOperationException("User isn't in the room!");
            }

            Console.WriteLine("Removing {0} from {1}", client.nick, name);
            send_to_room(client, String.Format(":{0} QUIT {1}", client.mask(), reason), false);
            clients.Remove(client.nick);
        }


        /*
         * Useful internals (public methods) 
        */
        public void send_to_room(Client client, String message) {
            // Default: assume send to everyone including the client
            send_to_room(client, message, true);
        }
        public void send_to_room(Client client, String message, Boolean sendSelf) {
            Parallel.ForEach(clients, (iClient) => {
                if (!sendSelf && iClient.Value.Equals(client)) {
                    return;
                }
                iClient.Value.write(message);
            });
        }

        public bool inhabits(Client client) {
            return clients.ContainsValue(client);
        }
        public bool inhabits(String nick) {
            return clients.ContainsKey(nick);
        }
        public void add(Client client, String nick) {
            clients.Add(nick, client);
        }
        public void remove(Client client) {
            clients.Remove(client.nick);
        }
        public void remove(String nick) {
            clients.Remove(nick);
        }
        public int size() {
            return clients.Count();
        }

    }
}
