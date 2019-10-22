using System;
using System.Collections.Generic;
using System.Linq;

namespace cmpctircd
{
    /// <summary>
    /// Represents function arguments for a packet handler.
    /// </summary>
    public class HandlerArgs
    {
        public IRCd IRCd { get; }
        // TODO: Make read-only
        public Client Client { get; set; }
        public Server Server { get; }
        // TODO: Make read-only
        public string Line { get; set; }
        public bool Force { get; set; }

        public IList<string> SpacedArgs { get; }
        public string       Trailer { get; }

        public HandlerArgs(IRCd ircd, string line, bool force) {
            IRCd = ircd;
            Line = line;
            Force = force;

            // Split the arguments first by space (we use this a few times)
            SpacedArgs     = Line.Split(new char[] {' '}).ToList();

            // Trailing arguments
            // Denoted by a :, usually one continuous argument
            // Only interested in arguments not in the first chunk
            List<string> modifiedLine = SpacedArgs.ToList();
            string       modifiedLineJoined;

            modifiedLine.RemoveAt(0);
            modifiedLineJoined = String.Join(" ", modifiedLine);
            Trailer            = modifiedLineJoined.Contains(":") ? modifiedLineJoined.Split(new char[] {':'}, 2)[1] : null;

            // Spaced arguments
            // Make a list of the spaced arguments up until we see a colon (:)
            // e.g. LIST #channel -> {"LIST", "#channel"}

            // Check if there's :(U)UID in there, if so, strip it out
            // Those who need it can grab it from the raw Line
            if(SpacedArgs.Any() && SpacedArgs[0].Contains(":")) {
                SpacedArgs.RemoveAt(0);
            }

            SpacedArgs = SpacedArgs.TakeWhile(arg => !arg.Contains(":")).ToList<string>();
        }

        public HandlerArgs(IRCd ircd, Client client, string line, bool force) : this(ircd, line, force) {
            Client = client;
        }

        public HandlerArgs(IRCd ircd, Server server, string line, bool force) : this(ircd, line, force) {
            Server = server;
        }

    }
}
