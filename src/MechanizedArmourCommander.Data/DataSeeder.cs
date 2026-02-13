using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.Data;

/// <summary>
/// Seeds the database with initial game data
/// </summary>
public class DataSeeder
{
    private readonly DatabaseContext _context;
    private readonly ChassisRepository _chassisRepo;
    private readonly WeaponRepository _weaponRepo;
    private readonly PilotRepository _pilotRepo;
    private readonly FrameInstanceRepository _frameRepo;
    private readonly LoadoutRepository _loadoutRepo;
    private readonly PlayerStateRepository _stateRepo;
    private readonly FactionRepository _factionRepo;
    private readonly FactionStandingRepository _standingRepo;
    private readonly EquipmentRepository _equipmentRepo;
    private readonly StarSystemRepository _systemRepo;
    private readonly PlanetRepository _planetRepo;
    private readonly JumpRouteRepository _jumpRouteRepo;

    public DataSeeder(DatabaseContext context)
    {
        _context = context;
        _chassisRepo = new ChassisRepository(context);
        _weaponRepo = new WeaponRepository(context);
        _pilotRepo = new PilotRepository(context);
        _frameRepo = new FrameInstanceRepository(context);
        _loadoutRepo = new LoadoutRepository(context);
        _stateRepo = new PlayerStateRepository(context);
        _factionRepo = new FactionRepository(context);
        _standingRepo = new FactionStandingRepository(context);
        _equipmentRepo = new EquipmentRepository(context);
        _systemRepo = new StarSystemRepository(context);
        _planetRepo = new PlanetRepository(context);
        _jumpRouteRepo = new JumpRouteRepository(context);
    }

    public void SeedAll(string companyName = "Iron Wolves")
    {
        SeedFactions();
        SeedFactionStandings();
        SeedWeapons();
        SeedEquipment();
        SeedChassis();
        SeedStarSystems();
        SeedPlanets();
        SeedJumpRoutes();
        SeedPilots();
        SeedStartingFrames();
        SeedPlayerState(companyName);
    }

    public void SeedFactions()
    {
        _factionRepo.Insert(new Faction
        {
            Name = "Crucible Industries",
            ShortName = "CRI",
            Description = "A corporate megacorp specializing in advanced energy systems and heavy armored platforms.",
            Color = "#00CCFF",
            WeaponPreference = "Energy",
            ChassisPreference = "Heavy",
            EnemyPrefix = "CRI-"
        });

        _factionRepo.Insert(new Faction
        {
            Name = "Terran Directorate",
            ShortName = "TDR",
            Description = "The central military authority maintaining order across settled space with balanced doctrine.",
            Color = "#FFCC00",
            WeaponPreference = "Balanced",
            ChassisPreference = "All",
            EnemyPrefix = "TDR-"
        });

        _factionRepo.Insert(new Faction
        {
            Name = "Outer Reach Collective",
            ShortName = "ORC",
            Description = "A loose confederation of frontier settlements and pirate bands favoring fast strikes and heavy ordnance.",
            Color = "#FF6633",
            WeaponPreference = "Ballistic",
            ChassisPreference = "Light",
            EnemyPrefix = "ORC-"
        });
    }

    public void SeedFactionStandings()
    {
        _standingRepo.Initialize(1, 0); // Crucible - Neutral
        _standingRepo.Initialize(2, 0); // Directorate - Neutral
        _standingRepo.Initialize(3, 0); // Outer Reach - Neutral
    }

    public void SeedPilots()
    {
        var pilots = new List<Pilot>
        {
            new Pilot
            {
                Callsign = "Viper",
                GunnerySkill = 4,
                PilotingSkill = 4,
                TacticsSkill = 3,
                ExperiencePoints = 200,
                MissionsCompleted = 8,
                Kills = 5,
                Status = "Active",
                InjuryDays = 0,
                Morale = 90
            },
            new Pilot
            {
                Callsign = "Hawk",
                GunnerySkill = 5,
                PilotingSkill = 3,
                TacticsSkill = 3,
                ExperiencePoints = 150,
                MissionsCompleted = 6,
                Kills = 7,
                Status = "Active",
                InjuryDays = 0,
                Morale = 85
            },
            new Pilot
            {
                Callsign = "Steel",
                GunnerySkill = 3,
                PilotingSkill = 5,
                TacticsSkill = 4,
                ExperiencePoints = 180,
                MissionsCompleted = 7,
                Kills = 3,
                Status = "Active",
                InjuryDays = 0,
                Morale = 95
            },
            new Pilot
            {
                Callsign = "Rookie",
                GunnerySkill = 2,
                PilotingSkill = 2,
                TacticsSkill = 1,
                ExperiencePoints = 0,
                MissionsCompleted = 0,
                Kills = 0,
                Status = "Active",
                InjuryDays = 0,
                Morale = 80
            }
        };

        foreach (var pilot in pilots)
        {
            _pilotRepo.Insert(pilot);
        }
    }

