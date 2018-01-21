using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using cmpctircd;

namespace cmpctircd {

    public class Config {

        public struct ConfigData {
            // <ircd>
            public string SID;
            public string Host;
            public string Network;
            public string Description;

            // <server>
            public List<ListenerInfo> Listeners;

            // <tls>
            public string TLS_PfxLocation;
            public string TLS_PfxPassword;

            // <log>
            public List<LoggerInfo> Loggers;

            // <advanced>
            public bool ResolveHostnames;
            public bool RequirePongCookie;
            public int PingTimeout;
            public int MaxTargets;
            public string CloakKey;
            // mode => param
            public Dictionary<string, string> AutoModes;
            public Dictionary<string, string> AutoUModes;

            // <servers>
            public List<ServerLink> ServerLinks;

            // <opers>
            public List<Oper> Opers;
            public List<string> OperChan;

        }

        public struct ListenerInfo {
            public ListenerType Type;
            public IPAddress IP;
            public int Port;
            public bool TLS;
        }

        public struct LoggerInfo {
            public LoggerType Type;
            public LogType Level;
            public Dictionary<string, string> Settings;
        }

        public struct ServerLink {
            public string Host;
            public int Port;
            public string Password;
            public bool TLS;
            public List<string> Masks;
        }

        public struct Oper {
            public string Name;
            public string Password;
            public bool TLS;
            public List<string> Host;
        }

        private string FileName { get; } = "ircd.xml";
        private XmlDocument Xml = new XmlDocument();
        private Log _Log;


        public Config(Log log) {
            _Log = log;
            try {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(FileName, readerSettings);
                Xml.Load(reader);
            } catch(System.IO.IOException e) {
                _Log.Error($"Unable to open the configuration file: {FileName}. Exiting.");
                throw e;
            } catch(XmlException) {
                _Log.Error($"Invalid XML syntax! Exiting.");
                throw;
            } catch(NullReferenceException) {
                _Log.Error($"Missing tag from config file! Exiting.");
                throw;
            }
        }

