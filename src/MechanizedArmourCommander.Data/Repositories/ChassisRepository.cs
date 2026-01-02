using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

/// <summary>
/// Repository for managing chassis data
/// </summary>
public class ChassisRepository
{
    private readonly DatabaseContext _context;

    public ChassisRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Chassis> GetAll()
    {
        var chassis = new List<Chassis>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Chassis ORDER BY Class, Designation";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            chassis.Add(MapFromReader(reader));
        }

        return chassis;
    }

    public Chassis? GetById(int chassisId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Chassis WHERE ChassisId = @id";
        command.Parameters.AddWithValue("@id", chassisId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<Chassis> GetByClass(string chassisClass)
    {
        var chassis = new List<Chassis>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Chassis WHERE Class = @class ORDER BY Designation";
        command.Parameters.AddWithValue("@class", chassisClass);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            chassis.Add(MapFromReader(reader));
        }

        return chassis;
    }

    public int Insert(Chassis chassis)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Chassis (Designation, Name, Class, HardpointSmall, HardpointMedium,
                                HardpointLarge, HeatCapacity, AmmoCapacity, ArmorPoints,
                                BaseSpeed, BaseEvasion)
            VALUES (@designation, @name, @class, @hpSmall, @hpMedium, @hpLarge,
                   @heat, @ammo, @armor, @speed, @evasion);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@designation", chassis.Designation);
        command.Parameters.AddWithValue("@name", chassis.Name);
        command.Parameters.AddWithValue("@class", chassis.Class);
        command.Parameters.AddWithValue("@hpSmall", chassis.HardpointSmall);
        command.Parameters.AddWithValue("@hpMedium", chassis.HardpointMedium);
        command.Parameters.AddWithValue("@hpLarge", chassis.HardpointLarge);
        command.Parameters.AddWithValue("@heat", chassis.HeatCapacity);
        command.Parameters.AddWithValue("@ammo", chassis.AmmoCapacity);
        command.Parameters.AddWithValue("@armor", chassis.ArmorPoints);
        command.Parameters.AddWithValue("@speed", chassis.BaseSpeed);
        command.Parameters.AddWithValue("@evasion", chassis.BaseEvasion);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private Chassis MapFromReader(SqliteDataReader reader)
    {
        return new Chassis
        {
            ChassisId = reader.GetInt32(0),
            Designation = reader.GetString(1),
            Name = reader.GetString(2),
            Class = reader.GetString(3),
            HardpointSmall = reader.GetInt32(4),
            HardpointMedium = reader.GetInt32(5),
            HardpointLarge = reader.GetInt32(6),
            HeatCapacity = reader.GetInt32(7),
            AmmoCapacity = reader.GetInt32(8),
            ArmorPoints = reader.GetInt32(9),
            BaseSpeed = reader.GetInt32(10),
            BaseEvasion = reader.GetInt32(11)
        };
    }
}
