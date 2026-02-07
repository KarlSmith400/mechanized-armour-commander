using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class InventoryRepository
{
    private readonly DatabaseContext _context;

    public InventoryRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<InventoryItem> GetAll()
    {
        var items = new List<InventoryItem>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT i.InventoryId, i.WeaponId, w.Name, w.HardpointSize, w.WeaponType,
                   w.EnergyCost, w.AmmoPerShot, w.SpaceCost, w.Damage, w.RangeClass,
                   w.BaseAccuracy, w.SalvageValue, w.PurchaseCost, w.SpecialEffect
            FROM Inventory i
            INNER JOIN Weapon w ON i.WeaponId = w.WeaponId
            ORDER BY w.HardpointSize, w.Name";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public List<InventoryItem> GetByHardpointSize(string size)
    {
        var items = new List<InventoryItem>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT i.InventoryId, i.WeaponId, w.Name, w.HardpointSize, w.WeaponType,
                   w.EnergyCost, w.AmmoPerShot, w.SpaceCost, w.Damage, w.RangeClass,
                   w.BaseAccuracy, w.SalvageValue, w.PurchaseCost, w.SpecialEffect
            FROM Inventory i
            INNER JOIN Weapon w ON i.WeaponId = w.WeaponId
            WHERE w.HardpointSize = @size
            ORDER BY w.Name";
        command.Parameters.AddWithValue("@size", size);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public int Insert(int weaponId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Inventory (WeaponId) VALUES (@weaponId);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@weaponId", weaponId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Delete(int inventoryId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM Inventory WHERE InventoryId = @id";
        command.Parameters.AddWithValue("@id", inventoryId);
        command.ExecuteNonQuery();
    }

    public int Count()
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Inventory";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private InventoryItem MapFromReader(SqliteDataReader reader)
    {
        return new InventoryItem
        {
            InventoryId = reader.GetInt32(reader.GetOrdinal("InventoryId")),
            WeaponId = reader.GetInt32(reader.GetOrdinal("WeaponId")),
            Weapon = new Weapon
            {
                WeaponId = reader.GetInt32(reader.GetOrdinal("WeaponId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                HardpointSize = reader.GetString(reader.GetOrdinal("HardpointSize")),
                WeaponType = reader.GetString(reader.GetOrdinal("WeaponType")),
                EnergyCost = reader.GetInt32(reader.GetOrdinal("EnergyCost")),
                AmmoPerShot = reader.GetInt32(reader.GetOrdinal("AmmoPerShot")),
                SpaceCost = reader.GetInt32(reader.GetOrdinal("SpaceCost")),
                Damage = reader.GetInt32(reader.GetOrdinal("Damage")),
                RangeClass = reader.GetString(reader.GetOrdinal("RangeClass")),
                BaseAccuracy = reader.GetInt32(reader.GetOrdinal("BaseAccuracy")),
                SalvageValue = reader.GetInt32(reader.GetOrdinal("SalvageValue")),
                PurchaseCost = reader.GetInt32(reader.GetOrdinal("PurchaseCost")),
                SpecialEffect = reader.IsDBNull(reader.GetOrdinal("SpecialEffect"))
                    ? null : reader.GetString(reader.GetOrdinal("SpecialEffect"))
            }
        };
    }
}
