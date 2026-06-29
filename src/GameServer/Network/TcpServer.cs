using System.Net;
using System.Net.Sockets;

namespace GameServer.Network;

public sealed class TcpServer
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public TcpServer(int port)
        => _listener = new TcpListener(IPAddress.Any, port);

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine($"[TcpServer] Listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");
        await AcceptLoopAsync();
    }

    public Task StopAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        Console.WriteLine("[TcpServer] Stopped.");
        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                var session = new ClientSession(client, _cts.Token);
                SessionManager.Instance.Add(session);
                Console.WriteLine($"[TcpServer] Session connected: {session.SessionId} (total: {SessionManager.Instance.Count})");
                _ = session.StartAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer] Accept error: {ex.Message}");
            }
        }
    }
}
