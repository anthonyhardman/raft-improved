using Raft.Node;
using Raft.Shared;
using Raft.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Raft.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    // make a method for cleaning up the test data
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists("raft-data"))
            Directory.Delete("raft-data", true);
    }

    [Test]
    public void CorrectStateMachine()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node2.MostRecentLeader = node1.Id;
        node3.MostRecentLeader = node1.Id;
        var request = new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "new value",
            Version = 0
        };
        node1.CompareAndSwap(request);
        node1.DoAction(null, null);

        node1.StateMachine.Count.Should().Be(1);
        node1.StateMachine["key"].term.Should().Be(0);
        node1.StateMachine["key"].value.Should().Be("new value");

        node2.StateMachine.Count.Should().Be(1);
        node2.StateMachine["key"].term.Should().Be(0);
        node2.StateMachine["key"].value.Should().Be("new value");

        node3.StateMachine.Count.Should().Be(1);
        node3.StateMachine["key"].term.Should().Be(0);
        node3.StateMachine["key"].value.Should().Be("new value");

        Assert.Pass();
    }

    [Test]
    public void CorrectCommitIndex()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node2.MostRecentLeader = node1.Id;
        node3.MostRecentLeader = node1.Id;
        var request = new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "new value",
            Version = 0
        };
        node1.CompareAndSwap(request);
        node1.DoAction(null, null);

        node1.CommitIndex.Should().Be(0);
        node2.CommitIndex.Should().Be(0);
        node3.CommitIndex.Should().Be(0);
    }

    [Test]
    public void LeaderBecomesFollowerIfTermIsHigher()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node1.CurrentTerm = 0;
        node2.CurrentTerm = 1;
        node2.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Follower);
        node2.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void LogIsCorrect()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node2.MostRecentLeader = node1.Id;
        node3.MostRecentLeader = node1.Id;
        var request = new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "new value",
            Version = 0
        };
        node1.CompareAndSwap(request);
        node1.DoAction(null, null);

        node1.Log.Count.Should().Be(1);
        node1.Log[0].Term.Should().Be(0);
        node1.Log[0].Key.Should().Be("key");
        node1.Log[0].Value.Should().Be("new value");

        node2.Log.Count.Should().Be(1);
        node2.Log[0].Term.Should().Be(0);
        node2.Log[0].Key.Should().Be("key");
        node2.Log[0].Value.Should().Be("new value");

        node3.Log.Count.Should().Be(1);
        node3.Log[0].Term.Should().Be(0);
        node3.Log[0].Key.Should().Be("key");
        node3.Log[0].Value.Should().Be("new value");

        node1.DoAction(null, null);

        node1.Log.Count.Should().Be(1);
        node1.Log[0].Term.Should().Be(0);
        node1.Log[0].Key.Should().Be("key");
    }

    [Test]
    public void LeaderGetsElectedIfTwoOfThreeNodesAreHealthy()
    {
        RaftNode node1, node2;
        UnhealthyNode node3;
        TwoNodesHealthyOneNodeUnhealthy(out node1, out node2, out node3);

        node1.Role = RaftRole.Follower;
        node2.Role = RaftRole.Follower;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Leader);
        node2.Role.Should().Be(RaftRole.Follower);
    }

    [Test]
    public void LeaderDoesNotGetElectedIfOneOfThreeNodesIsHealthy()
    {
        RaftNode node1;
        UnhealthyNode node2, node3;
        OneNodeHealthyTwoNodesUnhealthy(out node1, out node2, out node3);

        node1.Role = RaftRole.Follower;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Candidate);
    }

    [Test]
    public void LeaderGetsElectedIfThreeOfFiveNodesAreHealthy()
    {
        RaftNode node1, node2, node5;
        UnhealthyNode node3, node4;
        ThreeNodeHealthyTwoNodeUnhealthy(out node1, out node2, out node3, out node4, out node5);

        node1.Role = RaftRole.Follower;
        node2.Role = RaftRole.Follower;
        node5.Role = RaftRole.Follower;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void NodeWillContinueToBeLeaderIfAllNodesAreHealthy()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void NodeWillCallForElectionIfMessagesFromLeaderTakeTooLong()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;

        node2.DoAction(null, null);

        node2.Role.Should().Be(RaftRole.Leader);
        node1.Role.Should().Be(RaftRole.Follower);
    }

    public static void TwoNodeSetup()
    {
        var node1 = new RaftNode("node1", []);
        var node2 = new RaftNode("node2", []);
        node1.Peers = new List<IRaftNode> { node2 };
        node1.NextIndex = new List<int> { 0 };
        node1.MatchIndex = new List<int> { 0 };
        node2.Peers = new List<IRaftNode> { node1 };
        node2.NextIndex = new List<int> { 0 };
        node2.MatchIndex = new List<int> { 0 };
    }

    [Test]
    public void NodeWillContinueAsLeaderEvenIfTwoNodesAreUnhealthy()
    {
        RaftNode node1, node2, node5;
        UnhealthyNode node3, node4;
        ThreeNodeHealthyTwoNodeUnhealthy(out node1, out node2, out node3, out node4, out node5);

        node1.Role = RaftRole.Leader;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void AvoidDoubleVoting()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.DoAction(null, null);
        node1.Role.Should().Be(RaftRole.Leader);

        node2.CurrentTerm = 0;
        node2.DoAction(null, null);
        node2.Role.Should().Be(RaftRole.Candidate);
        node1.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void AvoidDoubleVoting2()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.CurrentTerm = 1;
        node1.DoAction(null, null);
        node1.Role.Should().Be(RaftRole.Leader);

        node2.CurrentTerm = 0;
        node2.DoAction(null, null);
        node2.Role.Should().Be(RaftRole.Follower);
        node1.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public void NodeWillBecomeFollowerIfLeaderIsElected()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Candidate;
        node2.Role = RaftRole.Candidate;
        node3.Role = RaftRole.Candidate;

        node1.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Leader);
        node2.Role.Should().Be(RaftRole.Follower);
        node3.Role.Should().Be(RaftRole.Follower);
    }

    [Test]
    public void OldLeaderWillBecomeFollowerIfNewLeaderIsElected()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;

        node2.DoAction(null, null);

        node1.Role.Should().Be(RaftRole.Follower);
        node2.Role.Should().Be(RaftRole.Leader);
    }

    [Test]
    public async Task StrongGetWorksIfLeader()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node2.MostRecentLeader = node1.Id;
        node3.MostRecentLeader = node1.Id;
        await node1.CompareAndSwap(new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "value",
            Version = 0
        });
        node1.DoAction(null, null);

        var result = await node1.StrongGet("key");

        result.Value.Should().Be("value");
        result.Version.Should().Be(0);
    }


    [Test]
    public async Task StrongGetDoesntWorkIfNotLeader()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        await node1.CompareAndSwap(new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "value",
            Version = 0
        });
        node1.DoAction(null, null);

        var result = await node2.StrongGet("key");
        result.Value.Should().Be(null);
        result.Version.Should().Be(-1);
    }

    [Test]
    public async Task StrongGetDoesntWorkIfNotActualLeader()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        await node1.CompareAndSwap(new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "value",
            Version = 0
        });
        node1.DoAction(null, null);

        node2.Role = RaftRole.Leader;
        var result = await node2.StrongGet("key");
        result.Value.Should().Be(null);
        result.Version.Should().Be(-1);
    }

    [Test]
    public async Task EventualGetWorks()
    {
        RaftNode node1, node2, node3;
        ThreeNodeSetup(out node1, out node2, out node3);

        node1.Role = RaftRole.Leader;
        node2.MostRecentLeader = node1.Id;
        node3.MostRecentLeader = node1.Id;
        await node1.CompareAndSwap(new CompareAndSwapRequest
        {
            Key = "key",
            ExpectedValue = "some value",
            NewValue = "value",
            Version = 0
        });
        node1.DoAction(null, null);
        
        var result = await node2.EventualGet("key");

        result.Should().Be("value");
    }


    public static void TwoNodesHealthyOneNodeUnhealthy(out RaftNode node1, out RaftNode node2, out UnhealthyNode node3)
    {
        node1 = new RaftNode("node1", []);
        node2 = new RaftNode("node2", []);
        node3 = new UnhealthyNode();
        node1.Peers = new List<IRaftNode> { node2, node3 };
        node1.NextIndex = new List<int> { 0, 0 };
        node1.MatchIndex = new List<int> { 0, 0 };
        node2.Peers = new List<IRaftNode> { node1, node3 };
        node2.NextIndex = new List<int> { 0, 0 };
        node2.MatchIndex = new List<int> { 0, 0 };
    }

    public static void OneNodeHealthyTwoNodesUnhealthy(out RaftNode node1, out UnhealthyNode node2, out UnhealthyNode node3)
    {
        node1 = new RaftNode("node1", []);
        node2 = new UnhealthyNode();
        node3 = new UnhealthyNode();
        node1.Peers = new List<IRaftNode> { node2, node3 };
        node1.NextIndex = new List<int> { 0, 0 };
        node1.MatchIndex = new List<int> { 0, 0 };
    }

    private static void ThreeNodeSetup(out RaftNode node1, out RaftNode node2, out RaftNode node3)
    {
        node1 = new RaftNode("node1", []);
        node2 = new RaftNode("node2", []);
        node3 = new RaftNode("node3", []);
        node1.Peers = new List<IRaftNode> { node2, node3 };
        node1.NextIndex = new List<int> { 0, 0 };
        node1.MatchIndex = new List<int> { 0, 0 };
        node2.Peers = new List<IRaftNode> { node1, node3 };
        node2.NextIndex = new List<int> { 0, 0 };
        node2.MatchIndex = new List<int> { 0, 0 };
        node3.Peers = new List<IRaftNode> { node1, node2 };
        node3.NextIndex = new List<int> { 0, 0 };
        node3.MatchIndex = new List<int> { 0, 0 };
    }

    private static void ThreeNodeHealthyTwoNodeUnhealthy(out RaftNode node1, out RaftNode node2, out UnhealthyNode node3, out UnhealthyNode node4, out RaftNode node5)
    {
        node1 = new RaftNode("node1", []);
        node2 = new RaftNode("node2", []);
        node3 = new UnhealthyNode();
        node4 = new UnhealthyNode();
        node5 = new RaftNode("node5", []);
        node1.Peers = new List<IRaftNode> { node2, node3, node4, node5 };
        node1.NextIndex = new List<int> { 0, 0, 0, 0 };
        node1.MatchIndex = new List<int> { 0, 0, 0, 0 };
        node2.Peers = new List<IRaftNode> { node1, node3, node4, node5 };
        node2.NextIndex = new List<int> { 0, 0, 0, 0 };
        node2.MatchIndex = new List<int> { 0, 0, 0, 0 };
        node5.Peers = new List<IRaftNode> { node1, node2, node3, node4 };
        node5.NextIndex = new List<int> { 0, 0, 0, 0 };
        node5.MatchIndex = new List<int> { 0, 0, 0, 0 };
    }

}


