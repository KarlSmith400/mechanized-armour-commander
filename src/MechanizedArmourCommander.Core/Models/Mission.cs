namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Represents a mission contract available for the player to take
/// </summary>
public class Mission
{
    public int MissionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Difficulty { get; set; } // 1-5 stars
    public int CreditReward { get; set; }
    public int BonusCredits { get; set; }
    public List<EnemySpec> EnemyComposition { get; set; } = new();
    public int SalvageChance { get; set; } // % chance to salvage per destroyed enemy
    public int ReputationReward { get; set; }

    // Faction info
    public int EmployerFactionId { get; set; }
    public string EmployerFactionName { get; set; } = string.Empty;
    public string EmployerFactionColor { get; set; } = string.Empty;
    public int OpponentFactionId { get; set; }
    public string OpponentFactionName { get; set; } = string.Empty;
    public string OpponentFactionColor { get; set; } = string.Empty;
    public string OpponentPrefix { get; set; } = string.Empty;
}

/// <summary>
/// Specifies an enemy unit in a mission's composition
/// </summary>
public class EnemySpec
{
    public string ChassisClass { get; set; } = string.Empty; // Light/Medium/Heavy/Assault
    public int Count { get; set; }
}