    public void SeedStartingFrames()
    {
        // Starting frame 1: Enforcer (Medium, ChassisId=5)
        // EN-50: 2S 2M 2L, Reactor=17, MaxArmor=110
        var enforcerId = _frameRepo.Insert(new FrameInstance
        {
            ChassisId = 5, // Enforcer
            CustomName = "Alpha",
            ArmorHead = 8,
            ArmorCenterTorso = 22,
            ArmorLeftTorso = 16,
            ArmorRightTorso = 16,
            ArmorLeftArm = 12,
            ArmorRightArm = 12,
            ArmorLegs = 24,
            ReactorStress = 0,
            Status = "Ready",
            RepairCost = 0,
            RepairTime = 0,
            AcquisitionDate = DateTime.Now,
            PilotId = 1 // Viper
        });

        // Enforcer loadout:
        // Group 1: Medium Laser (Id=5) left arm + Autocannon-5 (Id=6) right arm
        // Group 2: Heavy Laser (Id=9) left torso
        // Group 1: Light Laser (Id=1) center torso
        _loadoutRepo.Insert(new Loadout { InstanceId = enforcerId, HardpointSlot = "medium_1", WeaponId = 5, WeaponGroup = 1, MountLocation = "LeftArm" });
        _loadoutRepo.Insert(new Loadout { InstanceId = enforcerId, HardpointSlot = "medium_2", WeaponId = 6, WeaponGroup = 1, MountLocation = "RightArm" });
        _loadoutRepo.Insert(new Loadout { InstanceId = enforcerId, HardpointSlot = "large_1", WeaponId = 9, WeaponGroup = 2, MountLocation = "LeftTorso" });
        _loadoutRepo.Insert(new Loadout { InstanceId = enforcerId, HardpointSlot = "small_1", WeaponId = 1, WeaponGroup = 1, MountLocation = "CenterTorso" });

        // Starting frame 2: Raider (Light, ChassisId=2)
        // RD-30: 3S 3M 0L, Reactor=11, MaxArmor=70
        var raiderId = _frameRepo.Insert(new FrameInstance
        {
            ChassisId = 2, // Raider
            CustomName = "Bravo",
            ArmorHead = 6,
            ArmorCenterTorso = 14,
            ArmorLeftTorso = 10,
            ArmorRightTorso = 10,
            ArmorLeftArm = 8,
            ArmorRightArm = 8,
            ArmorLegs = 14,
            ReactorStress = 0,
            Status = "Ready",
            RepairCost = 0,
            RepairTime = 0,
            AcquisitionDate = DateTime.Now,
            PilotId = 2 // Hawk
        });

        // Raider loadout:
        // Group 1: Medium Laser (Id=5) left arm + Light Laser (Id=1) right arm
        // Group 2: Autocannon-5 (Id=6) center torso
        _loadoutRepo.Insert(new Loadout { InstanceId = raiderId, HardpointSlot = "medium_1", WeaponId = 5, WeaponGroup = 1, MountLocation = "LeftArm" });
        _loadoutRepo.Insert(new Loadout { InstanceId = raiderId, HardpointSlot = "small_1", WeaponId = 1, WeaponGroup = 1, MountLocation = "RightArm" });
        _loadoutRepo.Insert(new Loadout { InstanceId = raiderId, HardpointSlot = "medium_2", WeaponId = 6, WeaponGroup = 2, MountLocation = "CenterTorso" });
    }

    public void SeedPlayerState(string companyName = "Iron Wolves")
    {
        _stateRepo.Initialize(new PlayerState
        {
            Credits = 500000,
            Reputation = 0,
            MissionsCompleted = 0,
            MissionsWon = 0,
            CompanyName = companyName,
            CurrentDay = 1,
            CurrentSystemId = 10, // Crossroads
            CurrentPlanetId = 21, // Junction Station
            Fuel = 50
        });
    }

