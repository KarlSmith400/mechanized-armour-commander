using Microsoft.Data.Sqlite;

namespace MechanizedArmourCommander.Data;

/// <summary>
/// Manages SQLite database connection and initialization
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseContext(string databasePath = "MechanizedArmourCommander.db")
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    public void Initialize()
    {
        var connection = GetConnection();
        CreateTables(connection);
        SeedInitialData(connection);
    }

    private void CreateTables(SqliteConnection connection)
    {
        var createTablesScript = @"
            -- Chassis table
            CREATE TABLE IF NOT EXISTS Chassis (
                ChassisId INTEGER PRIMARY KEY AUTOINCREMENT,
                Designation TEXT NOT NULL,
                Name TEXT NOT NULL,
                Class TEXT NOT NULL,
                HardpointSmall INTEGER NOT NULL,
                HardpointMedium INTEGER NOT NULL,
                HardpointLarge INTEGER NOT NULL,
                HeatCapacity INTEGER NOT NULL,
                AmmoCapacity INTEGER NOT NULL,
                ArmorPoints INTEGER NOT NULL,
                BaseSpeed INTEGER NOT NULL,
                BaseEvasion INTEGER NOT NULL
            );

            -- Weapon table
            CREATE TABLE IF NOT EXISTS Weapon (
                WeaponId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                HardpointSize TEXT NOT NULL,
                HeatGeneration INTEGER NOT NULL,
                AmmoConsumption INTEGER NOT NULL,
                Damage INTEGER NOT NULL,
                RangeClass TEXT NOT NULL,
                BaseAccuracy INTEGER NOT NULL,
                SalvageValue INTEGER NOT NULL,
                PurchaseCost INTEGER NOT NULL,
                SpecialEffect TEXT
            );

            -- FrameInstance table
            CREATE TABLE IF NOT EXISTS FrameInstance (
                InstanceId INTEGER PRIMARY KEY AUTOINCREMENT,
                ChassisId INTEGER NOT NULL,
                CustomName TEXT NOT NULL,
                CurrentArmor INTEGER NOT NULL,
                Status TEXT NOT NULL,
                RepairCost INTEGER NOT NULL,
                RepairTime INTEGER NOT NULL,
                AcquisitionDate TEXT NOT NULL,
                FOREIGN KEY (ChassisId) REFERENCES Chassis(ChassisId)
            );

            -- Loadout table
            CREATE TABLE IF NOT EXISTS Loadout (
                LoadoutId INTEGER PRIMARY KEY AUTOINCREMENT,
                InstanceId INTEGER NOT NULL,
                HardpointSlot TEXT NOT NULL,
                WeaponId INTEGER NOT NULL,
                FOREIGN KEY (InstanceId) REFERENCES FrameInstance(InstanceId),
                FOREIGN KEY (WeaponId) REFERENCES Weapon(WeaponId)
            );

            -- Pilot table
            CREATE TABLE IF NOT EXISTS Pilot (
                PilotId INTEGER PRIMARY KEY AUTOINCREMENT,
                Callsign TEXT NOT NULL,
                GunnerySkill INTEGER NOT NULL,
                PilotingSkill INTEGER NOT NULL,
                TacticsSkill INTEGER NOT NULL,
                ExperiencePoints INTEGER NOT NULL,
                MissionsCompleted INTEGER NOT NULL,
                Kills INTEGER NOT NULL,
                Status TEXT NOT NULL,
                InjuryDays INTEGER NOT NULL,
                Morale INTEGER NOT NULL
            );
        ";

        using var command = connection.CreateCommand();
        command.CommandText = createTablesScript;
        command.ExecuteNonQuery();
    }

    private void SeedInitialData(SqliteConnection connection)
    {
        // Check if data already exists
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Chassis";
        var count = (long?)checkCommand.ExecuteScalar();

        if (count > 0)
        {
            return; // Data already seeded
        }

        // Seed initial data using DataSeeder
        var seeder = new DataSeeder(this);
        seeder.SeedAll();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
