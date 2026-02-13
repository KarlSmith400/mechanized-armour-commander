namespace MechanizedArmourCommander.Data.Models;

public class JumpRoute
{
    public int RouteId { get; set; }
    public int FromSystemId { get; set; }
    public int ToSystemId { get; set; }
    public int Distance { get; set; }
    public int TravelDays { get; set; }
    public string? FromSystemName { get; set; }
    public string? ToSystemName { get; set; }
}
