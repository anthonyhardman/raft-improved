// using Raft.Node.Services;

using Raft.Node;
using Raft.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

Console.WriteLine("Starting node...");
var id = Environment.GetEnvironmentVariable("NODE_ID") ?? "node1";
var peers_addresses = (Environment.GetEnvironmentVariable("PEER_ADDRESSES") ?? "").Split(",");
foreach (var address in peers_addresses)
{
    Console.WriteLine($"Peer address: {address}");
}
var peer_ids = (Environment.GetEnvironmentVariable("PEER_IDS") ?? "").Split(",");
foreach (var peer_id in peer_ids)
{
    Console.WriteLine($"Peer id: {peer_id}");
}
var peers = peer_ids.Zip(peers_addresses, (id, address) =>
{
    var node = new GrpcRaftNode(id, address);
    return node;
}).ToList<IRaftNode>();

Console.WriteLine($"Node id: {id} has {peers.Count} peers");

var node = new RaftNode(id, peers);
builder.Services.AddSingleton<IRaftNode>(node);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<RaftNodeService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
