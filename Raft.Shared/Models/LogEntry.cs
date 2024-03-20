namespace Raft.Shared;

public class LogEntry
{
    public int Term { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }

    public LogEntry(int term, string key, string value)
    {
        Term = term;
        Key = key;
        Value = value;
    }
}
