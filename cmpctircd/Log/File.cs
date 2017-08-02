using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class File : BaseLogger {

        StreamWriter writer;

        public File(IRCd ircd, Log.LogType type) : base(ircd, type) {}

        override public void Create(Dictionary<string, string> arguments) {
            var path  = arguments["path"];
            writer    = new StreamWriter(path, true);
            writer.AutoFlush = true;
        }

        override public void Close() {
            writer.Close();
        }

        override public async void WriteLine(string msg, Log.LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);

            await writer.WriteLineAsync(msg);
        }


    }
}