        public ConfigData Parse() {
            var data    = new ConfigData();
            var configs = Xml.GetElementsByTagName("config");
            var config  = configs.Item(0);

            if(configs.Count > 1) {
                _Log.Warn("Multiple <config> tags in ircd.xml. Only the first will be parsed.");
                _Log.Warn("Please delete one <config> tag from the config file.");
            }

            data.Listeners = new List<ListenerInfo>();
            data.AutoModes = new Dictionary<string, string>();
            data.AutoUModes = new Dictionary<string, string>();
            data.Loggers  = new List<LoggerInfo>();
            data.Opers = new List<Oper>();
            data.OperChan = new List<string>();
            data.ServerLinks = new List<ServerLink>();

            foreach(XmlElement node in config) {
                // Valid types: ircd, server, channelmodes, usermodes, cloak, sockets, log, advanced, opers
                switch(node.Name.ToLower()) {
                    case "ircd":
                        data.SID         = node["sid"].InnerText;
                        data.Host        = node["host"].InnerText;
                        data.Network     = node["network"].InnerText;
                        data.Description = node["desc"].InnerText;

                        // TODO check if conflict
                        if(data.SID == "auto") {
                            data.SID = IRCd.GenerateSID(data.Host, data.Description);
                        }
                        break;

                    case "server":
                        foreach(XmlElement listenNode in node.GetElementsByTagName("listener")) {
                            // <listener> properties: type, ip, port, tls
                            ListenerInfo listener = new ListenerInfo();
                            listener.Type = (ListenerType) Enum.Parse(typeof(ListenerType), listenNode.Attributes["type"].InnerText, true);
                            listener.IP   = IPAddress.Parse(listenNode.Attributes["ip"].InnerText);
                            listener.Port = Int32.Parse(listenNode.Attributes["port"].InnerText);
                            listener.TLS  = Boolean.Parse(listenNode.Attributes["tls"].InnerText);
                            data.Listeners.Add(listener);

                            _Log.Debug($"Got a listener: {listener.IP}:{listener.Port} type={listener.Type} tls={listener.TLS}");
                        }
                        break;

                    case "tls":
                        data.TLS_PfxLocation = node.Attributes["pfx_file"].InnerText;
                        data.TLS_PfxPassword = node.Attributes["pfx_pass"].InnerText;
                        break;

                    case "log":
                        foreach(XmlElement logLine in node.GetElementsByTagName("logger")) {
                            LoggerInfo logger = new LoggerInfo();
                            logger.Settings = new Dictionary<string, string>();

                            foreach(XmlAttribute attribute in logLine.Attributes) {
                                // Check each of the attributes
                                switch(attribute.Name) {
                                    case "type":
                                        // Parse out the type
                                        logger.Type = (LoggerType) Enum.Parse(typeof(LoggerType), logLine.Attributes["type"].InnerText);
                                        break;

                                    case "level":
                                        logger.Level = (LogType) Enum.Parse(typeof(LogType), logLine.Attributes["level"].InnerText);
                                        break;

                                    default:
                                        // For anything else, add it to a dictionary of: <name_of_attribute, value_of_attribute>
                                        logger.Settings.Add(attribute.Name, attribute.InnerText);
                                        break;
                                }
                            }

                            data.Loggers.Add(logger);
                        }
                        break;


                    case "advanced":
                        // <advanced> properties: resolve_hostnames, require_pong_cookie, ping_timeout, max_targets, cloak_key
                        data.ResolveHostnames  = Boolean.Parse(node["resolve_hostnames"].InnerText);
                        data.RequirePongCookie = Boolean.Parse(node["require_pong_cookie"].InnerText);
                        data.PingTimeout       = Int32.Parse(node["ping_timeout"].InnerText);
                        data.MaxTargets        = Int32.Parse(node["max_targets"].InnerText);
                        data.CloakKey          = node["cloak_key"].InnerText;
                        _Log.Info($"Configured with advanced options: RequirePongCookie={data.RequirePongCookie}, PingTimeout={data.PingTimeout}, MaxTargets={data.MaxTargets}, CloakKey={data.CloakKey}");
                        break;

                    case "cmodes":
                        foreach (XmlElement listenNode in node.GetElementsByTagName("mode")) {
                            data.AutoModes.Add(listenNode.Attributes["name"].InnerText, listenNode.Attributes["param"].InnerText);
                        }
                    break;

                    case "umodes":
                        foreach (XmlElement listenNode in node.GetElementsByTagName("mode")) {
                            data.AutoUModes.Add(listenNode.Attributes["name"].InnerText, listenNode.Attributes["param"].InnerText);
                        }
                    break;

                    case "servers":
                        foreach (XmlElement serverNode in node.GetElementsByTagName("server")) {
                            var masks = serverNode.Attributes["masks"].InnerText.Split(' ').ToList();
                            ServerLink server = new ServerLink {
                                Host     = serverNode.Attributes["host"].InnerText,
                                Port     = Int32.Parse(serverNode.Attributes["port"].InnerText),
                                Password = serverNode.Attributes["password"].InnerText,
                                Masks    = masks,
                                TLS      = Boolean.Parse(serverNode.Attributes["tls"].InnerText),
                            };
                            data.ServerLinks.Add(server);
                        }
                    break;

                    case "opers":
                        foreach(XmlElement listenNode in node.GetElementsByTagName("oper")) {
                            Oper oper = new Oper();
                            oper.Host = new List<string>();
                            string[] hosts = listenNode.Attributes["host"].InnerText.Split(' ');
                            foreach(string host in hosts) {
                                oper.Host.Add(host);
                            }
                            oper.Name      = listenNode.Attributes["name"].InnerText;
                            oper.Password  = listenNode.Attributes["password"].InnerText;
                            oper.TLS       = Boolean.Parse(listenNode.Attributes["tls"].InnerText);
                            data.Opers.Add(oper);
                        }
                        foreach(XmlElement listenNode in node.GetElementsByTagName("operjoin")) {
                            string[] chans = listenNode.Attributes["channel"].InnerText.Split(' ');
                            foreach(string chan in chans) {
                                data.OperChan.Add(chan);
                            }
                        }
                        break;
                    default:
                        _Log.Warn($"Unrecognised tag name: {node.Name.ToLower()}");
                        _Log.Warn("Check that you haven't misspelt the tag and/or you're running the latest version?");
                        break;

                }
            }
            return data;
        }

    }

}