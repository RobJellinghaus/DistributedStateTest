// Copyright (c) 2020 by Rob Jellinghaus.
using Distributed.Thing;
using LiteNetLib;
using NUnit.Framework;
using System.Linq;

namespace Distributed.State.Test
{
    public class SinglePeerTests
    {
        [Test]
        public void ConstructPeer()
        {
            var testWorkQueue = new TestWorkQueue();
            using DistributedPeer peer = new DistributedPeer(testWorkQueue, DistributedPeer.DefaultListenPort);

            Assert.IsNotNull(peer);

            // Should be no work after construction.
            Assert.AreEqual(0, testWorkQueue.Count);

            // Start announcing.
            peer.Announce();

            // should have sent one Announce message, and queued the action to send the next
            Assert.AreEqual(1, testWorkQueue.Count);
        }

        [Test]
        public void TestListenForAnnounce()
        {
            var testWorkQueue = new TestWorkQueue();

            var testBroadcastListener = new TestBroadcastNetEventListener();
            var testNetManager = new TestNetManager(new NetManager(testBroadcastListener));
            testNetManager.NetManager.BroadcastReceiveEnabled = true;
            testNetManager.NetManager.Start(DistributedPeer.DefaultListenPort);

            // the peer under test
            using DistributedPeer peer = new DistributedPeer(testWorkQueue, DistributedPeer.DefaultListenPort, isListener: false);

            // start announcing
            peer.Announce();

            // the list of all pollable objects, to ensure forward progress
            IPollEvents[] pollables = new IPollEvents[] { peer, testNetManager };

            // should have received Announce message
            WaitUtils.WaitUntil(pollables, () => testBroadcastListener.ReceivedMessages.Count == 1);
            Assert.IsTrue(testBroadcastListener.ReceivedMessages.TryDequeue(out object announceMessage));
            ValidateAnnounceMessage(announceMessage, peer);

            // now execute pending work
            testWorkQueue.PollEvents();

            // should still be one queued item -- the *next* announce message
            Assert.AreEqual(1, testWorkQueue.Count);

            // wait to receive second Announce
            WaitUtils.WaitUntil(pollables, () => testBroadcastListener.ReceivedMessages.Count == 1);
            Assert.IsTrue(testBroadcastListener.ReceivedMessages.TryDequeue(out announceMessage));
            ValidateAnnounceMessage(announceMessage, peer);

            static void ValidateAnnounceMessage(object possibleMessage, DistributedPeer peer)
            {
                AnnounceMessage announceMessage = possibleMessage as AnnounceMessage;
                Assert.IsNotNull(announceMessage);
                Assert.AreEqual(peer.SocketAddress, announceMessage.AnnouncerSocketAddress.SocketAddress);
                Assert.AreEqual(0, announceMessage.KnownPeers.Length);
            }
        }


        [Test]
        public void PeerListenForAnnounce()
        {
            var testWorkQueue = new TestWorkQueue();

            // the peer under test
            using DistributedPeer peer = new DistributedPeer(testWorkQueue, DistributedPeer.DefaultListenPort, isListener: true);

            // start announcing
            peer.Announce();

            // the list of all pollable objects, to ensure forward progress
            IPollEvents[] pollables = new IPollEvents[] { peer };

            // should have received Announce message
            WaitUtils.WaitUntil(pollables, () => peer.PeerAnnounceCount == 1);

            // now execute pending work
            testWorkQueue.PollEvents();

            // should still be one queued item -- the *next* announce message
            Assert.AreEqual(1, testWorkQueue.Count);

            // wait to receive second Announce
            WaitUtils.WaitUntil(pollables, () => peer.PeerAnnounceCount == 2);
        }

        [Test]
        public void PeerCreateObjects()
        {
            var testWorkQueue = new TestWorkQueue();

            // the first peer under test
            using DistributedPeer peer = new DistributedPeer(testWorkQueue, DistributedPeer.DefaultListenPort, isListener: true);

            // create a Distributed.Thing
            var distributedThing = new DistributedThing(
                1,
                isOwner: true,
                localThing: new LocalThing(1));

            peer.AddOwner(distributedThing);

            Assert.AreEqual(1, peer.Owners.Count);
            Assert.True(peer.Owners.Values.First() == distributedThing);
        }
    }
}
