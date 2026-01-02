using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.Data;

/// <summary>
/// Seeds the database with initial game data from the design document
/// </summary>
public class DataSeeder
{
    private readonly DatabaseContext _context;
    private readonly ChassisRepository _chassisRepo;
    private readonly WeaponRepository _weaponRepo;

    public DataSeeder(DatabaseContext context)
    {
        _context = context;
        _chassisRepo = new ChassisRepository(context);
        _weaponRepo = new WeaponRepository(context);
    }

    /// <summary>
    /// Seeds all initial data (chassis and weapons)
    /// </summary>
    public void SeedAll()
    {
        SeedWeapons();
        SeedChassis();
    }

    /// <summary>
    /// Seeds all weapon data from the design document
    /// </summary>
    public void SeedWeapons()
    {
        var weapons = new List<Weapon>
        {
            // Small Hardpoint Weapons
            new Weapon
            {
                Name = "Light Laser",
                HardpointSize = "Small",
                HeatGeneration = 2,
                AmmoConsumption = 0,
                Damage = 5,
                RangeClass = "Medium",
                BaseAccuracy = 85,
                SalvageValue = 5000,
                PurchaseCost = 10000
            },
            new Weapon
            {
                Name = "Machine Gun",
                HardpointSize = "Small",
                HeatGeneration = 0,
                AmmoConsumption = 10,
                Damage = 3,
                RangeClass = "Short",
                BaseAccuracy = 90,
                SalvageValue = 3000,
                PurchaseCost = 6000
            },
            new Weapon
            {
                Name = "Flamer",
                HardpointSize = "Small",
                HeatGeneration = 4,
                AmmoConsumption = 0,
                Damage = 2,
                RangeClass = "Short",
                BaseAccuracy = 95,
                SalvageValue = 4000,
                PurchaseCost = 8000,
                SpecialEffect = "Increases enemy heat"
            },
            new Weapon
            {
                Name = "Small Missile Rack",
                HardpointSize = "Small",
                HeatGeneration = 2,
                AmmoConsumption = 8,
                Damage = 6,
                RangeClass = "Short",
                BaseAccuracy = 80,
                SalvageValue = 6000,
                PurchaseCost = 12000
            },

            // Medium Hardpoint Weapons
            new Weapon
            {
                Name = "Medium Laser",
                HardpointSize = "Medium",
                HeatGeneration = 4,
                AmmoConsumption = 0,
                Damage = 10,
                RangeClass = "Medium",
                BaseAccuracy = 80,
                SalvageValue = 10000,
                PurchaseCost = 20000
            },
            new Weapon
            {
                Name = "Autocannon-5",
                HardpointSize = "Medium",
                HeatGeneration = 1,
                AmmoConsumption = 5,
                Damage = 8,
                RangeClass = "Long",
                BaseAccuracy = 80,
                SalvageValue = 12000,
                PurchaseCost = 24000
            },
            new Weapon
            {
                Name = "Missile Pod (SRM-6)",
                HardpointSize = "Medium",
                HeatGeneration = 3,
                AmmoConsumption = 10,
                Damage = 12,
                RangeClass = "Short",
                BaseAccuracy = 75,
                SalvageValue = 15000,
                PurchaseCost = 30000
            },
            new Weapon
            {
                Name = "Gauss Rifle (Light)",
                HardpointSize = "Medium",
                HeatGeneration = 1,
                AmmoConsumption = 8,
                Damage = 15,
                RangeClass = "Long",
                BaseAccuracy = 85,
                SalvageValue = 25000,
                PurchaseCost = 50000
            },

            // Large Hardpoint Weapons
            new Weapon
            {
                Name = "Heavy Laser",
                HardpointSize = "Large",
                HeatGeneration = 8,
                AmmoConsumption = 0,
                Damage = 20,
                RangeClass = "Long",
                BaseAccuracy = 75,
                SalvageValue = 25000,
                PurchaseCost = 50000
            },
            new Weapon
            {
                Name = "Heavy Autocannon-10",
                HardpointSize = "Large",
                HeatGeneration = 2,
                AmmoConsumption = 8,
                Damage = 20,
                RangeClass = "Long",
                BaseAccuracy = 70,
                SalvageValue = 30000,
                PurchaseCost = 60000
            },
            new Weapon
            {
                Name = "Plasma Lance",
                HardpointSize = "Large",
                HeatGeneration = 10,
                AmmoConsumption = 0,
                Damage = 25,
                RangeClass = "Medium",
                BaseAccuracy = 65,
                SalvageValue = 40000,
                PurchaseCost = 80000
            },
            new Weapon
            {
                Name = "LRM-15 (Long Range Missiles)",
                HardpointSize = "Large",
                HeatGeneration = 5,
                AmmoConsumption = 12,
                Damage = 15,
                RangeClass = "Long",
                BaseAccuracy = 70,
                SalvageValue = 35000,
                PurchaseCost = 70000,
                SpecialEffect = "Indirect fire capable"
            },
            new Weapon
            {
                Name = "Gauss Cannon (Heavy)",
                HardpointSize = "Large",
                HeatGeneration = 1,
                AmmoConsumption = 6,
                Damage = 30,
                RangeClass = "Long",
                BaseAccuracy = 80,
                SalvageValue = 50000,
                PurchaseCost = 100000
            }
        };

        foreach (var weapon in weapons)
        {
            _weaponRepo.Insert(weapon);
        }
    }

