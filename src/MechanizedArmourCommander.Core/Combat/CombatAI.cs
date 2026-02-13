using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// AI decision-making system for hex grid combat
/// </summary>
public class CombatAI
{
    private readonly Random _random = new();
    private readonly PositioningSystem _positioning = new();

    /// <summary>
    /// Generates AI actions for a frame on the hex grid
    /// </summary>
    public FrameActions GenerateHexActions(CombatFrame frame, List<CombatFrame> enemies,
        TacticalOrders orders, ActionSystem actionSystem, HexGrid grid)
    {
        var actions = new FrameActions();

        if (frame.IsDestroyed || frame.IsShutDown)
            return actions;

        var activeEnemies = enemies.Where(e => !e.IsDestroyed).ToList();
        if (!activeEnemies.Any())
            return actions;

        var target = SelectTarget(frame, activeEnemies, orders.TargetPriority);
        actions.FocusTargetId = target.InstanceId;

        int optimalDist = DetermineOptimalDistance(frame, orders);

        switch (orders.Stance)
        {
            case Stance.Aggressive:
                PlanAggressiveHexActions(frame, actions, target, optimalDist, actionSystem, grid);
                break;
            case Stance.Defensive:
                PlanDefensiveHexActions(frame, actions, target, optimalDist, actionSystem, grid);
                break;
            default:
                PlanBalancedHexActions(frame, actions, target, optimalDist, actionSystem, grid);
                break;
        }

        return actions;
    }

    private void PlanAggressiveHexActions(CombatFrame frame, FrameActions actions,
        CombatFrame target, int optimalDist, ActionSystem actionSystem, HexGrid grid)
    {
        int availableAP = frame.MaxActionPoints;

        int currentDist = HexCoord.Distance(frame.HexPosition, target.HexPosition);

        // Move toward target if not at optimal distance
        if (currentDist > optimalDist + 1 && availableAP >= 1
            && !frame.DestroyedLocations.Contains(HitLocation.Legs))
        {
            var moveDest = ChooseMoveDestination(frame, target, grid, optimalDist, false);
            if (moveDest.HasValue)
            {
                actions.Actions.Add(new PlannedAction
                {
                    Action = CombatAction.Move,
                    TargetHex = moveDest.Value
                });
                availableAP--;
            }
        }

        // Fire with remaining AP
        FillRemainingAPWithFire(frame, actions, ref availableAP, target, grid);
    }

