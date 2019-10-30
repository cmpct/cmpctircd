using System.Collections.Generic;

namespace cmpctircd {
    public class AwayNotify : ICap {
        public string Name { get; private set; } = "away-notify";
        public bool Enabled { get; private set; } = false;
        public int MinVersion { get; private set; } = 0;

        public CapManager Manager { get; private set; }
        public List<string> Parameters { get; } = new List<string>();

        public bool CanEnable => !Enabled;
        public bool CanDisable => Enabled;

        public AwayNotify(CapManager manager) {
            Manager = manager;
        }

        public bool Enable() {
            if (CanEnable) {
                Enabled = true;
                return true;
            }

            return false;
        }

        public bool Disable() {
            if (CanDisable) {
                Enabled = false;
                return true;
            }

            return false;
        }

    }

}