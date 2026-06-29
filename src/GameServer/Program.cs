using GameServer.Network;

var server = new TcpServer(9000);

Console.CancelKeyPress += async (_, e) =>
{
    e.Cancel = true;
    await server.StopAsync();
};

await server.StartAsync();