    public void SeedWeapons()
    {
        var weapons = new List<Weapon>
        {
            // === Small Hardpoint Weapons ===

            // Universal
            new Weapon
            {
                Name = "Light Laser",
                HardpointSize = "Small",
                WeaponType = "Energy",
                EnergyCost = 4,
                AmmoPerShot = 0,
                SpaceCost = 2,
                Damage = 5,
                RangeClass = "Medium",
                BaseAccuracy = 85,
                SalvageValue = 5000,
                PurchaseCost = 10000,
                FactionId = null
            },
            // Universal
            new Weapon
            {
                Name = "Machine Gun",
                HardpointSize = "Small",
                WeaponType = "Ballistic",
                EnergyCost = 0,
                AmmoPerShot = 10,
                SpaceCost = 2,
                Damage = 3,
                RangeClass = "Short",
                BaseAccuracy = 90,
                SalvageValue = 3000,
                PurchaseCost = 6000,
                FactionId = null
            },
            // Crucible Industries
            new Weapon
            {
                Name = "Flamer",
                HardpointSize = "Small",
                WeaponType = "Energy",
                EnergyCost = 6,
                AmmoPerShot = 0,
                SpaceCost = 2,
                Damage = 2,
                RangeClass = "Short",
                BaseAccuracy = 95,
                SalvageValue = 4000,
                PurchaseCost = 8000,
                SpecialEffect = "Increases enemy reactor stress",
                FactionId = 1
            },
            // Outer Reach Collective
            new Weapon
            {
                Name = "Small Missile Rack",
                HardpointSize = "Small",
                WeaponType = "Missile",
                EnergyCost = 1,
                AmmoPerShot = 8,
                SpaceCost = 3,
                Damage = 6,
                RangeClass = "Short",
                BaseAccuracy = 80,
                SalvageValue = 6000,
                PurchaseCost = 12000,
                FactionId = 3
            },

            // === Medium Hardpoint Weapons ===

            // Universal
            new Weapon
            {
                Name = "Medium Laser",
                HardpointSize = "Medium",
                WeaponType = "Energy",
                EnergyCost = 8,
                AmmoPerShot = 0,
                SpaceCost = 5,
                Damage = 10,
                RangeClass = "Medium",
                BaseAccuracy = 80,
                SalvageValue = 10000,
                PurchaseCost = 20000,
                FactionId = null
            },
            // Universal
            new Weapon
            {
                Name = "Autocannon-5",
                HardpointSize = "Medium",
                WeaponType = "Ballistic",
                EnergyCost = 1,
                AmmoPerShot = 5,
                SpaceCost = 6,
                Damage = 8,
                RangeClass = "Long",
                BaseAccuracy = 80,
                SalvageValue = 12000,
                PurchaseCost = 24000,
                FactionId = null
            },
            // Outer Reach Collective
            new Weapon
            {
                Name = "Missile Pod (SRM-6)",
                HardpointSize = "Medium",
                WeaponType = "Missile",
                EnergyCost = 2,
                AmmoPerShot = 10,
                SpaceCost = 6,
                Damage = 12,
                RangeClass = "Short",
                BaseAccuracy = 75,
                SalvageValue = 15000,
                PurchaseCost = 30000,
                FactionId = 3
            },
            // Terran Directorate
            new Weapon
            {
                Name = "Gauss Rifle (Light)",
                HardpointSize = "Medium",
                WeaponType = "Ballistic",
                EnergyCost = 2,
                AmmoPerShot = 8,
                SpaceCost = 7,
                Damage = 15,
                RangeClass = "Long",
                BaseAccuracy = 85,
                SalvageValue = 25000,
                PurchaseCost = 50000,
                FactionId = 2
            },

            // === Large Hardpoint Weapons ===

            // Crucible Industries
            new Weapon
            {
                Name = "Heavy Laser",
                HardpointSize = "Large",
                WeaponType = "Energy",
                EnergyCost = 14,
                AmmoPerShot = 0,
                SpaceCost = 10,
                Damage = 20,
                RangeClass = "Long",
                BaseAccuracy = 75,
                SalvageValue = 25000,
                PurchaseCost = 50000,
                FactionId = 1
            },
            // Outer Reach Collective
            new Weapon
            {
                Name = "Heavy Autocannon-10",
                HardpointSize = "Large",
                WeaponType = "Ballistic",
                EnergyCost = 1,
                AmmoPerShot = 8,
                SpaceCost = 12,
                Damage = 20,
                RangeClass = "Long",
                BaseAccuracy = 70,
                SalvageValue = 30000,
                PurchaseCost = 60000,
                FactionId = 3
            },
            // Crucible Industries
            new Weapon
            {
                Name = "Plasma Lance",
                HardpointSize = "Large",
                WeaponType = "Energy",
                EnergyCost = 18,
                AmmoPerShot = 0,
                SpaceCost = 11,
                Damage = 25,
                RangeClass = "Medium",
                BaseAccuracy = 65,
                SalvageValue = 40000,
                PurchaseCost = 80000,
                FactionId = 1
            },
            // Terran Directorate
            new Weapon
            {
                Name = "LRM-15 (Long Range Missiles)",
                HardpointSize = "Large",
                WeaponType = "Missile",
                EnergyCost = 2,
                AmmoPerShot = 12,
                SpaceCost = 12,
                Damage = 15,
                RangeClass = "Long",
                BaseAccuracy = 70,
                SalvageValue = 35000,
                PurchaseCost = 70000,
                SpecialEffect = "Indirect fire capable",
                FactionId = 2
            },
            // Terran Directorate
            new Weapon
            {
                Name = "Gauss Cannon (Heavy)",
                HardpointSize = "Large",
                WeaponType = "Ballistic",
                EnergyCost = 2,
                AmmoPerShot = 6,
                SpaceCost = 14,
                Damage = 30,
                RangeClass = "Long",
                BaseAccuracy = 80,
                SalvageValue = 50000,
                PurchaseCost = 100000,
                FactionId = 2
            },

            // === Faction Exclusive Weapons (require Allied standing 200+) ===

            // Crucible exclusive
            new Weapon
            {
                Name = "Fusion Lance",
                HardpointSize = "Large",
                WeaponType = "Energy",
                EnergyCost = 22,
                AmmoPerShot = 0,
                SpaceCost = 13,
                Damage = 35,
                RangeClass = "Medium",
                BaseAccuracy = 60,
                SalvageValue = 60000,
                PurchaseCost = 120000,
                SpecialEffect = "Crucible exclusive",
                FactionId = 1
            },
            // Directorate exclusive
            new Weapon
            {
                Name = "Precision Gauss",
                HardpointSize = "Large",
                WeaponType = "Ballistic",
                EnergyCost = 3,
                AmmoPerShot = 4,
                SpaceCost = 15,
                Damage = 28,
                RangeClass = "Long",
                BaseAccuracy = 90,
                SalvageValue = 55000,
                PurchaseCost = 110000,
                SpecialEffect = "Directorate exclusive",
                FactionId = 2
            },
            // Outer Reach exclusive
            new Weapon
            {
                Name = "Swarm Launcher",
                HardpointSize = "Large",
                WeaponType = "Missile",
                EnergyCost = 3,
                AmmoPerShot = 15,
                SpaceCost = 14,
                Damage = 22,
                RangeClass = "Medium",
                BaseAccuracy = 75,
                SalvageValue = 45000,
                PurchaseCost = 90000,
                SpecialEffect = "Outer Reach exclusive",
                FactionId = 3
            }
        };

        foreach (var weapon in weapons)
        {
            _weaponRepo.Insert(weapon);
        }
    }

