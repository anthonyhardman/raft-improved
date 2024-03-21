using System.Collections.Concurrent;
using System.Text.Json;
using System.Timers;
using Raft.Shared;
using Raft.Shared.Models;
using Timer = System.Timers.Timer;
using Polly;
using Polly.Retry;

namespace Raft.Node;

public enum RaftRole
{
    Follower,
    Candidate,
    Leader
}

public class RaftState
{
    public int CurrentTerm { get; set; }
    public string VotedFor { get; set; }
}

public class RaftNode : IRaftNode
{
    public string Id { get; set; }
    public RaftRole Role { get; set; }
    public int CurrentTerm { get; set; }
    public string? VotedFor { get; set; }
    public Log Log { get; set; }
    public int CommitIndex { get; set; }
    public int LastApplied { get; set; }
    public List<IRaftNode> Peers { get; set; }
    public List<int> NextIndex { get; set; }
    public List<int> MatchIndex { get; set; }
    public Timer ActionTimer { get; set; }
    private string StateFile => $"raft-data/state.json";
    public ConcurrentDictionary<string, (int term, string value)> StateMachine { get; set; }
    public string MostRecentLeader { get; set; }

    private readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<Exception>().RetryAsync(3, (exception, retryCount) =>
        {
            Console.WriteLine($"Error connecting to node Retry:{retryCount} {exception.Message}. Retrying...");
        });


    public RaftNode(string id, List<IRaftNode> peers)
    {
        Id = id;
        Role = RaftRole.Follower;
        var state = LoadState();
        CurrentTerm = state.CurrentTerm;
        VotedFor = state.VotedFor;
        Log = new(id);
        CommitIndex = -1;
        LastApplied = -1;
        StateMachine = new();
        UpdateStateMachine();
        Peers = peers;
        NextIndex = peers.Select(_ => 0).ToList();
        MatchIndex = peers.Select(_ => 0).ToList();
        ActionTimer = new Timer();
        ActionTimer.Elapsed += DoAction;
        ActionTimer.Interval = GetTimerInterval();
        ActionTimer.Start();
    }


    public async void DoAction(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine($"{Id} is doing action as {Role}");
        switch (Role)
        {
            case RaftRole.Follower:
            case RaftRole.Candidate:
                Console.WriteLine($"{Id} is holding election");
                await HoldElection();
                break;
            case RaftRole.Leader:
                Console.WriteLine($"{Id} is sending heartbeat");
                await SendHeartbeat();
                break;
        }
    }

    private int GetTimerInterval()
    {
        if (Role == RaftRole.Leader)
        {
            return 50;
        }

        return new Random().Next(150, 300);
    }

    private RaftState LoadState()
    {
        if (!File.Exists(StateFile))
        {
            return new RaftState { VotedFor = null, CurrentTerm = 0 };
        }

        var json = File.ReadAllText(StateFile);
        var state = JsonSerializer.Deserialize<RaftState>(json);
        return state;
    }

    private void SaveState()
    {
        var state = new RaftState { CurrentTerm = CurrentTerm, VotedFor = VotedFor };
        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(StateFile, json);
    }

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

        MostRecentLeader = request.LeaderId;

        if (request.Term > CurrentTerm)
        {
            CurrentTerm = request.Term;
            VotedFor = null;
            Role = RaftRole.Follower;
            SaveState();
        }

        var hasLogAtPrevIndex = Log.Count > request.PrevLogIndex;
        if (hasLogAtPrevIndex && request.PrevLogIndex >= 0 && Log[request.PrevLogIndex].Term != request.PrevLogTerm)
        {
            Log.RemoveRange(request.PrevLogIndex, Log.Count - request.PrevLogIndex);

            return new AppendEntriesResponse
            {
                Term = CurrentTerm,
                Success = false
            };
        }

        Console.WriteLine($"{Id} received append entries from {request.LeaderId} prevLogIndex: {request.PrevLogIndex} prevLogTerm: {request.PrevLogTerm} entries: {request.PrevLogTermEntries.Count}");
        Log.AppendRange(request.PrevLogTermEntries);

        if (request.LeaderCommit > CommitIndex)
        {
            CommitIndex = Math.Min(request.LeaderCommit, Log.Count - 1);
            UpdateStateMachine();
        }

