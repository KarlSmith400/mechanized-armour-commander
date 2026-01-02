using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Handles positional tracking and movement calculations for combat frames
/// </summary>
public class PositioningSystem
{
    private const int DefaultDeploymentDistance = 10;

    /// <summary>
    /// Initialize positions for both teams at combat start
    /// Team 1 deploys on left (negative positions), Team 2 on right (positive positions)
    /// </summary>
    public void InitializePositions(
        List<CombatFrame> team1Frames,
        List<CombatFrame> team2Frames,
        Formation team1Formation,
        Formation team2Formation)
    {
        DeployTeam(team1Frames, -DefaultDeploymentDistance, team1Formation);
        DeployTeam(team2Frames, DefaultDeploymentDistance, team2Formation);
    }

    /// <summary>
    /// Deploy a team at their starting position with formation offsets
    /// </summary>
    private void DeployTeam(List<CombatFrame> frames, int basePosition, Formation formation)
    {
        var activeFrames = frames.Where(f => !f.IsDestroyed).ToList();

        for (int i = 0; i < activeFrames.Count; i++)
        {
            var frame = activeFrames[i];
            int offset = CalculateFormationOffset(i, activeFrames.Count, formation, frame.Class);

            frame.StartPosition = basePosition;
            frame.Position = basePosition + offset;
        }
    }

    /// <summary>
    /// Calculate formation offset for a specific frame
    /// </summary>
    private int CalculateFormationOffset(int index, int totalFrames, Formation formation, string frameClass)
    {
        return formation switch
        {
            Formation.Tight => 0, // All frames at same position
            Formation.Spread => (index - totalFrames / 2) * 2, // Spread 2 units apart
            Formation.Flanking => CalculateFlankingOffset(index, frameClass),
            _ => 0
        };
    }

    /// <summary>
    /// Calculate flanking formation - lights/mediums flank, heavies/assaults center
    /// </summary>
    private int CalculateFlankingOffset(int index, string frameClass)
    {
        return frameClass switch
        {
            "Light" => index % 2 == 0 ? -4 : 4,  // Lights on flanks
            "Medium" => index % 2 == 0 ? -3 : 3, // Mediums near flanks
            "Heavy" => 0,                         // Heavies center
            "Assault" => 0,                       // Assaults center
            _ => 0
        };
    }

    /// <summary>
    /// Move all frames based on their stance and speed
    /// </summary>
    public void ProcessMovement(
        List<CombatFrame> team1Frames,
        List<CombatFrame> team2Frames,
        TacticalOrders team1Orders,
        TacticalOrders team2Orders)
    {
        // Team 1 moves toward positive (right)
        MoveTeam(team1Frames, team1Orders, 1);

        // Team 2 moves toward negative (left)
        MoveTeam(team2Frames, team2Orders, -1);
    }

    /// <summary>
    /// Move a team based on stance
    /// </summary>
    private void MoveTeam(List<CombatFrame> frames, TacticalOrders orders, int direction)
    {
        foreach (var frame in frames.Where(f => !f.IsDestroyed))
        {
            int movementAmount = CalculateMovement(frame, orders.Stance);
            frame.Position += movementAmount * direction;
        }
    }

    /// <summary>
    /// Calculate movement distance based on frame speed and stance
    /// </summary>
    private int CalculateMovement(CombatFrame frame, Stance stance)
    {
        double speedMultiplier = stance switch
        {
            Stance.Aggressive => 0.50,  // 50% of speed - close quickly
            Stance.Balanced => 0.25,    // 25% of speed - cautious advance
            Stance.Defensive => 0.33,   // 33% of speed - maintain distance
            _ => 0.25
        };

        return (int)(frame.Speed * speedMultiplier);
    }

    /// <summary>
    /// Calculate distance between two frames
    /// </summary>
    public int GetDistance(CombatFrame frame1, CombatFrame frame2)
    {
        return Math.Abs(frame1.Position - frame2.Position);
    }

    /// <summary>
    /// Determine range band based on actual distance
    /// </summary>
    public string GetRangeBand(int distance)
    {
        return distance switch
        {
            <= 5 => "Short",
            <= 15 => "Medium",
            _ => "Long"
        };
    }

    /// <summary>
    /// Calculate range modifier for weapon accuracy
    /// Weapons get bonus at optimal range, penalty at non-optimal range
    /// </summary>
    public int GetRangeAccuracyModifier(EquippedWeapon weapon, string actualRange)
    {
        if (weapon.RangeClass == actualRange)
            return 10; // +10% at optimal range

        // Calculate penalty for range mismatch
        return (weapon.RangeClass, actualRange) switch
        {
            ("Short", "Medium") => -5,
            ("Short", "Long") => -15,
            ("Medium", "Short") => -5,
            ("Medium", "Long") => -5,
            ("Long", "Short") => -15,
            ("Long", "Medium") => -5,
            _ => 0
        };
    }

    /// <summary>
    /// Get average distance between two teams
    /// </summary>
    public int GetAverageDistance(List<CombatFrame> team1, List<CombatFrame> team2)
    {
        var activeTeam1 = team1.Where(f => !f.IsDestroyed).ToList();
        var activeTeam2 = team2.Where(f => !f.IsDestroyed).ToList();

        if (!activeTeam1.Any() || !activeTeam2.Any())
            return 0;

        int totalDistance = 0;
        int count = 0;

        foreach (var frame1 in activeTeam1)
        {
            foreach (var frame2 in activeTeam2)
            {
                totalDistance += GetDistance(frame1, frame2);
                count++;
            }
        }

        return count > 0 ? totalDistance / count : 0;
    }
}
