using Raft.Shop;
using Raft.Shop.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var raftGatewayUrl = System.Environment.GetEnvironmentVariable("RAFT_GATEWAY_URL") ?? "http://localhost:5001";
builder.Services.AddHttpClient("RaftClient", client =>
{
    client.BaseAddress = new Uri(raftGatewayUrl);
});
builder.Services.AddScoped<RaftService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