        return new AppendEntriesResponse
        {
            Term = CurrentTerm,
            Success = true
        };
    }

    private async Task SendHeartbeat()
    {
        for (var i = 0; i < Peers.Count; i++)
        {
            var follower = Peers[i];
            var nextIndex = NextIndex[i];
            var prevLogIndex = nextIndex - 1;
            var prevLogTerm = prevLogIndex >= 0 ? Log[prevLogIndex].Term : 0;

            Console.WriteLine($"{Id} sending heartbeat to {follower.Id} nextIndex: {nextIndex} prevLogIndex: {prevLogIndex} prevLogTerm: {prevLogTerm}");   

            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var request = new AppendEntriesRequest
                        {
                            Term = CurrentTerm,
                            LeaderId = Id,
                            PrevLogIndex = prevLogIndex,
                            PrevLogTerm = prevLogTerm,
                            PrevLogTermEntries = Log.GetRange(NextIndex[i], Log.Count - NextIndex[i]),
                            LeaderCommit = CommitIndex,
                        };

                        var response = await follower.AppendEntries(request);

                        if (response.Term > CurrentTerm)
                        {
                            CurrentTerm = response.Term;
                            VotedFor = null;
                            Role = RaftRole.Follower;
                            SaveState();
                            return;
                        }

                        if (response.Success)
                        {
                            NextIndex[i] = Log.Count;
                            MatchIndex[i] = Log.Count - 1;
                        }
                        else if (NextIndex[i] > 0)
                        {
                            
                            NextIndex[i]--;
                        }
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending heartbeat to peer: {e.Message} {e.StackTrace.ToString()}");
            }
        }

        for (var newIndex = Log.Count - 1; newIndex > CommitIndex; newIndex--)
        {
            var matchCount = MatchIndex.Count(matchIndex => matchIndex >= newIndex);
            if (matchCount > Peers.Count / 2 && Log[newIndex].Term == CurrentTerm)
            {
                CommitIndex = newIndex;
                UpdateStateMachine();
                break;
            }
        }
    }

    private void UpdateStateMachine()
    {
        while (LastApplied < CommitIndex)
        {
            LastApplied++;
            var entry = Log[LastApplied];
            StateMachine.AddOrUpdate(entry.Key, (entry.Term, entry.Value), (key, oldValue) => (entry.Term, entry.Value));
        }
    }

    public async Task<bool> CompareAndSwap(CompareAndSwapRequest request)
    {
        if (Role != RaftRole.Leader || !await MajorityOfPeersHaveMeAsLeader())
        {
            return false;
        }

        if (StateMachine.TryGetValue(request.Key, out var value))
        {
            if (value.term == request.Version && value.value == request.ExpectedValue)
            {
                Log.Append(new LogEntry(CurrentTerm, request.Key, request.NewValue));
                SendHeartbeat();
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            Log.Append(new LogEntry(CurrentTerm, request.Key, request.NewValue));
            SendHeartbeat();
            return true;
        }
    }

    public async Task<string> EventualGet(string key)
    {
        if (StateMachine.TryGetValue(key, out var value))
        {
            return value.value;
        }

        return null;
    }

    public async Task HoldElection()
    {
        Role = RaftRole.Candidate;
        CurrentTerm++;
        VotedFor = Id;
        SaveState();
        var votes = 1;

        var request = new RequestVoteRequest
        {
            Term = CurrentTerm,
            CandidateId = Id,
            LastLogIndex = Log.Count - 1,
            LastLogTerm = Log.Count > 0 ? Log[Log.Count - 1].Term : 0
        };

        foreach (var peer in Peers)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {

                    var response = await peer.RequestVote(request);
                    if (response.Term > CurrentTerm)
                    {
                        CurrentTerm = response.Term;
                        VotedFor = null;
                        SaveState();
                        Role = RaftRole.Follower;
                        return;
                    }

                    if (response.VoteGranted)
                    {
                        votes++;
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error requesting vote from peer: {e.Message}");
            }

        }

        if (votes > Peers.Count / 2)
        {
            Console.WriteLine($"{Id} won election Num Votes: {votes} peers: {Peers.Count}");
            Role = RaftRole.Leader;
            await SendHeartbeat();
            return;
        }

        Console.WriteLine($"{Id} lost election Num Votes: {votes} peers: {Peers.Count}");
    }

    public async Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
    {
        Console.WriteLine($"{Id} received vote request from {request.CandidateId}");
        if (request.Term < CurrentTerm)
        {
            Console.WriteLine($"{Id} denied vote to {request.CandidateId} because term is less");
            return new RequestVoteResponse
            {
                Term = CurrentTerm,
                VoteGranted = false
            };
        }

        if (VotedFor == null || VotedFor == request.CandidateId)
        {
            if (IsLogUpToDate(request.LastLogIndex, request.LastLogTerm))
            {
                Console.WriteLine($"{Id} granted vote to {request.CandidateId}");
                VotedFor = request.CandidateId;
                CurrentTerm = request.Term;
                Role = RaftRole.Follower;
                ResetActionTimer();
                SaveState();
                return new RequestVoteResponse
                {
                    Term = CurrentTerm,
                    VoteGranted = true
                };
            }
        }

        return new RequestVoteResponse
        {
            Term = CurrentTerm,
            VoteGranted = false
        };
    }

    private bool IsLogUpToDate(int candidateLastLogIndex, int candidateLastLogTerm)
    {
        var localLogLastIndex = Log.Count - 1;
        var localLogLastTerm = Log.Count > 0 ? Log[Log.Count - 1].Term : 0;

        if (candidateLastLogTerm > localLogLastTerm)
        {
            return true;
        }

        if (candidateLastLogTerm == localLogLastTerm)
        {
            return candidateLastLogIndex >= localLogLastIndex;
        }

        return false;
    }

    public async Task<StrongGetResponse> StrongGet(string key)
    {
        if (Role != RaftRole.Leader || !await MajorityOfPeersHaveMeAsLeader() || !StateMachine.TryGetValue(key, out var value))
        {
            return new StrongGetResponse
            {
                Value = null,
                Version = -1
            };
        }

        return new StrongGetResponse
        {
            Value = value.value,
            Version = value.term
        };
    }

    private void ResetActionTimer()
    {
        ActionTimer.Stop();
        ActionTimer.Interval = new Random().Next(500, 1000);
        ActionTimer.Start();
    }

    public async Task<bool> IsMostRecentLeader(string leaderId)
    {
        return MostRecentLeader == leaderId;
    }

    public async Task<bool> MajorityOfPeersHaveMeAsLeader()
    {
        var count = 0;
        foreach (var peer in Peers)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (await peer.IsMostRecentLeader(Id))
                    {
                        count++;
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error checking if peer has me as leader: {e.Message}");
            }

            if (count > Peers.Count / 2)
            {
                return true;
            }
        }

        return false;
    }
}