    public void SeedEquipment()
    {
        var equipment = new List<Equipment>
        {
            // === Passive Equipment (no hardpoint, always-on) ===

            new Equipment
            {
                Name = "Cooling Vents",
                Category = "Passive",
                SpaceCost = 4,
                EnergyCost = 0,
                Effect = "ReactorBoost",
                EffectValue = 3,
                PurchaseCost = 25000,
                SalvageValue = 12500,
                Description = "+3 reactor output per round"
            },
            new Equipment
            {
                Name = "Reactive Armor",
                Category = "Passive",
                SpaceCost = 6,
                EnergyCost = 0,
                Effect = "DamageReduction",
                EffectValue = 15,
                PurchaseCost = 35000,
                SalvageValue = 17500,
                Description = "-15% structure damage taken"
            },
            new Equipment
            {
                Name = "Ammo Bin",
                Category = "Passive",
                SpaceCost = 3,
                EnergyCost = 0,
                Effect = "AmmoBonus",
                EffectValue = 4,
                PurchaseCost = 15000,
                SalvageValue = 7500,
                Description = "+4 reloads for all ballistic/missile weapons"
            },
            new Equipment
            {
                Name = "Gyro Stabilizer",
                Category = "Passive",
                SpaceCost = 5,
                EnergyCost = 0,
                Effect = "EvasionReduction",
                EffectValue = 10,
                PurchaseCost = 30000,
                SalvageValue = 15000,
                Description = "-10% evasion penalty on your attacks"
            },

            // === Active Equipment (no hardpoint, costs AP + energy to activate) ===

            new Equipment
            {
                Name = "Thrust Pack",
                Category = "Active",
                SpaceCost = 5,
                EnergyCost = 4,
                Effect = "Jump",
                EffectValue = 3,
                PurchaseCost = 40000,
                SalvageValue = 20000,
                Description = "Jump 3 hexes ignoring terrain (1 AP)"
            },
            new Equipment
            {
                Name = "Countermeasure Suite",
                Category = "Active",
                SpaceCost = 4,
                EnergyCost = 3,
                Effect = "ECM",
                EffectValue = 20,
                PurchaseCost = 45000,
                SalvageValue = 22500,
                Description = "-20% enemy accuracy vs this frame (1 AP, 1 round)"
            },
            new Equipment
            {
                Name = "Targeting Uplink",
                Category = "Active",
                SpaceCost = 3,
                EnergyCost = 5,
                Effect = "TargetUplink",
                EffectValue = 15,
                PurchaseCost = 50000,
                SalvageValue = 25000,
                Description = "+15% accuracy for allies within 4 hexes (1 AP)"
            },
            new Equipment
            {
                Name = "Barrier Projector",
                Category = "Active",
                SpaceCost = 6,
                EnergyCost = 6,
                Effect = "Barrier",
                EffectValue = 20,
                PurchaseCost = 55000,
                SalvageValue = 27500,
                Description = "+20 temp armor to adjacent ally (1 AP)"
            },

            // === Slot Equipment (uses a hardpoint slot, always-on) ===

            new Equipment
            {
                Name = "Sensor Array",
                Category = "Slot",
                HardpointSize = "Small",
                SpaceCost = 2,
                EnergyCost = 0,
                Effect = "LongRangeBonus",
                EffectValue = 10,
                PurchaseCost = 20000,
                SalvageValue = 10000,
                Description = "+10% accuracy at Long range"
            },
            new Equipment
            {
                Name = "Point Defense System",
                Category = "Slot",
                HardpointSize = "Small",
                SpaceCost = 3,
                EnergyCost = 2,
                Effect = "MissileDefense",
                EffectValue = 50,
                PurchaseCost = 35000,
                SalvageValue = 17500,
                Description = "50% chance to negate incoming missile hit"
            },
            new Equipment
            {
                Name = "Phantom Emitter",
                Category = "Slot",
                HardpointSize = "Medium",
                SpaceCost = 5,
                EnergyCost = 4,
                Effect = "RangedECM",
                EffectValue = 25,
                PurchaseCost = 60000,
                SalvageValue = 30000,
                Description = "-25% enemy accuracy beyond 5 hexes"
            },
            new Equipment
            {
                Name = "Stealth Plating",
                Category = "Slot",
                HardpointSize = "Large",
                SpaceCost = 10,
                EnergyCost = 0,
                Effect = "StealthPlating",
                EffectValue = 20,
                PurchaseCost = 75000,
                SalvageValue = 37500,
                Description = "+20% defense bonus, -1 movement"
            }
        };

        foreach (var eq in equipment)
        {
            _equipmentRepo.Insert(eq);
        }
    }

