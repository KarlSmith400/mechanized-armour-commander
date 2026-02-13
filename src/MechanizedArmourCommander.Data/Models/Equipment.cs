namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Defines an equipment item that can be installed on a frame
/// </summary>
public class Equipment
{
    public int EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;       // Passive, Active, Slot
    public string? HardpointSize { get; set; }                  // null for Passive/Active; Small/Medium/Large for Slot
    public int SpaceCost { get; set; }                          // Competes with weapons for TotalSpace
    public int EnergyCost { get; set; }                         // Energy cost for Active equipment activation
    public string Effect { get; set; } = string.Empty;          // Machine-readable effect key
    public int EffectValue { get; set; }                        // Numeric magnitude of the effect
    public int PurchaseCost { get; set; }
    public int SalvageValue { get; set; }
    public string? Description { get; set; }                    // Human-readable tooltip
}
