namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Tactical decisions made at the start of each combat round (per-frame action orders)
/// </summary>
public class RoundTacticalDecision
{
    /// <summary>
    /// Per-frame action orders (key: InstanceId)
    /// </summary>
    public Dictionary<int, FrameActions> FrameOrders { get; set; } = new();

    /// <summary>
    /// Attempt to withdraw this round
    /// </summary>
    public bool AttemptWithdrawal { get; set; }
}

/// <summary>
/// Planned actions for a single frame in a round
/// </summary>
public class FrameActions
{
    public List<PlannedAction> Actions { get; set; } = new();
    public int? FocusTargetId { get; set; }
}

/// <summary>
/// A single planned action for a frame
/// </summary>
public class PlannedAction
{
    public CombatAction Action { get; set; }
    public int? WeaponGroupId { get; set; }            // For FireGroup action
    public HitLocation? CalledShotLocation { get; set; } // For CalledShot action
    public MovementDirection? MoveDirection { get; set; } // For Move/Sprint action
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
    public RangeBand CurrentRangeBand { get; set; }
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

    // Per-location armor status
    public Dictionary<HitLocation, int> Armor { get; set; } = new();
    public Dictionary<HitLocation, int> MaxArmor { get; set; } = new();
    public Dictionary<HitLocation, int> Structure { get; set; } = new();
    public Dictionary<HitLocation, int> MaxStructure { get; set; } = new();
    public HashSet<HitLocation> DestroyedLocations { get; set; } = new();

    // Reactor status
    public int ReactorOutput { get; set; }
    public int CurrentEnergy { get; set; }
    public int ReactorStress { get; set; }
    public int MovementEnergyCost { get; set; }

    // Action points
    public int ActionPoints { get; set; }

    // Range
    public RangeBand CurrentRange { get; set; }

    // Weapon groups with status
    public Dictionary<int, List<WeaponGroupInfo>> WeaponGroups { get; set; } = new();

    // Ammo
    public Dictionary<string, int> AmmoByType { get; set; } = new();

    // Component damage
    public List<ComponentDamage> DamagedComponents { get; set; } = new();

    // State
    public bool IsDestroyed { get; set; }
    public bool IsShutDown { get; set; }

    public float ArmorPercent
    {
        get
        {
            int total = MaxArmor.Values.Sum();
            return total > 0 ? (float)Armor.Values.Sum() / total * 100 : 0;
        }
    }

    public string Status
    {
        get
        {
            if (IsDestroyed) return "DESTROYED";
            if (IsShutDown) return "SHUTDOWN";
            if (ArmorPercent < 25) return "CRITICAL";
            if (ArmorPercent < 50) return "DAMAGED";
            return "OPERATIONAL";
        }
    }
}

/// <summary>
/// Summary info for a weapon in a group (for UI display)
/// </summary>
public class WeaponGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string WeaponType { get; set; } = string.Empty;
    public int EnergyCost { get; set; }
    public int AmmoPerShot { get; set; }
    public int Damage { get; set; }
    public string RangeClass { get; set; } = string.Empty;
    public bool IsDestroyed { get; set; }
}
