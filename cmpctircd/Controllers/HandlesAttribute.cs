using System;
using System.Collections.Generic;
using System.Text;

namespace cmpctircd.Controllers {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class HandlesAttribute : Attribute {
        public string Command { get; }

        public HandlesAttribute(string command) {
            Command = command;
        }
    }
}
