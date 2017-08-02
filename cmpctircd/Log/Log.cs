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

        private void Write(LogType type, string message, List<BaseLogger> skip = null) {
            if(_Loggers.Count == 0) {
                // No Loggers currently registered
                // Create a temporary Stdout
                var stdout = new Stdout(_IRCd, LogType.Info);
                if(type.CompareTo(stdout.Level) < 0) return;
                stdout.WriteLine(stdout.Prepare(message, type), type);
                return;
            }

            // Output the message to all of the applicable loggers
            foreach(BaseLogger logger in _Loggers) {
                if(skip != null && skip.Contains(logger)) continue;
                if(type.CompareTo(logger.Level) < 0) {
                    // Skip this logger if the level of this message is less than its minimum
                    continue;
                }

                try {
                    var line = logger.Prepare(message, type);
                    logger.WriteLine(line, type);
                } catch(Exception e) {
                    if(skip == null || skip.Count == 0) {
                        skip = new List<BaseLogger>();
                    }
                    skip.Add(logger);

                    Write(LogType.Debug, $"Unable to write to logger: {logger.Type}; {e.GetType()}", skip);
                    continue;
                }
            }
        }

    }
}