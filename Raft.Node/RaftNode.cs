using System.Timers;
using Raft.Shared;
using Timer = System.Timers.Timer;

namespace Raft.Node;

public enum RaftRole
{
    Follower,
    Candidate,
    Leader
}

public class RaftNode : IRaftNode
{
    public string Id { get; set; }
    public RaftRole Role { get; set; }
    public int CurrentTerm { get; set; }
    public string VotedFor { get; set; }
    public List<LogEntry> Log { get; set; }
    public int CommitIndex { get; set; }
    public int LastApplied { get; set; }
    public string[] NextIndex { get; set; }
    public string[] MatchIndex { get; set; }
    public Timer ActionTimer { get; set; }


    public async Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request)
    {
        if (request.Term < CurrentTerm)
        {
            return new AppendEntriesResponse
            {
                Term = CurrentTerm,
                Success = false
            };
        }

        ResetActionTimer();

        var hasLogAtPrevIndex = Log.Count > request.PrevLogIndex;
        if (hasLogAtPrevIndex && Log[request.PrevLogIndex].Term != request.PrevLogTerm)
        {
            Log.RemoveRange(request.PrevLogIndex, Log.Count - request.PrevLogIndex);

            return new AppendEntriesResponse
            {
                Term = CurrentTerm,
                Success = false
            };
        }

        foreach (var entry in request.PrevLogTermEntries)
        {
            if (Log.Count <= request.PrevLogIndex || Log[request.PrevLogIndex].Term != entry.Term)
            {
                Log.Add(entry);
            }
        }

        if (request.LeaderCommit > CommitIndex)
        {
            CommitIndex = Math.Min(request.LeaderCommit, Log.Count - 1);
        }

        return new AppendEntriesResponse
        {
            Term = CurrentTerm,
            Success = true
        };
    }

    public async Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
    {
        throw new NotImplementedException();
    }

    private void ResetActionTimer()
    {
        ActionTimer.Stop();
        ActionTimer.Interval = new Random().Next(150, 300);
        ActionTimer.Start();
    }
}
