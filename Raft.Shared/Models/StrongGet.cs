namespace Raft.Shared.Models;

public class StrongGetRequest
{
    public string Key { get; set; }
}

public class StrongGetResponse
{
    public string Value { get; set; }
    public int Version { get; set; }
}