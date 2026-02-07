using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Manages the action point economy for combat frames
/// </summary>
public class ActionSystem
{
    /// <summary>
    /// AP costs for each action type
    /// </summary>
    public static int GetActionCost(CombatAction action)
    {
        return action switch
        {
            CombatAction.Move => 1,
            CombatAction.FireGroup => 1,
            CombatAction.Brace => 1,
            CombatAction.CalledShot => 2,
            CombatAction.Overwatch => 1,
            CombatAction.VentReactor => 1,
            CombatAction.Sprint => 2,
            _ => 1
        };
    }

    /// <summary>
    /// Refreshes action points at the start of a round
    /// </summary>
    public void RefreshActionPoints(CombatFrame frame)
    {
        if (frame.IsShutDown)
        {
            frame.ActionPoints = 0;
            return;
        }

        frame.MaxActionPoints = frame.HasGyroHit ? 1 : 2;
        frame.ActionPoints = frame.MaxActionPoints;

        // Clear round-start flags
        frame.IsBracing = false;
        frame.IsOnOverwatch = false;
    }

    /// <summary>
    /// Checks if a frame can perform a given action
    /// </summary>
    public bool CanPerformAction(CombatFrame frame, PlannedAction action, ReactorSystem reactorSystem)
    {
        if (frame.IsDestroyed || frame.IsShutDown)
            return false;

        int cost = GetActionCost(action.Action);
        if (frame.ActionPoints < cost)
            return false;

        switch (action.Action)
        {
            case CombatAction.Move:
                // Must have energy to move and a valid direction
                if (action.MoveDirection == null || action.MoveDirection == MovementDirection.Hold)
                    return false;
                // Can't move past range band limits
                if (action.MoveDirection == MovementDirection.Close && frame.CurrentRange == RangeBand.PointBlank)
                    return false;
                if (action.MoveDirection == MovementDirection.PullBack && frame.CurrentRange == RangeBand.Long)
                    return false;
                // Can't move with destroyed legs
                if (frame.DestroyedLocations.Contains(HitLocation.Legs))
                    return false;
                return true;

            case CombatAction.FireGroup:
                if (action.WeaponGroupId == null)
                    return false;
                if (!frame.WeaponGroups.TryGetValue(action.WeaponGroupId.Value, out var weapons))
                    return false;
                // Need at least one functional weapon in the group
                return weapons.Any(w => !w.IsDestroyed);

            case CombatAction.CalledShot:
                if (action.WeaponGroupId == null || action.CalledShotLocation == null)
                    return false;
                if (!frame.WeaponGroups.TryGetValue(action.WeaponGroupId.Value, out var calledWeapons))
                    return false;
                return calledWeapons.Any(w => !w.IsDestroyed);

            case CombatAction.Sprint:
                if (frame.DestroyedLocations.Contains(HitLocation.Legs))
                    return false;
                if (action.MoveDirection == null || action.MoveDirection == MovementDirection.Hold)
                    return false;
                return true;

            case CombatAction.Brace:
            case CombatAction.Overwatch:
            case CombatAction.VentReactor:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Consumes action points for an action
    /// </summary>
    public void ConsumeActionPoints(CombatFrame frame, CombatAction action)
    {
        frame.ActionPoints -= GetActionCost(action);
    }

    /// <summary>
    /// Gets all valid actions a frame can currently perform
    /// </summary>
    public List<CombatAction> GetAvailableActions(CombatFrame frame)
    {
        var available = new List<CombatAction>();

        if (frame.IsDestroyed || frame.IsShutDown || frame.ActionPoints <= 0)
            return available;

        // 1 AP actions
        if (frame.ActionPoints >= 1)
        {
            if (!frame.DestroyedLocations.Contains(HitLocation.Legs))
                available.Add(CombatAction.Move);

            if (frame.WeaponGroups.Any(g => g.Value.Any(w => !w.IsDestroyed)))
                available.Add(CombatAction.FireGroup);

            available.Add(CombatAction.Brace);
            available.Add(CombatAction.Overwatch);

            if (frame.ReactorStress > 0)
                available.Add(CombatAction.VentReactor);
        }

        // 2 AP actions
        if (frame.ActionPoints >= 2)
        {
            if (!frame.DestroyedLocations.Contains(HitLocation.Legs))
                available.Add(CombatAction.Sprint);

            if (frame.WeaponGroups.Any(g => g.Value.Any(w => !w.IsDestroyed)))
                available.Add(CombatAction.CalledShot);
        }

        return available;
    }
}
