# Test script to verify database seeding
Write-Host "Testing Database Seeding..." -ForegroundColor Cyan

# Check if database file exists
if (Test-Path "MechanizedArmourCommander.db") {
    Remove-Item "MechanizedArmourCommander.db"
    Write-Host "Removed existing database" -ForegroundColor Yellow
}

# Run the application briefly to trigger database initialization
Write-Host "Building and initializing database..." -ForegroundColor Cyan
dotnet build MechanizedArmourCommander.sln --nologo --verbosity quiet

# Create a simple C# script to query the database
$testScript = @'
using Microsoft.Data.Sqlite;

var connString = "Data Source=MechanizedArmourCommander.db";

// First, trigger database creation by running the app's Initialize
using (var context = new MechanizedArmourCommander.Data.DatabaseContext())
{
    context.Initialize();
}

using var conn = new SqliteConnection(connString);
conn.Open();

// Query chassis count
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*), Class FROM Chassis GROUP BY Class ORDER BY Class";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n=== CHASSIS BY CLASS ===");
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString(1)}: {reader.GetInt32(0)} chassis");
    }
}

// Query weapon count
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*), HardpointSize FROM Weapon GROUP BY HardpointSize ORDER BY HardpointSize";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n=== WEAPONS BY SIZE ===");
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString(1)}: {reader.GetInt32(0)} weapons");
    }
}

// Show sample chassis
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Designation, Name, Class, ArmorPoints FROM Chassis LIMIT 5";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n=== SAMPLE CHASSIS ===");
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString(0)} {reader.GetString(1)} ({reader.GetString(2)}) - Armor: {reader.GetInt32(3)}");
    }
}

// Show sample weapons
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT Name, HardpointSize, Damage, RangeClass FROM Weapon LIMIT 5";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("\n=== SAMPLE WEAPONS ===");
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString(0)} ({reader.GetString(1)}) - Damage: {reader.GetInt32(2)}, Range: {reader.GetString(3)}");
    }
}

Console.WriteLine("\n=== Database Seeding Successful! ===");
'@

Set-Content -Path "test-db-query.csx" -Value $testScript

# Run the test using dotnet-script or direct execution
Write-Host "Querying database..." -ForegroundColor Cyan
dotnet script test-db-query.csx

# Cleanup
if (Test-Path "test-db-query.csx") {
    Remove-Item "test-db-query.csx"
}

Write-Host "`nTest complete!" -ForegroundColor Green
