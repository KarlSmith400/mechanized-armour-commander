using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.Core.Services;

/// <summary>
/// Generates missions, builds enemy forces, and processes combat results
/// </summary>
public class MissionService
{
    private readonly DatabaseContext _dbContext;
    private readonly ChassisRepository _chassisRepo;
    private readonly WeaponRepository _weaponRepo;
    private readonly FactionRepository _factionRepo;
    private readonly FactionStandingRepository _standingRepo;
    private readonly StarSystemRepository _systemRepo;
    private readonly PlanetRepository _planetRepo;
    private readonly Random _random = new();

    private static readonly Dictionary<int, string[]> FactionTitles = new()
    {
        { 1, new[] { "Asset Protection", "Facility Defense", "Corporate Extraction",
                      "Industrial Sabotage Response", "R&D Escort", "Hostile Acquisition", "Perimeter Enforcement" } },
        { 2, new[] { "Border Patrol", "Garrison Relief", "Peacekeeping Operation",
                      "Sector Enforcement", "Forward Reconnaissance", "Strategic Interdiction", "Defensive Stand" } },
        { 3, new[] { "Frontier Skirmish", "Convoy Raid", "Supply Interdiction",
                      "Outpost Defense", "Smuggler Escort", "Territory Claim", "Pirate Suppression" } }
    };

    private static readonly Dictionary<int, string[]> FactionDescriptions = new()
    {
        { 1, new[] { "Crucible Industries requires armed escorts for a high-value asset transfer.",
                      "Corporate security forces have been overwhelmed. Mercenary support authorized.",
                      "An industrial complex is under threat. Defend until reinforcements arrive.",
                      "Rival corporation forces have been detected. Neutralize and secure the area." } },
        { 2, new[] { "Directorate command has authorized a military operation in this sector.",
                      "Hostile forces threaten a Directorate garrison. Reinforcement requested.",
                      "Intelligence reports enemy activity along the border. Investigate and engage.",
                      "A strategic position must be held against incoming assault forces." } },
        { 3, new[] { "Outer Reach settlements need protection from hostile incursions.",
                      "A Collective supply convoy requires armed escort through contested space.",
                      "Frontier raiders are threatening Collective territory. Push them back.",
                      "The Collective needs experienced pilots for a high-risk raid operation." } }
    };

    public MissionService(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        _chassisRepo = new ChassisRepository(dbContext);
        _weaponRepo = new WeaponRepository(dbContext);
        _factionRepo = new FactionRepository(dbContext);
        _standingRepo = new FactionStandingRepository(dbContext);
        _systemRepo = new StarSystemRepository(dbContext);
        _planetRepo = new PlanetRepository(dbContext);
    }

    /// <summary>
    /// Generates a set of mission contracts scaled to player reputation and current location
    /// </summary>
    public List<Mission> GenerateContracts(int count, int reputation, List<FactionStanding> standings,
        int? currentSystemId = null, int? currentPlanetId = null)
    {
        var factions = _factionRepo.GetAll();
        var missions = new List<Mission>();
        int baseDifficulty = Math.Clamp(1 + reputation / 5, 1, 4);

        // Get location context for faction biasing
        StarSystem? currentSystem = currentSystemId.HasValue ? _systemRepo.GetById(currentSystemId.Value) : null;
        Planet? currentPlanet = currentPlanetId.HasValue ? _planetRepo.GetById(currentPlanetId.Value) : null;

        // Clamp difficulty to planet's allowed range
        int diffMin = currentPlanet?.ContractDifficultyMin ?? 1;
        int diffMax = currentPlanet?.ContractDifficultyMax ?? 5;

        // Filter out hostile factions as employers
        var availableFactions = factions.Where(f =>
        {
            var standing = standings.FirstOrDefault(s => s.FactionId == f.FactionId);
            return standing == null || standing.Standing >= -50;
        }).ToList();

        if (!availableFactions.Any())
            availableFactions = factions; // Fallback: always have missions

        for (int i = 0; i < count; i++)
        {
            Faction employer;

            // In faction-controlled systems, 80% chance employer is the controlling faction
            if (currentSystem?.ControllingFactionId != null
                && availableFactions.Any(f => f.FactionId == currentSystem.ControllingFactionId)
                && _random.Next(100) < 80)
            {
                employer = availableFactions.First(f => f.FactionId == currentSystem.ControllingFactionId);
            }
            else
            {
                employer = availableFactions[_random.Next(availableFactions.Count)];
            }

            // Opponent is a different faction
            var opponents = factions.Where(f => f.FactionId != employer.FactionId).ToList();
            var opponent = opponents[_random.Next(opponents.Count)];

            int difficulty = Math.Clamp(baseDifficulty + _random.Next(-1, 2), diffMin, diffMax);

            // Better standing = better pay
            var employerStanding = standings.FirstOrDefault(s => s.FactionId == employer.FactionId);
            float payMultiplier = employerStanding?.StandingLevel switch
            {
                "Trusted" => 1.30f,
                "Allied" => 1.20f,
                "Friendly" => 1.10f,
                _ => 1.0f
            };

            var mission = GenerateMission(i + 1, difficulty, employer, opponent, payMultiplier);

            // Set landscape from planet type for terrain generation
            mission.Landscape = currentPlanet?.PlanetType ?? "Habitable";

            // Add location info to mission description
            if (currentPlanet != null && currentSystem != null)
            {
                mission.Description += $" Contract posted at {currentPlanet.Name}, {currentSystem.Name} system.";
            }

            missions.Add(mission);
        }

        return missions;
    }

