using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Core combat resolution engine - orchestrates all combat subsystems
/// </summary>
public class CombatEngine
{
    private readonly Random _random = new();
    private readonly PositioningSystem _positioning = new();
    private readonly DamageSystem _damage = new();
    private readonly ReactorSystem _reactor = new();
    private readonly ActionSystem _actions = new();
    private readonly CombatAI _ai = new();
    private const int CriticalHitChance = 5;

    /// <summary>
    /// Resolves a full combat engagement (auto-resolve mode)
    /// </summary>
    public CombatLog ResolveCombat(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders playerOrders,
        TacticalOrders enemyOrders)
    {
        var log = new CombatLog { Result = CombatResult.Ongoing };
        int roundNumber = 1;

        _positioning.InitializeRangeBands(playerFrames, enemyFrames);

        while (!IsCombatOver(playerFrames, enemyFrames, log))
        {
            // Check withdrawal conditions
            if (_ai.ShouldWithdraw(playerFrames, playerOrders.WithdrawalThreshold))
            {
                log.Result = CombatResult.Withdrawal;
                log.Rounds.Add(new CombatRound
                {
                    RoundNumber = roundNumber,
                    Events = new List<CombatEvent>
                    {
                        new CombatEvent
                        {
                            Type = CombatEventType.Movement,
                            Message = "Player forces withdraw from combat!"
                        }
                    }
                });
                break;
            }

            if (_ai.ShouldWithdraw(enemyFrames, enemyOrders.WithdrawalThreshold))
            {
                log.Result = CombatResult.Victory;
                log.Rounds.Add(new CombatRound
                {
                    RoundNumber = roundNumber,
                    Events = new List<CombatEvent>
                    {
                        new CombatEvent
                        {
                            Type = CombatEventType.Movement,
                            Message = "Enemy forces withdraw from combat!"
                        }
                    }
                });
                break;
            }

            // Generate AI decisions for both sides
            var playerDecisions = GenerateAIDecisions(playerFrames, enemyFrames, playerOrders);
            var enemyDecisions = GenerateAIDecisions(enemyFrames, playerFrames, enemyOrders);

            var round = ResolveRound(playerFrames, enemyFrames,
                playerDecisions, enemyDecisions,
                playerOrders, enemyOrders, roundNumber);
            log.Rounds.Add(round);
            roundNumber++;

            if (roundNumber > 100)
            {
                log.Result = CombatResult.Withdrawal;
                break;
            }
        }

        log.TotalRounds = log.Rounds.Count;
        return log;
    }

    /// <summary>
    /// Resolves a single combat round with provided decisions
    /// </summary>
    public CombatRound ResolveRound(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        RoundTacticalDecision playerDecisions,
        RoundTacticalDecision enemyDecisions,
        TacticalOrders playerOrders,
        TacticalOrders enemyOrders,
        int roundNumber)
    {
        var round = new CombatRound { RoundNumber = roundNumber };
        var allFrames = playerFrames.Concat(enemyFrames).ToList();

        // 1. Start of round: refresh energy and action points
        foreach (var frame in allFrames.Where(f => !f.IsDestroyed))
        {
            _reactor.RefreshEnergy(frame);
            _actions.RefreshActionPoints(frame);

            if (frame.IsShutDown)
            {
                round.Events.Add(new CombatEvent
                {
                    Type = CombatEventType.ReactorShutdown,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} reactor recovering from shutdown..."
                });
            }
        }

        // 2. Initiative: order by Speed, ties broken by PilotPiloting
        var initiativeOrder = allFrames
            .Where(f => !f.IsDestroyed && !f.IsShutDown)
            .OrderByDescending(f => f.Speed)
            .ThenByDescending(f => f.PilotPiloting)
            .ToList();

