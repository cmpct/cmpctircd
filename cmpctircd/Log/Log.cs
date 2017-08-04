using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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


        // Useful for finding out if ANY loggers are interested in the a given level (e.g. debug)
        public bool ShouldLogLevel(LogType level) => _Loggers.Exists(log => (log.Level <= level));
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
                    break;

                    case LoggerType.File:
                        log = new File(_IRCd, cLogger.Level);
                        break;

                    case LoggerType.Stdout:
                        log = new Stdout(_IRCd, cLogger.Level);
                        break;

                    default:
                        continue;
                }

                if(log != null) {
                    log.Create(cLogger.Settings);
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
            Parallel.ForEach(_Loggers, (logger) => {
                if(skip != null && skip.Contains(logger)) return;
                if(type.CompareTo(logger.Level) < 0) {
                    // Skip this logger if the level of this message is less than its minimum
                    return;
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
                }
            });
        }

    }
}