    private Mission GenerateMission(int id, int difficulty, Faction employer, Faction opponent, float payMultiplier)
    {
        var titles = FactionTitles.GetValueOrDefault(employer.FactionId, FactionTitles[2]);
        var descriptions = FactionDescriptions.GetValueOrDefault(employer.FactionId, FactionDescriptions[2]);

        var mission = new Mission
        {
            MissionId = id,
            Title = titles[_random.Next(titles.Length)],
            Description = descriptions[_random.Next(descriptions.Length)],
            Difficulty = difficulty,
            SalvageChance = 15 + difficulty * 5,
            ReputationReward = difficulty,
            EmployerFactionId = employer.FactionId,
            EmployerFactionName = employer.Name,
            EmployerFactionColor = employer.Color,
            OpponentFactionId = opponent.FactionId,
            OpponentFactionName = opponent.Name,
            OpponentFactionColor = opponent.Color,
            OpponentPrefix = opponent.EnemyPrefix,
            MapSize = difficulty switch
            {
                <= 2 => MapSize.Small,
                3 => MapSize.Medium,
                _ => MapSize.Large
            }
        };

        // Scale rewards to difficulty with pay multiplier
        int baseReward = difficulty switch
        {
            1 => 50000 + _random.Next(0, 20000),
            2 => 80000 + _random.Next(0, 30000),
            3 => 120000 + _random.Next(0, 50000),
            4 => 200000 + _random.Next(0, 60000),
            5 => 300000 + _random.Next(0, 80000),
            _ => 100000
        };
        mission.CreditReward = (int)(baseReward * payMultiplier);
        mission.BonusCredits = mission.CreditReward / 4;

        // Build enemy composition based on difficulty
        mission.EnemyComposition = difficulty switch
        {
            1 => new List<EnemySpec>
            {
                new() { ChassisClass = "Light", Count = 2 }
            },
            2 => new List<EnemySpec>
            {
                new() { ChassisClass = "Light", Count = 1 },
                new() { ChassisClass = "Medium", Count = 1 }
            },
            3 => new List<EnemySpec>
            {
                new() { ChassisClass = "Medium", Count = 2 }
            },
            4 => new List<EnemySpec>
            {
                new() { ChassisClass = "Medium", Count = 1 },
                new() { ChassisClass = "Heavy", Count = 1 }
            },
            5 => new List<EnemySpec>
            {
                new() { ChassisClass = "Heavy", Count = 1 },
                new() { ChassisClass = "Assault", Count = 1 }
            },
            _ => new List<EnemySpec>
            {
                new() { ChassisClass = "Medium", Count = 1 }
            }
        };

        return mission;
    }

