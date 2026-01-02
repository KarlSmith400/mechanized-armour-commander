namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents a frame participating in combat with runtime state
/// </summary>
public class CombatFrame
{
    public int InstanceId { get; set; }
    public string CustomName { get; set; } = string.Empty;
    public string ChassisDesignation { get; set; } = string.Empty;
    public string ChassisName { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;

    // Combat stats
    public int CurrentArmor { get; set; }
    public int MaxArmor { get; set; }
    public int CurrentHeat { get; set; }
    public int MaxHeat { get; set; }
    public int CurrentAmmo { get; set; }
    public int MaxAmmo { get; set; }

    // Performance stats
    public int Speed { get; set; }
    public int Evasion { get; set; }

    // Pilot assignment
    public int? PilotId { get; set; }
    public string? PilotCallsign { get; set; }
    public int PilotGunnery { get; set; }
    public int PilotPiloting { get; set; }
    public int PilotTactics { get; set; }

    // Loadout
    public List<EquippedWeapon> Weapons { get; set; } = new();

    // Positional tracking
    public int Position { get; set; } // Distance from center (negative = left, positive = right)
    public int StartPosition { get; set; } // Initial deployment position

    // Combat state
    public bool IsDestroyed => CurrentArmor <= 0;
    public bool IsOverheating => CurrentHeat >= MaxHeat;
    public bool IsOutOfAmmo => CurrentAmmo <= 0;
}

/// <summary>
/// Represents a weapon currently equipped and ready to fire
/// </summary>
public class EquippedWeapon
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HardpointSize { get; set; } = string.Empty;
    public int HeatGeneration { get; set; }
    public int AmmoConsumption { get; set; }
    public int Damage { get; set; }
    public string RangeClass { get; set; } = string.Empty;
    public int BaseAccuracy { get; set; }
    public string? SpecialEffect { get; set; }
}
