using System;
using System.Collections.Generic;
using System.IO;

namespace cmpctircd {
    public class File : BaseLogger {

        StreamWriter writer;

        public File(IRCd ircd, LogType type) : base(ircd, type) {}

        override public void Create(IReadOnlyDictionary<string, string> arguments) {
            var path  = arguments["path"];
            writer    = new StreamWriter(path, true) {
                AutoFlush = true
            };
        }

        override public void Close() {
            writer.Close();
        }

        override public void WriteLine(string msg, LogType type, bool prepared = true) {
            if(!prepared) msg = Prepare(msg, type);

            writer.WriteLine(msg);
        }


    }
}