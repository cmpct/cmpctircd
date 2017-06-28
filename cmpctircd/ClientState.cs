using System;
namespace cmpctircd {
    public enum ClientState {
        Auth    = 1, // Authenticated (logged in, able to run commands)
        PreAuth = 0, // Pre-authentication (limited set of commands, e.g. USER/NICK)
        //Shun  = -1 // Logged in but can now longer run commands
    }
}
