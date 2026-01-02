namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents tactical orders set by the player before combat
/// </summary>
public class TacticalOrders
{
    public Stance Stance { get; set; } = Stance.Balanced;
    public TargetPriority TargetPriority { get; set; } = TargetPriority.ThreatPriority;
    public Formation Formation { get; set; } = Formation.Tight;
    public WithdrawalThreshold WithdrawalThreshold { get; set; } = WithdrawalThreshold.FightToEnd;
}

/// <summary>
/// Determines how aggressively frames engage
/// </summary>
public enum Stance
{
    Aggressive,  // Close distance, prioritize damage output
    Balanced,    // Maintain optimal range, balanced approach
    Defensive    // Maintain distance, prioritize survival
}

/// <summary>
/// Determines which enemies to target
/// </summary>
public enum TargetPriority
{
    FocusFire,      // All units target same enemy
    SpreadDamage,   // Distribute fire across multiple targets
    ThreatPriority, // Target heaviest/most dangerous first
    Opportunity     // Target weakest/most damaged first
}

/// <summary>
/// Determines unit positioning
/// </summary>
public enum Formation
{
    Tight,     // Stay close for mutual support
    Spread,    // Disperse to avoid concentrated fire
    Flanking   // Attempt to surround enemy
}

/// <summary>
/// Determines when to retreat from combat
/// </summary>
public enum WithdrawalThreshold
{
    FightToEnd,   // No retreat
    RetreatAt50,  // Pull back when half frames damaged
    RetreatAt25   // Conservative approach
}
