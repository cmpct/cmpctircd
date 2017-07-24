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

            // <advanced>
            public bool RequirePongCookie;
            public int PingTimeout;
            public int MaxTargets;

        }

        private string FileName { get; } = "ircd.xml";
        private XmlDocument Xml = new XmlDocument();


        public Config() {
            try {
                Xml.Load(FileName);
            } catch(System.IO.IOException e) {
                Console.WriteLine($"Unable to open the configuration file: {FileName}");
                throw e;
            }
        }

        public ConfigData Parse() {
            var data   = new ConfigData();
            var config = Xml.GetElementsByTagName("config").Item(0);

            foreach(XmlElement node in config) {
                // Valid types: ircd, server, channelmodes, usermodes, cloak, sockets, log, advanced, opers
                switch(node.Name.ToLower()) {
                    case "ircd":
                        data.Host        = node["host"].InnerText;
                        data.Network     = node["network"].InnerText;
                        data.Description = node["desc"].InnerText;
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