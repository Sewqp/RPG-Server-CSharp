using GameServer.DB.Repository;

namespace GameServer.DB;

public sealed class SyncWorker
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private readonly CancellationToken _ct;

    public SyncWorker(CancellationToken ct) => _ct = ct;

    public async Task RunAsync()
    {
        Console.WriteLine("[SyncWorker] Started (interval: 30s).");
        while (!_ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, _ct);
                await SyncDirtyCharactersAsync();
            }
            catch (OperationCanceledException)
            {
                // 종료 시 마지막 동기화
                await SyncDirtyCharactersAsync();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] Error: {ex.Message}");
            }
        }
        Console.WriteLine("[SyncWorker] Stopped.");
    }

    private async Task SyncDirtyCharactersAsync()
    {
        var db = RedisClient.Instance.Db;
        var members = await db.SetMembersAsync("dirty_characters");
        if (members.Length == 0) return;

        // 먼저 Set에서 제거 후 DB 반영 (중복 처리 방지)
        await db.SetRemoveAsync("dirty_characters", members);

        var tasks = members
            .Select(m => long.TryParse((string?)m, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => FlushOneAsync(id!.Value));

        await Task.WhenAll(tasks);
        Console.WriteLine($"[SyncWorker] Flushed {members.Length} character(s) to MySQL.");
    }

    private async Task FlushOneAsync(long playerId)
    {
        var stat = await CharacterStatRepository.Instance.GetFromCacheOnlyAsync(playerId);
        if (stat == null) return;
        await CharacterStatRepository.Instance.WriteToDbAsync(stat);
    }
}
