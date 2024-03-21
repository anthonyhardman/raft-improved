using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raft.Shared;
using Raft.Shared.Models;

namespace MyApp.Namespace
{
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
    public async Task<ActionResult<string>> StrongGet(string key)
    {
      var node = _raftNodes.First();

      var response = await node.StrongGet(key);
      return Ok(response);
    }

    [HttpGet("eventual")]
    public async Task<ActionResult<string>> EventualGet(string key)
    {
      var node = _raftNodes.First();

      var response = await node.EventualGet(key);
      return Ok(response);
    }

    [HttpPost("compare-and-swap")]
    public async Task<ActionResult<bool>> CompareAndSwap(CompareAndSwapRequest request)
    {
      var node = _raftNodes.First();

      var response = await node.CompareAndSwap(request);
      return Ok(response);
    }
  }
}
