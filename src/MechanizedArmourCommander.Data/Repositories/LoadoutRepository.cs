using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class LoadoutRepository
{
    private readonly DatabaseContext _context;

    public LoadoutRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Loadout> GetByFrameInstance(int instanceId)
    {
        var loadouts = new List<Loadout>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT l.*, w.Name AS WeaponName, w.HardpointSize, w.WeaponType, w.EnergyCost,
                   w.AmmoPerShot, w.SpaceCost, w.Damage, w.RangeClass, w.BaseAccuracy,
                   w.SalvageValue, w.PurchaseCost, w.SpecialEffect
            FROM Loadout l
            INNER JOIN Weapon w ON l.WeaponId = w.WeaponId
            WHERE l.InstanceId = @instanceId
            ORDER BY l.WeaponGroup, l.HardpointSlot";
        command.Parameters.AddWithValue("@instanceId", instanceId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            loadouts.Add(MapFromReader(reader));
        }

        return loadouts;
    }

    public int Insert(Loadout loadout)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Loadout (InstanceId, HardpointSlot, WeaponId, WeaponGroup, MountLocation)
            VALUES (@instanceId, @slot, @weaponId, @group, @mount);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@instanceId", loadout.InstanceId);
        command.Parameters.AddWithValue("@slot", loadout.HardpointSlot);
        command.Parameters.AddWithValue("@weaponId", loadout.WeaponId);
        command.Parameters.AddWithValue("@group", loadout.WeaponGroup);
        command.Parameters.AddWithValue("@mount", loadout.MountLocation);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteByFrameInstance(int instanceId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM Loadout WHERE InstanceId = @instanceId";
        command.Parameters.AddWithValue("@instanceId", instanceId);
        command.ExecuteNonQuery();
    }

    public void ReplaceLoadout(int instanceId, List<Loadout> newLoadout)
    {
        var connection = _context.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete existing loadout
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM Loadout WHERE InstanceId = @instanceId";
            deleteCmd.Parameters.AddWithValue("@instanceId", instanceId);
            deleteCmd.ExecuteNonQuery();

            // Insert new loadout
            foreach (var loadout in newLoadout)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO Loadout (InstanceId, HardpointSlot, WeaponId, WeaponGroup, MountLocation)
                    VALUES (@instanceId, @slot, @weaponId, @group, @mount)";
                insertCmd.Parameters.AddWithValue("@instanceId", instanceId);
                insertCmd.Parameters.AddWithValue("@slot", loadout.HardpointSlot);
                insertCmd.Parameters.AddWithValue("@weaponId", loadout.WeaponId);
                insertCmd.Parameters.AddWithValue("@group", loadout.WeaponGroup);
                insertCmd.Parameters.AddWithValue("@mount", loadout.MountLocation);
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

    private Loadout MapFromReader(SqliteDataReader reader)
    {
        return new Loadout
        {
            LoadoutId = reader.GetInt32(reader.GetOrdinal("LoadoutId")),
            InstanceId = reader.GetInt32(reader.GetOrdinal("InstanceId")),
            HardpointSlot = reader.GetString(reader.GetOrdinal("HardpointSlot")),
            WeaponId = reader.GetInt32(reader.GetOrdinal("WeaponId")),
            WeaponGroup = reader.GetInt32(reader.GetOrdinal("WeaponGroup")),
            MountLocation = reader.GetString(reader.GetOrdinal("MountLocation")),
            Weapon = new Weapon
            {
                WeaponId = reader.GetInt32(reader.GetOrdinal("WeaponId")),
                Name = reader.GetString(reader.GetOrdinal("WeaponName")),
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
                SpecialEffect = reader.IsDBNull(reader.GetOrdinal("SpecialEffect")) ? null : reader.GetString(reader.GetOrdinal("SpecialEffect"))
            }
        };
    }
}