        // 3. Action phase: each frame executes planned actions
        foreach (var frame in initiativeOrder)
        {
            if (frame.IsDestroyed) continue;

            bool isPlayerFrame = playerFrames.Contains(frame);
            var decisions = isPlayerFrame ? playerDecisions : enemyDecisions;
            var orders = isPlayerFrame ? playerOrders : enemyOrders;
            var targets = isPlayerFrame ? enemyFrames : playerFrames;
            var activeTargets = targets.Where(t => !t.IsDestroyed).ToList();

            if (!activeTargets.Any()) continue;

            // Get planned actions for this frame
            FrameActions frameActions;
            if (decisions.FrameOrders.TryGetValue(frame.InstanceId, out var planned))
            {
                frameActions = planned;
            }
            else
            {
                // No specific orders, let AI decide
                frameActions = _ai.GenerateActions(frame, activeTargets, orders, _actions);
            }

            // Select target
            var target = frameActions.FocusTargetId.HasValue
                ? activeTargets.FirstOrDefault(t => t.InstanceId == frameActions.FocusTargetId.Value) ?? activeTargets.First()
                : _ai.SelectTarget(frame, activeTargets, orders.TargetPriority);

            // Execute each planned action
            foreach (var action in frameActions.Actions)
            {
                if (frame.ActionPoints <= 0) break;
                if (frame.IsDestroyed) break;

                var actionEvents = ExecuteAction(frame, action, target, activeTargets);
                round.Events.AddRange(actionEvents);
            }
        }

        // 4. Overwatch resolution
        var overwatchEvents = ResolveOverwatch(allFrames, playerFrames, enemyFrames);
        round.Events.AddRange(overwatchEvents);

        // 5. End of round: process reactor stress, clear flags
        foreach (var frame in allFrames.Where(f => !f.IsDestroyed))
        {
            var reactorEvents = _reactor.ProcessEndOfRound(frame);
            round.Events.AddRange(reactorEvents);
        }

        // Check victory/defeat
        CheckCombatEnd(playerFrames, enemyFrames, round);

