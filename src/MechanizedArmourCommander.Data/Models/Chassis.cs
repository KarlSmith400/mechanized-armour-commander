namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a mech chassis template (e.g., VG-45 Vanguard)
/// </summary>
public class Chassis
{
    public int ChassisId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty; // Light/Medium/Heavy/Assault

    // Hardpoints
    public int HardpointSmall { get; set; }
    public int HardpointMedium { get; set; }
    public int HardpointLarge { get; set; }

    // Reactor System
    public int ReactorOutput { get; set; }        // Energy generated per round
    public int MovementEnergyCost { get; set; }    // Energy to move one range band

    // Space Budget
    public int TotalSpace { get; set; }            // Total available space for armor/weapons/ammo
    public int MaxArmorTotal { get; set; }         // Max armor points distributable across locations

    // Structure per location (fixed by chassis, not configurable)
    public int StructureHead { get; set; }
    public int StructureCenterTorso { get; set; }
    public int StructureSideTorso { get; set; }    // Same for left and right
    public int StructureArm { get; set; }          // Same for left and right
    public int StructureLegs { get; set; }

    // Performance
    public int BaseSpeed { get; set; }
    public int BaseEvasion { get; set; }
    public int? FactionId { get; set; }
}
