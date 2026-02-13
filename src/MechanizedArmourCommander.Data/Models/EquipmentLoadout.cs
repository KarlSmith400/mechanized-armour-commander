namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Maps equipment to a frame instance (equipped on a specific frame)
/// </summary>
public class EquipmentLoadout
{
    public int EquipmentLoadoutId { get; set; }
    public int InstanceId { get; set; }
    public int EquipmentId { get; set; }
    public string? HardpointSlot { get; set; }  // null for Passive/Active; slot name for Slot-type
    public Equipment? Equipment { get; set; }
}
