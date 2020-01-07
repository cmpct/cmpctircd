using System;
using System.Linq;
using System.Collections.Generic;

namespace cmpctircd.Packets {
    public static class Cap {
        internal class CapStatus {
            public enum CapAction {
                Enable,
                Disable,
            }

            public ICap Cap { get; set; }
            public CapAction Action { get; set; }

            public bool Toggle() {
               switch (Action) {
                    case CapAction.Enable:
                        return Cap.Enable();

                    case CapAction.Disable:
                        return Cap.Disable();

                    default:
                        return false;
                }
            }


            public override string ToString() {
                var cap = string.Empty;

                if (Action == CapAction.Disable) {
                    cap += '-';
                }

                cap += Cap.Name;
                return cap;
            }
        }

        [Handler("CAP", ListenerType.Client)]
        public static bool CapHandler(HandlerArgs args) {
            if (args.SpacedArgs.Count == 1) {
                throw new IrcErrNotEnoughParametersException(args.Client, "CAP");
            }

            var subCommand = args.SpacedArgs[1].ToUpper();

            switch (subCommand) {
                // List available capabilities on server
                case "LS":
                    // Don't send welcome messages yet
                    args.Client.CapManager.StallRegistration(true);
                    CapHandleLS(args);
                    break;

                // Client requests to enable a capability
                case "REQ":
                    // Don't send welcome messages yet
                    args.Client.CapManager.StallRegistration(true);
                    CapHandleReq(args);
                    break;

                case "LIST":
                    CapHandleList(args);
                    break;

                // Client requests to end negotiation
                case "END":
                    // Allow the welcome messages to be sent (and send them)
                    args.Client.CapManager.StallRegistration(false);
                    break;

                default:
                    // Unrecognised!
                    // At this point, try to recover by unstalling registration
                    args.Client.CapManager.StallRegistration(false);
                    throw new IrcErrInvalidCapCommandException(args.Client, subCommand);
            }

            return true;
        }

        public static void CapHandleLS(HandlerArgs args) {
            var version = 0;
            try {
                version = int.Parse(args.SpacedArgs.ElementAtOrDefault(2) ?? "0");

                if (version < 0) {
                    throw new FormatException("CAP version cannot be negative");
                }

                // Set the client's version for later use too
                args.Client.CapManager.Version = version;
            } catch (FormatException) {
                // TODO: Make FormatException an inner exception
                throw new IrcErrNotEnoughParametersException(args.Client, "CAP");
            }

            var caps = args.Client.CapManager.GetAvailable(version);
            var capString = string.Empty;
            var parameters = string.Empty;

            if (args.Client.CapManager.Version >= 302) {
                // CAP 302: "capability values" support
                // https://ircv3.net/specs/core/capability-negotiation#cap-ls-version-features
                // Check if we have parameters to send
                var capsWithArgs = caps.Where(cap => cap.Parameters.Any());

                // Assemble a string like: CAP LS :sasl=PLAIN,EXTERNAL away-notify
                foreach (var cap in capsWithArgs) {
                    parameters = string.Join(",", cap.Parameters);
                    capString += $"{cap.Name}={parameters}";
                }

                // Add on those without parameters
                capString += string.Join(" ", caps.Where(cap => !cap.Parameters.Any()).Select(cap => cap.Name));
            } else {
                // Simply collect the names of the available capabilities
                capString = string.Join(" ", caps.Select(cap => cap.Name));
            }

            args.Client.Write($":{args.IRCd.Host} CAP {args.Client.NickIfSet()} LS :{capString}");
        }

        public static void CapHandleReq(HandlerArgs args) {
            var caps = new List<string>();
            var ackCaps = new List<CapStatus>(); // Caps we're going to ACK (successfully added)
            var badCaps = new List<CapStatus>(); // Caps we're going to NAK (could not be added)

            if (args.Trailer != null) {
                // Syntax: CAP REQ :<cap1> <cap2> ...
                caps = args.Trailer.Split(" ").ToList();
            } else {
                // Syntax: CAP REQ <cap1>
                if (args.SpacedArgs.Count() < 3) {
                    throw new IrcErrInvalidCapCommandException(args.Client, "CAP REQ");
                }

                if (args.SpacedArgs.Count() > 3) {
                    // Complain if they sent: CAP REQ <cap1> <cap2> ...
                    // rather than just CAP REQ <cap1>
                    args.Client.Write($":{args.IRCd.Host} NOTICE {args.Client.NickIfSet()} :Your client is sending invalid CAP REQ packets, please report this as a bug!");
                }

                // Tolerate CAP REQ <cap1> <cap2> ...
                // (We'd prefer a colon though)
                caps.AddRange(args.SpacedArgs.Skip(2));
            }

            var manager = args.Client.CapManager;
            foreach (var cap in caps) {
                try {
                    // Find cap with this name
                    var name = cap;
                    var success = false;
                    var symbol = string.Empty;
                    var capStatus = new CapStatus();

                    if (cap.StartsWith('-')) {
                        // This cap needs to be disabled
                        // e.g. CAP REQ :-away-notify
                        capStatus.Action = CapStatus.CapAction.Disable;

                        // Skip first character (-)
                        name = cap.Substring(1);
                        symbol = "-";

                        // Find it
                        capStatus.Cap = manager.Caps.First(c => c.Name == name);

                        // Disable (check if we can)
                        success = capStatus.Cap.CanDisable;
                    } else {
                        // We're enabling the capability here
                        capStatus.Action = CapStatus.CapAction.Enable;

                        // Find it
                        capStatus.Cap = manager.Caps.First(c => c.Name == name);

                        // Enable (check if we can)
                        success = capStatus.Cap.CanEnable;
                    }

                    if (success) {
                        ackCaps.Add(capStatus);
                    } else {
                        // A CAP can only be NAKed if the "requested capability change was rejected"
                        // Where the CAP was already enabled, we must pretend it was a successful change (keep enabled); do not NAK.
                        badCaps.Add(capStatus);
                    }
                } catch (InvalidOperationException) {
                    // Invalid capability requested
                    // Create a mock cap with the name filled in as the invalid name
                    var mock = new MockCap(manager);
                    var mockStatus = new CapStatus();

                    mock.Name = cap;
                    mockStatus.Cap = mock;
                    badCaps.Add(mockStatus);
                }
            }

            // Send out the ACKs (successful)
            if (ackCaps.Any()) {
                args.Client.Write($":{args.IRCd.Host} CAP {args.Client.NickIfSet()} ACK: {string.Join(" ", ackCaps)}");
            }

            // We've told the client which CAPs are enabled, now actually enable/disable it
            foreach (var cap in ackCaps) {
                cap.Toggle();
            }

            // Send out the NAKs (unsuccessful)
            if (badCaps.Any()) {
                args.Client.Write($":{args.IRCd.Host} CAP {args.Client.NickIfSet()} NAK: {string.Join(" ", badCaps)}");
            }

            return;
        }

        public static void CapHandleList(HandlerArgs args) {
            var enabled = args.Client.CapManager.GetEnabled().Select(cap => cap.Name);
            var enabledString = string.Join(" ", enabled);

            args.Client.Write($":{args.IRCd.Host} CAP {args.Client.NickIfSet()} LIST :{enabledString}");
        }

    }
}
