using System.Collections.Generic;

namespace cmpctircd {
    public abstract class BaseLogger {

        public LoggerType Type { get; }
        public LogType Level { get; }
        public IRCd IRCd { get; }

        public BaseLogger(IRCd ircd, LogType level) {
            this.IRCd = ircd;
            this.Level = level;
        }

        abstract public void Create(Dictionary<string, string> arguments); 

        // TODO: partial with the Prepare()?  
        abstract public void WriteLine(string msg, LogType type, bool prepared = true);
        abstract public void Close();

        virtual public string Prepare(string msg, LogType type) {
            return $"[{type.ToString().ToUpper()}] {msg}";
        }
    }
}