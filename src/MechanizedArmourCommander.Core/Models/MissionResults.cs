namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Results of a completed mission, used to apply rewards and consequences
/// </summary>
public class MissionResults
{
    public CombatResult Outcome { get; set; }
    public int CreditsEarned { get; set; }
    public int BonusCredits { get; set; }
    public int ReputationGained { get; set; }
    public List<int> SalvagedWeaponIds { get; set; } = new();

    /// <summary>
    /// Full pool of weapons available for salvage from destroyed enemies.
    /// Player picks from this pool; max picks determined by SalvageAllowance.
    /// </summary>
    public List<SalvageItem> SalvagePool { get; set; } = new();

    /// <summary>
    /// How many items the player may pick from the salvage pool
    /// </summary>
    public int SalvageAllowance { get; set; }

    public Dictionary<int, int> PilotXPGained { get; set; } = new(); // pilotId -> xp
    public List<FrameDamageReport> FrameDamageReports { get; set; } = new();
    public List<int> PilotsInjured { get; set; } = new(); // pilot IDs
    public List<int> PilotsKIA { get; set; } = new(); // pilot IDs
    public Dictionary<int, int> FactionStandingChanges { get; set; } = new(); // factionId -> delta

    /// <summary>
    /// Weapons auto-recovered from unpicked salvage pool items (scavenge rolls)
    /// </summary>
    public List<SalvageItem> ScavengedItems { get; set; } = new();

    /// <summary>
    /// Bonus weapons found from destroyed enemies (lucky finds)
    /// </summary>
    public List<SalvageItem> BonusLootItems { get; set; } = new();

    /// <summary>
    /// Payout level chosen pre-mission (0-4), used to scale credits and salvage
    /// </summary>
    public int PayoutLevel { get; set; } = 2;

    /// <summary>
    /// Frames available for salvage purchase from head-killed enemies
    /// </summary>
    public List<SalvageFrame> SalvageFrames { get; set; } = new();

    /// <summary>
    /// Frames the player has chosen to purchase from salvage
    /// </summary>
    public List<SalvageFrame> PurchasedSalvageFrames { get; set; } = new();
}

/// <summary>
/// A single weapon available in the salvage pool
/// </summary>
public class SalvageItem
{
    public int WeaponId { get; set; }
    public string WeaponName { get; set; } = string.Empty;
    public string HardpointSize { get; set; } = string.Empty;
    public int SalvageValue { get; set; }
    public string SourceFrame { get; set; } = string.Empty;
}

/// <summary>
/// An enemy frame available for salvage purchase (head-kill only)
/// </summary>
public class SalvageFrame
{
    public int ChassisId { get; set; }
    public string ChassisDesignation { get; set; } = string.Empty;
    public string ChassisName { get; set; } = string.Empty;
    public string ChassisClass { get; set; } = string.Empty;
    public string SourceFrame { get; set; } = string.Empty;

    // Combat damage snapshot — becomes the FrameInstance armor values
    public int ArmorHead { get; set; }
    public int ArmorCenterTorso { get; set; }
    public int ArmorLeftTorso { get; set; }
    public int ArmorRightTorso { get; set; }
    public int ArmorLeftArm { get; set; }
    public int ArmorRightArm { get; set; }
    public int ArmorLegs { get; set; }

    // Price = 40% of chassis base price
    public int SalvagePrice { get; set; }
}

/// <summary>
/// Damage report for a single frame after combat
/// </summary>
public class FrameDamageReport
{
    public int InstanceId { get; set; }
    public string FrameName { get; set; } = string.Empty;
    public int RepairCost { get; set; }
    public int RepairDays { get; set; }
    public bool IsDestroyed { get; set; }
    public float ArmorPercentRemaining { get; set; }
    public List<string> DestroyedLocations { get; set; } = new();
}
