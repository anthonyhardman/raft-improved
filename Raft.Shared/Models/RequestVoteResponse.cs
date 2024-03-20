namespace Raft.Shared;

public class RequestVoteResponse
{
    public int Term { get; set; }
    public bool VoteGranted { get; set; }
}
