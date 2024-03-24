using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raft.Shared;
using Raft.Shared.Models;

namespace MyApp.Namespace;

[Route("api/[controller]")]
[ApiController]
public class StorageController : ControllerBase
{
  private readonly List<IRaftNode> _raftNodes;

  public StorageController(List<IRaftNode> raftNodes)
  {
    _raftNodes = raftNodes;
  }

  [HttpGet("strong")]
  public async Task<ActionResult<StrongGetResponse>> StrongGet(string key)
  {
    while (true)
    {
      var node = GetRandomNode();
      var leaderId = await node.MostRecentLeader();
      var leader = _raftNodes.FirstOrDefault(x => x.Id == leaderId);

      try
      {
        var response = await leader.StrongGet(key);
        Console.WriteLine($"Response: {response.Value} {response.Version}");
        return response switch
        {
          { Version: int.MinValue, Value: "NOT_FOUND" } => NotFound(),
          { Version: not int.MinValue, Value: not "NOT_LEADER" } => Ok(response),
          _ => BadRequest() 
        };
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error: {e.Message}");
      }

      Console.WriteLine("Failed to find leader, retrying...");
    }
  }

  [HttpGet("eventual")]
  public async Task<ActionResult<string>> EventualGet(string key)
  {
    var node = GetRandomNode();
    var response = await node.EventualGet(key);
    return Ok(response);
  }

  [HttpPost("compare-and-swap")]
  public async Task<ActionResult<CompareAndSwapResponse>> CompareAndSwap(CompareAndSwapRequest request)
  {
    const int maxAttempts = 3;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
      try
      {
        var node = GetRandomNode();
        Console.WriteLine($"Requesting leader from {node.Id}");
        var leaderId = await node.MostRecentLeader();
        Console.WriteLine($"Leader is {leaderId}");
        var leader = _raftNodes.FirstOrDefault(x => x.Id == leaderId);
        var response = await leader.CompareAndSwap(request);

        if (response.Version >= 0)
        {
          return Ok(response);
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error on attempt {attempt + 1}: {e.Message}");
      }

      await Task.Delay(333);
    }

    return StatusCode(500, "Failed to complete compare-and-swap after maximum attempts");
  }

  private IRaftNode GetRandomNode()
  {
    var random = new Random();
    var index = random.Next(0, _raftNodes.Count);
    return _raftNodes[index];
  }
}
