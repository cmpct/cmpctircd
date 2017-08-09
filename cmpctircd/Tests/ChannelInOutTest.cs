using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;
using Meebey.SmartIrc4net;

namespace cmpctircd.Tests {
    [TestFixture]
    public class ChannelInOutTest {
        /// <summary>
        /// Basic set of tests to ensure that a user can JOIN and PART a channel successfully.
        /// </summary>

        public static IrcClient irc = new IrcClient();
        public static string channel = "#test";
        public static bool joined = false;
        public static bool parted = false;
        public static bool setup  = false;

        [Test, Order(1)]
        [NonParallelizable]
        public void CheckJoinedChannel() {
            // Wait 5 seconds before verifying we've joined the room
            var constraint = Is.True.After((int) new TimeSpan(0, 0, 5).TotalMilliseconds);
            Assert.That(() => ChannelInOutTest.joined, constraint);
        }

        [Test, Order(2)]
        [NonParallelizable]
        public void CheckPartedChannel() {
            // Wait 5 seconds before verifying we've parted the room
            var constraint = Is.True.After((int) new TimeSpan(0, 0, 5).TotalMilliseconds);
            Assert.That(() => ChannelInOutTest.parted, constraint);      
        }

        public static void OnJoin(object sender, JoinEventArgs e) {
            if(e.Channel == channel) {
                joined = true;
            }
        }

        public static void OnPart(object sender, PartEventArgs e) {
            if(e.Channel == channel) {
                parted = true;
            }
        }
        
        [SetUp]
        public void SetUp() {
            if(setup) {
                return;
            }
            setup = true;

            irc.Encoding = System.Text.Encoding.UTF8;
            irc.SendDelay = 200;
            irc.ActiveChannelSyncing = true;
            irc.SupportNonRfc = true;

            irc.OnJoin += new JoinEventHandler(OnJoin);
            irc.OnPart += new PartEventHandler(OnPart);

            string[] servers = new string[] {"127.0.0.1"};
            int port = 6667;
            string channel = "#cmpctircd-test";
            
            try {
                irc.Connect(servers, port);
            } catch (ConnectionException e) {
                Assert.Warn($"Couldn't connect! Reason: {e.Message}");
            }
            
            try {
                irc.Login("SmartIRC", "SmartIrc4net Test Bot");
                irc.RfcJoin(channel);
                irc.RfcPart(channel);
                new Thread(irc.Listen).Start();
            } catch (Exception e) {
                Assert.Warn($"Error occurred! Message: {e.Message}");
            }
        }

        [TearDown]
        public void TearDown() {
            setup = false;
            irc.Disconnect();
        }

    }
}
