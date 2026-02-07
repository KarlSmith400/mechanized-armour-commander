namespace MechanizedArmourCommander.Data.Models;

public class FactionStanding
{
    public int FactionId { get; set; }
    public int Standing { get; set; }
    public string FactionName { get; set; } = string.Empty;
    public string FactionColor { get; set; } = string.Empty;

    public string StandingLevel => Standing switch
    {
        < -50 => "Hostile",
        < 100 => "Neutral",
        < 200 => "Friendly",
        < 400 => "Allied",
        _ => "Trusted"
    };

    public float PriceModifier => Standing switch
    {
        >= 400 => 0.80f,
        >= 200 => 0.90f,
        >= 100 => 0.95f,
        _ => 1.0f
    };
}
