using Raft.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
var node_ids = (Environment.GetEnvironmentVariable("NODE_IDS") ?? "").Split(",");
var node_addresses = (Environment.GetEnvironmentVariable("NODE_ADDRESSES") ?? "").Split(",");
var nodes = node_ids.Zip(node_addresses, (id, address) =>
{
    var node = new GrpcRaftNode(id, address);
    return node;
}).ToList<IRaftNode>();

builder.Services.AddSingleton<List<IRaftNode>>(nodes);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
