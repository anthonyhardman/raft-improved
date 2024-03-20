namespace Raft.Shared;

public class RequestVoteRequest
{
    public int Term { get; set; }
    public string CandidateId { get; set; }
    public int LastLogIndex { get; set; }
    public int LastLogTerm { get; set; }
}
