using System.Text;
using GameServer.Network;
using GameServer.Packet;

namespace GameServer.Service;

public static class ChatService
{
    public static async Task BroadcastToChannelAsync(int channelId, long senderId, string message)
    {
        var msgBytes = Encoding.UTF8.GetBytes(message);
        // payload: [4B channelId][8B senderId][2B msgLen][msgBytes]
        var payload = new byte[14 + msgBytes.Length];
        BitConverter.TryWriteBytes(payload.AsSpan(0), channelId);
        BitConverter.TryWriteBytes(payload.AsSpan(4), senderId);
        BitConverter.TryWriteBytes(payload.AsSpan(12), (ushort)msgBytes.Length);
        msgBytes.CopyTo(payload.AsSpan(14));

        var packet = PacketWriter.Build(PacketId.ChatMessage, payload);
        var members = ChannelManager.Instance.GetMembers(channelId);
        var tasks = members
            .Select(id => SessionManager.Instance.Get(id))
            .Where(s => s != null)
            .Select(s => s!.SendAsync(packet));
        await Task.WhenAll(tasks);
    }

    public static async Task SendWhisperAsync(long targetPlayerId, long senderId, string message)
    {
        var target = SessionManager.Instance.GetByPlayerId(targetPlayerId);
        if (target == null) return;

        var msgBytes = Encoding.UTF8.GetBytes(message);
        // payload: [8B senderId][2B msgLen][msgBytes]
        var payload = new byte[10 + msgBytes.Length];
        BitConverter.TryWriteBytes(payload.AsSpan(0), senderId);
        BitConverter.TryWriteBytes(payload.AsSpan(8), (ushort)msgBytes.Length);
        msgBytes.CopyTo(payload.AsSpan(10));

        await target.SendAsync(PacketWriter.Build(PacketId.WhisperMessage, payload));
    }
}
