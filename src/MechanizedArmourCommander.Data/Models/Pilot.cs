namespace MechanizedArmourCommander.Data.Models;

/// <summary>
/// Represents a pilot who can operate mech frames
/// </summary>
public class Pilot
{
    public int PilotId { get; set; }
    public string Callsign { get; set; } = string.Empty;
    public int GunnerySkill { get; set; }
    public int PilotingSkill { get; set; }
    public int TacticsSkill { get; set; }
    public int ExperiencePoints { get; set; }
    public int MissionsCompleted { get; set; }
    public int Kills { get; set; }
    public string Status { get; set; } = "Active"; // Active/Injured/KIA
    public int InjuryDays { get; set; }
    public int Morale { get; set; } = 100;
}
