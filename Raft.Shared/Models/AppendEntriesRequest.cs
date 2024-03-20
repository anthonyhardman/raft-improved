namespace Raft.Shared;

public class AppendEntriesRequest
{
    public int Term { get; set; }
    public string LeaderId { get; set; }
    public int PrevLogIndex { get; set; }
    public int PrevLogTerm { get; set; }
    public List<LogEntry> PrevLogTermEntries { get; set; }
    public int LeaderCommit { get; set; }
}
