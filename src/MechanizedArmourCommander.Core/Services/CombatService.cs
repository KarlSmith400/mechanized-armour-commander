using MechanizedArmourCommander.Core.Combat;
using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Services;

/// <summary>
/// High-level service for managing combat operations
/// </summary>
public class CombatService
{
    private readonly CombatEngine _engine;

    public CombatService()
    {
        _engine = new CombatEngine();
    }

    /// <summary>
    /// Initiates and resolves a combat encounter
    /// </summary>
    public CombatLog ExecuteCombat(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders? playerOrders = null,
        TacticalOrders? enemyOrders = null)
    {
        // Use default orders if none provided
        playerOrders ??= new TacticalOrders();
        enemyOrders ??= new TacticalOrders();

        // Validate frames
        if (!playerFrames.Any())
            throw new InvalidOperationException("Player must have at least one frame");

        if (!enemyFrames.Any())
            throw new InvalidOperationException("Combat requires at least one enemy");

        // Execute combat
        return _engine.ResolveCombat(playerFrames, enemyFrames, playerOrders, enemyOrders);
    }

    /// <summary>
    /// Generates a formatted text output of the combat log
    /// </summary>
    public string FormatCombatLog(CombatLog log)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine("=== COMBAT ENGAGEMENT ===");
        output.AppendLine($"Started: {log.Timestamp:yyyy-MM-dd HH:mm:ss}");
        output.AppendLine();

        foreach (var round in log.Rounds)
        {
            output.AppendLine($"=== ROUND {round.RoundNumber} ===");

            foreach (var evt in round.Events)
            {
                output.AppendLine(FormatEvent(evt));
            }

            output.AppendLine();
        }

        output.AppendLine($"=== COMBAT RESULT: {log.Result.ToString().ToUpper()} ===");
        output.AppendLine($"Total Rounds: {log.TotalRounds}");

        return output.ToString();
    }

    private string FormatEvent(CombatEvent evt)
    {
        return evt.Type switch
        {
            CombatEventType.Movement => $"  {evt.Message}",
            CombatEventType.Hit => $"  > {evt.Message}",
            CombatEventType.Critical => $"  > ** {evt.Message} **",
            CombatEventType.Miss => $"  > {evt.Message}",
            CombatEventType.FrameDestroyed => $"  >> {evt.Message}",
            _ => $"  {evt.Message}"
        };
    }
}
