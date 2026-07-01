using System.Net.Sockets;
using GameServer.Packet;
using GameServer.Service;

namespace GameServer.Network;

public sealed class ClientSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly PacketBuffer _recvBuffer = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationToken _ct;

    public Guid SessionId { get; } = Guid.NewGuid();
    public long PlayerId { get; set; }
    public DateTime LastReceivedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateLastReceived() => LastReceivedAt = DateTime.UtcNow;

    public ClientSession(TcpClient client, CancellationToken ct)
    {
        _client = client;
        _stream = client.GetStream();
        _ct = ct;
    }

    public async Task StartAsync()
    {
        try
        {
            await RecvLoopAsync();
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    public async Task SendAsync(byte[] data)
    {
        await _sendLock.WaitAsync(_ct);
        try
        {
            await _stream.WriteAsync(data, _ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public Task DisconnectAsync()
    {
        ChannelManager.Instance.LeaveAll(SessionId);
        SessionManager.Instance.UnregisterPlayerId(PlayerId);
        SessionManager.Instance.Remove(SessionId);
        _stream.Close();
        _client.Close();
        return Task.CompletedTask;
    }

    private async Task RecvLoopAsync()
    {
        var buffer = new byte[PacketBuffer.MaxPacketSize];

        while (!_ct.IsCancellationRequested)
        {
            int read = await _stream.ReadAsync(buffer, _ct);
            if (read == 0) break;

            UpdateLastReceived();
            if (!_recvBuffer.Write(buffer.AsSpan(0, read))) break;

            Memory<byte>? packet;
            while ((packet = _recvBuffer.TryAssemble()) != null)
                await PacketDispatcher.Instance.DispatchAsync(this, packet.Value);
        }
    }
}
