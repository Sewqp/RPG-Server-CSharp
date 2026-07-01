using GameServer.Network;
using GameServer.Service;

namespace GameServer.Packet.Handler;

public static class MatchHandler
{
    public static async Task HandleAsync(ClientSession session, Memory<byte> packet)
    {
        await MatchmakingService.Instance.EnqueueAsync(session.SessionId);
    }
}
