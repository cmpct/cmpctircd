using System;
using System.Collections.Generic;

namespace cmpctircd {
    public class Log {

        private IRCd _IRCd;
        private List<Config.LoggerInfo> _Loggers;

        public Log(IRCd ircd, List<Config.LoggerInfo> loggers) {
            _IRCd = ircd;
            _Loggers = loggers;
            // Setup the loggers if applicable?
        }

        enum LogType {
            Error,
            Warn,
            Info,
            Debug
        }

        public enum LoggerType {
            File,
            Stdout
        }

        public void Debug(string msg) => Write(LogType.Debug, msg);
        public void Info(string msg)  => Write(LogType.Info, msg);
        public void Warn(string msg) => Write(LogType.Warn, msg);
        public void Error(string msg) => Write(LogType.Error, msg);

        private void Write(LogType type, string message) {
            // Output the message to all of the applicable loggers
            foreach(var logger in _Loggers) {
                switch(logger.Type) {
                    case LoggerType.Stdout:
                        Console.WriteLine($"[{type.ToString().ToUpper()}] {message}");
                        break;
                }
            }
        }
        

    }
}