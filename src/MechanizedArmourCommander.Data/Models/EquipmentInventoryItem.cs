namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// An equipment item stored in company inventory (not equipped on any frame)
/// </summary>
public class EquipmentInventoryItem
{
    public int EquipmentInventoryId { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
}