    public void SeedChassis()
    {
        var chassisList = new List<Chassis>
        {
            // === Light Class (20-35 tons) ===

            // Universal
            new Chassis
            {
                Designation = "SC-20",
                Name = "Scout",
                Class = "Light",
                HardpointSmall = 4,
                HardpointMedium = 2,
                HardpointLarge = 0,
                ReactorOutput = 10,
                MovementEnergyCost = 2,
                TotalSpace = 35,
                MaxArmorTotal = 60,
                StructureHead = 2,
                StructureCenterTorso = 6,
                StructureSideTorso = 4,
                StructureArm = 3,
                StructureLegs = 5,
                BaseSpeed = 9,
                BaseEvasion = 25,
                FactionId = null
            },
            // Terran Directorate
            new Chassis
            {
                Designation = "RD-30",
                Name = "Raider",
                Class = "Light",
                HardpointSmall = 3,
                HardpointMedium = 3,
                HardpointLarge = 0,
                ReactorOutput = 11,
                MovementEnergyCost = 2,
                TotalSpace = 40,
                MaxArmorTotal = 70,
                StructureHead = 2,
                StructureCenterTorso = 7,
                StructureSideTorso = 5,
                StructureArm = 3,
                StructureLegs = 6,
                BaseSpeed = 8,
                BaseEvasion = 22,
                FactionId = 2
            },
            // Outer Reach Collective
            new Chassis
            {
                Designation = "HR-35",
                Name = "Harrier",
                Class = "Light",
                HardpointSmall = 2,
                HardpointMedium = 3,
                HardpointLarge = 1,
                ReactorOutput = 12,
                MovementEnergyCost = 3,
                TotalSpace = 45,
                MaxArmorTotal = 75,
                StructureHead = 3,
                StructureCenterTorso = 8,
                StructureSideTorso = 5,
                StructureArm = 4,
                StructureLegs = 6,
                BaseSpeed = 7,
                BaseEvasion = 20,
                FactionId = 3
            },

            // === Medium Class (40-55 tons) ===

            // Universal
            new Chassis
            {
                Designation = "VG-45",
                Name = "Vanguard",
                Class = "Medium",
                HardpointSmall = 3,
                HardpointMedium = 3,
                HardpointLarge = 1,
                ReactorOutput = 15,
                MovementEnergyCost = 5,
                TotalSpace = 55,
                MaxArmorTotal = 100,
                StructureHead = 3,
                StructureCenterTorso = 10,
                StructureSideTorso = 7,
                StructureArm = 5,
                StructureLegs = 8,
                BaseSpeed = 6,
                BaseEvasion = 15,
                FactionId = null
            },
            // Universal
            new Chassis
            {
                Designation = "EN-50",
                Name = "Enforcer",
                Class = "Medium",
                HardpointSmall = 2,
                HardpointMedium = 2,
                HardpointLarge = 2,
                ReactorOutput = 17,
                MovementEnergyCost = 5,
                TotalSpace = 60,
                MaxArmorTotal = 110,
                StructureHead = 4,
                StructureCenterTorso = 11,
                StructureSideTorso = 7,
                StructureArm = 5,
                StructureLegs = 9,
                BaseSpeed = 5,
                BaseEvasion = 13,
                FactionId = null
            },
            // Terran Directorate
            new Chassis
            {
                Designation = "RG-55",
                Name = "Ranger",
                Class = "Medium",
                HardpointSmall = 4,
                HardpointMedium = 4,
                HardpointLarge = 0,
                ReactorOutput = 16,
                MovementEnergyCost = 4,
                TotalSpace = 55,
                MaxArmorTotal = 95,
                StructureHead = 3,
                StructureCenterTorso = 10,
                StructureSideTorso = 6,
                StructureArm = 5,
                StructureLegs = 8,
                BaseSpeed = 6,
                BaseEvasion = 16,
                FactionId = 2
            },

            // === Heavy Class (60-75 tons) ===

            // Terran Directorate
            new Chassis
            {
                Designation = "WD-60",
                Name = "Warden",
                Class = "Heavy",
                HardpointSmall = 2,
                HardpointMedium = 3,
                HardpointLarge = 2,
                ReactorOutput = 22,
                MovementEnergyCost = 7,
                TotalSpace = 75,
                MaxArmorTotal = 140,
                StructureHead = 4,
                StructureCenterTorso = 14,
                StructureSideTorso = 10,
                StructureArm = 7,
                StructureLegs = 11,
                BaseSpeed = 4,
                BaseEvasion = 11,
                FactionId = 2
            },
            // Outer Reach Collective
            new Chassis
            {
                Designation = "BR-70",
                Name = "Bruiser",
                Class = "Heavy",
                HardpointSmall = 1,
                HardpointMedium = 3,
                HardpointLarge = 3,
                ReactorOutput = 24,
                MovementEnergyCost = 8,
                TotalSpace = 80,
                MaxArmorTotal = 150,
                StructureHead = 5,
                StructureCenterTorso = 15,
                StructureSideTorso = 10,
                StructureArm = 7,
                StructureLegs = 12,
                BaseSpeed = 4,
                BaseEvasion = 10,
                FactionId = 3
            },
            // Crucible Industries
            new Chassis
            {
                Designation = "SN-75",
                Name = "Sentinel",
                Class = "Heavy",
                HardpointSmall = 3,
                HardpointMedium = 4,
                HardpointLarge = 2,
                ReactorOutput = 23,
                MovementEnergyCost = 7,
                TotalSpace = 78,
                MaxArmorTotal = 145,
                StructureHead = 5,
                StructureCenterTorso = 14,
                StructureSideTorso = 10,
                StructureArm = 7,
                StructureLegs = 12,
                BaseSpeed = 4,
                BaseEvasion = 12,
                FactionId = 1
            },

            // === Assault Class (80-100 tons) ===

            // Crucible Industries
            new Chassis
            {
                Designation = "TN-85",
                Name = "Titan",
                Class = "Assault",
                HardpointSmall = 2,
                HardpointMedium = 4,
                HardpointLarge = 3,
                ReactorOutput = 26,
                MovementEnergyCost = 10,
                TotalSpace = 90,
                MaxArmorTotal = 180,
                StructureHead = 5,
                StructureCenterTorso = 18,
                StructureSideTorso = 12,
                StructureArm = 8,
                StructureLegs = 14,
                BaseSpeed = 3,
                BaseEvasion = 8,
                FactionId = 1
            },
            // Crucible Industries
            new Chassis
            {
                Designation = "JG-95",
                Name = "Juggernaut",
                Class = "Assault",
                HardpointSmall = 1,
                HardpointMedium = 3,
                HardpointLarge = 4,
                ReactorOutput = 28,
                MovementEnergyCost = 11,
                TotalSpace = 100,
                MaxArmorTotal = 200,
                StructureHead = 6,
                StructureCenterTorso = 20,
                StructureSideTorso = 14,
                StructureArm = 9,
                StructureLegs = 16,
                BaseSpeed = 2,
                BaseEvasion = 6,
                FactionId = 1
            },
            // Outer Reach Collective
            new Chassis
            {
                Designation = "CL-100",
                Name = "Colossus",
                Class = "Assault",
                HardpointSmall = 2,
                HardpointMedium = 5,
                HardpointLarge = 4,
                ReactorOutput = 30,
                MovementEnergyCost = 12,
                TotalSpace = 110,
                MaxArmorTotal = 220,
                StructureHead = 6,
                StructureCenterTorso = 22,
                StructureSideTorso = 15,
                StructureArm = 10,
                StructureLegs = 17,
                BaseSpeed = 2,
                BaseEvasion = 5,
                FactionId = 3
            }
        };

        foreach (var chassis in chassisList)
        {
            _chassisRepo.Insert(chassis);
        }
    }

