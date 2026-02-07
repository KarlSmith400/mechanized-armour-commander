namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents a frame participating in combat with full runtime state
/// </summary>
public class CombatFrame
{
    public int InstanceId { get; set; }
    public string CustomName { get; set; } = string.Empty;
    public string ChassisDesignation { get; set; } = string.Empty;
    public string ChassisName { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;

    // Reactor System
    public int ReactorOutput { get; set; }         // Max energy per round
    public int CurrentEnergy { get; set; }         // Energy remaining this round
    public int ReactorStress { get; set; }         // Accumulated overload stress
    public int MovementEnergyCost { get; set; }    // Energy to move one range band
    public bool IsShutDown { get; set; }           // Forced shutdown from reactor stress

    // Action Economy
    public int ActionPoints { get; set; } = 2;
    public int MaxActionPoints { get; set; } = 2;

    // Location-based armor (current values, ablative protection)
    public Dictionary<HitLocation, int> Armor { get; set; } = new();
    public Dictionary<HitLocation, int> MaxArmor { get; set; } = new();

    // Location-based structure (fixed by chassis)
    public Dictionary<HitLocation, int> Structure { get; set; } = new();
    public Dictionary<HitLocation, int> MaxStructure { get; set; } = new();

    // Location destruction tracking
    public HashSet<HitLocation> DestroyedLocations { get; set; } = new();

    // Component damage tracking
    public List<ComponentDamage> DamagedComponents { get; set; } = new();

    // Ammo tracking by weapon type
    public Dictionary<string, int> AmmoByType { get; set; } = new();

    // Performance
    public int Speed { get; set; }
    public int Evasion { get; set; }

    // Pilot assignment
    public int? PilotId { get; set; }
    public string? PilotCallsign { get; set; }
    public int PilotGunnery { get; set; }
    public int PilotPiloting { get; set; }
    public int PilotTactics { get; set; }

    // Weapon Groups (group number -> weapons in that group)
    public Dictionary<int, List<EquippedWeapon>> WeaponGroups { get; set; } = new();

    // Range band positioning
    public RangeBand CurrentRange { get; set; } = RangeBand.Long;

    // Display coordinates for battlefield map (UI use)
    public double MapX { get; set; }
    public double MapY { get; set; }

    // Combat state flags
    public bool IsBracing { get; set; }
    public bool IsOnOverwatch { get; set; }

    // State queries
    public bool IsDestroyed => DestroyedLocations.Contains(HitLocation.CenterTorso)
                             || Structure.GetValueOrDefault(HitLocation.CenterTorso, 0) <= 0;

    public bool HasGyroHit => DamagedComponents.Any(c => c.Type == ComponentDamageType.GyroHit);
    public bool HasSensorHit => DamagedComponents.Any(c => c.Type == ComponentDamageType.SensorHit);
    public bool HasReactorHit => DamagedComponents.Any(c => c.Type == ComponentDamageType.ReactorHit);

    public int TotalArmorRemaining => Armor.Values.Sum();
    public int TotalArmorMax => MaxArmor.Values.Sum();
    public float ArmorPercent => TotalArmorMax > 0 ? (float)TotalArmorRemaining / TotalArmorMax * 100 : 0;

    /// <summary>
    /// Gets the effective reactor output (reduced by reactor hits)
    /// </summary>
    public int EffectiveReactorOutput
    {
        get
        {
            int reactorHits = DamagedComponents.Count(c => c.Type == ComponentDamageType.ReactorHit);
            return Math.Max(1, ReactorOutput - (reactorHits * 3));
        }
    }

    /// <summary>
    /// Gets all functional (non-destroyed) weapons across all groups
    /// </summary>
    public IEnumerable<EquippedWeapon> FunctionalWeapons =>
        WeaponGroups.Values.SelectMany(g => g).Where(w => !w.IsDestroyed);
}

/// <summary>
/// Represents a weapon currently equipped and ready to fire
/// </summary>
public class EquippedWeapon
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HardpointSize { get; set; } = string.Empty;
    public string WeaponType { get; set; } = string.Empty;     // Energy/Ballistic/Missile
    public int EnergyCost { get; set; }
    public int AmmoPerShot { get; set; }
    public string AmmoType { get; set; } = string.Empty;
    public int Damage { get; set; }
    public string RangeClass { get; set; } = string.Empty;
    public int BaseAccuracy { get; set; }
    public int WeaponGroup { get; set; }
    public HitLocation MountLocation { get; set; }
    public bool IsDestroyed { get; set; }
    public string? SpecialEffect { get; set; }
}
