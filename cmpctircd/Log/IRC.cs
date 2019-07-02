using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class IRC : BaseLogger {

        private Channel _Channel;
        private Client _PsuedoClient;

        public IRC(IRCd ircd, LogType type) : base(ircd, type) {}

        override public void Create(Dictionary<string, string> arguments) {
            var channelName = arguments["channel"];

            if(IRCd.ChannelManager == null) {
                System.Threading.Tasks.Task.Delay((int) TimeSpan.FromSeconds(10).TotalMilliseconds).ContinueWith(t => Create(arguments));
                return;
            }

            try {
                // Use the channel if it already exists (very unlikely)
                _Channel = IRCd.ChannelManager.Channels[channelName];
            } catch(KeyNotFoundException) {
                _Channel = IRCd.ChannelManager.Create(channelName);
            }

            // Create a psuedo client with which to send messages
            var pseudoTcpClient = new System.Net.Sockets.TcpClient();
            _PsuedoClient = new Client(IRCd, pseudoTcpClient, null, pseudoTcpClient.GetStream());
            _PsuedoClient.Nick = IRCd.Host;

            // Apply any modes - if we have them
            if(arguments.ContainsKey("modes")) {
                // We expect the modes in the format "+<modes>" (e.g. modes="+nt")
                // (or "-<modes>" but that's not likely)
                // TODO: Allow modes with arguments (e.g. +k; only becomes pressing once that is implemented)
                var modes     = arguments["modes"];
                var modifiers = new List<string>() { "+", "-" };
                var modifier  = "";
                foreach (var modeCharacter in modes.ToCharArray()) {
                    if (modifiers.Contains(modeCharacter.ToString())) {
                        modifier = modeCharacter.ToString();
                        continue;
                    }

                    var mode = false;
                    try {
                        switch (modifier) {
                            case "+":
                                mode = _Channel.Modes[modeCharacter.ToString()].Grant(_PsuedoClient, null, true, true, false);
                                break;

                            case "-":
                                mode = _Channel.Modes[modeCharacter.ToString()].Revoke(_PsuedoClient, null, true, true, false);
                                break;

                            default:
                                IRCd.Log.Warn("No mode prefix +/- in logger IRC settings; please add one xor the other (e.g. +nt)");
                                break;
                        }
                    } catch (Exception) {}

                    if(!mode) {
                        IRCd.Log.Error($"Could not set mode {modeCharacter.ToString()} on {_Channel.Name} for logging!");
                        IRCd.Log.Error($"Please review your log config (modes) for the IRC logger because of this failure.");
                    }
                }
            }

            _Channel.CanDestroy = false;
        }

        override public void Close() {
            _Channel.CanDestroy = true;
            _Channel.Destroy();
        }

        override public string Prepare(string msg, LogType Type) {
            return $":{IRCd.Host} PRIVMSG {_Channel.Name} :{msg}";
        }

        override public void WriteLine(string msg, LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);
            _Channel.SendToRoom(_PsuedoClient, msg);
        }


    }
}