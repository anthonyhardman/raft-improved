using Grpc.Net.Client;
using Raft.Shared.Models;
using Polly;
using Polly.Retry;

namespace Raft.Shared;

public class GrpcRaftNode : IRaftNode
{
    private GrpcChannel _channel;
    private readonly string _address;
    private readonly AsyncRetryPolicy _retryPolicy;

    public string Id { get; set; }

    public GrpcRaftNode(string id, string address)
    {
        Id = id;
        _address = address;
        Console.WriteLine($"Creating channel for address: {_address}");
        _channel = GrpcChannel.ForAddress(_address);
        _retryPolicy = Policy.Handle<Exception>().RetryAsync(3, (exception, retryCount) =>
        {
            _channel = GrpcChannel.ForAddress(_address);
        });
    }

    public async Task<AppendEntriesResponse> AppendEntries(AppendEntriesRequest request)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var rpcRequest = request.ToGrpc();
                var response = await client.AppendEntriesAsync(rpcRequest);
                return response.ToRaft();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error Appending Entries from {request.LeaderId} to {Id}");
            return new AppendEntriesResponse { Term = 0, Success = false };
        }
    }

    public async Task<bool> CompareAndSwap(CompareAndSwapRequest request)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var rpcRequest = request.ToGrpc();
                var response = await client.CompareAndSwapAsync(rpcRequest);
                return response.Success;

            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error making compare and swap request to {Id}");
            return false;
        }
    }

    public async Task<string> EventualGet(string key)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {

                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var response = await client.EventualGetAsync(new Grpc.EventualGetRequest { Key = key });
                var value = response.Value;
                return value;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error with eventual get request to {Id}");
            return null;
        }
    }

    public async Task<RequestVoteResponse> RequestVote(RequestVoteRequest request)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {

                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var rpcRequest = request.ToGrpc();
                var response = await client.RequestVoteAsync(rpcRequest);
                return response.ToRaft();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error {request.CandidateId} failed to request vote from {Id}");
            return new RequestVoteResponse { Term = 0, VoteGranted = false };
        }
    }

    public async Task<StrongGetResponse> StrongGet(string key)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {

                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var response = await client.StrongGetAsync(new Grpc.StrongGetRequest { Key = key });
                return response.ToRaft();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error with strong get request to {Id} ");
            return new StrongGetResponse { Value = null, Version = 0 };
        }
    }

    public async Task<bool> IsMostRecentLeader(string leaderId)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var client = new Grpc.RaftNode.RaftNodeClient(_channel);
                var response = await client.IsMostRecentLeaderAsync(new Grpc.IsMostRecentLeaderRequest { LeaderId = leaderId });
                return response.IsMostRecentLeader;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error with IsMostRecentLeader request for {leaderId}");
            return false;
        }
    }
}
