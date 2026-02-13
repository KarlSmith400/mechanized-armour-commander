using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class EquipmentRepository
{
    private readonly DatabaseContext _context;

    public EquipmentRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Equipment> GetAll()
    {
        var items = new List<Equipment>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Equipment ORDER BY Category, Name";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public Equipment? GetById(int equipmentId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Equipment WHERE EquipmentId = @id";
        command.Parameters.AddWithValue("@id", equipmentId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<Equipment> GetByCategory(string category)
    {
        var items = new List<Equipment>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Equipment WHERE Category = @cat ORDER BY Name";
        command.Parameters.AddWithValue("@cat", category);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public int Insert(Equipment equipment)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Equipment (Name, Category, HardpointSize, SpaceCost, EnergyCost,
                                   Effect, EffectValue, PurchaseCost, SalvageValue, Description)
            VALUES (@name, @cat, @size, @space, @energy, @effect, @value, @cost, @salvage, @desc);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@name", equipment.Name);
        command.Parameters.AddWithValue("@cat", equipment.Category);
        command.Parameters.AddWithValue("@size", (object?)equipment.HardpointSize ?? DBNull.Value);
        command.Parameters.AddWithValue("@space", equipment.SpaceCost);
        command.Parameters.AddWithValue("@energy", equipment.EnergyCost);
        command.Parameters.AddWithValue("@effect", equipment.Effect);
        command.Parameters.AddWithValue("@value", equipment.EffectValue);
        command.Parameters.AddWithValue("@cost", equipment.PurchaseCost);
        command.Parameters.AddWithValue("@salvage", equipment.SalvageValue);
        command.Parameters.AddWithValue("@desc", (object?)equipment.Description ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private Equipment MapFromReader(SqliteDataReader reader)
    {
        return new Equipment
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
        };
    }
}
