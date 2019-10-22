namespace cmpctircd {
    public class AwayNotify : ICap {
        public string Name { get; private set; } = "away-notify";
        public bool Enabled { get; private set; } = false;
        public int MinVersion { get; private set; } = 0;

        public CapManager Manager { get; private set; }

        public AwayNotify(CapManager manager) {
            Manager = manager;
        }

        public bool Enable() {
            if (!Enabled) {
                Enabled = true;
            }

            return Enabled;
        }

    }

}