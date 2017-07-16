using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    // General exceptions
    public class IrcModeNotEnabledException : Exception {
        public IrcModeNotEnabledException(String mode) {
            // TODO: for debug mode only with logging
            //Console.WriteLine($"{mode} does not exist!");
        }
    }


    // IRC ERR_* response exceptions
    public class IrcErrNoSuchTargetNickException : Exception {
        private Client client;

        public IrcErrNoSuchTargetNickException(Client client, String target) {
            this.client = client;
            client.Write(String.Format(":{0} {1} {2} {3} :No such nick/channel", client.IRCd.host, IrcNumeric.ERR_NOSUCHNICK.Printable(), client.Nick, target));
        }
    }

    public class IrcErrNoSuchTargetChannelException : Exception {
        private Client client;

        public IrcErrNoSuchTargetChannelException(Client client, String target) {
            this.client = client;
            client.Write(String.Format(":{0} {1} {2} {3} :No such nick/channel", client.IRCd.host, IrcNumeric.ERR_NOSUCHCHANNEL.Printable(), client.Nick, target));
        }
    }

    public class IrcErrNoRecipientException : Exception {
        private Client client;

        public IrcErrNoRecipientException(Client client, String command) {
            this.client = client;
            client.Write(String.Format(":{0} {1} {2} :No recipient given ({3})", client.IRCd.host, IrcNumeric.ERR_NORECIPIENT.Printable(), client.Nick, command));
        }
    }

    public class IrcErrNotEnoughParametersException : Exception {
        private Client client;

        public IrcErrNotEnoughParametersException(Client client, String command) {
            this.client = client;
            String currentNick = client.Nick;
            if(String.IsNullOrEmpty(currentNick)) {
                currentNick = "NICK";
            }
            client.Write(String.Format(":{0} {1} {2} {3} :Not enough parameters", client.IRCd.host, IrcNumeric.ERR_NEEDMOREPARAMS.Printable(), currentNick, command));
        }
    }

    public class IrcErrNotRegisteredException : Exception {
        private Client client;

        public IrcErrNotRegisteredException(Client client) {
            this.client = client;
            client.Write(String.Format(":{0} {1} * :You have not registered", client.IRCd.host, IrcNumeric.ERR_NOTREGISTERED.Printable()));
        }
    }

    public class IrcErrAlreadyRegisteredException : Exception {
        private Client client;

        public IrcErrAlreadyRegisteredException(Client client) {
            this.client = client;
            client.Write(String.Format(":{0} {1} * :You may not reregister", client.IRCd.host, IrcNumeric.ERR_ALREADYREGISTERED.Printable()));
        }
    }

    public class IrcErrErroneusNicknameException : Exception {
        private Client client;

        public IrcErrErroneusNicknameException(Client client, String badNick) {
            this.client = client;
            // Ironically, the word 'erroenous' was spelt 'erroneus' (sans 'o') in the RFC.
            // But we'll spell it right when we send it to the user...
            String currentNick = client.Nick;
            if(String.IsNullOrEmpty(currentNick)) {
                currentNick = "NICK";
            }
            client.Write(String.Format(":{0} {1} {2} {3} :Erroneous nickname: Illegal characters", client.IRCd.host, IrcNumeric.ERR_ERRONEUSNICKNAME.Printable(), currentNick, badNick));
        }
    }

    public class IrcErrNicknameInUseException : Exception {
        private Client client;

        public IrcErrNicknameInUseException(Client client, String nick) {
            this.client = client;
            client.Write(String.Format(":{0} {1} * {2} :Nickname is already in use", client.IRCd.host, IrcNumeric.ERR_NICKNAMEINUSE.Printable(), nick));
        }
    }

    public class IrcErrNotOnChannelException : Exception {
        private Client client;

        public IrcErrNotOnChannelException(Client client, String channel) {
            this.client = client;
            client.Write(String.Format(":{0} {1} {2} {3} :You're not on that channel", client.IRCd.host, IrcNumeric.ERR_NOTONCHANNEL.Printable(), client.Nick, channel));
        }
    }

    public class IrcErrChanOpPrivsNeededException : Exception {
        public IrcErrChanOpPrivsNeededException(Client client, String channel) {
            client.Write($":{client.IRCd.host} {IrcNumeric.ERR_CHANOPRIVSNEEDED.Printable()} {client.Nick} {channel} :You must be a channel operator");
        }
    }

    public class IrcErrNoTextToSendException : Exception {
        private Client client;

        public IrcErrNoTextToSendException(Client client) {
            client.Write(String.Format(":{0} {1} {2} :No text to send", client.IRCd.host, IrcNumeric.ERR_NOTEXTTOSEND.Printable(), client.Nick));
        }
    }

     public class IrcErrUnknownCommandException : Exception {
        private Client client;

        public IrcErrUnknownCommandException(Client client, String packet) {
            client.Write($":{client.IRCd.host} {IrcNumeric.ERR_UNKNOWNCOMMAND.Printable()} {client.Nick} {packet} :Unknown command");
        }
    }

}
