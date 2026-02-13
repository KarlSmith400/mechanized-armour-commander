using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Manages hex grid positioning, accuracy modifiers, and movement for combat frames
/// </summary>
public class PositioningSystem
{
    /// <summary>
    /// Place all frames on the hex grid in their deployment zones
    /// </summary>
    public void InitializeHexPositions(HexGrid grid, List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames)
    {
        var playerZone = grid.GetDeploymentZone(true, playerFrames.Count);
        for (int i = 0; i < playerFrames.Count && i < playerZone.Count; i++)
        {
            playerFrames[i].HexPosition = playerZone[i];
            grid.PlaceFrame(playerFrames[i].InstanceId, playerZone[i]);
        }

        InitializeEnemyPositions(grid, enemyFrames);
    }

    /// <summary>
    /// Place only enemy frames on the hex grid
    /// </summary>
    public void InitializeEnemyPositions(HexGrid grid, List<CombatFrame> enemyFrames)
    {
        var enemyZone = grid.GetDeploymentZone(false, enemyFrames.Count);
        for (int i = 0; i < enemyFrames.Count && i < enemyZone.Count; i++)
        {
            enemyFrames[i].HexPosition = enemyZone[i];
            grid.PlaceFrame(enemyFrames[i].InstanceId, enemyZone[i]);
        }
    }

    /// <summary>
    /// Gets accuracy modifier based on weapon range class and hex distance.
    /// Returns a percentage modifier to hit chance.
    /// </summary>
    public int GetHexRangeAccuracyModifier(EquippedWeapon weapon, int hexDistance)
    {
        var (optimalMin, optimalMax, maxRange) = GetWeaponRangeProfile(weapon.RangeClass);

        if (hexDistance <= 0) return -100;
        if (hexDistance > maxRange) return -100;

        // At optimal range: +10
        if (hexDistance >= optimalMin && hexDistance <= optimalMax)
            return 10;

        // Closer than optimal: penalty varies by weapon type
        if (hexDistance < optimalMin)
        {
            return weapon.RangeClass switch
            {
                "Short" => 5,
                "Medium" => -5,
                "Long" => -15,
                _ => -5
            };
        }

        // Beyond optimal: linear penalty scaling to -25 at max range
        int distBeyond = hexDistance - optimalMax;
        int rangeBeyond = maxRange - optimalMax;
        if (rangeBeyond <= 0) return -25;
        int penalty = -(distBeyond * 25 / rangeBeyond);
        return penalty;
    }

    /// <summary>
    /// Gets the weapon range profile: (optimalMin, optimalMax, maxRange) in hexes
    /// </summary>
    public static (int optimalMin, int optimalMax, int maxRange) GetWeaponRangeProfile(string rangeClass)
    {
        return rangeClass switch
        {
            "Short" => (2, 4, 7),
            "Medium" => (4, 7, 10),
            "Long" => (7, 10, 14),
            _ => (4, 7, 10)
        };
    }

    /// <summary>
    /// Gets max weapon range in hexes
    /// </summary>
    public static int GetWeaponMaxRange(string rangeClass)
    {
        return rangeClass switch
        {
            "Short" => 7,
            "Medium" => 10,
            "Long" => 14,
            _ => 10
        };
    }

    /// <summary>
    /// Gets the max range across all functional weapons on a frame
    /// </summary>
    public static int GetFrameMaxWeaponRange(CombatFrame frame)
    {
        int maxRange = 0;
        foreach (var weapon in frame.FunctionalWeapons)
        {
            int range = GetWeaponMaxRange(weapon.RangeClass);
            if (range > maxRange) maxRange = range;
        }
        return maxRange;
    }

    /// <summary>
    /// Gets the movement energy cost for a frame (increased by leg actuator damage)
    /// </summary>
    public int GetMovementEnergyCost(CombatFrame frame)
    {
        int baseCost = frame.MovementEnergyCost;

        int legActuatorHits = frame.DamagedComponents.Count(c =>
            c.Type == ComponentDamageType.ActuatorDamaged && c.Location == HitLocation.Legs);
        baseCost += legActuatorHits * 2;

        return baseCost;
    }

    /// <summary>
    /// Gets the effective hex movement range (0 if legs destroyed)
    /// </summary>
    public static int GetEffectiveHexMovement(CombatFrame frame)
    {
        if (frame.DestroyedLocations.Contains(HitLocation.Legs))
            return 0;
        return frame.HexMovement;
    }

    /// <summary>
    /// Gets sprint range (double normal movement)
    /// </summary>
    public static int GetSprintRange(CombatFrame frame)
    {
        if (frame.DestroyedLocations.Contains(HitLocation.Legs))
            return 0;
        return frame.HexMovement * 2;
    }

    /// <summary>
    /// Formats hex distance as a descriptive range string
    /// </summary>
    public static string FormatHexRange(int hexDistance)
    {
        if (hexDistance <= 1) return "Point Blank";
        if (hexDistance <= 4) return "Short";
        if (hexDistance <= 7) return "Medium";
        return "Long";
    }
}
