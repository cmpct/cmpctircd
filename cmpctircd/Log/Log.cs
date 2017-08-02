using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class Log {

        private IRCd _IRCd;
        private List<BaseLogger> _Loggers;

        public Log() {
            _Loggers = new List<BaseLogger>();
        }

        ~Log() {
            foreach(var logger in _Loggers) {
                logger.Close();
            }
        }

        public enum LogType {
            Error = 4,
            Warn = 3,
            Info = 2,
            Debug = 1
        }

        public enum LoggerType {
            IRC,
            File,
            Stdout
        }

        public void Debug(string msg) => Write(LogType.Debug, msg);
        public void Info(string msg)  => Write(LogType.Info, msg);
        public void Warn(string msg) => Write(LogType.Warn, msg);
        public void Error(string msg) => Write(LogType.Error, msg);

        public void Initialise(IRCd ircd, List<Config.LoggerInfo> loggers) {
            _IRCd = ircd;

            // Setup the loggers if applicable?
            foreach(var cLogger in loggers) {
                Debug($"Initialised logger: Type={cLogger.Type.ToString()} Args={string.Join(";", cLogger.Settings)}");

                BaseLogger log = null;
                switch(cLogger.Type) {
                    case LoggerType.IRC:
                        log = new IRC(_IRCd, cLogger.Level);
                        log.Create(cLogger.Settings);
                    break;

                    case LoggerType.File:
                        log = new File(_IRCd, cLogger.Level);
                        log.Create(cLogger.Settings);
                        break;

                    case LoggerType.Stdout:
                        log = new Stdout(_IRCd, cLogger.Level);
                        log.Create(cLogger.Settings);
                        break;

                    default:
                        continue;
                }

                if(log != null) {
                    _Loggers.Add(log);
                }
            }
        }

        private void Write(LogType type, string message) {
            // Output the message to all of the applicable loggers
            foreach(BaseLogger logger in _Loggers) {
                if(type.CompareTo(logger.Level) < 0) {
                    // Skip this logger if the level of this message is less than its minimum
                    continue;
                }

                var line = logger.Prepare(message, type);
                logger.WriteLine(line, type);
            }
        }

    }
}