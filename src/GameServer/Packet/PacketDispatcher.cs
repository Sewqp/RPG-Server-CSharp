using GameServer.Network;

namespace GameServer.Packet;

public sealed class PacketDispatcher
{
    public static readonly PacketDispatcher Instance = new();

    private readonly Dictionary<PacketId, Func<ClientSession, Memory<byte>, Task>> _handlers = new();

    private PacketDispatcher() { }

    public void Register(PacketId id, Func<ClientSession, Memory<byte>, Task> handler)
        => _handlers[id] = handler;

    public async Task DispatchAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize) return;

        var id = (PacketId)BitConverter.ToUInt16(packet.Span[sizeof(ushort)..]);
        if (_handlers.TryGetValue(id, out var handler))
            await handler(session, packet);
    }
}
