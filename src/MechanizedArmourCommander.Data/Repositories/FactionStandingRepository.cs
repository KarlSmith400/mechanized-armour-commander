using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class FactionStandingRepository
{
    private readonly DatabaseContext _context;

    public FactionStandingRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<FactionStanding> GetAll()
    {
        var standings = new List<FactionStanding>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT fs.FactionId, fs.Standing, f.Name AS FactionName, f.Color AS FactionColor
            FROM FactionStanding fs
            INNER JOIN Faction f ON fs.FactionId = f.FactionId
            ORDER BY fs.FactionId";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            standings.Add(MapFromReader(reader));
        }

        return standings;
    }

    public FactionStanding? GetByFaction(int factionId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT fs.FactionId, fs.Standing, f.Name AS FactionName, f.Color AS FactionColor
            FROM FactionStanding fs
            INNER JOIN Faction f ON fs.FactionId = f.FactionId
            WHERE fs.FactionId = @fid";
        command.Parameters.AddWithValue("@fid", factionId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public void Initialize(int factionId, int standing = 0)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "INSERT INTO FactionStanding (FactionId, Standing) VALUES (@fid, @standing)";
        command.Parameters.AddWithValue("@fid", factionId);
        command.Parameters.AddWithValue("@standing", standing);
        command.ExecuteNonQuery();
    }

    public void UpdateStanding(int factionId, int newStanding)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "UPDATE FactionStanding SET Standing = @standing WHERE FactionId = @fid";
        command.Parameters.AddWithValue("@fid", factionId);
        command.Parameters.AddWithValue("@standing", newStanding);
        command.ExecuteNonQuery();
    }

    private static FactionStanding MapFromReader(SqliteDataReader reader)
    {
        return new FactionStanding
        {
            FactionId = reader.GetInt32(reader.GetOrdinal("FactionId")),
            Standing = reader.GetInt32(reader.GetOrdinal("Standing")),
            FactionName = reader.GetString(reader.GetOrdinal("FactionName")),
            FactionColor = reader.GetString(reader.GetOrdinal("FactionColor"))
        };
    }
}
