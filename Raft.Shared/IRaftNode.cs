namespace Raft.Shared;


public interface IRaftNode
{
    Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request);
    Task<RequestVoteResponse> RequestVote(RequestVoteRequest request);
}
