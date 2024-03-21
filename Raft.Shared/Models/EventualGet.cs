namespace Raft.Shared.Models;

public class EventualGetRequest
{
    public string Key { get; set; }
}

public class EventualGetResponse
{
    public string Value { get; set; }
}