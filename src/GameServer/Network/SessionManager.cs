using System.Collections.Concurrent;

namespace GameServer.Network;

public sealed class SessionManager
{
    public static readonly SessionManager Instance = new();

    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();

    private SessionManager() { }

    public void Add(ClientSession session) => _sessions[session.SessionId] = session;

    public bool Remove(Guid sessionId) => _sessions.TryRemove(sessionId, out _);

    public ClientSession? Get(Guid sessionId) => _sessions.GetValueOrDefault(sessionId);

    public int Count => _sessions.Count;

    public async Task BroadcastAsync(byte[] data)
    {
        var tasks = _sessions.Values.Select(s => s.SendAsync(data));
        await Task.WhenAll(tasks);
    }
}
