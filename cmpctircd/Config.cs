using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Collections.Generic;

namespace cmpctircd {

    public class Config {

        public struct ConfigData {
            // <ircd>
            public string Host;
            public string Network;
            public string Description;

            // <server>
            public List<ListenerInfo> Listeners;

            // <advanced>
            public bool RequirePongCookie;
            public int PingTimeout;
            public int MaxTargets;

        }

        public struct ListenerInfo {
            public IPAddress IP;
            public int Port;
            public bool TLS;
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

                    case "advanced":
                        // <advanced> properties: require_pong_cookie, ping_timeout, max_targets
                        data.RequirePongCookie = Boolean.Parse(node["require_pong_cookie"].InnerText);
                        data.PingTimeout       = Int32.Parse(node["ping_timeout"].InnerText);
                        data.MaxTargets        = Int32.Parse(node["max_targets"].InnerText);
                        Console.WriteLine($"Configured with advanced options: RequirePongCookie={data.RequirePongCookie}, PingTimeout={data.PingTimeout}, MaxTargets={data.MaxTargets}");
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