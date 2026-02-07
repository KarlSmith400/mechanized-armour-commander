using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class FrameInstanceRepository
{
    private readonly DatabaseContext _context;

    public FrameInstanceRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<FrameInstance> GetAll()
    {
        var frames = new List<FrameInstance>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT fi.*, c.Designation, c.Name AS ChassisName, c.Class, c.HardpointSmall,
                   c.HardpointMedium, c.HardpointLarge, c.ReactorOutput, c.MovementEnergyCost,
                   c.TotalSpace, c.MaxArmorTotal, c.StructureHead, c.StructureCenterTorso,
                   c.StructureSideTorso, c.StructureArm, c.StructureLegs, c.BaseSpeed, c.BaseEvasion,
                   p.Callsign AS PilotCallsign
            FROM FrameInstance fi
            INNER JOIN Chassis c ON fi.ChassisId = c.ChassisId
            LEFT JOIN Pilot p ON fi.PilotId = p.PilotId
            ORDER BY fi.InstanceId";
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            frames.Add(MapFromReader(reader));
        }

        return frames;
    }

    public FrameInstance? GetById(int instanceId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT fi.*, c.Designation, c.Name AS ChassisName, c.Class, c.HardpointSmall,
                   c.HardpointMedium, c.HardpointLarge, c.ReactorOutput, c.MovementEnergyCost,
                   c.TotalSpace, c.MaxArmorTotal, c.StructureHead, c.StructureCenterTorso,
                   c.StructureSideTorso, c.StructureArm, c.StructureLegs, c.BaseSpeed, c.BaseEvasion,
                   p.Callsign AS PilotCallsign
            FROM FrameInstance fi
            INNER JOIN Chassis c ON fi.ChassisId = c.ChassisId
            LEFT JOIN Pilot p ON fi.PilotId = p.PilotId
            WHERE fi.InstanceId = @id";
        command.Parameters.AddWithValue("@id", instanceId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapFromReader(reader) : null;
    }

    public List<FrameInstance> GetByStatus(string status)
    {
        var frames = new List<FrameInstance>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT fi.*, c.Designation, c.Name AS ChassisName, c.Class, c.HardpointSmall,
                   c.HardpointMedium, c.HardpointLarge, c.ReactorOutput, c.MovementEnergyCost,
                   c.TotalSpace, c.MaxArmorTotal, c.StructureHead, c.StructureCenterTorso,
                   c.StructureSideTorso, c.StructureArm, c.StructureLegs, c.BaseSpeed, c.BaseEvasion,
                   p.Callsign AS PilotCallsign
            FROM FrameInstance fi
            INNER JOIN Chassis c ON fi.ChassisId = c.ChassisId
            LEFT JOIN Pilot p ON fi.PilotId = p.PilotId
            WHERE fi.Status = @status
            ORDER BY fi.InstanceId";
        command.Parameters.AddWithValue("@status", status);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            frames.Add(MapFromReader(reader));
        }

        return frames;
    }

    public int Insert(FrameInstance frame)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO FrameInstance (ChassisId, CustomName, ArmorHead, ArmorCenterTorso,
                ArmorLeftTorso, ArmorRightTorso, ArmorLeftArm, ArmorRightArm, ArmorLegs,
                ReactorStress, Status, RepairCost, RepairTime, AcquisitionDate, PilotId)
            VALUES (@chassisId, @name, @aHead, @aCT, @aLT, @aRT, @aLA, @aRA, @aLegs,
                @stress, @status, @repairCost, @repairTime, @acqDate, @pilotId);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@chassisId", frame.ChassisId);
        command.Parameters.AddWithValue("@name", frame.CustomName);
        command.Parameters.AddWithValue("@aHead", frame.ArmorHead);
        command.Parameters.AddWithValue("@aCT", frame.ArmorCenterTorso);
        command.Parameters.AddWithValue("@aLT", frame.ArmorLeftTorso);
        command.Parameters.AddWithValue("@aRT", frame.ArmorRightTorso);
        command.Parameters.AddWithValue("@aLA", frame.ArmorLeftArm);
        command.Parameters.AddWithValue("@aRA", frame.ArmorRightArm);
        command.Parameters.AddWithValue("@aLegs", frame.ArmorLegs);
        command.Parameters.AddWithValue("@stress", frame.ReactorStress);
        command.Parameters.AddWithValue("@status", frame.Status);
        command.Parameters.AddWithValue("@repairCost", frame.RepairCost);
        command.Parameters.AddWithValue("@repairTime", frame.RepairTime);
        command.Parameters.AddWithValue("@acqDate", frame.AcquisitionDate.ToString("o"));
        command.Parameters.AddWithValue("@pilotId", (object?)frame.PilotId ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(FrameInstance frame)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE FrameInstance SET
                CustomName = @name,
                ArmorHead = @aHead, ArmorCenterTorso = @aCT,
                ArmorLeftTorso = @aLT, ArmorRightTorso = @aRT,
                ArmorLeftArm = @aLA, ArmorRightArm = @aRA, ArmorLegs = @aLegs,
                ReactorStress = @stress, Status = @status,
                RepairCost = @repairCost, RepairTime = @repairTime,
                PilotId = @pilotId
            WHERE InstanceId = @id";

        command.Parameters.AddWithValue("@id", frame.InstanceId);
        command.Parameters.AddWithValue("@name", frame.CustomName);
        command.Parameters.AddWithValue("@aHead", frame.ArmorHead);
        command.Parameters.AddWithValue("@aCT", frame.ArmorCenterTorso);
        command.Parameters.AddWithValue("@aLT", frame.ArmorLeftTorso);
        command.Parameters.AddWithValue("@aRT", frame.ArmorRightTorso);
        command.Parameters.AddWithValue("@aLA", frame.ArmorLeftArm);
        command.Parameters.AddWithValue("@aRA", frame.ArmorRightArm);
        command.Parameters.AddWithValue("@aLegs", frame.ArmorLegs);
        command.Parameters.AddWithValue("@stress", frame.ReactorStress);
        command.Parameters.AddWithValue("@status", frame.Status);
        command.Parameters.AddWithValue("@repairCost", frame.RepairCost);
        command.Parameters.AddWithValue("@repairTime", frame.RepairTime);
        command.Parameters.AddWithValue("@pilotId", (object?)frame.PilotId ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void Delete(int instanceId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM FrameInstance WHERE InstanceId = @id";
        command.Parameters.AddWithValue("@id", instanceId);
        command.ExecuteNonQuery();
    }

    private FrameInstance MapFromReader(SqliteDataReader reader)
    {
        var frame = new FrameInstance
        {
            InstanceId = reader.GetInt32(reader.GetOrdinal("InstanceId")),
            ChassisId = reader.GetInt32(reader.GetOrdinal("ChassisId")),
            CustomName = reader.GetString(reader.GetOrdinal("CustomName")),
            ArmorHead = reader.GetInt32(reader.GetOrdinal("ArmorHead")),
            ArmorCenterTorso = reader.GetInt32(reader.GetOrdinal("ArmorCenterTorso")),
            ArmorLeftTorso = reader.GetInt32(reader.GetOrdinal("ArmorLeftTorso")),
            ArmorRightTorso = reader.GetInt32(reader.GetOrdinal("ArmorRightTorso")),
            ArmorLeftArm = reader.GetInt32(reader.GetOrdinal("ArmorLeftArm")),
            ArmorRightArm = reader.GetInt32(reader.GetOrdinal("ArmorRightArm")),
            ArmorLegs = reader.GetInt32(reader.GetOrdinal("ArmorLegs")),
            ReactorStress = reader.GetInt32(reader.GetOrdinal("ReactorStress")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            RepairCost = reader.GetInt32(reader.GetOrdinal("RepairCost")),
            RepairTime = reader.GetInt32(reader.GetOrdinal("RepairTime")),
            AcquisitionDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("AcquisitionDate"))),
            PilotId = reader.IsDBNull(reader.GetOrdinal("PilotId")) ? null : reader.GetInt32(reader.GetOrdinal("PilotId")),
            Chassis = new Chassis
            {
                ChassisId = reader.GetInt32(reader.GetOrdinal("ChassisId")),
                Designation = reader.GetString(reader.GetOrdinal("Designation")),
                Name = reader.GetString(reader.GetOrdinal("ChassisName")),
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
                BaseEvasion = reader.GetInt32(reader.GetOrdinal("BaseEvasion"))
            }
        };

        return frame;
    }
}
