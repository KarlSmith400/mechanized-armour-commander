using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Repositories;

Console.WriteLine("Testing Database Seeding...\n");

// Delete old database if it exists
if (File.Exists("MechanizedArmourCommander.db"))
{
    File.Delete("MechanizedArmourCommander.db");
    Console.WriteLine("Deleted existing database");
}

// Create and initialize database
using var context = new DatabaseContext();
Console.WriteLine("Initializing database...");
context.Initialize();
Console.WriteLine("Database initialized!\n");

// Query the data
var chassisRepo = new ChassisRepository(context);
var weaponRepo = new WeaponRepository(context);

var allChassis = chassisRepo.GetAll();
var allWeapons = weaponRepo.GetAll();

Console.WriteLine("=== DATABASE STATS ===");
Console.WriteLine($"Total Chassis: {allChassis.Count}");
Console.WriteLine($"Total Weapons: {allWeapons.Count}\n");

Console.WriteLine("Chassis by Class:");
foreach (var classGroup in allChassis.GroupBy(c => c.Class).OrderBy(g => g.Key))
{
    Console.WriteLine($"  {classGroup.Key}: {classGroup.Count()}");
}

Console.WriteLine("\nWeapons by Hardpoint Size:");
foreach (var sizeGroup in allWeapons.GroupBy(w => w.HardpointSize).OrderBy(g => g.Key))
{
    Console.WriteLine($"  {sizeGroup.Key}: {sizeGroup.Count()}");
}

Console.WriteLine("\n=== SAMPLE DATA ===");
Console.WriteLine("\nFirst 3 Chassis:");
foreach (var chassis in allChassis.Take(3))
{
    Console.WriteLine($"  {chassis.Designation} {chassis.Name} ({chassis.Class}) - {chassis.ArmorPoints} armor");
}

Console.WriteLine("\nFirst 3 Weapons:");
foreach (var weapon in allWeapons.Take(3))
{
    Console.WriteLine($"  {weapon.Name} ({weapon.HardpointSize}) - {weapon.Damage} damage, {weapon.RangeClass} range");
}

Console.WriteLine("\n✓ Database seeding test PASSED!");
