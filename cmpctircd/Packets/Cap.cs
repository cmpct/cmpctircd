using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace cmpctircd.Packets {
    public static class Cap {


        public enum CapAction {
            Enable,
            Disable
        }

        class CapStatus {
            public ICap Cap;
            public CapAction Action;

            public override string ToString() {
                var cap = "";

                if (Action == CapAction.Disable) {
                    cap += '-';
                }

                cap += Cap.Name;
                return cap;
            }
        }

        [Handler("CAP", ListenerType.Client)]
        public static bool CapHandler(HandlerArgs args) {
            if (args.SpacedArgs.Count == 0) {
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
                throw new IrcErrNotEnoughParametersException(args.Client, "CAP");
            }

            var caps = string.Join(" ", args.Client.CapManager.GetAvailable(version).Select(cap => cap.Name));

            args.Client.Write($"CAP {args.Client.NickIfSet()} LS :{caps}");
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
                    var capStatus = new CapStatus();
                    var symbol = "";

                    if (cap.First() == '-') {
                        // This cap needs to be disabled
                        // e.g. CAP REQ :-away-notify
                        capStatus.Action = CapAction.Disable;

                        // Skip first character (-)
                        name = cap.Substring(1);
                        symbol = "-";

                        // Find it
                        capStatus.Cap = manager.Caps.First(c => c.Name == name);

                        // Disable (check if we can)
                        success = capStatus.Cap.CanDisable;
                    } else {
                        // We're enabling the capability here
                        capStatus.Action = CapAction.Enable;

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
                args.Client.Write($"CAP {args.Client.NickIfSet()} ACK: {string.Join(" ", ackCaps)}");
            }

            // We've told the client which CAPs are enabled, now actually enable/disable it
            foreach (var cap in ackCaps) {
                switch (cap.Action) {
                    case CapAction.Enable:
                        cap.Cap.Enable();
                        break;

                    case CapAction.Disable:
                        cap.Cap.Disable();
                        break;
                }
            }

            // Send out the NAKs (unsuccessful)
            if (badCaps.Any()) {
                args.Client.Write($"CAP {args.Client.NickIfSet()} NAK: {string.Join(" ", badCaps)}");
            }

            return;
        }

        public static void CapHandleList(HandlerArgs args) {
            var enabled = args.Client.CapManager.GetEnabled().Select(cap => cap.Name);
            var enabledString = string.Join(" ", enabled);

            args.Client.Write($"CAP {args.Client.NickIfSet()} LIST :{enabledString}");
        }

    }
}