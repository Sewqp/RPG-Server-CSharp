namespace GameServer.Packet;

public enum PacketId : ushort
{
    CharacterInfo    = 1002,
    CharacterStat    = 1003,
    EnterRoom        = 1008,
    LeaveRoom        = 1009,
    ChatMessage      = 2000,
    WhisperMessage   = 2001,
    MatchRequest     = 3000,
    MatchResult      = 3001,
    Heartbeat        = 9000,
    ReconnectRequest = 9001,
    ReconnectResult  = 9002,
}
