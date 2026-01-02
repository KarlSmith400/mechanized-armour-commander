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
    public int HardpointSmall { get; set; }
    public int HardpointMedium { get; set; }
    public int HardpointLarge { get; set; }
    public int HeatCapacity { get; set; }
    public int AmmoCapacity { get; set; }
    public int ArmorPoints { get; set; }
    public int BaseSpeed { get; set; }
    public int BaseEvasion { get; set; }
}
