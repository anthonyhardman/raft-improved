using Raft.Shared.Models;

namespace Raft.Shared;

public static class GrpcModelExtentsions
{
    public static Grpc.AppendEntriesRequest ToGrpc(this AppendEntriesRequest request)
    {
        return new Grpc.AppendEntriesRequest
        {
            Term = request.Term,
            LeaderId = request.LeaderId,
            PrevLogIndex = request.PrevLogIndex,
            PrevLogTerm = request.PrevLogTerm,
            Entries = { request.PrevLogTermEntries.Select(e => e.ToGrpc()) },
            LeaderCommit = request.LeaderCommit
        };
    }

    public static AppendEntriesRequest ToRaft(this Grpc.AppendEntriesRequest request)
    {
        return new AppendEntriesRequest
        {
            Term = request.Term,
            LeaderId = request.LeaderId,
            PrevLogIndex = request.PrevLogIndex,
            PrevLogTerm = request.PrevLogTerm,
            PrevLogTermEntries = request.Entries.Select(e => e.ToRaft()).ToList(),
            LeaderCommit = request.LeaderCommit
        };
    }

    public static AppendEntriesResponse ToRaft(this Grpc.AppendEntriesResponse response)
    {
        return new AppendEntriesResponse
        {
            Term = response.Term,
            Success = response.Success
        };
    }

    public static Grpc.RequestVoteRequest ToGrpc(this RequestVoteRequest request)
    {
        return new Grpc.RequestVoteRequest
        {
            Term = request.Term,
            CandidateId = request.CandidateId,
            LastLogIndex = request.LastLogIndex,
            LastLogTerm = request.LastLogTerm
        };
    }

    public static RequestVoteResponse ToRaft(this Grpc.RequestVoteResponse response)
    {
        return new RequestVoteResponse
        {
            Term = response.Term,
            VoteGranted = response.VoteGranted
        };
    }


    public static Grpc.LogEntry ToGrpc(this LogEntry entry)
    {
        return new Grpc.LogEntry
        {
            Term = entry.Term,
            Key = entry.Key,
            Value = entry.Value
        };
    }

    public static LogEntry ToRaft(this Grpc.LogEntry entry)
    {
        return new LogEntry(entry.Term, entry.Key, entry.Value);
    }


    public static Grpc.StrongGetRequest ToGrpc(this StrongGetRequest request)
    {
        return new Grpc.StrongGetRequest
        {
            Key = request.Key
        };
    }

    public static StrongGetResponse ToRaft(this Grpc.StrongGetResponse response)
    {
        return new StrongGetResponse
        {
            Value = response.Value
        };
    }

    public static Grpc.EventualGetRequest ToGrpc(this EventualGetRequest request)
    {
        return new Grpc.EventualGetRequest
        {
            Key = request.Key
        };
    }


    public static EventualGetResponse ToRaft(this Grpc.EventualGetResponse response)
    {
        return new EventualGetResponse
        {
            Value = response.Value
        };
    }

    public static Grpc.CompareAndSwapRequest ToGrpc(this CompareAndSwapRequest request)
    {
        return new Grpc.CompareAndSwapRequest
        {
            Key = request.Key,
            ExpectedValue = request.ExpectedValue,
            NewValue = request.NewValue,
            Version = request.Version
        };
    }

    public static CompareAndSwapResponse ToRaft(this Grpc.CompareAndSwapResponse response)
    {
        return new CompareAndSwapResponse
        {
            Success = response.Success,
        };
    }
}
