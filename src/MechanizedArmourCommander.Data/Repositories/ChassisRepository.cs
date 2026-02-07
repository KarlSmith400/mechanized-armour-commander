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
                                HardpointLarge, ReactorOutput, MovementEnergyCost, TotalSpace,
                                MaxArmorTotal, StructureHead, StructureCenterTorso, StructureSideTorso,
                                StructureArm, StructureLegs, BaseSpeed, BaseEvasion, FactionId)
            VALUES (@designation, @name, @class, @hpSmall, @hpMedium, @hpLarge,
                   @reactor, @moveCost, @space, @maxArmor,
                   @strHead, @strCT, @strST, @strArm, @strLegs,
                   @speed, @evasion, @factionId);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@designation", chassis.Designation);
        command.Parameters.AddWithValue("@name", chassis.Name);
        command.Parameters.AddWithValue("@class", chassis.Class);
        command.Parameters.AddWithValue("@hpSmall", chassis.HardpointSmall);
        command.Parameters.AddWithValue("@hpMedium", chassis.HardpointMedium);
        command.Parameters.AddWithValue("@hpLarge", chassis.HardpointLarge);
        command.Parameters.AddWithValue("@reactor", chassis.ReactorOutput);
        command.Parameters.AddWithValue("@moveCost", chassis.MovementEnergyCost);
        command.Parameters.AddWithValue("@space", chassis.TotalSpace);
        command.Parameters.AddWithValue("@maxArmor", chassis.MaxArmorTotal);
        command.Parameters.AddWithValue("@strHead", chassis.StructureHead);
        command.Parameters.AddWithValue("@strCT", chassis.StructureCenterTorso);
        command.Parameters.AddWithValue("@strST", chassis.StructureSideTorso);
        command.Parameters.AddWithValue("@strArm", chassis.StructureArm);
        command.Parameters.AddWithValue("@strLegs", chassis.StructureLegs);
        command.Parameters.AddWithValue("@speed", chassis.BaseSpeed);
        command.Parameters.AddWithValue("@evasion", chassis.BaseEvasion);
        command.Parameters.AddWithValue("@factionId", (object?)chassis.FactionId ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private Chassis MapFromReader(SqliteDataReader reader)
    {
        return new Chassis
        {
            ChassisId = reader.GetInt32(reader.GetOrdinal("ChassisId")),
            Designation = reader.GetString(reader.GetOrdinal("Designation")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Class = reader.GetString(reader.GetOrdinal("Class")),
            HardpointSmall = reader.GetInt32(reader.GetOrdinal("HardpointSmall")),
            HardpointMedium = reader.GetInt32(reader.GetOrdinal("HardpointMedium")),
            HardpointLarge = reader.GetInt32(reader.GetOrdinal("HardpointLarge")),
            ReactorOutput = reader.GetInt32(reader.GetOrdinal("ReactorOutput")),
            MovementEnergyCost = reader.GetInt32(reader.GetOrdinal("MovementEnergyCost")),
            TotalSpace = reader.GetInt32(reader.GetOrdinal("TotalSpace")),
            MaxArmorTotal = reader.GetInt32(reader.GetOrdinal("MaxArmorTotal")),
            StructureHead = reader.GetInt32(reader.GetOrdinal("StructureHead")),
            StructureCenterTorso = reader.GetInt32(reader.GetOrdinal("StructureCenterTorso")),
            StructureSideTorso = reader.GetInt32(reader.GetOrdinal("StructureSideTorso")),
            StructureArm = reader.GetInt32(reader.GetOrdinal("StructureArm")),
            StructureLegs = reader.GetInt32(reader.GetOrdinal("StructureLegs")),
            BaseSpeed = reader.GetInt32(reader.GetOrdinal("BaseSpeed")),
            BaseEvasion = reader.GetInt32(reader.GetOrdinal("BaseEvasion")),
            FactionId = reader.IsDBNull(reader.GetOrdinal("FactionId"))
                ? null : reader.GetInt32(reader.GetOrdinal("FactionId"))
        };
    }

    public List<Chassis> GetByFaction(int factionId)
    {
        var chassis = new List<Chassis>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Chassis WHERE FactionId = @fid OR FactionId IS NULL ORDER BY Class, Designation";
        command.Parameters.AddWithValue("@fid", factionId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            chassis.Add(MapFromReader(reader));
        }

        return chassis;
    }
}
