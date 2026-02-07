namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a specific mech frame instance owned by the player
/// </summary>
public class FrameInstance
{
    public int InstanceId { get; set; }
    public int ChassisId { get; set; }
    public string CustomName { get; set; } = string.Empty;

    // Per-location armor (player-configured distribution)
    public int ArmorHead { get; set; }
    public int ArmorCenterTorso { get; set; }
    public int ArmorLeftTorso { get; set; }
    public int ArmorRightTorso { get; set; }
    public int ArmorLeftArm { get; set; }
    public int ArmorRightArm { get; set; }
    public int ArmorLegs { get; set; }

    public int ReactorStress { get; set; }

    public string Status { get; set; } = "Ready"; // Ready/Damaged/Destroyed/Deployed
    public int RepairCost { get; set; }
    public int RepairTime { get; set; }
    public DateTime AcquisitionDate { get; set; }

    // Pilot assignment
    public int? PilotId { get; set; }

    // Navigation properties
    public Chassis? Chassis { get; set; }
    public Pilot? Pilot { get; set; }
}
