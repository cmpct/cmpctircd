using System;
using System.Collections.Generic;
using System.Text;

namespace cmpctircd.Controllers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ControllerAttribute : Attribute {
        public ListenerType Type { get; }
        public ServerType ServerType { get; } = ServerType.Dummy;

        public ControllerAttribute(ListenerType type) {
            Type = type;
        }

        public ControllerAttribute(ListenerType type, ServerType serverType) : this(type) {
            if(type == ListenerType.Server) {
                ServerType = serverType;
            } else {
                throw new InvalidOperationException("Can only use ServerType for server controllers.");
            }
        }
    }
}
