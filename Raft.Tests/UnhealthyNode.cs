using Raft.Shared;
using Raft.Shared.Models;

namespace Raft.Tests;

public class UnhealthyNode : IRaftNode
{
    public string Id => "UnhealthyNode";

    public Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CompareAndSwapResponse> CompareAndSwap(CompareAndSwapRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<string> EventualGet(string key)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsMostRecentLeader(string leaderId)
    {
        throw new NotImplementedException();
    }

    public Task<string> MostRecentLeader()
    {
        throw new NotImplementedException();
    }

    public Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<StrongGetResponse> StrongGet(string key)
    {
        throw new NotImplementedException();
    }
}
