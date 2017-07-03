using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    /// <summary>
    /// Represents IRC numeric responses and errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because there are numbers with less than 3 digits, you should ensure
    /// the format string will always print 3 digits; filling in the missing
    /// ones with zeros.
    /// </para>
    /// <para>
    /// Consult these resources for more numerics:
    /// 
    /// https://www.alien.net.au/irc/irc2numerics.html 
    /// 
    /// http://www.valinor.sorcery.net/docs/rfc1459/6.1-error-replies.html
    /// </para>
    /// </remarks>
    public enum IrcNumeric
    {
        RPL_WELCOME  = 001,
        RPL_YOURHOST = 002,
        RPL_CREATED  = 003,
        RPL_MYINFO   = 004,
        RPL_ISUPPORT = 005,

        RPL_UMODEIS       = 221,
        RPL_RULES         = 232,
        RPL_AWAY          = 301,
        RPL_USERHOST      = 302,
        RPL_UNAWAY        = 305,
        RPL_NOWAWAY       = 306,
        RPL_RULESSTART    = 308,
        RPL_ENDOFRULES    = 309,


        RPL_WHOISUSER     = 311,
        RPL_WHOISSERVER   = 312,
        RPL_WHOISOPERATOR = 313,
        RPL_WHOISIDLE     = 317,
        RPL_ENDOFWHOIS    = 318,
        RPL_WHOISCHANNELS = 319,

        RPL_ENDOFWHO      = 315,
        RPL_CHANNELMODEIS = 324,
        RPL_CREATIONTIME  = 329,
        RPL_NOTOPIC       = 331,
        RPL_TOPIC         = 332,
        RPL_TOPICWHOTIME  = 333,
        RPL_VERSION       = 351,
        RPL_WHOREPLY      = 352,
        RPL_NAMREPLY      = 353,
        RPL_ENDOFNAMES    = 366,
        RPL_BANLIST       = 367,
        RPL_ENDOFBANLIST  = 368,
        RPL_MOTD          = 372,
        RPL_MOTDSTART     = 375,
        RPL_ENDOFMOTD     = 376,
        RPL_WHOISHOST     = 378,
        RPL_YOUREOPER     = 381,
        RPL_HOSTHIDDEN    = 396,
        RPL_WHOISSECURE   = 671,

        // Errors
        ERR_NOSUCHNICK       = 401,
        ERR_NOSUCHCHANNEL    = 403,
        ERR_CANNOTSENDTOCHAN = 404,
        ERR_NOTEXTTOSEND     = 412,
        ERR_UNKNOWNCOMMAND   = 421,
        ERR_ERRONEUSNICKNAME = 432,
        ERR_NICKNAMEINUSE    = 433,
        ERR_USERNOTINCHANNEL = 441,
        ERR_NOTONCHANNEL     = 442,
        ERR_USERONCHANNEL    = 443,
        ERR_NOTREGISTERED    = 451,
        ERR_NEEDMOREPARAMS   = 461,
        ERR_ALREADYREGISTERED = 462,
        ERR_CHANNELISFULL    = 471,
        ERR_BANNEDFROMCHAN   = 474,
        ERR_CHANOPRIVSNEEDED = 482,
        ERR_NOOPERHOST       = 491,
        ERR_OPERONLY         = 520,
    }
}
