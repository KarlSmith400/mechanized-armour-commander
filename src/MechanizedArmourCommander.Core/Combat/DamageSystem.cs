using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Handles layered damage resolution: armor → structure → component damage
/// </summary>
public class DamageSystem
{
    private readonly Random _random = new();

    // Hit location probability weights
    private static readonly (HitLocation location, int weight)[] HitLocationTable =
    {
        (HitLocation.Head, 5),
        (HitLocation.CenterTorso, 20),
        (HitLocation.LeftTorso, 15),
        (HitLocation.RightTorso, 15),
        (HitLocation.LeftArm, 10),
        (HitLocation.RightArm, 10),
        (HitLocation.Legs, 25)
    };

    /// <summary>
    /// Rolls a random hit location based on probability weights
    /// </summary>
    public HitLocation RollHitLocation()
    {
        int roll = _random.Next(100);
        int cumulative = 0;

        foreach (var (location, weight) in HitLocationTable)
        {
            cumulative += weight;
            if (roll < cumulative)
                return location;
        }

        return HitLocation.CenterTorso; // Fallback
    }

    /// <summary>
    /// Applies damage to a frame at a specific location, handling armor → structure → component cascade
    /// Returns list of events generated
    /// </summary>
    public List<CombatEvent> ApplyDamage(CombatFrame target, HitLocation location, int damage,
        int? attackerId = null, string? attackerName = null, string? weaponName = null)
    {
        var events = new List<CombatEvent>();

        // If this location is already destroyed, transfer to adjacent
        if (target.DestroyedLocations.Contains(location))
        {
            var transferLocation = GetTransferLocation(location);
            if (transferLocation != null && !target.DestroyedLocations.Contains(transferLocation.Value))
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.DamageTransfer,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Message = $"Damage transfers from destroyed {FormatLocation(location)} to {FormatLocation(transferLocation.Value)}"
                });
                location = transferLocation.Value;
            }
            else
            {
                return events; // Nowhere to transfer
            }
        }

        int remainingDamage = damage;
        int armorDamage = 0;
        int structureDamage = 0;

        // Phase 1: Absorb with armor
        int currentArmor = target.Armor.GetValueOrDefault(location, 0);
        if (currentArmor > 0)
        {
            armorDamage = Math.Min(currentArmor, remainingDamage);
            target.Armor[location] = currentArmor - armorDamage;
            remainingDamage -= armorDamage;
        }

        // Phase 2: Overflow to structure
        if (remainingDamage > 0)
        {
            // Reactive Armor: reduce structure damage by percentage
            int damageReduction = target.GetEquipmentValue("DamageReduction");
            if (damageReduction > 0)
                remainingDamage = Math.Max(1, remainingDamage - (remainingDamage * damageReduction / 100));

            int currentStructure = target.Structure.GetValueOrDefault(location, 0);
            structureDamage = Math.Min(currentStructure, remainingDamage);
            target.Structure[location] = currentStructure - structureDamage;

            // Structure damage triggers component check
            if (structureDamage > 0)
            {
                var componentEvents = CheckComponentDamage(target, location);
                events.AddRange(componentEvents);
            }

            // Check if location is destroyed
            if (target.Structure[location] <= 0)
            {
                target.DestroyedLocations.Add(location);
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.LocationDestroyed,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    TargetLocation = location,
                    Message = $"{target.CustomName} {FormatLocation(location)} DESTROYED!"
                });

                // Destroy weapons mounted at this location
                DestroyWeaponsAtLocation(target, location, events);

                // Handle damage transfer for torso destruction
                int overflowDamage = remainingDamage - structureDamage;
                if (overflowDamage > 0)
                {
                    var transferLocation = GetTransferLocation(location);
                    if (transferLocation != null)
                    {
                        events.Add(new CombatEvent
                        {
                            Type = CombatEventType.DamageTransfer,
                            DefenderId = target.InstanceId,
                            DefenderName = target.CustomName,
                            Damage = overflowDamage,
                            Message = $"{overflowDamage} damage transfers from {FormatLocation(location)} to {FormatLocation(transferLocation.Value)}"
                        });
                        var transferEvents = ApplyDamage(target, transferLocation.Value, overflowDamage,
                            attackerId, attackerName, weaponName);
                        events.AddRange(transferEvents);
                    }
                }

                // Check frame destruction (CT destroyed)
                if (location == HitLocation.CenterTorso)
                {
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.FrameDestroyed,
                        DefenderId = target.InstanceId,
                        DefenderName = target.CustomName,
                        Message = $"{target.CustomName} DESTROYED! Center torso breached!"
                    });
                }

                // Head destroyed — pilot survival roll, cockpit/sensors lost
                if (location == HitLocation.Head)
                {
                    var headEvents = ResolveHeadDestruction(target);
                    events.AddRange(headEvents);
                }
            }
        }

        return events;
    }

    /// <summary>
    /// Resolves head destruction: pilot survival roll, cockpit/sensor damage, possible frame shutdown
    /// Survival chance: 50% base + 5% per Piloting skill point
    /// </summary>
    private List<CombatEvent> ResolveHeadDestruction(CombatFrame target)
    {
        var events = new List<CombatEvent>();

        events.Add(new CombatEvent
        {
            Type = CombatEventType.HeadDestroyed,
            DefenderId = target.InstanceId,
            DefenderName = target.CustomName,
            TargetLocation = HitLocation.Head,
            Message = $"{target.CustomName} HEAD DESTROYED! Cockpit breached!"
        });

        // Add permanent sensor damage if not already present
        if (!target.HasSensorHit)
        {
            target.DamagedComponents.Add(new ComponentDamage
            {
                Location = HitLocation.Head,
                Type = ComponentDamageType.SensorHit,
                Description = "Sensors destroyed with head"
            });
        }

        // Pilot survival roll: 50% base + 5% per Piloting skill
        int survivalChance = 50 + (target.PilotPiloting * 5);
        int roll = _random.Next(100);

        if (roll < survivalChance)
        {
            // Pilot survives — injured but alive, frame keeps fighting with heavy penalties
            events.Add(new CombatEvent
            {
                Type = CombatEventType.PilotSurvivedHeadHit,
                DefenderId = target.InstanceId,
                DefenderName = target.CustomName,
                Message = $"{target.PilotCallsign ?? "Pilot"} survives cockpit breach! ({survivalChance}% chance) Injured, sensors destroyed, gunnery systems offline."
            });

            // Zero out pilot gunnery — cockpit targeting systems are gone
            target.PilotGunnery = 0;
        }
        else
        {
            // Pilot killed — frame shuts down immediately
            target.IsPilotDead = true;
            target.IsShutDown = true;

            events.Add(new CombatEvent
            {
                Type = CombatEventType.PilotKilledInCombat,
                DefenderId = target.InstanceId,
                DefenderName = target.CustomName,
                Message = $"{target.PilotCallsign ?? "Pilot"} KILLED IN ACTION! ({100 - survivalChance}% chance) {target.CustomName} shuts down — no pilot."
            });

            events.Add(new CombatEvent
            {
                Type = CombatEventType.FrameDestroyed,
                DefenderId = target.InstanceId,
                DefenderName = target.CustomName,
                Message = $"{target.CustomName} offline — pilot lost."
            });
        }

        return events;
    }

    /// <summary>
    /// Rolls for component damage when structure at a location takes a hit
    /// </summary>
    private List<CombatEvent> CheckComponentDamage(CombatFrame target, HitLocation location)
    {
        var events = new List<CombatEvent>();
        int roll = _random.Next(100);

        // ~40% chance of a component effect when structure is hit
        if (roll >= 43) return events; // No component damage

        ComponentDamageType damageType;
        string description;

        if (roll < 15)
        {
            // Weapon destroyed at this location
            damageType = ComponentDamageType.WeaponDestroyed;
            var weapon = FindWeaponAtLocation(target, location);
            if (weapon != null)
            {
                weapon.IsDestroyed = true;
                description = $"{weapon.Name} destroyed at {FormatLocation(location)}";
            }
            else
            {
                return events; // No weapon to destroy
            }
        }
        else if (roll < 25)
        {
            // Actuator damaged
            damageType = ComponentDamageType.ActuatorDamaged;
            description = location switch
            {
                HitLocation.LeftArm or HitLocation.RightArm =>
                    $"Arm actuator damaged at {FormatLocation(location)} - reduced accuracy for arm weapons",
                HitLocation.Legs =>
                    $"Leg actuator damaged - increased movement energy cost",
                _ => $"Actuator damaged at {FormatLocation(location)}"
            };
        }
        else if (roll < 30)
        {
            // Ammo explosion check
            damageType = ComponentDamageType.AmmoExplosion;
            if (HasAmmoAtLocation(target, location))
            {
                int explosionDamage = 10 + _random.Next(15); // 10-24 internal damage
                description = $"AMMO EXPLOSION at {FormatLocation(location)}! {explosionDamage} internal damage!";

                events.Add(new CombatEvent
                {
                    Type = CombatEventType.AmmoExplosion,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Damage = explosionDamage,
                    TargetLocation = location,
                    Message = description
                });

                // Apply explosion damage directly to structure
                int currentStructure = target.Structure.GetValueOrDefault(location, 0);
                target.Structure[location] = Math.Max(0, currentStructure - explosionDamage);
                return events;
            }
            else
            {
                return events; // No ammo to explode
            }
        }
        else if (roll < 35)
        {
            // Reactor hit
            damageType = ComponentDamageType.ReactorHit;
            target.ReactorStress += target.ReactorOutput / 4;
            description = $"Reactor hit! Stress increased. Effective output reduced.";
        }
        else if (roll < 38)
        {
            // Gyro hit
            damageType = ComponentDamageType.GyroHit;
            description = "Gyro damaged! Action points reduced.";
        }
        else if (roll < 40)
        {
            // Sensor hit
            damageType = ComponentDamageType.SensorHit;
            description = "Sensors damaged! Accuracy reduced across all weapons.";
        }
        else
        {
            // Cockpit hit (head only more likely, but can happen from internal damage)
            damageType = ComponentDamageType.CockpitHit;
            description = "Cockpit hit! Pilot injured!";
        }

        var componentDamage = new ComponentDamage
        {
            Location = location,
            Type = damageType,
            Description = description
        };
        target.DamagedComponents.Add(componentDamage);

        events.Add(new CombatEvent
        {
            Type = CombatEventType.ComponentDamage,
            DefenderId = target.InstanceId,
            DefenderName = target.CustomName,
            TargetLocation = location,
            ComponentEffect = damageType,
            Message = $"{target.CustomName}: {description}"
        });

        return events;
    }

    /// <summary>
    /// Gets the transfer location when a location is destroyed
    /// </summary>
    private HitLocation? GetTransferLocation(HitLocation destroyed)
    {
        return destroyed switch
        {
            HitLocation.LeftTorso => HitLocation.CenterTorso,
            HitLocation.RightTorso => HitLocation.CenterTorso,
            HitLocation.LeftArm => HitLocation.LeftTorso,
            HitLocation.RightArm => HitLocation.RightTorso,
            _ => null // Head, CT, Legs don't transfer
        };
    }

    /// <summary>
    /// Finds a functional weapon mounted at the given location
    /// </summary>
    private EquippedWeapon? FindWeaponAtLocation(CombatFrame frame, HitLocation location)
    {
        return frame.WeaponGroups.Values
            .SelectMany(g => g)
            .FirstOrDefault(w => !w.IsDestroyed && w.MountLocation == location);
    }

    /// <summary>
    /// Checks if the frame has ammo-consuming weapons at this location
    /// </summary>
    private bool HasAmmoAtLocation(CombatFrame frame, HitLocation location)
    {
        return frame.WeaponGroups.Values
            .SelectMany(g => g)
            .Any(w => w.MountLocation == location && w.AmmoPerShot > 0);
    }

    /// <summary>
    /// Destroys all weapons mounted at a destroyed location
    /// </summary>
    private void DestroyWeaponsAtLocation(CombatFrame frame, HitLocation location, List<CombatEvent> events)
    {
        foreach (var group in frame.WeaponGroups.Values)
        {
            foreach (var weapon in group.Where(w => !w.IsDestroyed && w.MountLocation == location))
            {
                weapon.IsDestroyed = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.ComponentDamage,
                    DefenderId = frame.InstanceId,
                    DefenderName = frame.CustomName,
                    ComponentEffect = ComponentDamageType.WeaponDestroyed,
                    Message = $"{frame.CustomName}: {weapon.Name} lost with {FormatLocation(location)}"
                });
            }
        }
    }

    public static string FormatLocation(HitLocation location)
    {
        return location switch
        {
            HitLocation.Head => "Head",
            HitLocation.CenterTorso => "Center Torso",
            HitLocation.LeftTorso => "Left Torso",
            HitLocation.RightTorso => "Right Torso",
            HitLocation.LeftArm => "Left Arm",
            HitLocation.RightArm => "Right Arm",
            HitLocation.Legs => "Legs",
            _ => location.ToString()
        };
    }
}
