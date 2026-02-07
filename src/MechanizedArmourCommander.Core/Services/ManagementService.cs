using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.Core.Services;

/// <summary>
/// Manages the player's roster, economy, and between-mission operations
/// </summary>
public class ManagementService
{
    private readonly DatabaseContext _dbContext;
    private readonly FrameInstanceRepository _frameRepo;
    private readonly LoadoutRepository _loadoutRepo;
    private readonly PilotRepository _pilotRepo;
    private readonly PlayerStateRepository _stateRepo;
    private readonly ChassisRepository _chassisRepo;
    private readonly WeaponRepository _weaponRepo;
    private readonly InventoryRepository _inventoryRepo;
    private readonly FactionRepository _factionRepo;
    private readonly FactionStandingRepository _standingRepo;

    public ManagementService(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        _frameRepo = new FrameInstanceRepository(dbContext);
        _loadoutRepo = new LoadoutRepository(dbContext);
        _pilotRepo = new PilotRepository(dbContext);
        _stateRepo = new PlayerStateRepository(dbContext);
        _chassisRepo = new ChassisRepository(dbContext);
        _weaponRepo = new WeaponRepository(dbContext);
        _inventoryRepo = new InventoryRepository(dbContext);
        _factionRepo = new FactionRepository(dbContext);
        _standingRepo = new FactionStandingRepository(dbContext);
    }

    // === Roster ===

    public List<FrameInstance> GetRoster()
    {
        return _frameRepo.GetAll();
    }

    public List<Loadout> GetLoadout(int instanceId)
    {
        return _loadoutRepo.GetByFrameInstance(instanceId);
    }

    public List<Pilot> GetAllPilots()
    {
        return _pilotRepo.GetAll();
    }

    public List<Pilot> GetAvailablePilots()
    {
        return _pilotRepo.GetByStatus("Active");
    }

    public PlayerState? GetPlayerState()
    {
        return _stateRepo.Get();
    }

    public void SavePlayerState(PlayerState state)
    {
        _stateRepo.Update(state);
    }

    public List<Chassis> GetAllChassis()
    {
        return _chassisRepo.GetAll();
    }

    public List<Weapon> GetAllWeapons()
    {
        return _weaponRepo.GetAll();
    }

    // === Factions ===

    public List<Faction> GetAllFactions()
    {
        return _factionRepo.GetAll();
    }

    public List<FactionStanding> GetAllStandings()
    {
        return _standingRepo.GetAll();
    }

    public List<Chassis> GetFactionMarketChassis(int factionId)
    {
        return _chassisRepo.GetByFaction(factionId);
    }

    public List<Weapon> GetFactionMarketWeapons(int factionId)
    {
        return _weaponRepo.GetByFaction(factionId);
    }

    public bool CanAccessExclusive(Weapon weapon, FactionStanding? standing)
    {
        if (weapon.FactionId == null) return true;
        if (standing == null) return false;
        if (weapon.SpecialEffect != null && weapon.SpecialEffect.Contains("exclusive"))
            return standing.Standing >= 200;
        return true;
    }

    // === Pilot Assignment ===

