using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class FactionRepository
{
    private readonly DatabaseContext _context;

    public FactionRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Faction> GetAll()
    {
        var factions = new List<Faction>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Faction ORDER BY FactionId";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            factions.Add(MapFromReader(reader));
        }

        return factions;
    }

    public Faction? GetById(int factionId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Faction WHERE FactionId = @id";
        command.Parameters.AddWithValue("@id", factionId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public int Insert(Faction faction)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Faction (Name, ShortName, Description, Color, WeaponPreference, ChassisPreference, EnemyPrefix)
            VALUES (@name, @short, @desc, @color, @weapPref, @chassPref, @prefix);
            SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("@name", faction.Name);
        command.Parameters.AddWithValue("@short", faction.ShortName);
        command.Parameters.AddWithValue("@desc", faction.Description);
        command.Parameters.AddWithValue("@color", faction.Color);
        command.Parameters.AddWithValue("@weapPref", faction.WeaponPreference);
        command.Parameters.AddWithValue("@chassPref", faction.ChassisPreference);
        command.Parameters.AddWithValue("@prefix", faction.EnemyPrefix);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static Faction MapFromReader(SqliteDataReader reader)
    {
        return new Faction
        {
            FactionId = reader.GetInt32(reader.GetOrdinal("FactionId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            ShortName = reader.GetString(reader.GetOrdinal("ShortName")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            Color = reader.GetString(reader.GetOrdinal("Color")),
            WeaponPreference = reader.GetString(reader.GetOrdinal("WeaponPreference")),
            ChassisPreference = reader.GetString(reader.GetOrdinal("ChassisPreference")),
            EnemyPrefix = reader.GetString(reader.GetOrdinal("EnemyPrefix"))
        };
    }
}
