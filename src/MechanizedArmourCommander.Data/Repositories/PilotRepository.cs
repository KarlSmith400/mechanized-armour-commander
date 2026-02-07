using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class PilotRepository
{
    private readonly DatabaseContext _context;

    public PilotRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<Pilot> GetAll()
    {
        var pilots = new List<Pilot>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Pilot ORDER BY Callsign";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            pilots.Add(MapFromReader(reader));
        }

        return pilots;
    }

    public Pilot? GetById(int pilotId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Pilot WHERE PilotId = @id";
        command.Parameters.AddWithValue("@id", pilotId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<Pilot> GetByStatus(string status)
    {
        var pilots = new List<Pilot>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM Pilot WHERE Status = @status ORDER BY Callsign";
        command.Parameters.AddWithValue("@status", status);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pilots.Add(MapFromReader(reader));
        }

        return pilots;
    }

    public int Insert(Pilot pilot)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Pilot (Callsign, GunnerySkill, PilotingSkill, TacticsSkill,
                ExperiencePoints, MissionsCompleted, Kills, Status, InjuryDays, Morale)
            VALUES (@callsign, @gunnery, @piloting, @tactics, @xp, @missions, @kills,
                @status, @injury, @morale);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@callsign", pilot.Callsign);
        command.Parameters.AddWithValue("@gunnery", pilot.GunnerySkill);
        command.Parameters.AddWithValue("@piloting", pilot.PilotingSkill);
        command.Parameters.AddWithValue("@tactics", pilot.TacticsSkill);
        command.Parameters.AddWithValue("@xp", pilot.ExperiencePoints);
        command.Parameters.AddWithValue("@missions", pilot.MissionsCompleted);
        command.Parameters.AddWithValue("@kills", pilot.Kills);
        command.Parameters.AddWithValue("@status", pilot.Status);
        command.Parameters.AddWithValue("@injury", pilot.InjuryDays);
        command.Parameters.AddWithValue("@morale", pilot.Morale);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Pilot pilot)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Pilot SET
                Callsign = @callsign, GunnerySkill = @gunnery, PilotingSkill = @piloting,
                TacticsSkill = @tactics, ExperiencePoints = @xp, MissionsCompleted = @missions,
                Kills = @kills, Status = @status, InjuryDays = @injury, Morale = @morale
            WHERE PilotId = @id";

        command.Parameters.AddWithValue("@id", pilot.PilotId);
        command.Parameters.AddWithValue("@callsign", pilot.Callsign);
        command.Parameters.AddWithValue("@gunnery", pilot.GunnerySkill);
        command.Parameters.AddWithValue("@piloting", pilot.PilotingSkill);
        command.Parameters.AddWithValue("@tactics", pilot.TacticsSkill);
        command.Parameters.AddWithValue("@xp", pilot.ExperiencePoints);
        command.Parameters.AddWithValue("@missions", pilot.MissionsCompleted);
        command.Parameters.AddWithValue("@kills", pilot.Kills);
        command.Parameters.AddWithValue("@status", pilot.Status);
        command.Parameters.AddWithValue("@injury", pilot.InjuryDays);
        command.Parameters.AddWithValue("@morale", pilot.Morale);

        command.ExecuteNonQuery();
    }

    public void Delete(int pilotId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM Pilot WHERE PilotId = @id";
        command.Parameters.AddWithValue("@id", pilotId);
        command.ExecuteNonQuery();
    }

    private Pilot MapFromReader(SqliteDataReader reader)
    {
        return new Pilot
        {
            PilotId = reader.GetInt32(reader.GetOrdinal("PilotId")),
            Callsign = reader.GetString(reader.GetOrdinal("Callsign")),
            GunnerySkill = reader.GetInt32(reader.GetOrdinal("GunnerySkill")),
            PilotingSkill = reader.GetInt32(reader.GetOrdinal("PilotingSkill")),
            TacticsSkill = reader.GetInt32(reader.GetOrdinal("TacticsSkill")),
            ExperiencePoints = reader.GetInt32(reader.GetOrdinal("ExperiencePoints")),
            MissionsCompleted = reader.GetInt32(reader.GetOrdinal("MissionsCompleted")),
            Kills = reader.GetInt32(reader.GetOrdinal("Kills")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            InjuryDays = reader.GetInt32(reader.GetOrdinal("InjuryDays")),
            Morale = reader.GetInt32(reader.GetOrdinal("Morale"))
        };
    }
}
