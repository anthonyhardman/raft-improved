using Grpc.Core;
using Raft.Grpc;
using Raft.Shared;
using Polly.Retry;
using Polly;

namespace Raft.Node;

public class RaftNodeService : Raft.Grpc.RaftNode.RaftNodeBase
{
    private readonly IRaftNode _node;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RaftNodeService(IRaftNode node)
    {
        _node = node;
        _retryPolicy = Policy.Handle<Exception>().RetryAsync(3, (exception, retryCount) =>
        {
            Console.WriteLine($"Error!!!: {exception.Message}");
            Console.WriteLine($"Retry count: {retryCount}");
            if (retryCount == 3)
            {
                Console.WriteLine(exception.StackTrace.ToString());
            }
        });
    }

    public override async Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request, ServerCallContext context)
    {
        var raftRequest = new Raft.Shared.Models.AppendEntriesRequest()
        {
            Term = request.Term,
            LeaderId = request.LeaderId,
            PrevLogIndex = request.PrevLogIndex,
            PrevLogTerm = request.PrevLogTerm,
            PrevLogTermEntries = request.Entries.Select(e => new Raft.Shared.Models.LogEntry(e.Term, e.Key, e.Value)).ToList(),
            LeaderCommit = request.LeaderCommit
        };

        return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _node.AppendEntries(raftRequest);
                return new AppendEntriesResponse
                {
                    Term = response.Term,
                    Success = response.Success
                };
            });
    }

    public override async Task<RequestVoteResponse> RequestVote(RequestVoteRequest request, ServerCallContext context)
    {
        var raftRequest = new Raft.Shared.Models.RequestVoteRequest()
        {
            Term = request.Term,
            CandidateId = request.CandidateId,
            LastLogIndex = request.LastLogIndex,
            LastLogTerm = request.LastLogTerm
        };

        return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _node.RequestVote(raftRequest);
                return new RequestVoteResponse
                {
                    Term = response.Term,
                    VoteGranted = response.VoteGranted
                };
            });
    }

    public override async Task<StrongGetResponse> StrongGet(StrongGetRequest request, ServerCallContext context)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _node.StrongGet(request.Key);
                return new StrongGetResponse
                {
                    Value = response.Value,
                    Version = response.Version
                };
            });
    }

    public override async Task<EventualGetResponse> EventualGet(EventualGetRequest request, ServerCallContext context)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _node.EventualGet(request.Key);
                return new EventualGetResponse
                {
                    Value = response
                };
            });
    }

    public override async Task<CompareAndSwapResponse> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        var raftRequest = new Raft.Shared.Models.CompareAndSwapRequest()
        {
            Key = request.Key,
            ExpectedValue = request.ExpectedValue,
            NewValue = request.NewValue,
            Version = request.Version
        };

        return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _node.CompareAndSwap(raftRequest);
                return new CompareAndSwapResponse
                {
                    Success = response
                };
            });
    }
}