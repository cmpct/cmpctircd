using System;
using System.Collections.Generic;

using System.Linq;

namespace cmpctircd {

    public class LogManager {

        private Channel Channel;
        // Map between user's nick and when they first joined
        private List<LogUser> UsersJoined;
        // Buffer of channel messages
        private Dictionary<long, string> Buffer;

        public LogManager(Channel channel) {
            UsersJoined = new List<LogUser>();
            Buffer      = new Dictionary<long, string>();
            Channel     = channel;
        }

        // TODO: Is this needed?
        // TODO: (May be useful for e.g. purging old entries)
        public void ClientJoined(Client client) {
            // Called when a client has joined a channel
            // The caller doesn't know if they've been here before
            // The caller should check that the user has authenticated with NickServ before calling this function
            LogUser info;

            try {
                info = GetLogUserInfo(client.Nick);

                // Update when the user was last seen
                info.LastSeenTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            } catch(InvalidOperationException) {
                // No such user existed
                // Create a record of them joining
                // Constructor sets FirstJoinedTime for us
                info = new LogUser(client.Nick);
                UsersJoined.Add(info);
            }
        }

        public void ClientQuit(Client client) {
            // Called when a client has quit (disconnected)
            // The caller should check that the user has authenticated with NickServ before calling this function
            var info = GetLogUserInfo(client.Nick);

            // TODO: Maybe subtract ping timeout (if that was why they left?)? (or anyway?)
            info.LastQuitTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void NewMessage(string message) {
            // Called with new PRIVMSGs (and NOTICEs?) to a channel to add to buffer
            // TODO: Figure out arguments etc
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Buffer.Add(time, message);
        }

        public List<string> GetDueMessages(Client client) {
            // Returns a list of messages for the caller to send to the client
            var buffer = new List<string>();
            try {
                var info = GetLogUserInfo(client.Nick);

                foreach(var line in Buffer) {
                    // Key is the time the message was sent
                    var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    // bool for whether they've left before, i.e. is this their first time in the channel?
                    // if so, don't send anything
                    // TODO: Could have this be optional
                    var joinedBefore = !(info.LastQuitTime == 0);

                    // bool for whether the current line was sent since they last left
                    // so only send if it happened while they were gone
                    var lineSinceLastHere = line.Key > info.LastQuitTime;

                    // Add it to the buffer to send if they've been in the channel
                    // in the past, and this message is new to them
                    if(joinedBefore && lineSinceLastHere) {
                        buffer.Add(line.Value);
                    }
                }
            } catch(InvalidOperationException) {
                client.IRCd.Log.Debug("Tried to call GetUserLogInfo on client but got NOE; returning empty messages");
            }

            return buffer;
        }
        
        private LogUser GetLogUserInfo(string nick) {
            return UsersJoined.Single(info => info.Nick == nick);
        }

        // TODO: What do we do if the user identifies after joining?
        // TODO: Nothing, send? ...?
        // TODO: Also worth remembering if user somehow logs out, to call ClientQuit first before setting flag so LogManager is informed
    }


}