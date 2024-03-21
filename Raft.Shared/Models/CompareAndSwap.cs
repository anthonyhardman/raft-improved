namespace Raft.Shared.Models;

public class CompareAndSwapRequest
{
    public string Key { get; set; }
    public string NewValue { get; set; }
    public string ExpectedValue { get; set; }
    public int Version { get; set; }
}

public class CompareAndSwapResponse
{
    public bool Success { get; set; }
}