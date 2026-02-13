using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class PlayerStateRepository
{
    private readonly DatabaseContext _context;

    public PlayerStateRepository(DatabaseContext context)
    {
        _context = context;
    }

    public PlayerState? Get()
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM PlayerState LIMIT 1";

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new PlayerState
        {
            Credits = reader.GetInt32(reader.GetOrdinal("Credits")),
            Reputation = reader.GetInt32(reader.GetOrdinal("Reputation")),
            MissionsCompleted = reader.GetInt32(reader.GetOrdinal("MissionsCompleted")),
            MissionsWon = reader.GetInt32(reader.GetOrdinal("MissionsWon")),
            CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
            CurrentDay = reader.GetInt32(reader.GetOrdinal("CurrentDay")),
            CurrentSystemId = reader.GetInt32(reader.GetOrdinal("CurrentSystemId")),
            CurrentPlanetId = reader.GetInt32(reader.GetOrdinal("CurrentPlanetId")),
            Fuel = reader.GetInt32(reader.GetOrdinal("Fuel"))
        };
    }

    public void Initialize(PlayerState state)
    {
        var connection = _context.GetConnection();

        // Clear any existing state
        using var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM PlayerState";
        deleteCmd.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO PlayerState (Credits, Reputation, MissionsCompleted, MissionsWon,
                CompanyName, CurrentDay, CurrentSystemId, CurrentPlanetId, Fuel)
            VALUES (@credits, @rep, @completed, @won, @company, @day, @systemId, @planetId, @fuel)";

        command.Parameters.AddWithValue("@credits", state.Credits);
        command.Parameters.AddWithValue("@rep", state.Reputation);
        command.Parameters.AddWithValue("@completed", state.MissionsCompleted);
        command.Parameters.AddWithValue("@won", state.MissionsWon);
        command.Parameters.AddWithValue("@company", state.CompanyName);
        command.Parameters.AddWithValue("@day", state.CurrentDay);
        command.Parameters.AddWithValue("@systemId", state.CurrentSystemId);
        command.Parameters.AddWithValue("@planetId", state.CurrentPlanetId);
        command.Parameters.AddWithValue("@fuel", state.Fuel);

        command.ExecuteNonQuery();
    }

    public void Update(PlayerState state)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE PlayerState SET
                Credits = @credits, Reputation = @rep,
                MissionsCompleted = @completed, MissionsWon = @won,
                CompanyName = @company, CurrentDay = @day,
                CurrentSystemId = @systemId, CurrentPlanetId = @planetId,
                Fuel = @fuel";

        command.Parameters.AddWithValue("@credits", state.Credits);
        command.Parameters.AddWithValue("@rep", state.Reputation);
        command.Parameters.AddWithValue("@completed", state.MissionsCompleted);
        command.Parameters.AddWithValue("@won", state.MissionsWon);
        command.Parameters.AddWithValue("@company", state.CompanyName);
        command.Parameters.AddWithValue("@day", state.CurrentDay);
        command.Parameters.AddWithValue("@systemId", state.CurrentSystemId);
        command.Parameters.AddWithValue("@planetId", state.CurrentPlanetId);
        command.Parameters.AddWithValue("@fuel", state.Fuel);

        command.ExecuteNonQuery();
    }
}
