using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class EquipmentLoadoutRepository
{
    private readonly DatabaseContext _context;

    public EquipmentLoadoutRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<EquipmentLoadout> GetByFrameInstance(int instanceId)
    {
        var items = new List<EquipmentLoadout>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT el.*, e.Name, e.Category, e.HardpointSize AS EqHardpointSize,
                   e.SpaceCost, e.EnergyCost, e.Effect, e.EffectValue,
                   e.PurchaseCost, e.SalvageValue, e.Description
            FROM EquipmentLoadout el
            INNER JOIN Equipment e ON el.EquipmentId = e.EquipmentId
            WHERE el.InstanceId = @instanceId
            ORDER BY e.Category, e.Name";
        command.Parameters.AddWithValue("@instanceId", instanceId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapFromReader(reader));
        }

        return items;
    }

    public int Insert(EquipmentLoadout loadout)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO EquipmentLoadout (InstanceId, EquipmentId, HardpointSlot)
            VALUES (@instanceId, @equipmentId, @slot);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@instanceId", loadout.InstanceId);
        command.Parameters.AddWithValue("@equipmentId", loadout.EquipmentId);
        command.Parameters.AddWithValue("@slot", (object?)loadout.HardpointSlot ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteByFrameInstance(int instanceId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM EquipmentLoadout WHERE InstanceId = @instanceId";
        command.Parameters.AddWithValue("@instanceId", instanceId);
        command.ExecuteNonQuery();
    }

    public void ReplaceEquipmentLoadout(int instanceId, List<EquipmentLoadout> newLoadout)
    {
        var connection = _context.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM EquipmentLoadout WHERE InstanceId = @instanceId";
            deleteCmd.Parameters.AddWithValue("@instanceId", instanceId);
            deleteCmd.ExecuteNonQuery();

            foreach (var loadout in newLoadout)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO EquipmentLoadout (InstanceId, EquipmentId, HardpointSlot)
                    VALUES (@instanceId, @equipmentId, @slot)";
                insertCmd.Parameters.AddWithValue("@instanceId", instanceId);
                insertCmd.Parameters.AddWithValue("@equipmentId", loadout.EquipmentId);
                insertCmd.Parameters.AddWithValue("@slot", (object?)loadout.HardpointSlot ?? DBNull.Value);
                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private EquipmentLoadout MapFromReader(SqliteDataReader reader)
    {
        return new EquipmentLoadout
        {
            EquipmentLoadoutId = reader.GetInt32(reader.GetOrdinal("EquipmentLoadoutId")),
            InstanceId = reader.GetInt32(reader.GetOrdinal("InstanceId")),
            EquipmentId = reader.GetInt32(reader.GetOrdinal("EquipmentId")),
            HardpointSlot = reader.IsDBNull(reader.GetOrdinal("HardpointSlot")) ? null : reader.GetString(reader.GetOrdinal("HardpointSlot")),
            Equipment = new Equipment
            {
                EquipmentId = reader.GetInt32(reader.GetOrdinal("EquipmentId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                HardpointSize = reader.IsDBNull(reader.GetOrdinal("EqHardpointSize")) ? null : reader.GetString(reader.GetOrdinal("EqHardpointSize")),
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
