using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class JumpRouteRepository
{
    private readonly DatabaseContext _context;

    public JumpRouteRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<JumpRoute> GetAll()
    {
        var routes = new List<JumpRoute>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT r.*, sf.Name AS FromSystemName, st.Name AS ToSystemName
            FROM JumpRoute r
            JOIN StarSystem sf ON r.FromSystemId = sf.SystemId
            JOIN StarSystem st ON r.ToSystemId = st.SystemId
            ORDER BY r.RouteId";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            routes.Add(MapFromReader(reader));
        }

        return routes;
    }

    /// <summary>
    /// Returns all jump routes that connect to the given system (both directions)
    /// </summary>
    public List<JumpRoute> GetBySystem(int systemId)
    {
        var routes = new List<JumpRoute>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT r.*, sf.Name AS FromSystemName, st.Name AS ToSystemName
            FROM JumpRoute r
            JOIN StarSystem sf ON r.FromSystemId = sf.SystemId
            JOIN StarSystem st ON r.ToSystemId = st.SystemId
            WHERE r.FromSystemId = @systemId OR r.ToSystemId = @systemId
            ORDER BY r.RouteId";
        command.Parameters.AddWithValue("@systemId", systemId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            routes.Add(MapFromReader(reader));
        }

        return routes;
    }

    public int Insert(JumpRoute route)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO JumpRoute (FromSystemId, ToSystemId, Distance, TravelDays)
            VALUES (@from, @to, @distance, @days);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@from", route.FromSystemId);
        command.Parameters.AddWithValue("@to", route.ToSystemId);
        command.Parameters.AddWithValue("@distance", route.Distance);
        command.Parameters.AddWithValue("@days", route.TravelDays);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static JumpRoute MapFromReader(SqliteDataReader reader)
    {
        return new JumpRoute
        {
            RouteId = reader.GetInt32(reader.GetOrdinal("RouteId")),
            FromSystemId = reader.GetInt32(reader.GetOrdinal("FromSystemId")),
            ToSystemId = reader.GetInt32(reader.GetOrdinal("ToSystemId")),
            Distance = reader.GetInt32(reader.GetOrdinal("Distance")),
            TravelDays = reader.GetInt32(reader.GetOrdinal("TravelDays")),
            FromSystemName = reader.GetString(reader.GetOrdinal("FromSystemName")),
            ToSystemName = reader.GetString(reader.GetOrdinal("ToSystemName"))
        };
    }
}
