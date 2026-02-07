using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// AI decision-making system for combat frame actions
/// </summary>
public class CombatAI
{
    private readonly Random _random = new();
    private readonly PositioningSystem _positioning = new();
    private readonly ReactorSystem _reactor = new();

    /// <summary>
    /// Generates AI actions for a frame based on tactical orders and combat state
    /// </summary>
    public FrameActions GenerateActions(CombatFrame frame, List<CombatFrame> enemies,
        TacticalOrders orders, ActionSystem actionSystem)
    {
        var actions = new FrameActions();

        if (frame.IsDestroyed || frame.IsShutDown)
            return actions;

        var activeEnemies = enemies.Where(e => !e.IsDestroyed).ToList();
        if (!activeEnemies.Any())
            return actions;

        // Select target
        var target = SelectTarget(frame, activeEnemies, orders.TargetPriority);
        actions.FocusTargetId = target.InstanceId;

        // Determine optimal range for this frame's loadout
        var optimalRange = DetermineOptimalRange(frame, orders);

        // Decide actions based on stance and situation
        switch (orders.Stance)
        {
            case Stance.Aggressive:
                PlanAggressiveActions(frame, actions, optimalRange, actionSystem);
                break;
            case Stance.Defensive:
                PlanDefensiveActions(frame, actions, optimalRange, actionSystem);
                break;
            default:
                PlanBalancedActions(frame, actions, optimalRange, actionSystem);
                break;
        }

        return actions;
    }

    private void PlanAggressiveActions(CombatFrame frame, FrameActions actions,
        RangeBand optimalRange, ActionSystem actionSystem)
    {
        // Plan based on MaxActionPoints (AP will be refreshed before execution)
        int availableAP = frame.MaxActionPoints;

        // Aggressive: close to optimal range, then fire everything possible
        if (frame.CurrentRange > optimalRange && availableAP >= 1
            && !frame.DestroyedLocations.Contains(HitLocation.Legs))
        {
            actions.Actions.Add(new PlannedAction
            {
                Action = CombatAction.Move,
                MoveDirection = MovementDirection.Close
            });
            availableAP--;
        }

        // Fire weapon groups with remaining AP
        FillRemainingAPWithFire(frame, actions, ref availableAP);
    }

