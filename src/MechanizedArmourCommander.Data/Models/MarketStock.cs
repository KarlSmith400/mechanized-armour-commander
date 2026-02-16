namespace MechanizedArmourCommander.Data.Models;

public class MarketStock
{
    public int MarketStockId { get; set; }
    public int PlanetId { get; set; }
    public string ItemType { get; set; } = string.Empty; // "Chassis", "Weapon", "Equipment"
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public int GeneratedOnDay { get; set; }
}
