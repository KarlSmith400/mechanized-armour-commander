using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

/// <summary>
/// Repository for managing weapon data
/// </summary>
public class WeaponRepository
{
    private readonly DatabaseContext _context;

    public WeaponRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Weapon> GetAll()
    {
        var weapons = new List<Weapon>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Weapon ORDER BY HardpointSize, Name";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            weapons.Add(MapFromReader(reader));
        }

        return weapons;
    }

    public Weapon? GetById(int weaponId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Weapon WHERE WeaponId = @id";
        command.Parameters.AddWithValue("@id", weaponId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<Weapon> GetByHardpointSize(string hardpointSize)
    {
        var weapons = new List<Weapon>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Weapon WHERE HardpointSize = @size ORDER BY Name";
        command.Parameters.AddWithValue("@size", hardpointSize);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            weapons.Add(MapFromReader(reader));
        }

        return weapons;
    }

    public int Insert(Weapon weapon)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Weapon (Name, HardpointSize, WeaponType, EnergyCost, AmmoPerShot,
                               SpaceCost, Damage, RangeClass, BaseAccuracy, SalvageValue,
                               PurchaseCost, SpecialEffect, FactionId)
            VALUES (@name, @size, @type, @energy, @ammo, @space, @damage, @range, @accuracy,
                   @salvage, @cost, @special, @factionId);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@name", weapon.Name);
        command.Parameters.AddWithValue("@size", weapon.HardpointSize);
        command.Parameters.AddWithValue("@type", weapon.WeaponType);
        command.Parameters.AddWithValue("@energy", weapon.EnergyCost);
        command.Parameters.AddWithValue("@ammo", weapon.AmmoPerShot);
        command.Parameters.AddWithValue("@space", weapon.SpaceCost);
        command.Parameters.AddWithValue("@damage", weapon.Damage);
        command.Parameters.AddWithValue("@range", weapon.RangeClass);
        command.Parameters.AddWithValue("@accuracy", weapon.BaseAccuracy);
        command.Parameters.AddWithValue("@salvage", weapon.SalvageValue);
        command.Parameters.AddWithValue("@cost", weapon.PurchaseCost);
        command.Parameters.AddWithValue("@special", (object?)weapon.SpecialEffect ?? DBNull.Value);
        command.Parameters.AddWithValue("@factionId", (object?)weapon.FactionId ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private Weapon MapFromReader(SqliteDataReader reader)
    {
        return new Weapon
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
            SpecialEffect = reader.IsDBNull(reader.GetOrdinal("SpecialEffect")) ? null : reader.GetString(reader.GetOrdinal("SpecialEffect")),
            FactionId = reader.IsDBNull(reader.GetOrdinal("FactionId")) ? null : reader.GetInt32(reader.GetOrdinal("FactionId"))
        };
    }

    public List<Weapon> GetByFaction(int factionId)
    {
        var weapons = new List<Weapon>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Weapon WHERE FactionId = @fid OR FactionId IS NULL ORDER BY HardpointSize, Name";
        command.Parameters.AddWithValue("@fid", factionId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            weapons.Add(MapFromReader(reader));
        }

        return weapons;
    }
}
