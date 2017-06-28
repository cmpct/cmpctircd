using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    [Serializable]
    class Errors
    {

        public class IrcErrNoSuchTargetException : Exception
        {
            private Client client;

            public IrcErrNoSuchTargetException(Client client, String target)
            {
                this.client = client;
                // >> :irc.cmpct.info 401 Josh dhd :No such nick/channel
                client.write(String.Format(":{0} {1} {2} {3} :No such nick/channel", client.ircd.host, IrcNumeric.ERR_NOSUCHNICK.Printable(), client.nick, target));
            }
        }


        public class IrcErrNotEnoughParametersException : Exception
        {
            private Client client;

            public IrcErrNotEnoughParametersException(Client client)
            {
                this.client = client;
                client.write(String.Format(":{0} {1} {2} TOPIC :Not enough parameters", client.ircd.host, IrcNumeric.ERR_NEEDMOREPARAMS.Printable(), client.nick));
            }
        }

        public class IrcErrNotRegisteredException : Exception
        {
            private Client client;

            public IrcErrNotRegisteredException(Client client)
            {
                this.client = client;
                client.write(String.Format(":{0} {1} * :You have not registered", client.ircd.host, IrcNumeric.ERR_NOTREGISTERED.Printable()));
            }
        }
    }
}