    public void SeedStarSystems()
    {
        // Faction IDs: 1=Crucible, 2=Directorate, 3=Outer Reach
        // X/Y coordinates for future graphical star map rendering
        var systems = new List<StarSystem>
        {
            // === Terran Directorate (Core) ===
            new StarSystem { Name = "Sol", X = 300, Y = 50, ControllingFactionId = 2, SystemType = "Core",
                Description = "Humanity's birthplace. Seat of the Terran Directorate and High Command." },
            new StarSystem { Name = "Terra Nova", X = 500, Y = 50, ControllingFactionId = 2, SystemType = "Core",
                Description = "Core colony world with major military shipyards and training academies." },
            new StarSystem { Name = "Centauri Gate", X = 500, Y = 200, ControllingFactionId = 2, SystemType = "Colony",
                Description = "Border garrison system. Primary gate hub connecting core space to the frontier." },

            // === Crucible Industries (Mid-rim) ===
            new StarSystem { Name = "Avalon", X = 200, Y = 400, ControllingFactionId = 1, SystemType = "Colony",
                Description = "Crucible Industries capital. Home to Foundry Station and vast mineral wealth." },
            new StarSystem { Name = "Forge", X = 50, Y = 350, ControllingFactionId = 1, SystemType = "Colony",
                Description = "Heavy industrial manufacturing hub. Crucible's primary weapons production." },
            new StarSystem { Name = "Meridian", X = 150, Y = 250, ControllingFactionId = 1, SystemType = "Colony",
                Description = "Corporate R&D center. Backdoor jump route connects to Sol." },

            // === Outer Reach Collective (Fringe) ===
            new StarSystem { Name = "Haven", X = 550, Y = 400, ControllingFactionId = 3, SystemType = "Frontier",
                Description = "Collective diplomatic hub. The most orderly of the frontier systems." },
            new StarSystem { Name = "The Drift", X = 600, Y = 550, ControllingFactionId = 3, SystemType = "Frontier",
                Description = "Home of the mobile capital station. Nomadic fleet anchorage." },
            new StarSystem { Name = "Rimward", X = 550, Y = 650, ControllingFactionId = 3, SystemType = "Frontier",
                Description = "Deep fringe. Rich salvage fields and mineral deposits. Lawless." },

            // === Contested / Independent ===
            new StarSystem { Name = "Crossroads", X = 350, Y = 300, ControllingFactionId = null, SystemType = "Contested",
                Description = "Border nexus where all faction territories meet. Mercenary haven." },
            new StarSystem { Name = "Deadlight", X = 450, Y = 500, ControllingFactionId = null, SystemType = "Contested",
                Description = "Pirate haven and black market hub. No law, no questions." }
        };

        foreach (var system in systems)
        {
            _systemRepo.Insert(system);
        }
    }