    /// <summary>
    /// Builds enemy CombatFrames for a mission, biased toward opponent faction
    /// </summary>
    public List<CombatFrame> BuildEnemyForce(Mission mission)
    {
        var enemies = new List<CombatFrame>();
        int enemyId = 101;

        foreach (var spec in mission.EnemyComposition)
        {
            var chassisOptions = GetFactionBiasedChassis(spec.ChassisClass, mission.OpponentFactionId);
            if (!chassisOptions.Any()) continue;

            for (int i = 0; i < spec.Count; i++)
            {
                var chassis = chassisOptions[_random.Next(chassisOptions.Count)];
                var enemy = BuildEnemyCombatFrame(chassis, enemyId, mission.Difficulty, mission.OpponentFactionId);
                enemy.CustomName = $"{mission.OpponentPrefix}{enemyId - 100}";
                enemyId++;
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    private List<Chassis> GetFactionBiasedChassis(string chassisClass, int opponentFactionId)
    {
        var allOfClass = _chassisRepo.GetByClass(chassisClass);

        var factionChassis = allOfClass.Where(c => c.FactionId == opponentFactionId).ToList();
        var universalChassis = allOfClass.Where(c => c.FactionId == null).ToList();

        // 70% chance to use faction chassis if available
        if (factionChassis.Any() && _random.Next(100) < 70)
            return factionChassis;

        var combined = factionChassis.Concat(universalChassis).ToList();
        return combined.Any() ? combined : allOfClass;
    }

    private CombatFrame BuildEnemyCombatFrame(Chassis chassis, int instanceId, int difficulty, int opponentFactionId)
    {
        int maxArmor = chassis.MaxArmorTotal;

        var frame = new CombatFrame
        {
            InstanceId = instanceId,
            CustomName = $"Hostile-{instanceId - 100}",
            ChassisDesignation = chassis.Designation,
            ChassisName = chassis.Name,
            Class = chassis.Class,
            ReactorOutput = chassis.ReactorOutput,
            CurrentEnergy = chassis.ReactorOutput,
            MovementEnergyCost = chassis.MovementEnergyCost,
            Speed = chassis.BaseSpeed,
            Evasion = chassis.BaseEvasion,
            // Enemy pilot skill scales with difficulty
            PilotGunnery = Math.Clamp(2 + difficulty, 2, 6),
            PilotPiloting = Math.Clamp(1 + difficulty, 2, 5),
            PilotTactics = Math.Clamp(1 + difficulty, 1, 5),
            ActionPoints = 2,
            MaxActionPoints = 2,
            Armor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, (int)(maxArmor * 0.07) },
                { HitLocation.CenterTorso, (int)(maxArmor * 0.20) },
                { HitLocation.LeftTorso, (int)(maxArmor * 0.145) },
                { HitLocation.RightTorso, (int)(maxArmor * 0.145) },
                { HitLocation.LeftArm, (int)(maxArmor * 0.11) },
                { HitLocation.RightArm, (int)(maxArmor * 0.11) },
                { HitLocation.Legs, (int)(maxArmor * 0.22) }
            },
            MaxArmor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, (int)(maxArmor * 0.07) },
                { HitLocation.CenterTorso, (int)(maxArmor * 0.20) },
                { HitLocation.LeftTorso, (int)(maxArmor * 0.145) },
                { HitLocation.RightTorso, (int)(maxArmor * 0.145) },
                { HitLocation.LeftArm, (int)(maxArmor * 0.11) },
                { HitLocation.RightArm, (int)(maxArmor * 0.11) },
                { HitLocation.Legs, (int)(maxArmor * 0.22) }
            },
            Structure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, chassis.StructureHead },
                { HitLocation.CenterTorso, chassis.StructureCenterTorso },
                { HitLocation.LeftTorso, chassis.StructureSideTorso },
                { HitLocation.RightTorso, chassis.StructureSideTorso },
                { HitLocation.LeftArm, chassis.StructureArm },
                { HitLocation.RightArm, chassis.StructureArm },
                { HitLocation.Legs, chassis.StructureLegs }
            },
            MaxStructure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, chassis.StructureHead },
                { HitLocation.CenterTorso, chassis.StructureCenterTorso },
                { HitLocation.LeftTorso, chassis.StructureSideTorso },
                { HitLocation.RightTorso, chassis.StructureSideTorso },
                { HitLocation.LeftArm, chassis.StructureArm },
                { HitLocation.RightArm, chassis.StructureArm },
                { HitLocation.Legs, chassis.StructureLegs }
            }
        };

        // Equip weapons biased toward opponent faction
        EquipEnemyWeapons(frame, chassis, opponentFactionId);

        return frame;
    }

    private void EquipEnemyWeapons(CombatFrame frame, Chassis chassis, int opponentFactionId)
    {
        var allWeapons = _weaponRepo.GetByFaction(opponentFactionId);
        // Filter out exclusive weapons from enemy loadouts
        allWeapons = allWeapons.Where(w => w.SpecialEffect == null || !w.SpecialEffect.Contains("exclusive")).ToList();

        var ammoTracker = new Dictionary<string, int>();
        int groupId = 1;

        // Fill large hardpoints first, then medium, then small
        var hardpointSlots = new List<(string size, int count, HitLocation[] mounts)>
        {
            ("Large", chassis.HardpointLarge, new[] { HitLocation.LeftTorso, HitLocation.RightTorso, HitLocation.CenterTorso }),
            ("Medium", chassis.HardpointMedium, new[] { HitLocation.LeftArm, HitLocation.RightArm, HitLocation.LeftTorso, HitLocation.RightTorso }),
            ("Small", chassis.HardpointSmall, new[] { HitLocation.CenterTorso, HitLocation.LeftArm, HitLocation.RightArm, HitLocation.Head })
        };

        foreach (var (size, count, mounts) in hardpointSlots)
        {
            var candidates = allWeapons.Where(w => w.HardpointSize == size).ToList();
            if (!candidates.Any()) continue;

            for (int i = 0; i < count && i < 3; i++) // Cap at 3 weapons per size
            {
                var weapon = candidates[_random.Next(candidates.Count)];
                var mount = mounts[i % mounts.Length];

                if (!frame.WeaponGroups.ContainsKey(groupId))
                    frame.WeaponGroups[groupId] = new List<EquippedWeapon>();

                frame.WeaponGroups[groupId].Add(new EquippedWeapon
                {
                    WeaponId = weapon.WeaponId,
                    Name = weapon.Name,
                    HardpointSize = weapon.HardpointSize,
                    WeaponType = weapon.WeaponType,
                    EnergyCost = weapon.EnergyCost,
                    AmmoPerShot = weapon.AmmoPerShot,
                    AmmoType = weapon.WeaponType == "Ballistic" ? $"AC{weapon.Damage}" :
                               weapon.WeaponType == "Missile" ? "SRM" : "",
                    Damage = weapon.Damage,
                    RangeClass = weapon.RangeClass,
                    BaseAccuracy = weapon.BaseAccuracy,
                    WeaponGroup = groupId,
                    MountLocation = mount,
                    SpecialEffect = weapon.SpecialEffect
                });

                if (weapon.AmmoPerShot > 0)
                {
                    string ammoType = weapon.WeaponType == "Ballistic" ? $"AC{weapon.Damage}" : "SRM";
                    if (!ammoTracker.ContainsKey(ammoType))
                        ammoTracker[ammoType] = 0;
                    ammoTracker[ammoType] += weapon.AmmoPerShot * 8;
                }

                groupId++;
            }
        }

        frame.AmmoByType = ammoTracker;
    }

    /// <summary>
    /// Processes combat results into mission rewards and consequences
    /// </summary>
    public MissionResults ProcessResults(Mission mission, CombatResult outcome,
        List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames)
    {
        var results = new MissionResults
        {
            Outcome = outcome
        };

        // Credits
        if (outcome == CombatResult.Victory)
        {
            results.CreditsEarned = mission.CreditReward;

            // Bonus for clean victory (no frames destroyed)
            if (playerFrames.All(f => !f.IsDestroyed))
                results.BonusCredits = mission.BonusCredits;

            results.ReputationGained = mission.ReputationReward;
        }
        else if (outcome == CombatResult.Defeat)
        {
            results.CreditsEarned = mission.CreditReward / 4; // Partial payment
            results.ReputationGained = -1;
        }
        else
        {
            results.CreditsEarned = mission.CreditReward / 2;
        }

        // Faction standing changes
        var allFactions = _factionRepo.GetAll();
        if (outcome == CombatResult.Victory)
        {
            results.FactionStandingChanges[mission.EmployerFactionId] = 10 + mission.Difficulty * 2;
            foreach (var f in allFactions.Where(f => f.FactionId != mission.EmployerFactionId))
            {
                results.FactionStandingChanges[f.FactionId] = -(2 + mission.Difficulty);
            }
        }
        else if (outcome == CombatResult.Defeat)
        {
            results.FactionStandingChanges[mission.EmployerFactionId] = -(5 + mission.Difficulty);
        }
        else // Withdrawal
        {
            results.FactionStandingChanges[mission.EmployerFactionId] = -3;
        }

        // Build salvage pool from ALL weapons on destroyed enemies
        if (outcome == CombatResult.Victory || outcome == CombatResult.Withdrawal)
        {
            var allDbWeapons = _weaponRepo.GetAll();

            foreach (var enemy in enemyFrames.Where(e => e.IsDestroyed))
            {
                var allWeapons = enemy.WeaponGroups.Values.SelectMany(g => g).ToList();
                foreach (var weapon in allWeapons)
                {
                    var dbWeapon = allDbWeapons.FirstOrDefault(w => w.WeaponId == weapon.WeaponId);
                    results.SalvagePool.Add(new SalvageItem
                    {
                        WeaponId = weapon.WeaponId,
                        WeaponName = weapon.Name,
                        HardpointSize = weapon.HardpointSize,
                        SalvageValue = dbWeapon?.SalvageValue ?? 0,
                        SourceFrame = enemy.CustomName
                    });
                }
            }

            // Salvage allowance: base 1 + 1 per 2 difficulty, capped by pool size
            int baseAllowance = outcome == CombatResult.Victory ? 1 + mission.Difficulty / 2 : 1;
            results.SalvageAllowance = Math.Min(baseAllowance, results.SalvagePool.Count);
        }

        // Frame damage reports
        foreach (var frame in playerFrames)
        {
            var report = new FrameDamageReport
            {
                InstanceId = frame.InstanceId,
                FrameName = frame.CustomName,
                IsDestroyed = frame.IsDestroyed,
                ArmorPercentRemaining = frame.ArmorPercent
            };

            if (frame.IsDestroyed)
            {
                report.RepairCost = 0; // Can't repair destroyed frames (for now)
                report.RepairDays = 0;
            }
            else
            {
                float damageRatio = 1.0f - (frame.ArmorPercent / 100f);
                report.RepairCost = (int)(ManagementService.GetChassisPrice(
                    new Chassis { Class = frame.Class }) * 0.3f * damageRatio);
                report.RepairCost += frame.DamagedComponents.Count * 5000;
                report.RepairDays = Math.Max(1, (int)(damageRatio * 5));
            }

            foreach (var loc in frame.DestroyedLocations)
                report.DestroyedLocations.Add(loc.ToString());

            results.FrameDamageReports.Add(report);
        }

        // Pilot XP
        int baseXP = mission.Difficulty * 25;
        int victoryBonus = outcome == CombatResult.Victory ? 50 : 0;

        foreach (var frame in playerFrames)
        {
            if (frame.PilotId.HasValue)
            {
                results.PilotXPGained[frame.PilotId.Value] = baseXP + victoryBonus;

                // Pilot killed by head destruction — guaranteed KIA
                if (frame.IsPilotDead)
                {
                    results.PilotsKIA.Add(frame.PilotId.Value);
                }
                // Frame destroyed (CT breach) — random survival roll
                else if (frame.IsDestroyed)
                {
                    if (_random.Next(100) < 30) // 30% KIA chance
                        results.PilotsKIA.Add(frame.PilotId.Value);
                    else
                        results.PilotsInjured.Add(frame.PilotId.Value);
                }
                // Head destroyed but pilot survived — injured
                else if (frame.HasHeadDestroyed)
                {
                    results.PilotsInjured.Add(frame.PilotId.Value);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Applies mission results to persistent state
    /// </summary>
    public void ApplyResults(MissionResults results, ManagementService management)
    {
        var state = management.GetPlayerState();
        if (state == null) return;

        // Credits
        state.Credits += results.CreditsEarned + results.BonusCredits;
        state.Reputation = Math.Max(0, state.Reputation + results.ReputationGained);
        state.MissionsCompleted++;
        if (results.Outcome == CombatResult.Victory)
            state.MissionsWon++;

        management.SavePlayerState(state);

        // Add selected salvage to company inventory
        foreach (var weaponId in results.SalvagedWeaponIds)
        {
            management.AddToInventory(weaponId);
        }

        // Apply faction standing changes
        foreach (var (factionId, delta) in results.FactionStandingChanges)
        {
            var current = _standingRepo.GetByFaction(factionId);
            if (current != null)
            {
                int newStanding = Math.Clamp(current.Standing + delta, -100, 500);
                _standingRepo.UpdateStanding(factionId, newStanding);
            }
        }

        // Apply pilot XP and status changes
        var pilotRepo = new PilotRepository(_dbContext);

        foreach (var (pilotId, xp) in results.PilotXPGained)
        {
            var pilot = pilotRepo.GetById(pilotId);
            if (pilot == null) continue;

            pilot.ExperiencePoints += xp;
            pilot.MissionsCompleted++;

            if (results.PilotsKIA.Contains(pilotId))
            {
                pilot.Status = "KIA";
            }
            else if (results.PilotsInjured.Contains(pilotId))
            {
                pilot.Status = "Injured";
                pilot.InjuryDays = 3 + new Random().Next(0, 8);
            }

            pilotRepo.Update(pilot);
        }
    }
}
