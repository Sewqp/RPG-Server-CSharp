using System.Text.Json;
using MySqlConnector;
using StackExchange.Redis;
using GameServer.DB.Model;

namespace GameServer.DB.Repository;

public sealed class CharacterStatRepository
{
    public static readonly CharacterStatRepository Instance = new();
    private CharacterStatRepository() { }

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private static RedisKey CacheKey(long playerId) => $"char:stat:{playerId}";

    // 캐시 우선 조회 (로그인/스탯 로드 시)
    public async Task<CharacterStatModel?> GetByIdAsync(long playerId)
    {
        var db = RedisClient.Instance.Db;
        var cached = await db.StringGetAsync(CacheKey(playerId));
        if (cached.HasValue)
            return JsonSerializer.Deserialize<CharacterStatModel>((string)cached!);

        var model = await FetchFromDbAsync(playerId);
        if (model != null)
            await db.StringSetAsync(CacheKey(playerId), JsonSerializer.Serialize(model), CacheTtl);

        return model;
    }

    // Redis 캐시만 업데이트 + dirty 마킹 (게임플레이 중 빠른 경로)
    public async Task UpdateCacheAndMarkDirtyAsync(CharacterStatModel stat)
    {
        var db = RedisClient.Instance.Db;
        var batch = db.CreateBatch();
        var t1 = batch.StringSetAsync(CacheKey(stat.PlayerId), JsonSerializer.Serialize(stat), CacheTtl);
        var t2 = batch.SetAddAsync("dirty_characters", stat.PlayerId.ToString());
        batch.Execute();
        await Task.WhenAll(t1, t2);
    }

    // DB + 캐시 동시 저장 (캐릭터 생성/강제 동기화 시)
    public async Task UpsertAsync(CharacterStatModel stat)
    {
        await WriteToDbAsync(stat);
        await RedisClient.Instance.Db.StringSetAsync(
            CacheKey(stat.PlayerId), JsonSerializer.Serialize(stat), CacheTtl);
    }

    // SyncWorker 전용: Redis에서 스탯 읽기
    internal async Task<CharacterStatModel?> GetFromCacheOnlyAsync(long playerId)
    {
        var cached = await RedisClient.Instance.Db.StringGetAsync(CacheKey(playerId));
        return cached.HasValue
            ? JsonSerializer.Deserialize<CharacterStatModel>((string)cached!)
            : null;
    }

    // SyncWorker 전용: MySQL에만 쓰기
    internal async Task WriteToDbAsync(CharacterStatModel stat)
    {
        await using var conn = DbConnectionPool.Instance.GetConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO character_stat (player_id, level, hp_max, hp, mp_max, mp, is_alive, last_map_id)
            VALUES (@pid, @level, @hpMax, @hp, @mpMax, @mp, @alive, @mapId)
            ON DUPLICATE KEY UPDATE
                level      = VALUES(level),
                hp_max     = VALUES(hp_max),
                hp         = VALUES(hp),
                mp_max     = VALUES(mp_max),
                mp         = VALUES(mp),
                is_alive   = VALUES(is_alive),
                last_map_id = VALUES(last_map_id)
            """;
        cmd.Parameters.AddWithValue("@pid",   stat.PlayerId);
        cmd.Parameters.AddWithValue("@level", stat.Level);
        cmd.Parameters.AddWithValue("@hpMax", stat.HpMax);
        cmd.Parameters.AddWithValue("@hp",    stat.Hp);
        cmd.Parameters.AddWithValue("@mpMax", stat.MpMax);
        cmd.Parameters.AddWithValue("@mp",    stat.Mp);
        cmd.Parameters.AddWithValue("@alive", stat.IsAlive ? 1 : 0);
        cmd.Parameters.AddWithValue("@mapId", stat.LastMapId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<CharacterStatModel?> FetchFromDbAsync(long playerId)
    {
        await using var conn = DbConnectionPool.Instance.GetConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT player_id, level, hp_max, hp, mp_max, mp, is_alive, last_map_id " +
            "FROM character_stat WHERE player_id = @id";
        cmd.Parameters.AddWithValue("@id", playerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new CharacterStatModel
        {
            PlayerId  = reader.GetInt64(0),
            Level     = reader.GetInt32(1),
            HpMax     = reader.GetInt32(2),
            Hp        = reader.GetInt32(3),
            MpMax     = reader.GetInt32(4),
            Mp        = reader.GetInt32(5),
            IsAlive   = reader.GetByte(6) == 1,
            LastMapId = reader.GetInt32(7),
        };
    }
}
