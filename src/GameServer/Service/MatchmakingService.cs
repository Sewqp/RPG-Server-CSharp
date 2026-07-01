using System.Collections.Concurrent;
using GameServer.Network;
using GameServer.Packet;

namespace GameServer.Service;

public sealed class MatchmakingService
{
    public static readonly MatchmakingService Instance = new();

    private readonly ConcurrentQueue<Guid> _queue = new();
    private long _nextMatchId;

    private MatchmakingService() { }

    public async Task EnqueueAsync(Guid sessionId)
    {
        _queue.Enqueue(sessionId);
        await TryMatchAsync();
    }

    private async Task TryMatchAsync()
    {
        if (_queue.Count < 2) return;
        if (!_queue.TryDequeue(out var a) || !_queue.TryDequeue(out var b))
        {
            // 두 번째 dequeue 실패 시 첫 번째를 돌려놓음
            if (_queue.TryPeek(out _) == false)
                _queue.Enqueue(a);
            return;
        }

        long matchId = Interlocked.Increment(ref _nextMatchId);
        Console.WriteLine($"[Match] {a} <-> {b}, matchId={matchId}");

        await Task.WhenAll(NotifyAsync(a, matchId), NotifyAsync(b, matchId));
    }

    private static async Task NotifyAsync(Guid sessionId, long matchId)
    {
        var session = SessionManager.Instance.Get(sessionId);
        if (session == null) return;

        // payload: [1B success][8B matchId]
        var payload = new byte[9];
        payload[0] = 1;
        BitConverter.TryWriteBytes(payload.AsSpan(1), matchId);
        await session.SendAsync(PacketWriter.Build(PacketId.MatchResult, payload));
    }
}
