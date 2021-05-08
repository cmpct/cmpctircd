namespace cmpctircd
{
    public class IrcContext
    {
        public object Sender { get; set; }

        public IRCd Daemon { get; set; }

        public HandlerArgs Args { get; set; }
    }
}
