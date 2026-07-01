using GameServer.Config;
using GameServer.DB;
using GameServer.Network;
using GameServer.Packet;
using GameServer.Packet.Handler;

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
_ = new HeartbeatManager(cts.Token).RunAsync();

var dispatcher = PacketDispatcher.Instance;
dispatcher.Register(PacketId.Heartbeat,        HeartbeatHandler.HandleAsync);
dispatcher.Register(PacketId.ReconnectRequest, ReconnectHandler.HandleAsync);
dispatcher.Register(PacketId.EnterRoom,        ChatHandler.HandleEnterRoomAsync);
dispatcher.Register(PacketId.LeaveRoom,        ChatHandler.HandleLeaveRoomAsync);
dispatcher.Register(PacketId.ChatMessage,      ChatHandler.HandleChatAsync);
dispatcher.Register(PacketId.WhisperMessage,   ChatHandler.HandleWhisperAsync);
dispatcher.Register(PacketId.MatchRequest,     MatchHandler.HandleAsync);

var server = new TcpServer(config.TcpPort, cts.Token);
await server.StartAsync();
