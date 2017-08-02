using System;
using System.Collections.Generic;

namespace cmpctircd {
    public class Stdout : BaseLogger {

        public Stdout(IRCd ircd, Log.LogType type) : base(ircd, type) {}

        override public void Create(Dictionary<string, string> arguments) {}

        override public void Close() {}

        override public async void WriteLine(string msg, Log.LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);

            await Console.Out.WriteLineAsync(msg);
        }


    }
}