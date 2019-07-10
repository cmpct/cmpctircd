namespace cmpctircd {
    public enum ClientState {
        Auth    = 1, // Authenticated (logged in, able to run commands)
        PreAuth = 0, // Pre-authentication (limited set of commands, e.g. USER/NICK)
        //Shun  = -1 // Logged in but can now longer run commands
        Disconnected = -1, // User is gone, don't send anything to them or disconnect them again
    }
}
