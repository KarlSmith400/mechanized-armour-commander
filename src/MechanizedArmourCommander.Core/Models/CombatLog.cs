namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents the complete log of a combat engagement
/// </summary>
public class CombatLog
{
    public List<CombatRound> Rounds { get; set; } = new();
    public CombatResult Result { get; set; }
    public int TotalRounds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Represents a single round of combat
/// </summary>
public class CombatRound
{
    public int RoundNumber { get; set; }
    public List<CombatEvent> Events { get; set; } = new();
}

/// <summary>
/// Represents a single event within a combat round
/// </summary>
public class CombatEvent
{
    public CombatEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? AttackerId { get; set; }
    public string? AttackerName { get; set; }
    public int? DefenderId { get; set; }
    public string? DefenderName { get; set; }
    public int? Damage { get; set; }
    public string? HitLocation { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// Types of combat events
/// </summary>
public enum CombatEventType
{
    Movement,
    Attack,
    Hit,
    Miss,
    Critical,
    HeatUpdate,
    AmmoUpdate,
    FrameDestroyed,
    PilotCheck,
    RoundSummary
}

/// <summary>
/// Final result of combat
/// </summary>
public enum CombatResult
{
    Victory,
    Defeat,
    Withdrawal,
    Ongoing
}
