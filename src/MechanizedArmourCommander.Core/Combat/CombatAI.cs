using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// AI decision-making system for auto-resolved combat
/// </summary>
public class CombatAI
{
    private readonly Random _random = new();

    /// <summary>
    /// Selects the best target based on tactical orders and combat situation
    /// </summary>
    public CombatFrame SelectTarget(
        CombatFrame attacker,
        List<CombatFrame> availableTargets,
        TargetPriority priority)
    {
        if (!availableTargets.Any())
            throw new InvalidOperationException("No targets available");

        return priority switch
        {
            TargetPriority.FocusFire => SelectFocusFireTarget(attacker, availableTargets),
            TargetPriority.SpreadDamage => SelectSpreadDamageTarget(attacker, availableTargets),
            TargetPriority.ThreatPriority => SelectThreatPriorityTarget(attacker, availableTargets),
            TargetPriority.Opportunity => SelectOpportunityTarget(attacker, availableTargets),
            _ => availableTargets.First()
        };
    }

    /// <summary>
    /// Determines optimal range based on stance and weapon loadout
    /// </summary>
    public string DetermineOptimalRange(CombatFrame frame, TacticalOrders orders)
    {
        // Analyze weapons to determine preferred range
        var rangeScores = new Dictionary<string, int>
        {
            { "Short", 0 },
            { "Medium", 0 },
            { "Long", 0 }
        };

        foreach (var weapon in frame.Weapons)
        {
            rangeScores[weapon.RangeClass] += weapon.Damage;
        }

        var preferredRange = rangeScores.OrderByDescending(kvp => kvp.Value).First().Key;

        // Modify based on stance
        return orders.Stance switch
        {
            Stance.Aggressive => ShiftRangeCloser(preferredRange),
            Stance.Defensive => ShiftRangeFarther(preferredRange),
            _ => preferredRange
        };
    }

    /// <summary>
    /// Calculates movement modifier based on stance and situation
    /// </summary>
    public int CalculateMovementModifier(CombatFrame frame, TacticalOrders orders, List<CombatFrame> enemies)
    {
        int modifier = 0;

        // Base movement on stance
        modifier += orders.Stance switch
        {
            Stance.Aggressive => frame.Speed / 2,  // Move quickly to close
            Stance.Defensive => frame.Speed / 3,   // Move cautiously
            _ => frame.Speed / 4                    // Balanced movement
        };

        // Adjust based on frame condition
        float armorPercent = (float)frame.CurrentArmor / frame.MaxArmor;
        if (armorPercent < 0.3f && orders.Stance != Stance.Aggressive)
        {
            modifier += 2; // Move more when damaged to avoid hits
        }

        // Adjust based on formation
        modifier += orders.Formation switch
        {
            Formation.Spread => 1,      // Spread out increases evasion
            Formation.Flanking => 2,    // Flanking requires more movement
            _ => 0
        };

        return modifier;
    }

    /// <summary>
    /// Determines if unit should attempt to withdraw based on tactical orders
    /// </summary>
    public bool ShouldWithdraw(
        List<CombatFrame> friendlyFrames,
        WithdrawalThreshold threshold)
    {
        if (threshold == WithdrawalThreshold.FightToEnd)
            return false;

        var totalFriendlyFrames = friendlyFrames.Count;
        var activeFriendlyFrames = friendlyFrames.Count(f => !f.IsDestroyed);
        var damagedFrames = friendlyFrames.Count(f => !f.IsDestroyed &&
            (float)f.CurrentArmor / f.MaxArmor < 0.5f);

        float lossRatio = 1.0f - ((float)activeFriendlyFrames / totalFriendlyFrames);
        float damageRatio = (float)damagedFrames / Math.Max(activeFriendlyFrames, 1);

        return threshold switch
        {
            WithdrawalThreshold.RetreatAt50 => lossRatio >= 0.5f || damageRatio >= 0.5f,
            WithdrawalThreshold.RetreatAt25 => lossRatio >= 0.25f || damageRatio >= 0.75f,
            _ => false
        };
    }

