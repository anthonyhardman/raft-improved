using Raft.Shared.Models;

namespace Raft.Shared;


public interface IRaftNode
{
    public string Id { get; }
    Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request);
    Task<RequestVoteResponse> RequestVote(RequestVoteRequest request);
    Task<StrongGetResponse> StrongGet(string key);
    Task<string> EventualGet(string key);
    Task<bool> CompareAndSwap(CompareAndSwapRequest request);
    Task<bool> IsMostRecentLeader(string leaderId);
}
