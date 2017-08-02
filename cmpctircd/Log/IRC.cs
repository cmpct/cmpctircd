using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class IRC : BaseLogger {

        private Channel _Channel;

        public IRC(IRCd ircd, Log.LogType type) : base(ircd, type) {}

        override public void Create(Dictionary<string, string> arguments) {
            var channelName = arguments["channel"];

            if(IRCd.ChannelManager == null) {
                System.Threading.Tasks.Task.Delay(10000).ContinueWith(t => Create(arguments));
                return;
            }

            try {
                // Use the channel if it already exists (very unlikely)
                _Channel = IRCd.ChannelManager.Channels[channelName];
            } catch(KeyNotFoundException) {
                _Channel = IRCd.ChannelManager.Create(channelName);
            }

            _Channel.CanDestroy = false;
        }

        override public void Close() {
            _Channel.CanDestroy = true;
            _Channel.Destroy();
        }

        override public string Prepare(string msg, Log.LogType Type) {
            return $":{IRCd.Host} PRIVMSG {_Channel.Name} :{msg}";
        }

        override public void WriteLine(string msg, Log.LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);
            _Channel.SendToRoom(null, msg);
        }


    }
}