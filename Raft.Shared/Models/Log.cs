using System.Text;

namespace Raft.Shared.Models;

public class Log
{
    private string _nodeId;
    private List<LogEntry> Entries { get; set; }
    private string logFile => $"raft-data/{_nodeId}log.dat";
    private readonly object _lock = new object();

    public Log(string nodeId)
    {
        _nodeId = nodeId.Replace(" ", "-");
        Entries = [];
        Load();
    }

    public LogEntry this[int index]
    {
        get
        {
            return Entries[index];
        }
    }

    public int Count => Entries.Count;

    public void Append(LogEntry entry)
    {
        lock (_lock)
        {
            AppendToFile(entry);
            Entries.Add(entry);
        }
    }

    public void AppendRange(List<LogEntry> entries)
    {
        lock (_lock)
        {
            foreach (var entry in entries)
            {
                AppendToFile(entry);
            }
            Entries.AddRange(entries);
        }
    }

    public void RemoveRange(int index, int count)
    {
        lock (_lock)
        {
            Entries.RemoveRange(index, count);
            RemoveRangeFromFile(index, count);
        }
    }

    public List<LogEntry> GetRange(int index, int count)
    {
        return Entries.GetRange(index, count);
    }

    private void AppendToFile(LogEntry entry)
    {
        lock (_lock)
        {
            var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Value));
            var stringEntry = $"{entry.Term} {entry.Key} {base64Value}\n";
            File.AppendAllText(logFile, stringEntry);
        }
    }

    private void RemoveRangeFromFile(int index, int count)
    {
        lock (_lock)
        {
            var lines = File.ReadAllLines(logFile).ToList();
            lines.RemoveRange(index, count);
            File.WriteAllLines(logFile, lines);
        }
    }

    private void Load()
    {
        lock (_lock)
        {
            Console.WriteLine("Loading log");
            if (!Directory.Exists("raft-data"))
            {
                Directory.CreateDirectory("raft-data");
            }
            if (!File.Exists(logFile))
            {
                File.Create(logFile).Close();
            }
            var lines = File.ReadAllLines(logFile);
            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                var term = int.Parse(parts[0]);
                var key = parts[1];
                var value = parts[2];
                value = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                Entries.Add(new LogEntry(term, key, value));
            }
        }
    }
}


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
