namespace cmpctircd.Modes {
    public enum ChannelModeType {
        
        // This enum is used for RPL_ISUPPORT - necessary for (some) irc clients to recognise mode changes
        // http://www.irc.org/tech_docs/005.html
        A = 1, // "Mode that adds or removes a nick or address to a list. Always has a parameter."
        B = 2, // "Mode that changes a setting and always has a parameter."
        C = 3, // "Mode that changes a setting and only has a parameter when set."
        D = 4, // "Mode that changes a setting and never has a parameter."
        PerUser = 5 // For modes which apply to each user, e.g. op (+o)
    }
}
