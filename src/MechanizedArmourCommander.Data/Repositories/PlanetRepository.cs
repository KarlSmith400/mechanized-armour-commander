using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class PlanetRepository
{
    private readonly DatabaseContext _context;

    public PlanetRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Planet> GetAll()
    {
        var planets = new List<Planet>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Planet ORDER BY SystemId, PlanetId";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            planets.Add(MapFromReader(reader));
        }

        return planets;
    }

    public Planet? GetById(int planetId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Planet WHERE PlanetId = @id";
        command.Parameters.AddWithValue("@id", planetId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<Planet> GetBySystem(int systemId)
    {
        var planets = new List<Planet>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Planet WHERE SystemId = @systemId ORDER BY PlanetId";
        command.Parameters.AddWithValue("@systemId", systemId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            planets.Add(MapFromReader(reader));
        }

        return planets;
    }

    public int Insert(Planet planet)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Planet (SystemId, Name, PlanetType, Description, HasMarket, HasHiring, ContractDifficultyMin, ContractDifficultyMax)
            VALUES (@systemId, @name, @type, @desc, @market, @hiring, @diffMin, @diffMax);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@systemId", planet.SystemId);
        command.Parameters.AddWithValue("@name", planet.Name);
        command.Parameters.AddWithValue("@type", planet.PlanetType);
        command.Parameters.AddWithValue("@desc", planet.Description);
        command.Parameters.AddWithValue("@market", planet.HasMarket ? 1 : 0);
        command.Parameters.AddWithValue("@hiring", planet.HasHiring ? 1 : 0);
        command.Parameters.AddWithValue("@diffMin", planet.ContractDifficultyMin);
        command.Parameters.AddWithValue("@diffMax", planet.ContractDifficultyMax);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static Planet MapFromReader(SqliteDataReader reader)
    {
        return new Planet
        {
            PlanetId = reader.GetInt32(reader.GetOrdinal("PlanetId")),
            SystemId = reader.GetInt32(reader.GetOrdinal("SystemId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            PlanetType = reader.GetString(reader.GetOrdinal("PlanetType")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            HasMarket = reader.GetInt32(reader.GetOrdinal("HasMarket")) == 1,
            HasHiring = reader.GetInt32(reader.GetOrdinal("HasHiring")) == 1,
            ContractDifficultyMin = reader.GetInt32(reader.GetOrdinal("ContractDifficultyMin")),
            ContractDifficultyMax = reader.GetInt32(reader.GetOrdinal("ContractDifficultyMax"))
        };
    }
}
