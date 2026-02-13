namespace MechanizedArmourCommander.Data.Models;

public class StarSystem
{
    public int SystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public int? ControllingFactionId { get; set; }
    public string SystemType { get; set; } = string.Empty; // "Core" | "Colony" | "Frontier" | "Contested"
    public string Description { get; set; } = string.Empty;
    public Faction? ControllingFaction { get; set; }
}
