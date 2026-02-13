using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class EquipmentInventoryRepository
{
    private readonly DatabaseContext _context;

    public EquipmentInventoryRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<EquipmentInventoryItem> GetAll()
    {
        var items = new List<EquipmentInventoryItem>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT ei.EquipmentInventoryId, ei.EquipmentId, e.Name, e.Category,
                   e.HardpointSize, e.SpaceCost, e.EnergyCost, e.Effect, e.EffectValue,
                   e.PurchaseCost, e.SalvageValue, e.Description
            FROM EquipmentInventory ei
            INNER JOIN Equipment e ON ei.EquipmentId = e.EquipmentId
            ORDER BY e.Category, e.Name";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public int Insert(int equipmentId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO EquipmentInventory (EquipmentId) VALUES (@equipmentId);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@equipmentId", equipmentId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Delete(int equipmentInventoryId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM EquipmentInventory WHERE EquipmentInventoryId = @id";
        command.Parameters.AddWithValue("@id", equipmentInventoryId);
        command.ExecuteNonQuery();
    }

    public int Count()
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM EquipmentInventory";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private EquipmentInventoryItem MapFromReader(SqliteDataReader reader)
    {
        return new EquipmentInventoryItem
        {
            EquipmentInventoryId = reader.GetInt32(reader.GetOrdinal("EquipmentInventoryId")),
            EquipmentId = reader.GetInt32(reader.GetOrdinal("EquipmentId")),
            Equipment = new Equipment
            {
                EquipmentId = reader.GetInt32(reader.GetOrdinal("EquipmentId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                HardpointSize = reader.IsDBNull(reader.GetOrdinal("HardpointSize")) ? null : reader.GetString(reader.GetOrdinal("HardpointSize")),
                SpaceCost = reader.GetInt32(reader.GetOrdinal("SpaceCost")),
                EnergyCost = reader.GetInt32(reader.GetOrdinal("EnergyCost")),
                Effect = reader.GetString(reader.GetOrdinal("Effect")),
                EffectValue = reader.GetInt32(reader.GetOrdinal("EffectValue")),
                PurchaseCost = reader.GetInt32(reader.GetOrdinal("PurchaseCost")),
                SalvageValue = reader.GetInt32(reader.GetOrdinal("SalvageValue")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
            }
        };
    }
}
