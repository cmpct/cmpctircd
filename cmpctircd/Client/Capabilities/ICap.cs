namespace cmpctircd {
    public interface ICap {
        string Name { get; }
        int MinVersion { get; }
        bool Enabled { get; }

        CapManager Manager { get; }

        // Client supports this capability
        bool Enable();
        // Client doesn't support this capability
        //void Disable();

    }

}