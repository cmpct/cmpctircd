using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Collections.Generic;

using cmpctircd;

namespace cmpctircd {

    public class Config {

        public struct ConfigData {
            // <ircd>
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
            public bool RequirePongCookie;
            public int PingTimeout;
            public int MaxTargets;
            public string CloakKey;
            // mode => param
            public Dictionary<string, string> AutoModes;
            public Dictionary<string, string> AutoUModes;

        }

        public struct ListenerInfo {
            public IPAddress IP;
            public int Port;
            public bool TLS;
        }

        public struct LoggerInfo {
            public cmpctircd.Log.LoggerType Type;
            public cmpctircd.Log.LogType Level;
            public Dictionary<string, string> Settings;
        }

        private string FileName { get; } = "ircd.xml";
        private XmlDocument Xml = new XmlDocument();


        public Config() {
            try {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(FileName, readerSettings);
                Xml.Load(reader);
            } catch(System.IO.IOException e) {
                Console.WriteLine($"Unable to open the configuration file: {FileName}");
                throw e;
            }
        }

        public ConfigData Parse() {
            var data   = new ConfigData();
            var config = Xml.GetElementsByTagName("config").Item(0);
            data.Listeners = new List<ListenerInfo>();
            data.AutoModes = new Dictionary<string, string>();
            data.AutoUModes = new Dictionary<string, string>();
            data.Loggers  = new List<LoggerInfo>();

            foreach(XmlElement node in config) {
                // Valid types: ircd, server, channelmodes, usermodes, cloak, sockets, log, advanced, opers
                switch(node.Name.ToLower()) {
                    case "ircd":
                        data.Host        = node["host"].InnerText;
                        data.Network     = node["network"].InnerText;
                        data.Description = node["desc"].InnerText;
                        break;

                    case "server":
                        foreach(XmlElement listenNode in node.GetElementsByTagName("listener")) {
                            // <listener> properties: ip, port, tls
                            ListenerInfo listener = new ListenerInfo();
                            listener.IP   = IPAddress.Parse(listenNode.Attributes["ip"].InnerText);
                            listener.Port = Int32.Parse(listenNode.Attributes["port"].InnerText);
                            listener.TLS  = Boolean.Parse(listenNode.Attributes["tls"].InnerText);
                            data.Listeners.Add(listener);

                            Console.WriteLine($"Got a listener: {listener.IP}:{listener.Port} tls={listener.TLS}");
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
                                        logger.Type = (Log.LoggerType) Enum.Parse(typeof(Log.LoggerType), logLine.Attributes["type"].InnerText);
                                        break;

                                    case "level":
                                        logger.Level = (Log.LogType) Enum.Parse(typeof(Log.LogType), logLine.Attributes["level"].InnerText);
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
                        // <advanced> properties: require_pong_cookie, ping_timeout, max_targets
                        data.RequirePongCookie = Boolean.Parse(node["require_pong_cookie"].InnerText);
                        data.PingTimeout       = Int32.Parse(node["ping_timeout"].InnerText);
                        data.MaxTargets        = Int32.Parse(node["max_targets"].InnerText);
                        data.CloakKey          = node["cloak_key"].InnerText;
                        Console.WriteLine($"Configured with advanced options: RequirePongCookie={data.RequirePongCookie}, PingTimeout={data.PingTimeout}, MaxTargets={data.MaxTargets}, CloakKey={data.CloakKey}");
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

                    default:
                        Console.WriteLine($"Unrecognised tag name: {node.Name.ToLower()}");
                        break;

                }
            }
            return data;
        }

    }

}