    /// <summary>
    /// Seeds all chassis data from the design document
    /// </summary>
    public void SeedChassis()
    {
        var chassisList = new List<Chassis>
        {
            // Light Class (20-35 tons)
            new Chassis
            {
                Designation = "SC-20",
                Name = "Scout",
                Class = "Light",
                HardpointSmall = 4,
                HardpointMedium = 2,
                HardpointLarge = 0,
                HeatCapacity = 20,
                AmmoCapacity = 100,
                ArmorPoints = 60,
                BaseSpeed = 9,
                BaseEvasion = 25
            },
            new Chassis
            {
                Designation = "RD-30",
                Name = "Raider",
                Class = "Light",
                HardpointSmall = 3,
                HardpointMedium = 3,
                HardpointLarge = 0,
                HeatCapacity = 25,
                AmmoCapacity = 120,
                ArmorPoints = 70,
                BaseSpeed = 8,
                BaseEvasion = 22
            },
            new Chassis
            {
                Designation = "HR-35",
                Name = "Harrier",
                Class = "Light",
                HardpointSmall = 2,
                HardpointMedium = 3,
                HardpointLarge = 1,
                HeatCapacity = 28,
                AmmoCapacity = 130,
                ArmorPoints = 75,
                BaseSpeed = 7,
                BaseEvasion = 20
            },

            // Medium Class (40-55 tons)
            new Chassis
            {
                Designation = "VG-45",
                Name = "Vanguard",
                Class = "Medium",
                HardpointSmall = 3,
                HardpointMedium = 3,
                HardpointLarge = 1,
                HeatCapacity = 30,
                AmmoCapacity = 150,
                ArmorPoints = 100,
                BaseSpeed = 6,
                BaseEvasion = 15
            },
            new Chassis
            {
                Designation = "EN-50",
                Name = "Enforcer",
                Class = "Medium",
                HardpointSmall = 2,
                HardpointMedium = 2,
                HardpointLarge = 2,
                HeatCapacity = 32,
                AmmoCapacity = 140,
                ArmorPoints = 110,
                BaseSpeed = 5,
                BaseEvasion = 13
            },
            new Chassis
            {
                Designation = "RG-55",
                Name = "Ranger",
                Class = "Medium",
                HardpointSmall = 4,
                HardpointMedium = 4,
                HardpointLarge = 0,
                HeatCapacity = 35,
                AmmoCapacity = 180,
                ArmorPoints = 95,
                BaseSpeed = 6,
                BaseEvasion = 16
            },

            // Heavy Class (60-75 tons)
            new Chassis
            {
                Designation = "WD-60",
                Name = "Warden",
                Class = "Heavy",
                HardpointSmall = 2,
                HardpointMedium = 3,
                HardpointLarge = 2,
                HeatCapacity = 40,
                AmmoCapacity = 200,
                ArmorPoints = 140,
                BaseSpeed = 4,
                BaseEvasion = 11
            },
            new Chassis
            {
                Designation = "BR-70",
                Name = "Bruiser",
                Class = "Heavy",
                HardpointSmall = 1,
                HardpointMedium = 3,
                HardpointLarge = 3,
                HeatCapacity = 38,
                AmmoCapacity = 180,
                ArmorPoints = 150,
                BaseSpeed = 4,
                BaseEvasion = 10
            },
            new Chassis
            {
                Designation = "SN-75",
                Name = "Sentinel",
                Class = "Heavy",
                HardpointSmall = 3,
                HardpointMedium = 4,
                HardpointLarge = 2,
                HeatCapacity = 45,
                AmmoCapacity = 220,
                ArmorPoints = 145,
                BaseSpeed = 4,
                BaseEvasion = 12
            },

            // Assault Class (80-100 tons)
            new Chassis
            {
                Designation = "TN-85",
                Name = "Titan",
                Class = "Assault",
                HardpointSmall = 2,
                HardpointMedium = 4,
                HardpointLarge = 3,
                HeatCapacity = 50,
                AmmoCapacity = 250,
                ArmorPoints = 180,
                BaseSpeed = 3,
                BaseEvasion = 8
            },
            new Chassis
            {
                Designation = "JG-95",
                Name = "Juggernaut",
                Class = "Assault",
                HardpointSmall = 1,
                HardpointMedium = 3,
                HardpointLarge = 4,
                HeatCapacity = 48,
                AmmoCapacity = 240,
                ArmorPoints = 200,
                BaseSpeed = 2,
                BaseEvasion = 6
            },
            new Chassis
            {
                Designation = "CL-100",
                Name = "Colossus",
                Class = "Assault",
                HardpointSmall = 2,
                HardpointMedium = 5,
                HardpointLarge = 4,
                HeatCapacity = 55,
                AmmoCapacity = 280,
                ArmorPoints = 220,
                BaseSpeed = 2,
                BaseEvasion = 5
            }
        };

        foreach (var chassis in chassisList)
        {
            _chassisRepo.Insert(chassis);
        }
    }
}
