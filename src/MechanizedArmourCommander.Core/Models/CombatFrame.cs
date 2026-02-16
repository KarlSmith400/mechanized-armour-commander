namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents a frame participating in combat with full runtime state
/// </summary>
public class CombatFrame
{
    public int InstanceId { get; set; }
    public int ChassisId { get; set; }
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

    // Equipped equipment
    public List<EquippedEquipment> Equipment { get; set; } = new();

    // Hex grid positioning
    public HexCoord HexPosition { get; set; }

    // Movement range in hexes per Move action (based on weight class)
    public int HexMovement => Class switch
    {
        "Light" => 4,
        "Medium" => 3,
        "Heavy" => 2,
        "Assault" => 1,
        _ => 2
    };

    // Display coordinates for battlefield map (UI use, derived from hex position)
    public double MapX { get; set; }
    public double MapY { get; set; }

    // Track if this frame has already acted this round
    public bool HasActedThisRound { get; set; }

    // Combat state flags
    public bool IsBracing { get; set; }
    public bool IsOnOverwatch { get; set; }
    public bool IsPilotDead { get; set; }        // Pilot killed by head destruction

    // State queries
    public bool IsDestroyed => DestroyedLocations.Contains(HitLocation.CenterTorso)
                             || Structure.GetValueOrDefault(HitLocation.CenterTorso, 0) <= 0
                             || IsPilotDead;
    public bool HasHeadDestroyed => DestroyedLocations.Contains(HitLocation.Head);

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
            int reactorBoost = GetEquipmentValue("ReactorBoost");
            return Math.Max(1, ReactorOutput + reactorBoost - (reactorHits * 3));
        }
    }

    public bool HasEquipment(string effect) => Equipment.Any(e => e.Effect == effect);
    public int GetEquipmentValue(string effect) => Equipment.Where(e => e.Effect == effect).Sum(e => e.EffectValue);

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

/// <summary>
/// Represents equipment installed on a combat frame
/// </summary>
public class EquippedEquipment
{
    public int EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;   // Passive, Active, Slot
    public string Effect { get; set; } = string.Empty;      // Machine-readable effect key
    public int EffectValue { get; set; }
    public int EnergyCost { get; set; }
    public bool IsActive { get; set; }   // For Active equipment: true when activated this round
}
