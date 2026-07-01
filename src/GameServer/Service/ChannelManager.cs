using System.Collections.Concurrent;

namespace GameServer.Service;

public sealed class ChannelManager
{
    public static readonly ChannelManager Instance = new();

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, byte>> _channels = new();

    private ChannelManager() { }

    public void Enter(int channelId, Guid sessionId)
    {
        var ch = _channels.GetOrAdd(channelId, _ => new ConcurrentDictionary<Guid, byte>());
        ch[sessionId] = 0;
    }

    public void Leave(int channelId, Guid sessionId)
    {
        if (_channels.TryGetValue(channelId, out var ch))
            ch.TryRemove(sessionId, out _);
    }

    public void LeaveAll(Guid sessionId)
    {
        foreach (var ch in _channels.Values)
            ch.TryRemove(sessionId, out _);
    }

    public IReadOnlyList<Guid> GetMembers(int channelId)
    {
        if (_channels.TryGetValue(channelId, out var ch))
            return ch.Keys.ToList();
        return Array.Empty<Guid>();
    }
}
