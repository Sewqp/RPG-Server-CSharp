using System.Text;
using GameServer.Network;
using GameServer.Service;

namespace GameServer.Packet.Handler;

public static class ChatHandler
{
    public static Task HandleEnterRoomAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + 4) return Task.CompletedTask;
        int channelId = BitConverter.ToInt32(packet.Span[PacketHeader.HeaderSize..]);
        ChannelManager.Instance.Enter(channelId, session.SessionId);
        return Task.CompletedTask;
    }

    public static Task HandleLeaveRoomAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + 4) return Task.CompletedTask;
        int channelId = BitConverter.ToInt32(packet.Span[PacketHeader.HeaderSize..]);
        ChannelManager.Instance.Leave(channelId, session.SessionId);
        return Task.CompletedTask;
    }

    public static async Task HandleChatAsync(ClientSession session, Memory<byte> packet)
    {
        // payload: [4B channelId][2B msgLen][msgBytes]
        if (packet.Length < PacketHeader.HeaderSize + 6) return;
        var payload = packet.Span[PacketHeader.HeaderSize..];
        int channelId = BitConverter.ToInt32(payload);
        ushort msgLen = BitConverter.ToUInt16(payload[4..]);
        if (payload.Length < 6 + msgLen) return;
        var message = Encoding.UTF8.GetString(payload.Slice(6, msgLen));
        await ChatService.BroadcastToChannelAsync(channelId, session.PlayerId, message);
    }

    public static async Task HandleWhisperAsync(ClientSession session, Memory<byte> packet)
    {
        // payload: [8B targetPlayerId][2B msgLen][msgBytes]
        if (packet.Length < PacketHeader.HeaderSize + 10) return;
        var payload = packet.Span[PacketHeader.HeaderSize..];
        long targetPlayerId = BitConverter.ToInt64(payload);
        ushort msgLen = BitConverter.ToUInt16(payload[8..]);
        if (payload.Length < 10 + msgLen) return;
        var message = Encoding.UTF8.GetString(payload.Slice(10, msgLen));
        await ChatService.SendWhisperAsync(targetPlayerId, session.PlayerId, message);
    }
}
