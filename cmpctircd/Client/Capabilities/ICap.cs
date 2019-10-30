using System.Collections.Generic;

namespace cmpctircd {
    public interface ICap {
        string Name { get; }
        int MinVersion { get; }
        bool Enabled { get; }
        List<string> Parameters { get; }

        CapManager Manager { get; }

        // Client supports this capability
        bool CanEnable { get; }
        bool Enable();
        // Client doesn't support this capability
        bool CanDisable { get; }
        bool Disable();
    }

}