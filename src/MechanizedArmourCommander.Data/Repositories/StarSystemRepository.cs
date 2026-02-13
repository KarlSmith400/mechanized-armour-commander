using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class StarSystemRepository
{
    private readonly DatabaseContext _context;

    public StarSystemRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<StarSystem> GetAll()
    {
        var systems = new List<StarSystem>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT s.*, f.Name AS FactionName, f.ShortName, f.Color AS FactionColor
            FROM StarSystem s
            LEFT JOIN Faction f ON s.ControllingFactionId = f.FactionId
            ORDER BY s.SystemId";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            systems.Add(MapFromReader(reader));
        }

        return systems;
    }

    public StarSystem? GetById(int systemId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT s.*, f.Name AS FactionName, f.ShortName, f.Color AS FactionColor
            FROM StarSystem s
            LEFT JOIN Faction f ON s.ControllingFactionId = f.FactionId
            WHERE s.SystemId = @id";
        command.Parameters.AddWithValue("@id", systemId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public int Insert(StarSystem system)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO StarSystem (Name, X, Y, ControllingFactionId, SystemType, Description)
            VALUES (@name, @x, @y, @factionId, @type, @desc);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@name", system.Name);
        command.Parameters.AddWithValue("@x", system.X);
        command.Parameters.AddWithValue("@y", system.Y);
        command.Parameters.AddWithValue("@factionId", system.ControllingFactionId.HasValue ? system.ControllingFactionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@type", system.SystemType);
        command.Parameters.AddWithValue("@desc", system.Description);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static StarSystem MapFromReader(SqliteDataReader reader)
    {
        var system = new StarSystem
        {
            SystemId = reader.GetInt32(reader.GetOrdinal("SystemId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            X = reader.GetFloat(reader.GetOrdinal("X")),
            Y = reader.GetFloat(reader.GetOrdinal("Y")),
            SystemType = reader.GetString(reader.GetOrdinal("SystemType")),
            Description = reader.GetString(reader.GetOrdinal("Description"))
        };

        var factionOrdinal = reader.GetOrdinal("ControllingFactionId");
        if (!reader.IsDBNull(factionOrdinal))
        {
            system.ControllingFactionId = reader.GetInt32(factionOrdinal);
            system.ControllingFaction = new Faction
            {
                FactionId = system.ControllingFactionId.Value,
                Name = reader.GetString(reader.GetOrdinal("FactionName")),
                ShortName = reader.GetString(reader.GetOrdinal("ShortName")),
                Color = reader.GetString(reader.GetOrdinal("FactionColor"))
            };
        }

        return system;
    }
}
