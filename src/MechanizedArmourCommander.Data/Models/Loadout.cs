namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a weapon equipped on a specific hardpoint of a frame
/// </summary>
public class Loadout
{
    public int LoadoutId { get; set; }
    public int InstanceId { get; set; }
    public string HardpointSlot { get; set; } = string.Empty; // small_1, medium_2, large_1, etc.
    public int WeaponId { get; set; }

    // Navigation properties
    public FrameInstance? Frame { get; set; }
    public Weapon? Weapon { get; set; }
}
