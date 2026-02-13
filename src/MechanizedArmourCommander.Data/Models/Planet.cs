namespace MechanizedArmourCommander.Data.Models;

public class Planet
{
    public int PlanetId { get; set; }
    public int SystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlanetType { get; set; } = string.Empty; // "Habitable" | "Industrial" | "Mining" | "Station" | "Outpost"
    public string Description { get; set; } = string.Empty;
    public bool HasMarket { get; set; }
    public bool HasHiring { get; set; }
    public int ContractDifficultyMin { get; set; } = 1;
    public int ContractDifficultyMax { get; set; } = 3;
    public StarSystem? System { get; set; }
}