    public void SeedPlanets()
    {
        var planets = new List<Planet>
        {
            // === Sol (SystemId=1) ===
            new Planet { SystemId = 1, Name = "Earth", PlanetType = "Habitable",
                Description = "Cradle of humanity. Directorate administrative capital.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },
            new Planet { SystemId = 1, Name = "Luna Station", PlanetType = "Station",
                Description = "Military headquarters orbiting Earth. High Command operations center.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 4, ContractDifficultyMax = 5 },
            new Planet { SystemId = 1, Name = "Mars Colony", PlanetType = "Industrial",
                Description = "Terraform-in-progress industrial complex. Frame production facilities.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },

            // === Terra Nova (SystemId=2) ===
            new Planet { SystemId = 2, Name = "New Geneva", PlanetType = "Habitable",
                Description = "Major colony world. Directorate officer training academies.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },
            new Planet { SystemId = 2, Name = "Dryden Yards", PlanetType = "Station",
                Description = "Military shipyards. Heavy frame construction and overhaul.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },

            // === Centauri Gate (SystemId=3) ===
            new Planet { SystemId = 3, Name = "Gate Station", PlanetType = "Station",
                Description = "Primary jump gate hub. Border patrol staging area.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },
            new Planet { SystemId = 3, Name = "Proxima Colony", PlanetType = "Mining",
                Description = "Mining settlement. Supplies raw materials to the core.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 1, ContractDifficultyMax = 3 },

            // === Avalon (SystemId=4) ===
            new Planet { SystemId = 4, Name = "Foundry Station", PlanetType = "Station",
                Description = "Crucible Industries headquarters. Corporate capital of the mid-rim.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },
            new Planet { SystemId = 4, Name = "Avalon Prime", PlanetType = "Habitable",
                Description = "Resource-rich world. Crucible's showcase colony.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },
            new Planet { SystemId = 4, Name = "Ore Belt", PlanetType = "Mining",
                Description = "Asteroid mining operation. Primary rare metal source.",
                HasMarket = false, HasHiring = false, ContractDifficultyMin = 1, ContractDifficultyMax = 3 },

            // === Forge (SystemId=5) ===
            new Planet { SystemId = 5, Name = "Smelter One", PlanetType = "Industrial",
                Description = "Massive foundry complex. Weapons and armor manufacturing.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },
            new Planet { SystemId = 5, Name = "Forge Station", PlanetType = "Station",
                Description = "Orbital depot. Frame assembly and testing facilities.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },

            // === Meridian (SystemId=6) ===
            new Planet { SystemId = 6, Name = "Meridian Labs", PlanetType = "Station",
                Description = "Crucible R&D station. Prototype weapons and experimental tech.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },
            new Planet { SystemId = 6, Name = "Aether Colony", PlanetType = "Habitable",
                Description = "Corporate residential colony. Crucible executive retreats.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 1, ContractDifficultyMax = 3 },

            // === Haven (SystemId=7) ===
            new Planet { SystemId = 7, Name = "Haven Prime", PlanetType = "Habitable",
                Description = "Collective's diplomatic capital. Most stable frontier world.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },
            new Planet { SystemId = 7, Name = "Port Yarrow", PlanetType = "Station",
                Description = "Trade station. Crossroads of frontier commerce.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 1, ContractDifficultyMax = 3 },

            // === The Drift (SystemId=8) ===
            new Planet { SystemId = 8, Name = "The Drift", PlanetType = "Station",
                Description = "Mobile capital station. Converted colony ship housing the Collective council.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },
            new Planet { SystemId = 8, Name = "Scatter Point", PlanetType = "Outpost",
                Description = "Salvage depot and nomad anchorage. Fleet repair facilities.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },

            // === Rimward (SystemId=9) ===
            new Planet { SystemId = 9, Name = "Rimward Station", PlanetType = "Outpost",
                Description = "Deep fringe outpost. Last stop before uncharted space.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 3, ContractDifficultyMax = 5 },
            new Planet { SystemId = 9, Name = "Junkyard", PlanetType = "Mining",
                Description = "Salvage fields. Ship and frame wreckage from forgotten battles.",
                HasMarket = false, HasHiring = false, ContractDifficultyMin = 2, ContractDifficultyMax = 4 },

            // === Crossroads (SystemId=10) ===
            new Planet { SystemId = 10, Name = "Junction Station", PlanetType = "Station",
                Description = "Trade hub where faction territories meet. Neutral ground for mercenaries.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 1, ContractDifficultyMax = 3 },
            new Planet { SystemId = 10, Name = "Freeport", PlanetType = "Station",
                Description = "Independent mercenary outpost. Contract boards and repair bays.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 1, ContractDifficultyMax = 4 },

            // === Deadlight (SystemId=11) ===
            new Planet { SystemId = 11, Name = "Deadlight Station", PlanetType = "Station",
                Description = "Pirate den. No questions asked, no allegiances required.",
                HasMarket = true, HasHiring = true, ContractDifficultyMin = 2, ContractDifficultyMax = 5 },
            new Planet { SystemId = 11, Name = "Shadow Market", PlanetType = "Outpost",
                Description = "Black market depot. Stolen goods and forbidden tech at a premium.",
                HasMarket = true, HasHiring = false, ContractDifficultyMin = 3, ContractDifficultyMax = 5 }
        };

        foreach (var planet in planets)
        {
            _planetRepo.Insert(planet);
        }
    }

    public void SeedJumpRoutes()
    {
        // All routes are bidirectional — store both directions for easy querying
        var routes = new (int from, int to, int distance, int days)[]
        {
            // Directorate internal
            (1, 2, 10, 2),    // Sol ↔ Terra Nova
            (1, 3, 15, 3),    // Sol ↔ Centauri Gate
            (2, 3, 10, 2),    // Terra Nova ↔ Centauri Gate

            // Core ↔ Crucible backdoor
            (1, 6, 20, 3),    // Sol ↔ Meridian

            // Border crossings
            (3, 10, 15, 3),   // Centauri Gate ↔ Crossroads

            // Crossroads connections
            (10, 4, 15, 3),   // Crossroads ↔ Avalon
            (10, 7, 15, 3),   // Crossroads ↔ Haven
            (10, 11, 10, 2),  // Crossroads ↔ Deadlight

            // Crucible internal
            (4, 5, 10, 2),    // Avalon ↔ Forge
            (4, 6, 10, 2),    // Avalon ↔ Meridian
            (5, 6, 15, 3),    // Forge ↔ Meridian

            // Outer Reach internal
            (7, 8, 10, 2),    // Haven ↔ The Drift
            (7, 9, 15, 3),    // Haven ↔ Rimward
            (8, 9, 10, 2),    // The Drift ↔ Rimward

            // Pirate routes
            (11, 9, 15, 3),   // Deadlight ↔ Rimward
            (11, 7, 20, 4),   // Deadlight ↔ Haven
        };

        foreach (var (from, to, distance, days) in routes)
        {
            _jumpRouteRepo.Insert(new JumpRoute
            {
                FromSystemId = from,
                ToSystemId = to,
                Distance = distance,
                TravelDays = days
            });
        }
    }
}
