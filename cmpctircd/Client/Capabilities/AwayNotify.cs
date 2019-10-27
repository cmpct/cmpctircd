namespace cmpctircd {
    public class AwayNotify : ICap {
        public string Name { get; private set; } = "away-notify";
        public bool Enabled { get; private set; } = false;
        public int MinVersion { get; private set; } = 0;

        public CapManager Manager { get; private set; }

        public bool CanEnable => !Enabled;
        public bool CanDisable => Enabled;

        public AwayNotify(CapManager manager) {
            Manager = manager;
        }

        // TODO: Base impl?
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