    public void AssignPilot(int instanceId, int pilotId)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null) return;

        // Unassign this pilot from any other frame
        var allFrames = _frameRepo.GetAll();
        foreach (var f in allFrames.Where(f => f.PilotId == pilotId))
        {
            f.PilotId = null;
            _frameRepo.Update(f);
        }

        frame.PilotId = pilotId;
        _frameRepo.Update(frame);
    }

    public void UnassignPilot(int instanceId)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null) return;

        frame.PilotId = null;
        _frameRepo.Update(frame);
    }

    // === Rename ===

    public bool RenameFrame(int instanceId, string newName)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null) return false;

        frame.CustomName = newName;
        _frameRepo.Update(frame);
        return true;
    }

    // === Repairs ===

    public bool RepairFrame(int instanceId)
    {
        var frame = _frameRepo.GetById(instanceId);
        var state = _stateRepo.Get();
        if (frame == null || state == null) return false;
        if (frame.Status != "Damaged") return false;
        if (state.Credits < frame.RepairCost) return false;

        state.Credits -= frame.RepairCost;

        // Restore armor to max
        var chassis = frame.Chassis ?? _chassisRepo.GetById(frame.ChassisId);
        if (chassis != null)
        {
            // Distribute armor evenly up to max
            int maxArmor = chassis.MaxArmorTotal;
            frame.ArmorHead = (int)(maxArmor * 0.07);
            frame.ArmorCenterTorso = (int)(maxArmor * 0.20);
            frame.ArmorLeftTorso = (int)(maxArmor * 0.145);
            frame.ArmorRightTorso = (int)(maxArmor * 0.145);
            frame.ArmorLeftArm = (int)(maxArmor * 0.11);
            frame.ArmorRightArm = (int)(maxArmor * 0.11);
            frame.ArmorLegs = (int)(maxArmor * 0.22);
        }

        frame.Status = "Ready";
        frame.RepairCost = 0;
        frame.RepairTime = 0;
        frame.ReactorStress = 0;

        _frameRepo.Update(frame);
        _stateRepo.Update(state);
        return true;
    }

    // === Market ===

    public bool PurchaseChassis(int chassisId, string customName, float priceModifier = 1.0f)
    {
        var chassis = _chassisRepo.GetById(chassisId);
        var state = _stateRepo.Get();
        if (chassis == null || state == null) return false;

        int price = (int)(GetChassisPrice(chassis) * priceModifier);
        if (state.Credits < price) return false;

        state.Credits -= price;

        int maxArmor = chassis.MaxArmorTotal;
        var newFrame = new FrameInstance
        {
            ChassisId = chassisId,
            CustomName = customName,
            ArmorHead = (int)(maxArmor * 0.07),
            ArmorCenterTorso = (int)(maxArmor * 0.20),
            ArmorLeftTorso = (int)(maxArmor * 0.145),
            ArmorRightTorso = (int)(maxArmor * 0.145),
            ArmorLeftArm = (int)(maxArmor * 0.11),
            ArmorRightArm = (int)(maxArmor * 0.11),
            ArmorLegs = (int)(maxArmor * 0.22),
            Status = "Ready",
            AcquisitionDate = DateTime.Now
        };

        _frameRepo.Insert(newFrame);
        _stateRepo.Update(state);
        return true;
    }

    public bool SellFrame(int instanceId)
    {
        var frame = _frameRepo.GetById(instanceId);
        var state = _stateRepo.Get();
        if (frame == null || state == null) return false;

        var chassis = frame.Chassis ?? _chassisRepo.GetById(frame.ChassisId);
        if (chassis == null) return false;

        // Sell at 50% of purchase price
        int sellPrice = GetChassisPrice(chassis) / 2;

        // Return equipped weapons to inventory instead of crediting salvage value
        var loadout = _loadoutRepo.GetByFrameInstance(instanceId);
        foreach (var slot in loadout.Where(l => l.Weapon != null))
        {
            _inventoryRepo.Insert(slot.WeaponId);
        }

        state.Credits += sellPrice;
        _loadoutRepo.DeleteByFrameInstance(instanceId);
        _frameRepo.Delete(instanceId);
        _stateRepo.Update(state);
        return true;
    }

    public bool RefitFrame(int instanceId, List<Loadout> newLoadout)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null || frame.Status != "Ready") return false;

        _loadoutRepo.ReplaceLoadout(instanceId, newLoadout);
        return true;
    }

    // === Inventory ===

    public List<InventoryItem> GetInventory()
    {
        return _inventoryRepo.GetAll();
    }

    public List<InventoryItem> GetInventoryBySize(string hardpointSize)
    {
        return _inventoryRepo.GetByHardpointSize(hardpointSize);
    }

    public void AddToInventory(int weaponId)
    {
        _inventoryRepo.Insert(weaponId);
    }

    public void RemoveFromInventory(int inventoryId)
    {
        _inventoryRepo.Delete(inventoryId);
    }

    /// <summary>
    /// Purchase a weapon from the market and add to company inventory
    /// </summary>
    public bool PurchaseWeapon(int weaponId, float priceModifier = 1.0f)
    {
        var weapon = _weaponRepo.GetById(weaponId);
        var state = _stateRepo.Get();
        if (weapon == null || state == null) return false;

        int price = (int)(weapon.PurchaseCost * priceModifier);
        if (state.Credits < price) return false;

        state.Credits -= price;
        _inventoryRepo.Insert(weaponId);
        _stateRepo.Update(state);
        return true;
    }

    /// <summary>
    /// Sell a weapon from inventory for its salvage value
    /// </summary>
    public bool SellWeapon(int inventoryId)
    {
        var inventory = _inventoryRepo.GetAll();
        var item = inventory.FirstOrDefault(i => i.InventoryId == inventoryId);
        if (item?.Weapon == null) return false;

        var state = _stateRepo.Get();
        if (state == null) return false;

        state.Credits += item.Weapon.SalvageValue;
        _inventoryRepo.Delete(inventoryId);
        _stateRepo.Update(state);
        return true;
    }

    /// <summary>
    /// Equip a weapon from inventory onto a frame's hardpoint slot
    /// </summary>
    public bool EquipFromInventory(int instanceId, int inventoryId, string hardpointSlot, int weaponGroup, string mountLocation)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null || frame.Status != "Ready") return false;

        var inventory = _inventoryRepo.GetAll();
        var item = inventory.FirstOrDefault(i => i.InventoryId == inventoryId);
        if (item == null) return false;

        // Add to loadout
        _loadoutRepo.Insert(new Loadout
        {
            InstanceId = instanceId,
            HardpointSlot = hardpointSlot,
            WeaponId = item.WeaponId,
            WeaponGroup = weaponGroup,
            MountLocation = mountLocation
        });

        // Remove from inventory
        _inventoryRepo.Delete(inventoryId);
        return true;
    }

    /// <summary>
    /// Unequip a weapon from a frame and return it to inventory
    /// </summary>
    public bool UnequipToInventory(int instanceId, int loadoutId)
    {
        var frame = _frameRepo.GetById(instanceId);
        if (frame == null || frame.Status != "Ready") return false;

        var loadout = _loadoutRepo.GetByFrameInstance(instanceId);
        var slot = loadout.FirstOrDefault(l => l.LoadoutId == loadoutId);
        if (slot == null) return false;

        // Add weapon to inventory
        _inventoryRepo.Insert(slot.WeaponId);

        // Remove from loadout - rebuild without this slot
        var remaining = loadout.Where(l => l.LoadoutId != loadoutId).ToList();
        _loadoutRepo.ReplaceLoadout(instanceId, remaining);
        return true;
    }

    // === Pilots ===

    public bool HirePilot(out Pilot? newPilot)
    {
        newPilot = null;
        var state = _stateRepo.Get();
        if (state == null) return false;

        int cost = 30000;
        if (state.Credits < cost) return false;

        state.Credits -= cost;

        var random = new Random();
        var callsigns = new[] { "Ghost", "Falcon", "Thunder", "Shadow", "Blade", "Phoenix",
            "Wolf", "Iron", "Storm", "Razor", "Fang", "Ember", "Frost", "Havoc" };

        // Avoid duplicates
        var existingCallsigns = _pilotRepo.GetAll().Select(p => p.Callsign).ToHashSet();
        var available = callsigns.Where(c => !existingCallsigns.Contains(c)).ToList();
        string callsign = available.Count > 0 ? available[random.Next(available.Count)] : $"Merc-{random.Next(100, 999)}";

        newPilot = new Pilot
        {
            Callsign = callsign,
            GunnerySkill = random.Next(2, 5),
            PilotingSkill = random.Next(2, 5),
            TacticsSkill = random.Next(1, 4),
            ExperiencePoints = 0,
            MissionsCompleted = 0,
            Kills = 0,
            Status = "Active",
            InjuryDays = 0,
            Morale = 70 + random.Next(0, 21)
        };

        newPilot.PilotId = _pilotRepo.Insert(newPilot);
        _stateRepo.Update(state);
        return true;
    }

    // === Day Advancement ===

    public void AdvanceDay()
    {
        var state = _stateRepo.Get();
        if (state == null) return;

        state.CurrentDay++;

        // Tick injury timers
        var pilots = _pilotRepo.GetAll();
        foreach (var pilot in pilots.Where(p => p.Status == "Injured"))
        {
            pilot.InjuryDays--;
            if (pilot.InjuryDays <= 0)
            {
                pilot.InjuryDays = 0;
                pilot.Status = "Active";
            }
            _pilotRepo.Update(pilot);
        }

        // Tick repair timers
        var frames = _frameRepo.GetAll();
        foreach (var frame in frames.Where(f => f.Status == "Repairing"))
        {
            frame.RepairTime--;
            if (frame.RepairTime <= 0)
            {
                frame.RepairTime = 0;
                frame.Status = "Ready";
            }
            _frameRepo.Update(frame);
        }

        _stateRepo.Update(state);
    }

    // === Combat Frame Building ===

    /// <summary>
    /// Converts persistent frame data into CombatFrame objects ready for combat
    /// </summary>
    public List<CombatFrame> BuildCombatFrames(List<int> instanceIds)
    {
        var combatFrames = new List<CombatFrame>();

        foreach (var instanceId in instanceIds)
        {
            var frame = _frameRepo.GetById(instanceId);
            if (frame?.Chassis == null) continue;

            var loadout = _loadoutRepo.GetByFrameInstance(instanceId);
            Pilot? pilot = frame.PilotId.HasValue ? _pilotRepo.GetById(frame.PilotId.Value) : null;

            var combatFrame = BuildCombatFrame(frame, loadout, pilot);
            combatFrames.Add(combatFrame);
        }

        return combatFrames;
    }

    private CombatFrame BuildCombatFrame(FrameInstance frame, List<Loadout> loadout, Pilot? pilot)
    {
        var chassis = frame.Chassis!;

        var combatFrame = new CombatFrame
        {
            InstanceId = frame.InstanceId,
            CustomName = frame.CustomName,
            ChassisDesignation = chassis.Designation,
            ChassisName = chassis.Name,
            Class = chassis.Class,
            ReactorOutput = chassis.ReactorOutput,
            CurrentEnergy = chassis.ReactorOutput,
            ReactorStress = frame.ReactorStress,
            MovementEnergyCost = chassis.MovementEnergyCost,
            Speed = chassis.BaseSpeed,
            Evasion = chassis.BaseEvasion,
            PilotId = pilot?.PilotId,
            PilotCallsign = pilot?.Callsign,
            PilotGunnery = pilot?.GunnerySkill ?? 3,
            PilotPiloting = pilot?.PilotingSkill ?? 3,
            PilotTactics = pilot?.TacticsSkill ?? 2,
            ActionPoints = 2,
            MaxActionPoints = 2,
            CurrentRange = RangeBand.Long,
            Armor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, frame.ArmorHead },
                { HitLocation.CenterTorso, frame.ArmorCenterTorso },
                { HitLocation.LeftTorso, frame.ArmorLeftTorso },
                { HitLocation.RightTorso, frame.ArmorRightTorso },
                { HitLocation.LeftArm, frame.ArmorLeftArm },
                { HitLocation.RightArm, frame.ArmorRightArm },
                { HitLocation.Legs, frame.ArmorLegs }
            },
            MaxArmor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, frame.ArmorHead },
                { HitLocation.CenterTorso, frame.ArmorCenterTorso },
                { HitLocation.LeftTorso, frame.ArmorLeftTorso },
                { HitLocation.RightTorso, frame.ArmorRightTorso },
                { HitLocation.LeftArm, frame.ArmorLeftArm },
                { HitLocation.RightArm, frame.ArmorRightArm },
                { HitLocation.Legs, frame.ArmorLegs }
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

        // Build weapon groups and ammo from loadout
        var ammoTracker = new Dictionary<string, int>();

        foreach (var slot in loadout)
        {
            if (slot.Weapon == null) continue;

            var weapon = slot.Weapon;
            if (!combatFrame.WeaponGroups.ContainsKey(slot.WeaponGroup))
                combatFrame.WeaponGroups[slot.WeaponGroup] = new List<EquippedWeapon>();

            HitLocation mountLoc = ParseMountLocation(slot.MountLocation);

            combatFrame.WeaponGroups[slot.WeaponGroup].Add(new EquippedWeapon
            {
                WeaponId = weapon.WeaponId,
                Name = weapon.Name,
                HardpointSize = weapon.HardpointSize,
                WeaponType = weapon.WeaponType,
                EnergyCost = weapon.EnergyCost,
                AmmoPerShot = weapon.AmmoPerShot,
                AmmoType = weapon.WeaponType == "Ballistic" ? $"AC{weapon.Damage}" :
                           weapon.WeaponType == "Missile" ? $"SRM" : "",
                Damage = weapon.Damage,
                RangeClass = weapon.RangeClass,
                BaseAccuracy = weapon.BaseAccuracy,
                WeaponGroup = slot.WeaponGroup,
                MountLocation = mountLoc,
                SpecialEffect = weapon.SpecialEffect
            });

            // Track ammo for ballistic/missile weapons
            if (weapon.AmmoPerShot > 0)
            {
                string ammoType = weapon.WeaponType == "Ballistic" ? $"AC{weapon.Damage}" :
                                  weapon.WeaponType == "Missile" ? "SRM" : weapon.Name;
                if (!ammoTracker.ContainsKey(ammoType))
                    ammoTracker[ammoType] = 0;
                ammoTracker[ammoType] += weapon.AmmoPerShot * 8; // 8 shots worth
            }
        }

        combatFrame.AmmoByType = ammoTracker;
        return combatFrame;
    }

    private static HitLocation ParseMountLocation(string location)
    {
        return location switch
        {
            "Head" => HitLocation.Head,
            "CenterTorso" => HitLocation.CenterTorso,
            "LeftTorso" => HitLocation.LeftTorso,
            "RightTorso" => HitLocation.RightTorso,
            "LeftArm" => HitLocation.LeftArm,
            "RightArm" => HitLocation.RightArm,
            "Legs" => HitLocation.Legs,
            _ => HitLocation.CenterTorso
        };
    }

    // === Pricing ===

    public static int GetChassisPrice(Chassis chassis)
    {
        return chassis.Class switch
        {
            "Light" => 100000,
            "Medium" => 200000,
            "Heavy" => 375000,
            "Assault" => 650000,
            _ => 200000
        };
    }

    /// <summary>
    /// Applies combat damage back to persistent frame data
    /// </summary>
    public void ApplyPostCombatDamage(CombatFrame combatFrame)
    {
        var frame = _frameRepo.GetById(combatFrame.InstanceId);
        if (frame == null) return;

        var chassis = frame.Chassis ?? _chassisRepo.GetById(frame.ChassisId);
        if (chassis == null) return;

        if (combatFrame.IsDestroyed)
        {
            frame.Status = "Destroyed";
            frame.RepairCost = GetChassisPrice(chassis); // Full replacement cost
            frame.RepairTime = 0;
            frame.PilotId = null; // Pilot ejected or KIA
        }
        else
        {
            // Update armor to post-combat values
            frame.ArmorHead = combatFrame.Armor.GetValueOrDefault(HitLocation.Head, 0);
            frame.ArmorCenterTorso = combatFrame.Armor.GetValueOrDefault(HitLocation.CenterTorso, 0);
            frame.ArmorLeftTorso = combatFrame.Armor.GetValueOrDefault(HitLocation.LeftTorso, 0);
            frame.ArmorRightTorso = combatFrame.Armor.GetValueOrDefault(HitLocation.RightTorso, 0);
            frame.ArmorLeftArm = combatFrame.Armor.GetValueOrDefault(HitLocation.LeftArm, 0);
            frame.ArmorRightArm = combatFrame.Armor.GetValueOrDefault(HitLocation.RightArm, 0);
            frame.ArmorLegs = combatFrame.Armor.GetValueOrDefault(HitLocation.Legs, 0);
            frame.ReactorStress = combatFrame.ReactorStress;

            // Calculate damage ratio for repair cost
            float damageRatio = 1.0f - (combatFrame.ArmorPercent / 100f);
            int baseRepairCost = (int)(GetChassisPrice(chassis) * 0.3f * damageRatio);

            // Component damage adds to repair cost
            int componentRepairCost = combatFrame.DamagedComponents.Count * 5000;

            frame.RepairCost = baseRepairCost + componentRepairCost;
            frame.RepairTime = Math.Max(1, (int)(damageRatio * 5));
            frame.Status = damageRatio > 0.05f ? "Damaged" : "Ready";
        }

        _frameRepo.Update(frame);
    }
}