        return round;
    }

    /// <summary>
    /// Executes a single action for a frame
    /// </summary>
    private List<CombatEvent> ExecuteAction(CombatFrame frame, PlannedAction action,
        CombatFrame target, List<CombatFrame> activeTargets)
    {
        var events = new List<CombatEvent>();

        int apCost = ActionSystem.GetActionCost(action.Action);
        if (frame.ActionPoints < apCost) return events;

        switch (action.Action)
        {
            case CombatAction.Move:
                events.AddRange(ExecuteMove(frame, action.MoveDirection ?? MovementDirection.Hold));
                break;

            case CombatAction.FireGroup:
                if (action.WeaponGroupId.HasValue)
                    events.AddRange(ExecuteFireGroup(frame, action.WeaponGroupId.Value, target, null));
                break;

            case CombatAction.CalledShot:
                if (action.WeaponGroupId.HasValue && action.CalledShotLocation.HasValue)
                    events.AddRange(ExecuteFireGroup(frame, action.WeaponGroupId.Value, target, action.CalledShotLocation));
                break;

            case CombatAction.Brace:
                frame.IsBracing = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Brace,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} braces for impact (+20% evasion)"
                });
                break;

            case CombatAction.Overwatch:
                frame.IsOnOverwatch = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Overwatch,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} sets overwatch"
                });
                break;

            case CombatAction.VentReactor:
                events.Add(_reactor.VentReactor(frame));
                break;

            case CombatAction.Sprint:
                events.AddRange(ExecuteSprint(frame, action.MoveDirection ?? MovementDirection.Close));
                break;
        }

        _actions.ConsumeActionPoints(frame, action.Action);
        return events;
    }

    private List<CombatEvent> ExecuteMove(CombatFrame frame, MovementDirection direction)
    {
        var events = new List<CombatEvent>();

        if (direction == MovementDirection.Hold)
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} holds position at {PositioningSystem.FormatRangeBand(frame.CurrentRange)} range"
            });
            return events;
        }

        // Calculate energy cost BEFORE moving (don't mutate state until confirmed)
        int energyCost = _positioning.GetMovementEnergyCost(frame);
        if (direction == MovementDirection.PullBack)
            energyCost = (int)(energyCost * 1.5);

        if (!_reactor.ConsumeEnergy(frame, energyCost))
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} insufficient energy to move (need {energyCost})"
            });
            return events;
        }

        // Energy confirmed, now actually move
        _positioning.ProcessMovement(frame, direction);

        string directionText = direction == MovementDirection.Close ? "advances" : "falls back";
        events.Add(new CombatEvent
        {
            Type = CombatEventType.Movement,
            AttackerId = frame.InstanceId,
            AttackerName = frame.CustomName,
            Message = $"{frame.CustomName} {directionText} to {PositioningSystem.FormatRangeBand(frame.CurrentRange)} range ({energyCost} energy)"
        });

        return events;
    }

    private List<CombatEvent> ExecuteSprint(CombatFrame frame, MovementDirection direction)
    {
        var events = new List<CombatEvent>();
        int totalEnergyCost = _positioning.GetMovementEnergyCost(frame) * 2;

        if (!_reactor.ConsumeEnergy(frame, totalEnergyCost))
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} insufficient energy to sprint"
            });
            return events;
        }

        // Move two bands
        _positioning.ProcessMovement(frame, direction);
        _positioning.ProcessMovement(frame, direction);

        string directionText = direction == MovementDirection.Close ? "sprints forward" : "sprints back";
        events.Add(new CombatEvent
        {
            Type = CombatEventType.Movement,
            AttackerId = frame.InstanceId,
            AttackerName = frame.CustomName,
            Message = $"{frame.CustomName} {directionText} to {PositioningSystem.FormatRangeBand(frame.CurrentRange)} range ({totalEnergyCost} energy)"
        });

        return events;
    }

    private List<CombatEvent> ExecuteFireGroup(CombatFrame frame, int groupId,
        CombatFrame target, HitLocation? calledShotLocation)
    {
        var events = new List<CombatEvent>();

        if (!frame.WeaponGroups.TryGetValue(groupId, out var weapons))
            return events;

        bool isCalledShot = calledShotLocation.HasValue;

        foreach (var weapon in weapons.Where(w => !w.IsDestroyed))
        {
            // Check energy
            if (weapon.EnergyCost > 0 && !_reactor.ConsumeEnergy(frame, weapon.EnergyCost))
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{weapon.Name} cannot fire - insufficient reactor energy"
                });
                continue;
            }

            // Check ammo
            if (weapon.AmmoPerShot > 0)
            {
                int currentAmmo = frame.AmmoByType.GetValueOrDefault(weapon.AmmoType, 0);
                if (currentAmmo < weapon.AmmoPerShot)
                {
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.Miss,
                        AttackerId = frame.InstanceId,
                        AttackerName = frame.CustomName,
                        Message = $"{weapon.Name} cannot fire - out of {weapon.AmmoType} ammo"
                    });
                    continue;
                }
                frame.AmmoByType[weapon.AmmoType] = currentAmmo - weapon.AmmoPerShot;
            }

            // Calculate to-hit
            bool hit = RollToHit(frame, target, weapon, isCalledShot);

            if (hit)
            {
                bool isCritical = _random.Next(100) < CriticalHitChance;
                int damage = weapon.Damage;

                // Determine hit location
                HitLocation hitLocation = calledShotLocation ?? _damage.RollHitLocation();

                // Apply damage through the layered system
                var damageEvents = _damage.ApplyDamage(target, hitLocation, damage,
                    frame.InstanceId, frame.CustomName, weapon.Name);

                // Add the initial hit event
                events.Add(new CombatEvent
                {
                    Type = isCritical ? CombatEventType.Critical : CombatEventType.Hit,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Damage = damage,
                    TargetLocation = hitLocation,
                    IsCritical = isCritical,
                    Message = $"{weapon.Name} {(isCritical ? "CRITICAL HIT" : "HIT")} - {damage} damage to {target.CustomName} {DamageSystem.FormatLocation(hitLocation)}"
                });

                // Add cascade events (component damage, ammo explosions, etc.)
                events.AddRange(damageEvents);

                // Critical hits trigger an extra component check
                if (isCritical && !target.IsDestroyed)
                {
                    // Already handled through damage cascade
                }
            }
            else
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Message = $"{weapon.Name} MISS - shot at {target.CustomName}"
                });
            }
        }

        return events;
    }

    private bool RollToHit(CombatFrame attacker, CombatFrame target, EquippedWeapon weapon, bool isCalledShot)
    {
        int baseChance = weapon.BaseAccuracy;
        int gunneryBonus = attacker.PilotGunnery * 2;
        int evasionPenalty = target.Evasion;
        int rangeModifier = _positioning.GetRangeAccuracyModifier(weapon, attacker.CurrentRange);
        int braceBonus = target.IsBracing ? 20 : 0;
        int sensorPenalty = attacker.HasSensorHit ? 10 : 0;
        int calledShotPenalty = isCalledShot ? 15 : 0;

        // Arm actuator damage reduces accuracy for arm-mounted weapons
        int actuatorPenalty = 0;
        if (weapon.MountLocation is HitLocation.LeftArm or HitLocation.RightArm)
        {
            actuatorPenalty = attacker.DamagedComponents.Count(c =>
                c.Type == ComponentDamageType.ActuatorDamaged
                && c.Location == weapon.MountLocation) * 10;
        }

        int hitChance = baseChance + gunneryBonus - evasionPenalty + rangeModifier
            - braceBonus - sensorPenalty - calledShotPenalty - actuatorPenalty;
        hitChance = Math.Clamp(hitChance, 5, 95);

        return _random.Next(100) < hitChance;
    }

    private List<CombatEvent> ResolveOverwatch(List<CombatFrame> allFrames,
        List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames)
    {
        var events = new List<CombatEvent>();

        // Frames on overwatch fire at enemies that moved this round
        // (Simplified: overwatch provides a free shot at reduced accuracy)
        foreach (var frame in allFrames.Where(f => f.IsOnOverwatch && !f.IsDestroyed))
        {
            var targets = playerFrames.Contains(frame) ? enemyFrames : playerFrames;
            var activeTargets = targets.Where(t => !t.IsDestroyed).ToList();
            if (!activeTargets.Any()) continue;

            // Pick the closest/most threatening target
            var target = activeTargets.First();
            var bestGroup = GetBestOverwatchGroup(frame);
            if (bestGroup < 0) continue;

            // Overwatch fires at -10% accuracy (reaction fire)
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Overwatch,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} fires overwatch!"
            });

            var fireEvents = ExecuteFireGroup(frame, bestGroup, target, null);
            events.AddRange(fireEvents);

            frame.IsOnOverwatch = false;
        }

        return events;
    }

    private int GetBestOverwatchGroup(CombatFrame frame)
    {
        // Pick the group with highest damage that has functional weapons
        int bestGroup = -1;
        int bestDamage = 0;

        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            int damage = weapons.Where(w => !w.IsDestroyed).Sum(w => w.Damage);
            if (damage > bestDamage)
            {
                bestDamage = damage;
                bestGroup = groupId;
            }
        }

        return bestGroup;
    }

    private RoundTacticalDecision GenerateAIDecisions(List<CombatFrame> frames,
        List<CombatFrame> enemies, TacticalOrders orders)
    {
        var decisions = new RoundTacticalDecision();
        var activeEnemies = enemies.Where(e => !e.IsDestroyed).ToList();

        foreach (var frame in frames.Where(f => !f.IsDestroyed))
        {
            decisions.FrameOrders[frame.InstanceId] =
                _ai.GenerateActions(frame, activeEnemies, orders, _actions);
        }

        return decisions;
    }

    private void CheckCombatEnd(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames,
        CombatRound round)
    {
        if (!playerFrames.Any(f => !f.IsDestroyed))
        {
            round.Events.Add(new CombatEvent
            {
                Type = CombatEventType.RoundSummary,
                Message = "All player frames destroyed."
            });
        }

        if (!enemyFrames.Any(f => !f.IsDestroyed))
        {
            round.Events.Add(new CombatEvent
            {
                Type = CombatEventType.RoundSummary,
                Message = "All enemy frames destroyed."
            });
        }
    }

    private bool IsCombatOver(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, CombatLog log)
    {
        bool playerDead = !playerFrames.Any(f => !f.IsDestroyed);
        bool enemyDead = !enemyFrames.Any(f => !f.IsDestroyed);

        if (playerDead && enemyDead)
        {
            // Mutual destruction — count as victory (player took them down too)
            log.Result = CombatResult.Victory;
            return true;
        }

        if (playerDead)
        {
            log.Result = CombatResult.Defeat;
            return true;
        }

        if (enemyDead)
        {
            log.Result = CombatResult.Victory;
            return true;
        }

        return false;
    }
}
