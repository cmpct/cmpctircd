namespace cmpctircd {
    public enum ServerType {
        // Designed as a catch-all default for non-server packets / misconfiguration (won't be called)
        Dummy,

        // Handlers marked with this will be called for (actual) server type
        // e.g. PING may be the same for all servers, so PING may be marked as Any
        Any,

        // Actual server protocols
        InspIRCd20
    }
}
