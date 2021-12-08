namespace cmpctircd.Configuration.Options {
    public class AdvancedOptions {
        public bool ResolveHostnames { get; set; }
        public bool RequirePongCookie { get; set; }
        public int PingTimeout { get; set; }
        public int MaxTargets { get; set; }
        public CloakOptions Cloak { get; set; }
    }

    public class CloakOptions {
        public string Key { get; set; }
        public string Prefix { get; set; }
        public int DomainParts { get; set; }
        public bool Full { get; set; }
    }
}