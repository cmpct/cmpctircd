using System;
using System.Collections.Generic;
using System.Text;

namespace cmpctircd.Packets {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class Handler : Attribute {
        public string Command { get; }
        public ListenerType Type { get; }
        public ServerType ServerType { get; } = ServerType.Dummy;

        public Handler(string command, ListenerType type) {
            this.Command = command;
            this.Type = type;
        }

        public Handler(string command, ListenerType type, ServerType serverType) : this(command, type) {
            if(type == ListenerType.Server) {
                this.ServerType = serverType;
            } else {
                throw new InvalidOperationException("Can only use ServerType for server packets");
            }
        }
    }
}
