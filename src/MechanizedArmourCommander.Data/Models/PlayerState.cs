namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Persistent player state for campaign progression
/// </summary>
public class PlayerState
{
    public int Credits { get; set; } = 500000;
    public int Reputation { get; set; }
    public int MissionsCompleted { get; set; }
    public int MissionsWon { get; set; }
    public string CompanyName { get; set; } = "Iron Wolves";
    public int CurrentDay { get; set; } = 1;
}
