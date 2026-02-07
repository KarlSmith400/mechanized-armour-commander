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
    /// Initiates and resolves a full combat encounter (auto-resolve)
    /// </summary>
    public CombatLog ExecuteCombat(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        TacticalOrders? playerOrders = null,
        TacticalOrders? enemyOrders = null)
    {
        playerOrders ??= new TacticalOrders();
        enemyOrders ??= new TacticalOrders();

        if (!playerFrames.Any())
            throw new InvalidOperationException("Player must have at least one frame");
        if (!enemyFrames.Any())
            throw new InvalidOperationException("Combat requires at least one enemy");

        return _engine.ResolveCombat(playerFrames, enemyFrames, playerOrders, enemyOrders);
    }

    /// <summary>
    /// Resolves a single round of tactical combat with player decisions
    /// </summary>
    public CombatRound ExecuteRound(
        List<CombatFrame> playerFrames,
        List<CombatFrame> enemyFrames,
        RoundTacticalDecision playerDecisions,
        TacticalOrders playerOrders,
        TacticalOrders enemyOrders,
        int roundNumber)
    {
        // Generate AI decisions for enemy frames
        var enemyDecisions = new RoundTacticalDecision();
        var ai = new CombatAI();
        var actionSystem = new ActionSystem();
        var activePlayerFrames = playerFrames.Where(f => !f.IsDestroyed).ToList();

        foreach (var enemy in enemyFrames.Where(f => !f.IsDestroyed))
        {
            enemyDecisions.FrameOrders[enemy.InstanceId] =
                ai.GenerateActions(enemy, activePlayerFrames, enemyOrders, actionSystem);
        }

        return _engine.ResolveRound(playerFrames, enemyFrames,
            playerDecisions, enemyDecisions,
            playerOrders, enemyOrders, roundNumber);
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

    /// <summary>
    /// Formats a single round's events for display
    /// </summary>
    public string FormatRoundEvents(CombatRound round)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine($"=== ROUND {round.RoundNumber} ===");

        foreach (var evt in round.Events)
        {
            output.AppendLine(FormatEvent(evt));
        }

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
            CombatEventType.ComponentDamage => $"  ! {evt.Message}",
            CombatEventType.AmmoExplosion => $"  !! {evt.Message}",
            CombatEventType.LocationDestroyed => $"  >> {evt.Message}",
            CombatEventType.FrameDestroyed => $"  >>> {evt.Message}",
            CombatEventType.ReactorOverload => $"  ~ {evt.Message}",
            CombatEventType.ReactorShutdown => $"  ~~ {evt.Message}",
            CombatEventType.ReactorVent => $"  ~ {evt.Message}",
            CombatEventType.DamageTransfer => $"  -> {evt.Message}",
            CombatEventType.Brace => $"  [{evt.Message}]",
            CombatEventType.Overwatch => $"  [{evt.Message}]",
            CombatEventType.RoundSummary => $"  --- {evt.Message}",
            _ => $"  {evt.Message}"
        };
    }

    /// <summary>
    /// Formats per-location damage status for a frame
    /// </summary>
    public static string FormatFrameDamageStatus(CombatFrame frame)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine($"{frame.CustomName} ({frame.ChassisDesignation} {frame.ChassisName})");
        output.AppendLine($"  Reactor: {frame.CurrentEnergy}/{frame.EffectiveReactorOutput} energy | Stress: {frame.ReactorStress}");
        output.AppendLine($"  Range: {PositioningSystem.FormatRangeBand(frame.CurrentRange)} | AP: {frame.ActionPoints}/{frame.MaxActionPoints}");

        foreach (HitLocation loc in Enum.GetValues<HitLocation>())
        {
            int armor = frame.Armor.GetValueOrDefault(loc, 0);
            int maxArmor = frame.MaxArmor.GetValueOrDefault(loc, 0);
            int structure = frame.Structure.GetValueOrDefault(loc, 0);
            int maxStructure = frame.MaxStructure.GetValueOrDefault(loc, 0);

            if (maxArmor == 0 && maxStructure == 0) continue;

            string status = frame.DestroyedLocations.Contains(loc) ? " [DESTROYED]" : "";
            output.AppendLine($"  {DamageSystem.FormatLocation(loc)}: A[{armor}/{maxArmor}] S[{structure}/{maxStructure}]{status}");
        }

        return output.ToString();
    }
}
