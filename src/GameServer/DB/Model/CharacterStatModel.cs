namespace GameServer.DB.Model;

public sealed class CharacterStatModel
{
    public long PlayerId { get; set; }
    public int Level { get; set; }
    public int HpMax { get; set; }
    public int Hp { get; set; }
    public int MpMax { get; set; }
    public int Mp { get; set; }
    public bool IsAlive { get; set; }
    public int LastMapId { get; set; }
}
