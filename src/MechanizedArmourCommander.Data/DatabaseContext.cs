using MechanizedArmourCommander.Data.Models;
using Microsoft.Data.Sqlite;

namespace MechanizedArmourCommander.Data;

/// <summary>
/// Manages SQLite database connection and initialization
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private const int SchemaVersion = 8; // Increment when schema changes

    public string DatabasePath { get; }

    public DatabaseContext(string databasePath = "MechanizedArmourCommander.db")
    {
        DatabasePath = databasePath;
        _connectionString = $"Data Source={databasePath};Pooling=False";
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

    public void Initialize(string? companyName = null)
    {
        var connection = GetConnection();

        if (!IsSchemaUpToDate(connection))
        {
            DropAllTables(connection);
        }

        CreateTables(connection);
        SeedInitialData(connection, companyName);
    }

    /// <summary>
    /// Reads PlayerState from a database file without initializing/seeding.
    /// Returns null if the file doesn't exist or has no PlayerState.
    /// </summary>
    public static PlayerState? PeekPlayerState(string databasePath)
    {
        if (!System.IO.File.Exists(databasePath))
            return null;

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Credits, Reputation, MissionsCompleted, MissionsWon, CompanyName, CurrentDay, CurrentSystemId, CurrentPlanetId, Fuel FROM PlayerState LIMIT 1";
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
        catch
        {
            return null;
        }
    }

    private bool IsSchemaUpToDate(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Version FROM SchemaVersion LIMIT 1";
            var version = (long?)command.ExecuteScalar();
            return version == SchemaVersion;
        }
        catch
        {
            return false;
        }
    }

    private void DropAllTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            DROP TABLE IF EXISTS MarketStock;
            DROP TABLE IF EXISTS JumpRoute;
            DROP TABLE IF EXISTS Planet;
            DROP TABLE IF EXISTS StarSystem;
            DROP TABLE IF EXISTS EquipmentInventory;
            DROP TABLE IF EXISTS EquipmentLoadout;
            DROP TABLE IF EXISTS Equipment;
            DROP TABLE IF EXISTS FactionStanding;
            DROP TABLE IF EXISTS Faction;
            DROP TABLE IF EXISTS Inventory;
            DROP TABLE IF EXISTS Loadout;
            DROP TABLE IF EXISTS FrameInstance;
            DROP TABLE IF EXISTS Weapon;
            DROP TABLE IF EXISTS Chassis;
            DROP TABLE IF EXISTS Pilot;
            DROP TABLE IF EXISTS PlayerState;
            DROP TABLE IF EXISTS SchemaVersion;
        ";
        command.ExecuteNonQuery();
    }

    private void CreateTables(SqliteConnection connection)
    {
        var createTablesScript = @"
            -- Schema version tracking
            CREATE TABLE IF NOT EXISTS SchemaVersion (
                Version INTEGER NOT NULL
            );

            -- Faction table
            CREATE TABLE IF NOT EXISTS Faction (
                FactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ShortName TEXT NOT NULL,
                Description TEXT NOT NULL,
                Color TEXT NOT NULL,
                WeaponPreference TEXT NOT NULL,
                ChassisPreference TEXT NOT NULL,
                EnemyPrefix TEXT NOT NULL
            );

            -- Faction standing
            CREATE TABLE IF NOT EXISTS FactionStanding (
                FactionId INTEGER NOT NULL,
                Standing INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (FactionId) REFERENCES Faction(FactionId)
            );

            -- Chassis table
            CREATE TABLE IF NOT EXISTS Chassis (
                ChassisId INTEGER PRIMARY KEY AUTOINCREMENT,
                Designation TEXT NOT NULL,
                Name TEXT NOT NULL,
                Class TEXT NOT NULL,
                HardpointSmall INTEGER NOT NULL,
                HardpointMedium INTEGER NOT NULL,
                HardpointLarge INTEGER NOT NULL,
                ReactorOutput INTEGER NOT NULL,
                MovementEnergyCost INTEGER NOT NULL,
                TotalSpace INTEGER NOT NULL,
                MaxArmorTotal INTEGER NOT NULL,
                StructureHead INTEGER NOT NULL,
                StructureCenterTorso INTEGER NOT NULL,
                StructureSideTorso INTEGER NOT NULL,
                StructureArm INTEGER NOT NULL,
                StructureLegs INTEGER NOT NULL,
                BaseSpeed INTEGER NOT NULL,
                BaseEvasion INTEGER NOT NULL,
                FactionId INTEGER,
                FOREIGN KEY (FactionId) REFERENCES Faction(FactionId)
            );

            -- Weapon table
            CREATE TABLE IF NOT EXISTS Weapon (
                WeaponId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                HardpointSize TEXT NOT NULL,
                WeaponType TEXT NOT NULL,
                EnergyCost INTEGER NOT NULL,
                AmmoPerShot INTEGER NOT NULL,
                SpaceCost INTEGER NOT NULL,
                Damage INTEGER NOT NULL,
                RangeClass TEXT NOT NULL,
                BaseAccuracy INTEGER NOT NULL,
                SalvageValue INTEGER NOT NULL,
                PurchaseCost INTEGER NOT NULL,
                SpecialEffect TEXT,
                FactionId INTEGER,
                FOREIGN KEY (FactionId) REFERENCES Faction(FactionId)
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

            -- FrameInstance table
            CREATE TABLE IF NOT EXISTS FrameInstance (
                InstanceId INTEGER PRIMARY KEY AUTOINCREMENT,
                ChassisId INTEGER NOT NULL,
                CustomName TEXT NOT NULL,
                ArmorHead INTEGER NOT NULL,
                ArmorCenterTorso INTEGER NOT NULL,
                ArmorLeftTorso INTEGER NOT NULL,
                ArmorRightTorso INTEGER NOT NULL,
                ArmorLeftArm INTEGER NOT NULL,
                ArmorRightArm INTEGER NOT NULL,
                ArmorLegs INTEGER NOT NULL,
                ReactorStress INTEGER NOT NULL DEFAULT 0,
                Status TEXT NOT NULL,
                RepairCost INTEGER NOT NULL,
                RepairTime INTEGER NOT NULL,
                AcquisitionDate TEXT NOT NULL,
                PilotId INTEGER,
                FOREIGN KEY (ChassisId) REFERENCES Chassis(ChassisId),
                FOREIGN KEY (PilotId) REFERENCES Pilot(PilotId)
            );

            -- Loadout table
            CREATE TABLE IF NOT EXISTS Loadout (
                LoadoutId INTEGER PRIMARY KEY AUTOINCREMENT,
                InstanceId INTEGER NOT NULL,
                HardpointSlot TEXT NOT NULL,
                WeaponId INTEGER NOT NULL,
                WeaponGroup INTEGER NOT NULL DEFAULT 1,
                MountLocation TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (InstanceId) REFERENCES FrameInstance(InstanceId),
                FOREIGN KEY (WeaponId) REFERENCES Weapon(WeaponId)
            );

            -- PlayerState table (single row for campaign state)
            CREATE TABLE IF NOT EXISTS PlayerState (
                Credits INTEGER NOT NULL,
                Reputation INTEGER NOT NULL,
                MissionsCompleted INTEGER NOT NULL,
                MissionsWon INTEGER NOT NULL,
                CompanyName TEXT NOT NULL,
                CurrentDay INTEGER NOT NULL,
                CurrentSystemId INTEGER NOT NULL DEFAULT 10,
                CurrentPlanetId INTEGER NOT NULL DEFAULT 21,
                Fuel INTEGER NOT NULL DEFAULT 50
            );

            -- Inventory table (company weapon storage)
            CREATE TABLE IF NOT EXISTS Inventory (
                InventoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                WeaponId INTEGER NOT NULL,
                FOREIGN KEY (WeaponId) REFERENCES Weapon(WeaponId)
            );

            -- Equipment definition table
            CREATE TABLE IF NOT EXISTS Equipment (
                EquipmentId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                HardpointSize TEXT,
                SpaceCost INTEGER NOT NULL,
                EnergyCost INTEGER NOT NULL,
                Effect TEXT NOT NULL,
                EffectValue INTEGER NOT NULL,
                PurchaseCost INTEGER NOT NULL,
                SalvageValue INTEGER NOT NULL,
                Description TEXT
            );

            -- Equipment loadout (equipped on frames)
            CREATE TABLE IF NOT EXISTS EquipmentLoadout (
                EquipmentLoadoutId INTEGER PRIMARY KEY AUTOINCREMENT,
                InstanceId INTEGER NOT NULL,
                EquipmentId INTEGER NOT NULL,
                HardpointSlot TEXT,
                FOREIGN KEY (InstanceId) REFERENCES FrameInstance(InstanceId),
                FOREIGN KEY (EquipmentId) REFERENCES Equipment(EquipmentId)
            );

            -- Equipment inventory (company storage)
            CREATE TABLE IF NOT EXISTS EquipmentInventory (
                EquipmentInventoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                EquipmentId INTEGER NOT NULL,
                FOREIGN KEY (EquipmentId) REFERENCES Equipment(EquipmentId)
            );

            -- Star system table
            CREATE TABLE IF NOT EXISTS StarSystem (
                SystemId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                X REAL NOT NULL,
                Y REAL NOT NULL,
                ControllingFactionId INTEGER,
                SystemType TEXT NOT NULL,
                Description TEXT NOT NULL,
                FOREIGN KEY (ControllingFactionId) REFERENCES Faction(FactionId)
            );

            -- Planet/station table
            CREATE TABLE IF NOT EXISTS Planet (
                PlanetId INTEGER PRIMARY KEY AUTOINCREMENT,
                SystemId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                PlanetType TEXT NOT NULL,
                Description TEXT NOT NULL,
                HasMarket INTEGER NOT NULL DEFAULT 1,
                HasHiring INTEGER NOT NULL DEFAULT 0,
                ContractDifficultyMin INTEGER NOT NULL DEFAULT 1,
                ContractDifficultyMax INTEGER NOT NULL DEFAULT 3,
                FOREIGN KEY (SystemId) REFERENCES StarSystem(SystemId)
            );

            -- Jump route table (bidirectional connections between systems)
            CREATE TABLE IF NOT EXISTS JumpRoute (
                RouteId INTEGER PRIMARY KEY AUTOINCREMENT,
                FromSystemId INTEGER NOT NULL,
                ToSystemId INTEGER NOT NULL,
                Distance INTEGER NOT NULL,
                TravelDays INTEGER NOT NULL,
                FOREIGN KEY (FromSystemId) REFERENCES StarSystem(SystemId),
                FOREIGN KEY (ToSystemId) REFERENCES StarSystem(SystemId)
            );

            -- Market stock (persistent per-planet inventory, refreshes weekly)
            CREATE TABLE IF NOT EXISTS MarketStock (
                MarketStockId INTEGER PRIMARY KEY AUTOINCREMENT,
                PlanetId INTEGER NOT NULL,
                ItemType TEXT NOT NULL,
                ItemId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 1,
                GeneratedOnDay INTEGER NOT NULL,
                FOREIGN KEY (PlanetId) REFERENCES Planet(PlanetId)
            );
        ";

        using var command = connection.CreateCommand();
        command.CommandText = createTablesScript;
        command.ExecuteNonQuery();

        // Insert schema version if not present
        using var versionCheck = connection.CreateCommand();
        versionCheck.CommandText = "SELECT COUNT(*) FROM SchemaVersion";
        var count = (long?)versionCheck.ExecuteScalar();
        if (count == 0)
        {
            using var insertVersion = connection.CreateCommand();
            insertVersion.CommandText = "INSERT INTO SchemaVersion (Version) VALUES (@version)";
            insertVersion.Parameters.AddWithValue("@version", SchemaVersion);
            insertVersion.ExecuteNonQuery();
        }
    }

    private void SeedInitialData(SqliteConnection connection, string? companyName = null)
    {
        // Check if data already exists
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Faction";
        var count = (long?)checkCommand.ExecuteScalar();

        if (count > 0)
        {
            return; // Data already seeded
        }

        // Seed initial data using DataSeeder
        var seeder = new DataSeeder(this);
        seeder.SeedAll(companyName ?? "Iron Wolves");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
