namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Tactical decisions that can be made at the start of each combat round
/// </summary>
public class RoundTacticalDecision
{
    /// <summary>
    /// Override stance for this round only (null = use default orders)
    /// </summary>
    public Stance? StanceOverride { get; set; }

    /// <summary>
    /// Override target priority for this round only
    /// </summary>
    public TargetPriority? TargetPriorityOverride { get; set; }

    /// <summary>
    /// Specific frame to focus fire on (by InstanceId)
    /// </summary>
    public int? FocusTargetId { get; set; }

    /// <summary>
    /// Attempt to withdraw this round
    /// </summary>
    public bool AttemptWithdrawal { get; set; }

    /// <summary>
    /// Frame-specific commands (key: InstanceId, value: command)
    /// </summary>
    public Dictionary<int, FrameCommand> FrameCommands { get; set; } = new();
}

/// <summary>
/// Commands for individual frames
/// </summary>
public class FrameCommand
{
    /// <summary>
    /// Hold fire this round (conserve ammo/heat)
    /// </summary>
    public bool HoldFire { get; set; }

    /// <summary>
    /// Focus on evasion this round (movement bonus, accuracy penalty)
    /// </summary>
    public bool Evasive { get; set; }

    /// <summary>
    /// All-out attack (accuracy bonus, evasion penalty)
    /// </summary>
    public bool AllOut { get; set; }

    /// <summary>
    /// Specific target for this frame
    /// </summary>
    public int? TargetId { get; set; }
}

/// <summary>
/// Information about combat state presented to player for decision-making
/// </summary>
public class RoundSituation
{
    public int RoundNumber { get; set; }
    public List<FrameSituation> PlayerFrames { get; set; } = new();
    public List<FrameSituation> EnemyFrames { get; set; } = new();
    public string LastRoundSummary { get; set; } = string.Empty;
    public int AverageDistance { get; set; }
    public string RangeBand { get; set; } = string.Empty;
    public int PlayerLosses { get; set; }
    public int EnemyLosses { get; set; }
}

/// <summary>
/// Status of an individual frame for tactical display
/// </summary>
public class FrameSituation
{
    public int InstanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int CurrentArmor { get; set; }
    public int MaxArmor { get; set; }
    public float ArmorPercent => MaxArmor > 0 ? (float)CurrentArmor / MaxArmor * 100 : 0;
    public int CurrentHeat { get; set; }
    public int MaxHeat { get; set; }
    public float HeatPercent => MaxHeat > 0 ? (float)CurrentHeat / MaxHeat * 100 : 0;
    public int CurrentAmmo { get; set; }
    public int MaxAmmo { get; set; }
    public int Position { get; set; }
    public bool IsDestroyed { get; set; }
    public bool IsOverheating { get; set; }
    public bool IsLowAmmo => CurrentAmmo < 20;
    public string Status
    {
        get
        {
            if (IsDestroyed) return "DESTROYED";
            if (IsOverheating) return "OVERHEATING";
            if (ArmorPercent < 25) return "CRITICAL";
            if (ArmorPercent < 50) return "DAMAGED";
            return "OPERATIONAL";
        }
    }
}
