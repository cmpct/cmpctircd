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

            // Tell everyone we've joined
            send_to_room(client, String.Format(":{0} JOIN :{1}", client.mask(), this.name));
            // TODO: op if size == 1

        }



        /*
         * Useful internals (public methods) 
        */
        public void send_to_room(Client client, String message) {
            clients.AsParallel().ForAll(pair => { pair.Value.write(message); });
        }
        public bool inhabits(Client client) {
            return clients.ContainsValue(client);
        }
        public int size() {
            return clients.Count();
        }

    }
}
