namespace GameServer.Config;

public sealed class ServerConfig
{
    public static readonly ServerConfig Instance = Load();

    public int TcpPort { get; init; }
    public string MySqlConnectionString { get; init; } = "";
    public string RedisConnectionString { get; init; } = "";

    private static ServerConfig Load() => new()
    {
        TcpPort = int.TryParse(Env("TCP_PORT"), out var p) ? p : 9000,
        MySqlConnectionString = Env("MYSQL_CONN")
            ?? "Server=127.0.0.1;Port=3306;Database=game_server_cs;Uid=root;Pwd=password;" +
               "Pooling=true;MinimumPoolSize=5;MaximumPoolSize=100;CharacterSet=utf8mb4;",
        RedisConnectionString = Env("REDIS_CONN") ?? "127.0.0.1:6379",
    };

    private static string? Env(string key) => Environment.GetEnvironmentVariable(key);
}