    private void PlanDefensiveActions(CombatFrame frame, FrameActions actions,
        RangeBand optimalRange, ActionSystem actionSystem)
    {
        int availableAP = frame.MaxActionPoints;

        // Defensive: brace if damaged, pull back if too close, conserve energy
        bool heavilyDamaged = frame.ArmorPercent < 40;

        if (heavilyDamaged && availableAP >= 1)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.Brace });
            availableAP--;
        }
        else if (frame.CurrentRange < optimalRange && availableAP >= 1
            && !frame.DestroyedLocations.Contains(HitLocation.Legs))
        {
            actions.Actions.Add(new PlannedAction
            {
                Action = CombatAction.Move,
                MoveDirection = MovementDirection.PullBack
            });
            availableAP--;
        }

        // Vent reactor if stressed
        if (frame.ReactorStress > frame.EffectiveReactorOutput / 2
            && availableAP >= 1)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.VentReactor });
            availableAP--;
        }

        // Fire weapon groups with remaining AP
        FillRemainingAPWithFire(frame, actions, ref availableAP);
    }

    private void PlanBalancedActions(CombatFrame frame, FrameActions actions,
        RangeBand optimalRange, ActionSystem actionSystem)
    {
        int availableAP = frame.MaxActionPoints;

        // Balanced: move toward optimal range if needed, then fire
        bool needsToMove = frame.CurrentRange != optimalRange
            && !frame.DestroyedLocations.Contains(HitLocation.Legs);

        // Vent if reactor stress is getting dangerous
        if (frame.ReactorStress >= frame.EffectiveReactorOutput * 0.75)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.VentReactor });
            availableAP--;
        }
        else if (needsToMove && availableAP >= 1)
        {
            var direction = frame.CurrentRange > optimalRange
                ? MovementDirection.Close
                : MovementDirection.PullBack;

            actions.Actions.Add(new PlannedAction
            {
                Action = CombatAction.Move,
                MoveDirection = direction
            });
            availableAP--;
        }

        // Fire weapon groups with remaining AP
        FillRemainingAPWithFire(frame, actions, ref availableAP);
    }

    /// <summary>
    /// Fills remaining AP slots with fire actions, choosing different weapon groups
    /// to maximize damage output across the round
    /// </summary>
    private void FillRemainingAPWithFire(CombatFrame frame, FrameActions actions, ref int availableAP)
    {
        var usedGroups = new HashSet<int>();

        while (availableAP >= 1)
        {
            var bestGroup = SelectBestWeaponGroup(frame, usedGroups);
            if (bestGroup < 0) break;

            actions.Actions.Add(new PlannedAction
            {
                Action = CombatAction.FireGroup,
                WeaponGroupId = bestGroup
            });
            usedGroups.Add(bestGroup);
            availableAP--;
        }
    }

    /// <summary>
    /// Selects the best weapon group to fire based on energy cost and expected damage.
    /// Excludes groups already used this round to avoid double-firing the same group.
    /// </summary>
    private int SelectBestWeaponGroup(CombatFrame frame, HashSet<int>? excludeGroups = null)
    {
        int bestGroup = -1;
        float bestScore = -1;

        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            if (excludeGroups != null && excludeGroups.Contains(groupId)) continue;

            var functionalWeapons = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!functionalWeapons.Any()) continue;

            int totalDamage = functionalWeapons.Sum(w => w.Damage);
            int totalEnergyCost = functionalWeapons.Sum(w => w.EnergyCost);
            bool hasAmmo = functionalWeapons.All(w =>
                w.AmmoPerShot == 0 || frame.AmmoByType.GetValueOrDefault(w.AmmoType, 0) >= w.AmmoPerShot);

            if (!hasAmmo) continue;

            // Score: damage per energy, with a baseline for ammo weapons (which cost no energy)
            float score = totalEnergyCost > 0 ? (float)totalDamage / totalEnergyCost : totalDamage * 2;

            // Bonus for range-appropriate weapons
            foreach (var weapon in functionalWeapons)
            {
                int rangeBonus = _positioning.GetRangeAccuracyModifier(weapon, frame.CurrentRange);
                score += rangeBonus * 0.5f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestGroup = groupId;
            }
        }

        return bestGroup;
    }

    /// <summary>
    /// Selects the best target based on priority
    /// </summary>
    public CombatFrame SelectTarget(CombatFrame attacker, List<CombatFrame> targets, TargetPriority priority)
    {
        if (!targets.Any())
            throw new InvalidOperationException("No targets available");

        return priority switch
        {
            TargetPriority.FocusFire => SelectFocusFireTarget(targets),
            TargetPriority.SpreadDamage => SelectSpreadDamageTarget(targets),
            TargetPriority.ThreatPriority => SelectThreatPriorityTarget(targets),
            TargetPriority.Opportunity => SelectOpportunityTarget(attacker, targets),
            _ => targets.First()
        };
    }

    /// <summary>
    /// Determines optimal range band based on weapon loadout
    /// </summary>
    public RangeBand DetermineOptimalRange(CombatFrame frame, TacticalOrders orders)
    {
        var rangeScores = new Dictionary<RangeBand, int>
        {
            { RangeBand.Short, 0 },
            { RangeBand.Medium, 0 },
            { RangeBand.Long, 0 }
        };

        foreach (var weapon in frame.FunctionalWeapons)
        {
            var band = weapon.RangeClass switch
            {
                "Short" => RangeBand.Short,
                "Medium" => RangeBand.Medium,
                "Long" => RangeBand.Long,
                _ => RangeBand.Medium
            };
            rangeScores[band] += weapon.Damage;
        }

        var preferred = rangeScores.OrderByDescending(kvp => kvp.Value).First().Key;

        // Stance shifts the preference
        return orders.Stance switch
        {
            Stance.Aggressive when preferred > RangeBand.PointBlank =>
                (RangeBand)((int)preferred - 1),
            Stance.Defensive when preferred < RangeBand.Long =>
                (RangeBand)((int)preferred + 1),
            _ => preferred
        };
    }

    /// <summary>
    /// Determines if forces should withdraw based on threshold
    /// </summary>
    public bool ShouldWithdraw(List<CombatFrame> friendlyFrames, WithdrawalThreshold threshold)
    {
        if (threshold == WithdrawalThreshold.FightToEnd)
            return false;

        var totalFrames = friendlyFrames.Count;
        var activeFrames = friendlyFrames.Count(f => !f.IsDestroyed);
        var damagedFrames = friendlyFrames.Count(f => !f.IsDestroyed && f.ArmorPercent < 50);

        float lossRatio = 1.0f - ((float)activeFrames / totalFrames);
        float damageRatio = (float)damagedFrames / Math.Max(activeFrames, 1);

        return threshold switch
        {
            WithdrawalThreshold.RetreatAt50 => lossRatio >= 0.5f || damageRatio >= 0.5f,
            WithdrawalThreshold.RetreatAt25 => lossRatio >= 0.25f || damageRatio >= 0.75f,
            _ => false
        };
    }

    #region Target Selection

    private CombatFrame SelectFocusFireTarget(List<CombatFrame> targets)
    {
        // Pick the most damaged enemy that's still a threat
        return targets
            .OrderBy(t => t.TotalArmorRemaining)
            .ThenByDescending(t => t.TotalArmorMax)
            .First();
    }

    private CombatFrame SelectSpreadDamageTarget(List<CombatFrame> targets)
    {
        // Engage the least damaged target
        return targets.OrderByDescending(t => t.ArmorPercent).First();
    }

    private CombatFrame SelectThreatPriorityTarget(List<CombatFrame> targets)
    {
        return targets.OrderByDescending(t => CalculateThreatScore(t)).First();
    }

    private CombatFrame SelectOpportunityTarget(CombatFrame attacker, List<CombatFrame> targets)
    {
        return targets.OrderByDescending(t => CalculateOpportunityScore(t, attacker)).First();
    }

    private int CalculateThreatScore(CombatFrame target)
    {
        int score = target.Class switch
        {
            "Assault" => 40,
            "Heavy" => 30,
            "Medium" => 20,
            "Light" => 10,
            _ => 0
        };

        score += target.TotalArmorRemaining / 10;
        score += target.FunctionalWeapons.Sum(w => w.Damage);
        return score;
    }

    private int CalculateOpportunityScore(CombatFrame target, CombatFrame attacker)
    {
        int score = (int)((1.0f - target.ArmorPercent / 100f) * 100);

        // Bonus if we can likely destroy a location
        int totalDamage = attacker.FunctionalWeapons.Sum(w => w.Damage);
        if (totalDamage >= target.TotalArmorRemaining)
            score += 50;

        if (target.IsShutDown)
            score += 30;

        return score;
    }

    #endregion
}
