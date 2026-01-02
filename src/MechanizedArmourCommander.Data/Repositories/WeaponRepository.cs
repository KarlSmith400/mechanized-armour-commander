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
            INSERT INTO Weapon (Name, HardpointSize, HeatGeneration, AmmoConsumption,
                               Damage, RangeClass, BaseAccuracy, SalvageValue,
                               PurchaseCost, SpecialEffect)
            VALUES (@name, @size, @heat, @ammo, @damage, @range, @accuracy,
                   @salvage, @cost, @special);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@name", weapon.Name);
        command.Parameters.AddWithValue("@size", weapon.HardpointSize);
        command.Parameters.AddWithValue("@heat", weapon.HeatGeneration);
        command.Parameters.AddWithValue("@ammo", weapon.AmmoConsumption);
        command.Parameters.AddWithValue("@damage", weapon.Damage);
        command.Parameters.AddWithValue("@range", weapon.RangeClass);
        command.Parameters.AddWithValue("@accuracy", weapon.BaseAccuracy);
        command.Parameters.AddWithValue("@salvage", weapon.SalvageValue);
        command.Parameters.AddWithValue("@cost", weapon.PurchaseCost);
        command.Parameters.AddWithValue("@special", (object?)weapon.SpecialEffect ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private Weapon MapFromReader(SqliteDataReader reader)
    {
        return new Weapon
        {
            WeaponId = reader.GetInt32(0),
            Name = reader.GetString(1),
            HardpointSize = reader.GetString(2),
            HeatGeneration = reader.GetInt32(3),
            AmmoConsumption = reader.GetInt32(4),
            Damage = reader.GetInt32(5),
            RangeClass = reader.GetString(6),
            BaseAccuracy = reader.GetInt32(7),
            SalvageValue = reader.GetInt32(8),
            PurchaseCost = reader.GetInt32(9),
            SpecialEffect = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
    }
}
