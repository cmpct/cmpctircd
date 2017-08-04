using System;
using System.Collections.Generic;

namespace cmpctircd {
    public class Stdout : BaseLogger {

        public Stdout(IRCd ircd, LogType type) : base(ircd, type) {}

        override public void Create(Dictionary<string, string> arguments) {}

        override public void Close() {}

        override public void WriteLine(string msg, LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);

            Console.Out.WriteLine(msg);
        }


    }
}