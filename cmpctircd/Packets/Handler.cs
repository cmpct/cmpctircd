using System;
using System.Collections.Generic;
using System.Text;

namespace cmpctircd.Packets {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class Handler : Attribute {
        public string Command { get; }
        public ListenerType Type { get; }

        public Handler(string command, ListenerType type) {
            this.Command = command;
            this.Type = type;
        }
    }
}
