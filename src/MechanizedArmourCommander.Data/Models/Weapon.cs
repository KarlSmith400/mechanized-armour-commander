namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a weapon type that can be equipped on frames
/// </summary>
public class Weapon
{
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HardpointSize { get; set; } = string.Empty; // Small/Medium/Large
    public int HeatGeneration { get; set; }
    public int AmmoConsumption { get; set; }
    public int Damage { get; set; }
    public string RangeClass { get; set; } = string.Empty; // Short/Medium/Long
    public int BaseAccuracy { get; set; }
    public int SalvageValue { get; set; }
    public int PurchaseCost { get; set; }
    public string? SpecialEffect { get; set; }
}
