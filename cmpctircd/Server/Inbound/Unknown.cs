// TODO: Fix namespaces?
namespace cmpctircd.Packets {
    public static class Unknown {
        
        [Handler("STARTTLS", ListenerType.Server, ServerType.Unknown)]
        public static bool StartTlsHandler(HandlerArgs args) {
            // TODO: Figure out what UnrealIRCd wants in response to this
            return true;
        }
    }
}