using System.Collections.Generic;

namespace cmpctircd {
    public class MockCap : ICap {
        public string Name { get; set; } = "";
        public bool Enabled { get; private set; } = false;
        public int MinVersion { get; private set; } = 0;

        public CapManager Manager { get; private set; }
        public List<string> Parameters { get; } = new List<string>();

        public bool CanEnable => !Enabled;
        public bool CanDisable => Enabled;

        public MockCap(CapManager manager) {
            Manager = manager;
        }

        public bool Enable() {
            Enabled = true;
            return true;
        }

        public bool Disable() {
            Enabled = false;
            return true;
        }

    }

}