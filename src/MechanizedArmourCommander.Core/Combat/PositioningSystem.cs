using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Manages range band positioning for combat frames
/// </summary>
public class PositioningSystem
{
    /// <summary>
    /// Initialize all frames to starting range band
    /// </summary>
    public void InitializeRangeBands(List<CombatFrame> team1Frames, List<CombatFrame> team2Frames,
        RangeBand startingRange = RangeBand.Long)
    {
        foreach (var frame in team1Frames.Concat(team2Frames))
        {
            frame.CurrentRange = startingRange;
        }
    }

    /// <summary>
    /// Moves a frame one range band in the specified direction.
    /// Returns the energy cost consumed.
    /// </summary>
    public int ProcessMovement(CombatFrame frame, MovementDirection direction)
    {
        int energyCost = GetMovementEnergyCost(frame);

        switch (direction)
        {
            case MovementDirection.Close:
                if (frame.CurrentRange > RangeBand.PointBlank)
                    frame.CurrentRange = (RangeBand)((int)frame.CurrentRange - 1);
                break;
            case MovementDirection.PullBack:
                if (frame.CurrentRange < RangeBand.Long)
                    frame.CurrentRange = (RangeBand)((int)frame.CurrentRange + 1);
                // Pulling back costs 50% more energy (retreating under fire)
                energyCost = (int)(energyCost * 1.5);
                break;
            case MovementDirection.Hold:
                energyCost = 0;
                break;
        }

        return energyCost;
    }

    /// <summary>
    /// Gets the movement energy cost for a frame (increased by leg actuator damage)
    /// </summary>
    public int GetMovementEnergyCost(CombatFrame frame)
    {
        int baseCost = frame.MovementEnergyCost;

        // Leg actuator damage increases cost
        int legActuatorHits = frame.DamagedComponents.Count(c =>
            c.Type == ComponentDamageType.ActuatorDamaged && c.Location == HitLocation.Legs);
        baseCost += legActuatorHits * 2;

        return baseCost;
    }

    /// <summary>
    /// Gets the accuracy modifier based on weapon range class and current range band.
    /// Returns a percentage modifier to hit chance.
    /// </summary>
    public int GetRangeAccuracyModifier(EquippedWeapon weapon, RangeBand rangeBand)
    {
        return (weapon.RangeClass, rangeBand) switch
        {
            // Short range weapons
            ("Short", RangeBand.PointBlank) => 5,
            ("Short", RangeBand.Short) => 10,
            ("Short", RangeBand.Medium) => -10,
            ("Short", RangeBand.Long) => -25,

            // Medium range weapons
            ("Medium", RangeBand.PointBlank) => -5,
            ("Medium", RangeBand.Short) => 5,
            ("Medium", RangeBand.Medium) => 10,
            ("Medium", RangeBand.Long) => -10,

            // Long range weapons
            ("Long", RangeBand.PointBlank) => -15,
            ("Long", RangeBand.Short) => -5,
            ("Long", RangeBand.Medium) => 5,
            ("Long", RangeBand.Long) => 10,

            _ => 0
        };
    }

    /// <summary>
    /// Determines the dominant range band across a team (most common range)
    /// </summary>
    public RangeBand GetTeamAverageRange(List<CombatFrame> frames)
    {
        var activeFrames = frames.Where(f => !f.IsDestroyed).ToList();
        if (!activeFrames.Any()) return RangeBand.Long;

        return activeFrames
            .GroupBy(f => f.CurrentRange)
            .OrderByDescending(g => g.Count())
            .First().Key;
    }

    /// <summary>
    /// Formats a range band for display
    /// </summary>
    public static string FormatRangeBand(RangeBand band)
    {
        return band switch
        {
            RangeBand.PointBlank => "Point Blank",
            RangeBand.Short => "Short",
            RangeBand.Medium => "Medium",
            RangeBand.Long => "Long",
            _ => band.ToString()
        };
    }
}
