namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a specific mech frame instance owned by the player
/// </summary>
public class FrameInstance
{
    public int InstanceId { get; set; }
    public int ChassisId { get; set; }
    public string CustomName { get; set; } = string.Empty;
    public int CurrentArmor { get; set; }
    public string Status { get; set; } = "Ready"; // Ready/Damaged/Destroyed/Deployed
    public int RepairCost { get; set; }
    public int RepairTime { get; set; }
    public DateTime AcquisitionDate { get; set; }

    // Navigation properties
    public Chassis? Chassis { get; set; }
}
