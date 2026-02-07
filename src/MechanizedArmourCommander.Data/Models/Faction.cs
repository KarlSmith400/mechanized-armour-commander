namespace MechanizedArmourCommander.Data.Models;

public class Faction
{
    public int FactionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string WeaponPreference { get; set; } = string.Empty;
    public string ChassisPreference { get; set; } = string.Empty;
    public string EnemyPrefix { get; set; } = string.Empty;
}
