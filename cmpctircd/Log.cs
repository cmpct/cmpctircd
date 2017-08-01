using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class Log {

        private IRCd _IRCd;
        private List<Config.LoggerInfo> _Loggers;

        private Dictionary<string, StreamWriter> _FileHandles;

        public Log(IRCd ircd, List<Config.LoggerInfo> loggers) {
            _IRCd = ircd;
            _Loggers = loggers;
            _FileHandles = new Dictionary<string, StreamWriter>();

            // Setup the loggers if applicable?
            foreach(var logger in _Loggers) {
                Debug($"Initialised logger: Type={logger.Type.ToString()} Args={string.Join(";", logger.Settings)}");

                switch(logger.Type) {
                    case LoggerType.File:
                        var path            = logger.Settings["path"];
                        StreamWriter writer = new StreamWriter(logger.Settings["path"], true);
                        writer.AutoFlush = true;

                        _FileHandles.Add(path, writer);
                        break;

                    default:
                        continue;
                }
            }
        }

        ~Log() {
            foreach(var handle in _FileHandles) {
                // Close all the file handles
                handle.Value.Close();
            }
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
            var line = $"[{type.ToString().ToUpper()}] {message}";

            // Output the message to all of the applicable loggers
            foreach(var logger in _Loggers) {
                switch(logger.Type) {
                    case LoggerType.File:
                        if(logger.Settings.ContainsKey("path") && _FileHandles.ContainsKey(logger.Settings["path"])) {
                            _FileHandles[logger.Settings["path"]].WriteLine(line);
                        }

                        break;

                    case LoggerType.Stdout:
                        Console.WriteLine($"[{type.ToString().ToUpper()}] {message}");
                        break;
                }
            }
        }
        

    }
}