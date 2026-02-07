namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a weapon stored in the company armory (not equipped on any frame)
/// </summary>
public class InventoryItem
{
    public int InventoryId { get; set; }
    public int WeaponId { get; set; }

    // Navigation property
    public Weapon? Weapon { get; set; }
}
