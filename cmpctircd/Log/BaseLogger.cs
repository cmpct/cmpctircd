using System.Collections.Generic;

namespace cmpctircd {
    public abstract class BaseLogger {

        public Log.LoggerType Type { get; }
        public Log.LogType Level { get; }
        public IRCd IRCd { get; }

        public BaseLogger(IRCd ircd, Log.LogType level) {
            this.IRCd = ircd;
            this.Level = level;
        }

        abstract public void Create(Dictionary<string, string> arguments); 

        // TODO: partial with the Prepare()?  
        abstract public void WriteLine(string msg, Log.LogType type, bool prepared = true);
        abstract public void Close();

        virtual public string Prepare(string msg, Log.LogType type) {
            return $"[{type.ToString().ToUpper()}] {msg}";
        }
    }
}