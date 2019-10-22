using System.Collections.Generic;
using System.Linq;

namespace cmpctircd {
    public class CapManager {
        public Client Client { get; private set; }
        public HashSet<ICap> Caps { get; private set; } = new HashSet<ICap>();

        public bool Negotiating { get; private set; } = false;

        public CapManager(Client client) {
            Client = client;

            // Create instances of all capabilities
            // ...
        }

        // CAP LS: send all available caps
        // CAP LS XXX: send all caps with version <= XXX

        public List<ICap> GetAvailable(int version = 0) {
            var caps = new List<ICap>();

            foreach (var cap in Caps) {
                // Add all caps if version is 0 (not provided)
                // Otherwise, only give caps supported by the client
                if (version == 0 || version >= cap.MinVersion) {
                    caps.Add(cap);
                }
            }

            return caps;
        }

        public bool HasCap(string name) {
            return GetCap(name) != null;
        }

        public ICap GetCap(string name) {
            return Caps.FirstOrDefault(cap => cap.Name == name);
        }

        public void StallRegistration(bool stall) {
            if (Negotiating && stall) {
                // Already stalled and we've been asked to stall
                // Do nothing
                return;
            }

            // Called when we receive a CAP LS / CAP REQ
            // Do not send welcome messages until told to (by way of a CAP END)

            if (Negotiating && !stall) {
                // If we're stalled and now we're told to unstall,
                // let's send the welcome messages.
                Negotiating = false;
                Client.SendWelcome();
            } else {
                // Not currently stalled and being told to enable stalling
                Negotiating = true;
            }
        }
    }
}