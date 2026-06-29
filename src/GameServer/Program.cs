using GameServer.Config;
using GameServer.DB;
using GameServer.Network;

var config = ServerConfig.Instance;

DbConnectionPool.Instance.Init(config.MySqlConnectionString);
Console.WriteLine("[DB] MySQL connection pool ready.");

RedisClient.Instance.Init(config.RedisConnectionString);
Console.WriteLine("[DB] Redis connected.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

_ = new SyncWorker(cts.Token).RunAsync();

var server = new TcpServer(config.TcpPort, cts.Token);
await server.StartAsync();
