namespace cmpctircd {

    public interface ITranslator {
        // This interface defines a 'Translator'
        // i.e. a set of functions to communicate back out to a server

        // It only handles what to send to the server
        // e.g. a new server has joined - how do we introduce ourselves?
        // e.g. a new user has joined - how do we tell this server about them?
        
        // It does not handle inbound packets
        // e.g. a new client joined on the remote side - how do we handle it locally?
        // e.g. a channel was made on the remote side - how do we handle it locally?
        // [See Server/Inbound/]

        // Return the type of IRCd this translator is for
        ServerType GetOutType();

        // Packets we need to be able to handle

        // Basic handshake
        void Handshake();
        void SendCapab();
        void SyncClient(Client client);
        void SyncChannel(Channel channel);

        //void IntroduceUser();



    }
}