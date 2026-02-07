using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Manages reactor energy allocation and overload stress
/// </summary>
public class ReactorSystem
{
    private readonly Random _random = new();

    /// <summary>
    /// Refreshes energy to full reactor output at the start of each round
    /// </summary>
    public void RefreshEnergy(CombatFrame frame)
    {
        if (frame.IsShutDown)
        {
            // Shutdown recovery: skip this round, reduce stress by 50%
            frame.ReactorStress = frame.ReactorStress / 2;
            frame.IsShutDown = false;
            frame.CurrentEnergy = 0; // No energy this round (still recovering)
            return;
        }

        frame.CurrentEnergy = frame.EffectiveReactorOutput;
    }

    /// <summary>
    /// Attempts to consume energy. Returns true if successful.
    /// Allows overload up to 150% of effective reactor output.
    /// </summary>
    public bool ConsumeEnergy(CombatFrame frame, int amount)
    {
        int maxAllowed = (int)(frame.EffectiveReactorOutput * 1.5);
        int energyUsedThisRound = frame.EffectiveReactorOutput - frame.CurrentEnergy;

        if (energyUsedThisRound + amount > maxAllowed)
            return false; // Would exceed 150% overload limit

        frame.CurrentEnergy -= amount;
        return true;
    }

    /// <summary>
    /// Checks if consuming additional energy would require overloading
    /// </summary>
    public bool WouldOverload(CombatFrame frame, int additionalEnergy)
    {
        return frame.CurrentEnergy - additionalEnergy < 0;
    }

    /// <summary>
    /// Returns the total energy cost to fire a weapon group
    /// </summary>
    public int GetWeaponGroupEnergyCost(CombatFrame frame, int groupId)
    {
        if (!frame.WeaponGroups.TryGetValue(groupId, out var weapons))
            return 0;

        return weapons.Where(w => !w.IsDestroyed).Sum(w => w.EnergyCost);
    }

    /// <summary>
    /// Processes overload stress at end of round.
    /// Energy used beyond 100% reactor output adds stress.
    /// </summary>
    public List<CombatEvent> ProcessEndOfRound(CombatFrame frame)
    {
        var events = new List<CombatEvent>();
        int effectiveOutput = frame.EffectiveReactorOutput;

        // Calculate how much energy was used beyond normal capacity
        int energyOveruse = Math.Max(0, -frame.CurrentEnergy);
        if (energyOveruse > 0)
        {
            frame.ReactorStress += energyOveruse;
            events.Add(new CombatEvent
            {
                Type = CombatEventType.ReactorOverload,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} reactor overloaded! +{energyOveruse} stress (total: {frame.ReactorStress})"
            });
        }

        // Check for shutdown
        if (frame.ReactorStress >= (int)(effectiveOutput * 1.5))
        {
            // Automatic shutdown + permanent damage
            frame.IsShutDown = true;
            frame.DamagedComponents.Add(new ComponentDamage
            {
                Location = HitLocation.CenterTorso,
                Type = ComponentDamageType.ReactorHit,
                Description = "Reactor emergency shutdown - permanent damage"
            });
            events.Add(new CombatEvent
            {
                Type = CombatEventType.ReactorShutdown,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} REACTOR EMERGENCY SHUTDOWN! Permanent reactor damage!"
            });
        }
        else if (frame.ReactorStress >= effectiveOutput)
        {
            // 25% chance of shutdown
            if (_random.Next(100) < 25)
            {
                frame.IsShutDown = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.ReactorShutdown,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} REACTOR SHUTDOWN from stress overload!"
                });
            }
        }

        // Natural stress dissipation (small amount per round)
        if (frame.ReactorStress > 0 && !frame.IsShutDown)
        {
            int dissipation = Math.Max(1, effectiveOutput / 10);
            frame.ReactorStress = Math.Max(0, frame.ReactorStress - dissipation);
        }

        return events;
    }

    /// <summary>
    /// Vents reactor stress (action: costs 1 AP)
    /// </summary>
    public CombatEvent VentReactor(CombatFrame frame)
    {
        int ventAmount = Math.Max(2, frame.EffectiveReactorOutput / 4);
        frame.ReactorStress = Math.Max(0, frame.ReactorStress - ventAmount);

        return new CombatEvent
        {
            Type = CombatEventType.ReactorVent,
            AttackerId = frame.InstanceId,
            AttackerName = frame.CustomName,
            Message = $"{frame.CustomName} vents reactor. Stress reduced by {ventAmount} (now {frame.ReactorStress})"
        };
    }
}
