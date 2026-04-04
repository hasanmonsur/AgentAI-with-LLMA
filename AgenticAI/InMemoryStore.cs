namespace AgenticAI;

/// <summary>
/// Thread-safe in-process memory store. Swap for Redis, SQLite, etc.
/// </summary>
public sealed class InMemoryStore : IMemoryStore
{
    private readonly Dictionary<string, object> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public void Set(string key, object value)
    {
        lock (_lock) _data[key] = value;
    }

    public T? Get<T>(string key)
    {
        lock (_lock)
        {
            if (_data.TryGetValue(key, out var raw) && raw is T typed)
                return typed;
            return default;
        }
    }

    public bool TryGet<T>(string key, out T? value)
    {
        lock (_lock)
        {
            if (_data.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }
    }

    public IReadOnlyDictionary<string, object> GetAll()
    {
        lock (_lock) return _data.ToDictionary(k => k.Key, v => v.Value);
    }

    public void Clear()
    {
        lock (_lock) _data.Clear();
    }

    public override string ToString()
    {
        lock (_lock)
        {
            if (_data.Count == 0) return "(empty)";
            return string.Join("\n", _data.Select(kv => $"  [{kv.Key}] = {kv.Value}"));
        }
    }
}