    private void PlanDefensiveHexActions(CombatFrame frame, FrameActions actions,
        CombatFrame target, int optimalDist, ActionSystem actionSystem, HexGrid grid)
    {
        int availableAP = frame.MaxActionPoints;
        bool heavilyDamaged = frame.ArmorPercent < 40;

        int currentDist = HexCoord.Distance(frame.HexPosition, target.HexPosition);

        // Brace if heavily damaged
        if (heavilyDamaged && availableAP >= 1)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.Brace });
            availableAP--;
        }
        // Pull back if too close
        else if (currentDist < optimalDist - 1 && availableAP >= 1
            && !frame.DestroyedLocations.Contains(HitLocation.Legs))
        {
            var moveDest = ChooseMoveDestination(frame, target, grid, optimalDist, false);
            if (moveDest.HasValue)
            {
                actions.Actions.Add(new PlannedAction
                {
                    Action = CombatAction.Move,
                    TargetHex = moveDest.Value
                });
                availableAP--;
            }
        }

        // Vent reactor if stressed
        if (frame.ReactorStress > frame.EffectiveReactorOutput / 2 && availableAP >= 1)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.VentReactor });
            availableAP--;
        }

        FillRemainingAPWithFire(frame, actions, ref availableAP, target, grid);
    }

    private void PlanBalancedHexActions(CombatFrame frame, FrameActions actions,
        CombatFrame target, int optimalDist, ActionSystem actionSystem, HexGrid grid)
    {
        int availableAP = frame.MaxActionPoints;
        int currentDist = HexCoord.Distance(frame.HexPosition, target.HexPosition);

        // Vent if reactor stress is high
        if (frame.ReactorStress >= frame.EffectiveReactorOutput * 0.75)
        {
            actions.Actions.Add(new PlannedAction { Action = CombatAction.VentReactor });
            availableAP--;
        }
        // Move toward optimal distance if needed
        else if (Math.Abs(currentDist - optimalDist) > 1 && availableAP >= 1
            && !frame.DestroyedLocations.Contains(HitLocation.Legs))
        {
            var moveDest = ChooseMoveDestination(frame, target, grid, optimalDist, false);
            if (moveDest.HasValue)
            {
                actions.Actions.Add(new PlannedAction
                {
                    Action = CombatAction.Move,
                    TargetHex = moveDest.Value
                });
                availableAP--;
            }
        }

        FillRemainingAPWithFire(frame, actions, ref availableAP, target, grid);
    }

    /// <summary>
    /// Choose the best hex to move to, aiming for optimal distance from target
    /// </summary>
    public HexCoord? ChooseMoveDestination(CombatFrame frame, CombatFrame target,
        HexGrid grid, int optimalDistance, bool isSprint)
    {
        int maxRange = isSprint
            ? PositioningSystem.GetSprintRange(frame)
            : PositioningSystem.GetEffectiveHexMovement(frame);

        if (maxRange <= 0) return null;

        var reachable = HexPathfinding.GetReachableHexes(grid, frame.HexPosition, maxRange);
        if (!reachable.Any()) return null;

        HexCoord? best = null;
        int bestScore = int.MinValue;

        foreach (var hex in reachable)
        {
            int dist = HexCoord.Distance(hex, target.HexPosition);
            int distFromOptimal = Math.Abs(dist - optimalDistance);
            int score = -distFromOptimal * 10;

            // Small bonus for not being adjacent to multiple enemies
            // Small penalty for staying at distance 0 from target
            if (dist == 0) score -= 50;

            if (score > bestScore)
            {
                bestScore = score;
                best = hex;
            }
        }

        return best;
    }

    /// <summary>
    /// Fills remaining AP with fire actions, picking best weapon groups
    /// </summary>
    private void FillRemainingAPWithFire(CombatFrame frame, FrameActions actions,
        ref int availableAP, CombatFrame target, HexGrid grid)
    {
        var usedGroups = new HashSet<int>();
        int hexDistance = HexCoord.Distance(frame.HexPosition, target.HexPosition);

        while (availableAP >= 1)
        {
            var bestGroup = SelectBestWeaponGroup(frame, hexDistance, usedGroups);
            if (bestGroup < 0) break;

            actions.Actions.Add(new PlannedAction
            {
                Action = CombatAction.FireGroup,
                WeaponGroupId = bestGroup,
                TargetFrameId = target.InstanceId
            });
            usedGroups.Add(bestGroup);
            availableAP--;
        }
    }

    /// <summary>
    /// Selects the best weapon group to fire based on hex distance and energy cost
    /// </summary>
    private int SelectBestWeaponGroup(CombatFrame frame, int hexDistance, HashSet<int>? excludeGroups = null)
    {
        int bestGroup = -1;
        float bestScore = -1;

        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            if (excludeGroups != null && excludeGroups.Contains(groupId)) continue;

            var functionalWeapons = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!functionalWeapons.Any()) continue;

            // Check if any weapon in group can reach
            bool anyInRange = functionalWeapons.Any(w =>
                hexDistance <= PositioningSystem.GetWeaponMaxRange(w.RangeClass));
            if (!anyInRange) continue;

            int totalDamage = functionalWeapons.Sum(w => w.Damage);
            int totalEnergyCost = functionalWeapons.Sum(w => w.EnergyCost);
            bool hasAmmo = functionalWeapons.All(w =>
                w.AmmoPerShot == 0 || frame.AmmoByType.GetValueOrDefault(w.AmmoType, 0) >= w.AmmoPerShot);

            if (!hasAmmo) continue;

            float score = totalEnergyCost > 0 ? (float)totalDamage / totalEnergyCost : totalDamage * 2;

            // Bonus for range-appropriate weapons
            foreach (var weapon in functionalWeapons)
            {
                int rangeBonus = _positioning.GetHexRangeAccuracyModifier(weapon, hexDistance);
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
    /// Determines optimal hex distance based on weapon loadout and stance
    /// </summary>
    public int DetermineOptimalDistance(CombatFrame frame, TacticalOrders orders)
    {
        int shortDmg = 0, medDmg = 0, longDmg = 0;
        foreach (var w in frame.FunctionalWeapons)
        {
            switch (w.RangeClass)
            {
                case "Short": shortDmg += w.Damage; break;
                case "Medium": medDmg += w.Damage; break;
                case "Long": longDmg += w.Damage; break;
            }
        }

        int optimalDist;
        if (shortDmg >= medDmg && shortDmg >= longDmg) optimalDist = 3;
        else if (medDmg >= longDmg) optimalDist = 5;
        else optimalDist = 8;

        return orders.Stance switch
        {
            Stance.Aggressive => Math.Max(1, optimalDist - 2),
            Stance.Defensive => optimalDist + 2,
            _ => optimalDist
        };
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
            TargetPriority.FocusFire => targets.OrderBy(t => t.TotalArmorRemaining).ThenByDescending(t => t.TotalArmorMax).First(),
            TargetPriority.SpreadDamage => targets.OrderByDescending(t => t.ArmorPercent).First(),
            TargetPriority.ThreatPriority => targets.OrderByDescending(t => CalculateThreatScore(t)).First(),
            TargetPriority.Opportunity => targets.OrderByDescending(t => CalculateOpportunityScore(t, attacker)).First(),
            _ => targets.First()
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

        int totalDamage = attacker.FunctionalWeapons.Sum(w => w.Damage);
        if (totalDamage >= target.TotalArmorRemaining)
            score += 50;

        if (target.IsShutDown)
            score += 30;

        return score;
    }
}
