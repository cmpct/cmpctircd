namespace cmpctircd.Configuration.Options {
    public class ConfigurationOptions {
        public string Sid { get; set; }
        public string Host { get; set; }
        public string Network { get; set; }
        public string Description { get; set; }
        public LoggerElement[] Loggers { get; set; }
        public ServerElement[] Servers { get; set; }
        public SocketElement[] Sockets { get; set; }
        public OperatorElement[] Opers { get; set; }
        public ModeElement[] CModes { get; set; }
        public ModeElement[] UModes { get; set; }
        public TlsOptions Tls { get; set; }
        public AdvancedOptions Advanced { get; set; }
        public string[] OperChan { get; set; }
    }
}