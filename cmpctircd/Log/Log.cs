using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class Log {

        private IRCd _IRCd;
        private List<Config.LoggerInfo> _Loggers;

        private Dictionary<string, Channel> _ChannelHandles;
        private Dictionary<string, StreamWriter> _FileHandles;

        public Log(IRCd ircd, List<Config.LoggerInfo> loggers) {
            _IRCd = ircd;
            _Loggers = loggers;
            _FileHandles = new Dictionary<string, StreamWriter>();
            _ChannelHandles = new Dictionary<string, Channel>();

            // Setup the loggers if applicable?
            foreach(var logger in _Loggers) {
                Debug($"Initialised logger: Type={logger.Type.ToString()} Args={string.Join(";", logger.Settings)}");

                switch(logger.Type) {
                    case LoggerType.IRC:
                        string channelName = logger.Settings["channel"];
                        Channel channel;
                        try {
                            // Use the channel if it already exists (very unlikely)
                            channel = ircd.ChannelManager.Channels[channelName];
                        } catch(KeyNotFoundException) {
                            channel = ircd.ChannelManager.Create(channelName);
                        }

                        _ChannelHandles.Add(channelName, channel);
                    break;

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

        private void Write(LogType type, string message) {
            var line = $"[{type.ToString().ToUpper()}] {message}";

            // Output the message to all of the applicable loggers
            foreach(var logger in _Loggers) {
                if(type.CompareTo(logger.Level) < 0) {
                    // Skip this logger if the level of this message is less than its minimum
                    continue;
                }

                switch(logger.Type) {
                    case LoggerType.IRC:
                        var channelName = logger.Settings["channel"];
                        if(logger.Settings.ContainsKey("channel") && _ChannelHandles.ContainsKey(channelName)) {
                            _ChannelHandles[channelName].SendToRoom(null, $":{_IRCd.Host} PRIVMSG {channelName} :{line}");
                        }
                        break;

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