    /// <summary>
    /// Calculates weapon priority score for target selection
    /// </summary>
    public int CalculateWeaponEffectiveness(EquippedWeapon weapon, CombatFrame target, string currentRange)
    {
        int score = weapon.Damage;

        // Bonus for optimal range
        if (weapon.RangeClass == currentRange)
            score = (int)(score * 1.5f);

        // Heat weapons less valuable against overheated targets
        if (weapon.HeatGeneration > 5 && target.IsOverheating)
            score = (int)(score * 0.7f);

        // Ammo weapons less valuable when low on ammo
        if (weapon.AmmoConsumption > 0)
        {
            // Estimate remaining shots (simplified)
            score = (int)(score * 0.9f);
        }

        return score;
    }

    /// <summary>
    /// Determines positioning based on formation orders
    /// </summary>
    public string DeterminePosition(
        CombatFrame frame,
        List<CombatFrame> friendlyFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders orders)
    {
        return orders.Formation switch
        {
            Formation.Tight => "Center formation",
            Formation.Spread => frame.Class == "Light" ? "Flanking position" : "Dispersed line",
            Formation.Flanking => DetermineFlanking(frame, friendlyFrames),
            _ => "Standard position"
        };
    }

    #region Target Selection Methods

    private CombatFrame SelectFocusFireTarget(CombatFrame attacker, List<CombatFrame> targets)
    {
        // Focus fire: pick the most damaged enemy that's still dangerous
        var mostDamaged = targets
            .OrderBy(t => t.CurrentArmor)
            .ThenByDescending(t => t.MaxArmor) // Prefer finishing off bigger threats
            .First();

        return mostDamaged;
    }

    private CombatFrame SelectSpreadDamageTarget(CombatFrame attacker, List<CombatFrame> targets)
    {
        // Spread damage: try to engage different targets
        // Pick the target with the most remaining armor (least damaged)
        return targets.OrderByDescending(t => (float)t.CurrentArmor / t.MaxArmor).First();
    }

    private CombatFrame SelectThreatPriorityTarget(CombatFrame attacker, List<CombatFrame> targets)
    {
        // Calculate threat score for each target
        var targetScores = targets.Select(t => new
        {
            Target = t,
            ThreatScore = CalculateThreatScore(t)
        }).OrderByDescending(x => x.ThreatScore);

        return targetScores.First().Target;
    }

    private CombatFrame SelectOpportunityTarget(CombatFrame attacker, List<CombatFrame> targets)
    {
        // Opportunity: finish off weakened targets
        var targetScores = targets.Select(t => new
        {
            Target = t,
            OpportunityScore = CalculateOpportunityScore(t, attacker)
        }).OrderByDescending(x => x.OpportunityScore);

        return targetScores.First().Target;
    }

    private int CalculateThreatScore(CombatFrame target)
    {
        int score = 0;

        // Heavier frames are bigger threats
        score += target.Class switch
        {
            "Assault" => 40,
            "Heavy" => 30,
            "Medium" => 20,
            "Light" => 10,
            _ => 0
        };

        // More armor = more threat
        score += target.CurrentArmor / 10;

        // Weapon firepower
        score += target.Weapons.Sum(w => w.Damage);

        return score;
    }

    private int CalculateOpportunityScore(CombatFrame target, CombatFrame attacker)
    {
        int score = 0;

        // Lower armor = better opportunity
        float armorPercent = (float)target.CurrentArmor / target.MaxArmor;
        score += (int)((1.0f - armorPercent) * 100);

        // Can we one-shot it?
        int totalDamage = attacker.Weapons.Sum(w => w.Damage);
        if (totalDamage >= target.CurrentArmor)
        {
            score += 50; // Big bonus for potential kill
        }

        // Overheated targets are easier
        if (target.IsOverheating)
            score += 20;

        return score;
    }

    #endregion

    #region Helper Methods

    private string ShiftRangeCloser(string currentRange)
    {
        return currentRange switch
        {
            "Long" => "Medium",
            "Medium" => "Short",
            _ => currentRange
        };
    }

    private string ShiftRangeFarther(string currentRange)
    {
        return currentRange switch
        {
            "Short" => "Medium",
            "Medium" => "Long",
            _ => currentRange
        };
    }

    private string DetermineFlanking(CombatFrame frame, List<CombatFrame> friendlyFrames)
    {
        // Light frames flank, heavy frames center
        if (frame.Class == "Light" || frame.Class == "Medium")
            return "Flanking maneuver";
        else
            return "Center support";
    }

    #endregion
}
