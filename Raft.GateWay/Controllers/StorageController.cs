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
        private readonly IRaftNode _raftNode;

        public StorageController(IRaftNode raftNode)
        {
            _raftNode = raftNode;
        }

        [HttpGet("strong")]
        public async Task<ActionResult<string>> StrongGet(string key)
        {
            var response = await _raftNode.StrongGet(key);
            return Ok(response);
        }

        [HttpGet("eventual")]
        public async Task<ActionResult<string>> EventualGet(string key)
        {
            var response = await _raftNode.EventualGet(key);
            return Ok(response);
        }

        [HttpPost("compare-and-swap")]
        public async Task<ActionResult<bool>> CompareAndSwap(CompareAndSwapRequest request)
        {
            var response = await _raftNode.CompareAndSwap(request);
            return Ok(response);
        }
    }
}
