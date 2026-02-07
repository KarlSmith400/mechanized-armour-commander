namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a weapon type that can be equipped on frames
/// </summary>
public class Weapon
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HardpointSize { get; set; } = string.Empty; // Small/Medium/Large
    public string WeaponType { get; set; } = string.Empty;    // Energy/Ballistic/Missile
    public int EnergyCost { get; set; }           // Reactor energy consumed per shot
    public int AmmoPerShot { get; set; }          // Ammo consumed per firing (0 for energy weapons)
    public int SpaceCost { get; set; }            // Space budget consumed when equipped
    public int Damage { get; set; }
    public string RangeClass { get; set; } = string.Empty; // Short/Medium/Long
    public int BaseAccuracy { get; set; }
    public int SalvageValue { get; set; }
    public int PurchaseCost { get; set; }
    public string? SpecialEffect { get; set; }
    public int? FactionId { get; set; }
}
