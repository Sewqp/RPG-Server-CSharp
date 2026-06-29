using MySqlConnector;
using GameServer.DB.Model;

namespace GameServer.DB.Repository;

public sealed class PlayerRepository
{
    public static readonly PlayerRepository Instance = new();
    private PlayerRepository() { }

    public async Task<PlayerModel?> GetByIdAsync(long playerId)
    {
        await using var conn = DbConnectionPool.Instance.GetConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT player_id, pname, status, created_at, updated_at " +
            "FROM player WHERE player_id = @id";
        cmd.Parameters.AddWithValue("@id", playerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new PlayerModel
        {
            PlayerId  = reader.GetInt64(0),
            PName     = reader.GetString(1),
            Status    = reader.GetByte(2),
            CreatedAt = reader.GetDateTime(3),
            UpdatedAt = reader.GetDateTime(4),
        };
    }

    public async Task<long> CreateAsync(string name)
    {
        await using var conn = DbConnectionPool.Instance.GetConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO player (pname) VALUES (@name)";
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
        return cmd.LastInsertedId;
    }
}
