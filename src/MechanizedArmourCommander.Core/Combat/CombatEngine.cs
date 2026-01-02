using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Core combat resolution engine
/// </summary>
public class CombatEngine
{
    private readonly Random _random = new();
    private readonly PositioningSystem _positioning = new();
    private readonly CombatAI _ai = new();
    private const int CriticalHitChance = 5; // 5% chance

    public CombatLog ResolveCombat(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders playerOrders,
        TacticalOrders enemyOrders)
    {
        var log = new CombatLog { Result = CombatResult.Ongoing };
        int roundNumber = 1;

        // Initialize positions before combat starts
        _positioning.InitializePositions(playerFrames, enemyFrames, playerOrders.Formation, enemyOrders.Formation);

        while (!IsCombatOver(playerFrames, enemyFrames, log))
        {
            // Check withdrawal conditions before round
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

            var round = ResolveRound(playerFrames, enemyFrames, playerOrders, enemyOrders, roundNumber);
            log.Rounds.Add(round);
            roundNumber++;

            // Safety limit to prevent infinite loops
            if (roundNumber > 100)
            {
                log.Result = CombatResult.Withdrawal;
                break;
            }
        }

        log.TotalRounds = log.Rounds.Count;
        return log;
    }

    private CombatRound ResolveRound(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders playerOrders,
        TacticalOrders enemyOrders,
        int roundNumber)
    {
        var round = new CombatRound { RoundNumber = roundNumber };

        // 1. Initiative determination (light frames first)
        var allFrames = playerFrames.Concat(enemyFrames)
            .Where(f => !f.IsDestroyed)
            .OrderByDescending(f => f.Speed)
            .ToList();

        // 2. Movement phase
        _positioning.ProcessMovement(playerFrames, enemyFrames, playerOrders, enemyOrders);

        foreach (var frame in allFrames)
        {
            var movementEvent = new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = GenerateMovementMessage(frame)
            };
            round.Events.Add(movementEvent);
        }

        // 3. Attack resolution
        foreach (var attacker in allFrames)
        {
            if (attacker.IsDestroyed) continue;

            var isPlayerFrame = playerFrames.Contains(attacker);
            var targets = isPlayerFrame ? enemyFrames : playerFrames;
            var activeTargets = targets.Where(t => !t.IsDestroyed).ToList();

            if (!activeTargets.Any()) continue;

            var target = SelectTarget(attacker, activeTargets,
                isPlayerFrame ? playerOrders : enemyOrders);

            var attackEvents = ResolveAttack(attacker, target, _positioning);
            round.Events.AddRange(attackEvents);
        }

        // 4. Status checks
        var statusEvents = GenerateStatusEvents(playerFrames.Concat(enemyFrames).ToList());
        round.Events.AddRange(statusEvents);

        return round;
    }

    private List<CombatEvent> ResolveAttack(CombatFrame attacker, CombatFrame target, PositioningSystem positioning)
    {
        var events = new List<CombatEvent>();

        // Calculate actual distance and range band
        int distance = positioning.GetDistance(attacker, target);
        string rangeBand = positioning.GetRangeBand(distance);

        foreach (var weapon in attacker.Weapons)
        {
            // Check if can fire (heat/ammo)
            if (attacker.CurrentHeat + weapon.HeatGeneration > attacker.MaxHeat * 1.5)
                continue;

            if (weapon.AmmoConsumption > 0 && attacker.CurrentAmmo < weapon.AmmoConsumption)
                continue;

            // Calculate to-hit with range modifier
            bool hit = RollToHit(attacker, target, weapon, positioning, rangeBand);

            if (hit)
            {
                bool isCritical = _random.Next(100) < CriticalHitChance;
                int damage = weapon.Damage;

                if (isCritical)
                {
                    damage *= 2;
                }

                target.CurrentArmor = Math.Max(0, target.CurrentArmor - damage);

                var hitLocation = RollHitLocation();

                events.Add(new CombatEvent
                {
                    Type = isCritical ? CombatEventType.Critical : CombatEventType.Hit,
                    AttackerId = attacker.InstanceId,
                    AttackerName = attacker.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Damage = damage,
                    HitLocation = hitLocation,
                    IsCritical = isCritical,
                    Message = $"{weapon.Name} {(isCritical ? "CRITICAL HIT" : "HIT")} - {damage} damage to {target.CustomName} {hitLocation}"
                });

                if (target.IsDestroyed)
                {
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.FrameDestroyed,
                        DefenderId = target.InstanceId,
                        DefenderName = target.CustomName,
                        Message = $"{target.CustomName} DESTROYED!"
                    });
                }
            }
            else
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = attacker.InstanceId,
                    AttackerName = attacker.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Message = $"{weapon.Name} MISS - shot at {target.CustomName}"
                });
            }

            // Update heat and ammo
            attacker.CurrentHeat += weapon.HeatGeneration;
            attacker.CurrentAmmo -= weapon.AmmoConsumption;
        }

        return events;
    }

    private bool RollToHit(CombatFrame attacker, CombatFrame target, EquippedWeapon weapon,
        PositioningSystem positioning, string rangeBand)
    {
        // Base calculation: weapon accuracy + pilot gunnery - target evasion
        int baseChance = weapon.BaseAccuracy;
        int gunneryBonus = attacker.PilotGunnery * 2; // +2% per gunnery level
        int evasionPenalty = target.Evasion;
        int rangeModifier = positioning.GetRangeAccuracyModifier(weapon, rangeBand);

        int hitChance = baseChance + gunneryBonus - evasionPenalty + rangeModifier;
        hitChance = Math.Clamp(hitChance, 5, 95); // 5-95% hit chance

        return _random.Next(100) < hitChance;
    }

    private string RollHitLocation()
    {
        var roll = _random.Next(100);
        return roll switch
        {
            < 30 => "CT", // Center Torso
            < 45 => "RT", // Right Torso
            < 60 => "LT", // Left Torso
            < 70 => "RA", // Right Arm
            < 80 => "LA", // Left Arm
            _ => "LEGS"
        };
    }

    private CombatFrame SelectTarget(CombatFrame attacker, List<CombatFrame> targets, TacticalOrders orders)
    {
        // Use AI for sophisticated target selection
        return _ai.SelectTarget(attacker, targets, orders.TargetPriority);
    }

    private string GenerateMovementMessage(CombatFrame frame)
    {
        int distanceFromStart = Math.Abs(frame.Position - frame.StartPosition);
        string direction = frame.Position > frame.StartPosition ? "advanced" : "holding";

        if (distanceFromStart == 0)
            return $"{frame.CustomName} holds position at {frame.Position}";

        return $"{frame.CustomName} {direction} to position {frame.Position} ({distanceFromStart} units moved)";
    }

    private List<CombatEvent> GenerateStatusEvents(List<CombatFrame> frames)
    {
        var events = new List<CombatEvent>();

        foreach (var frame in frames.Where(f => !f.IsDestroyed))
        {
            if (frame.CurrentHeat > 0)
            {
                // Cool down slightly each round
                frame.CurrentHeat = Math.Max(0, frame.CurrentHeat - 5);
            }
        }

        return events;
    }

    private bool IsCombatOver(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, CombatLog log)
    {
        var playerAlive = playerFrames.Any(f => !f.IsDestroyed);
        var enemyAlive = enemyFrames.Any(f => !f.IsDestroyed);

        if (!playerAlive)
        {
            log.Result = CombatResult.Defeat;
            return true;
        }

        if (!enemyAlive)
        {
            log.Result = CombatResult.Victory;
            return true;
        }

        return false;
    }
}
