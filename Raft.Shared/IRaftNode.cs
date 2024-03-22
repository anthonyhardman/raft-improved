using Raft.Shared.Models;

namespace Raft.Shared;


public interface IRaftNode
{
    public string Id { get; }
    Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request);
    Task<RequestVoteResponse> RequestVote(RequestVoteRequest request);
    Task<StrongGetResponse> StrongGet(string key);
    Task<string> EventualGet(string key);
    Task<CompareAndSwapResponse> CompareAndSwap(CompareAndSwapRequest request);
    Task<string> MostRecentLeader();
}
