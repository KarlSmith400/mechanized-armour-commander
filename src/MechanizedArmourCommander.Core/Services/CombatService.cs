using MechanizedArmourCommander.Core.Combat;
using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Services;

/// <summary>
/// High-level service for managing hex grid combat operations
/// </summary>
public class CombatService
{
    private readonly CombatEngine _engine;

    public CombatService()
    {
        _engine = new CombatEngine();
    }

    /// <summary>
    /// Initialize a new combat encounter on a hex grid
    /// </summary>
    public CombatState InitializeCombat(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, MapSize mapSize, string landscape = "Habitable")
    {
        if (!playerFrames.Any())
            throw new InvalidOperationException("Player must have at least one frame");
        if (!enemyFrames.Any())
            throw new InvalidOperationException("Combat requires at least one enemy");

        return _engine.InitializeCombat(playerFrames, enemyFrames, mapSize, landscape);
    }

    /// <summary>
    /// Initialize combat with manual player deployment
    /// </summary>
    public CombatState InitializeCombatForDeployment(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, MapSize mapSize, string landscape = "Habitable")
    {
        if (!playerFrames.Any())
            throw new InvalidOperationException("Player must have at least one frame");
        if (!enemyFrames.Any())
            throw new InvalidOperationException("Combat requires at least one enemy");

        return _engine.InitializeCombatForDeployment(playerFrames, enemyFrames, mapSize, landscape);
    }

    /// <summary>
    /// Start a new round (refresh energy/AP, build initiative)
    /// </summary>
    public List<CombatEvent> StartRound(CombatState state)
    {
        return _engine.StartRound(state);
    }

    /// <summary>
    /// Advance to the next unit in initiative. Returns null if round is over.
    /// </summary>
    public CombatFrame? AdvanceActivation(CombatState state)
    {
        return _engine.AdvanceActivation(state);
    }

    /// <summary>
    /// Execute a single player action
    /// </summary>
    public List<CombatEvent> ExecutePlayerAction(CombatState state, CombatFrame frame,
        CombatAction action, HexCoord? targetHex = null, int? targetFrameId = null,
        int? weaponGroupId = null, HitLocation? calledShotLocation = null)
    {
        return _engine.ExecuteAction(state, frame, action, targetHex, targetFrameId, weaponGroupId, calledShotLocation);
    }

    /// <summary>
    /// Execute a full AI turn for the given frame
    /// </summary>
    public List<CombatEvent> ExecuteAITurn(CombatState state, CombatFrame frame,
        TacticalOrders? orders = null)
    {
        orders ??= new TacticalOrders();
        return _engine.ExecuteAITurn(state, frame, orders);
    }

    /// <summary>
    /// End the current unit's activation and move to next
    /// </summary>
    public void EndActivation(CombatState state)
    {
        _engine.EndActivation(state);
    }

    /// <summary>
    /// Process end-of-round (overwatch, reactor stress, victory check)
    /// </summary>
    public List<CombatEvent> EndRound(CombatState state)
    {
        return _engine.EndRound(state);
    }

    /// <summary>
    /// Auto-resolve entire combat (AI controls all units)
    /// </summary>
    public CombatLog AutoResolveCombat(CombatState state,
        TacticalOrders? playerOrders = null, TacticalOrders? enemyOrders = null)
    {
        playerOrders ??= new TacticalOrders();
        enemyOrders ??= new TacticalOrders();
        return _engine.ResolveCombat(state, playerOrders, enemyOrders);
    }

    /// <summary>
    /// Formats a single combat event for display
    /// </summary>
    public static string FormatEvent(CombatEvent evt)
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
    /// Get hit chance breakdown for UI targeting display
    /// </summary>
    public HitChanceBreakdown GetHitChanceBreakdown(CombatFrame attacker, CombatFrame target,
        EquippedWeapon weapon, int hexDistance, HexGrid grid)
    {
        return _engine.GetHitChanceBreakdown(attacker, target, weapon, hexDistance, grid);
    }

    /// <summary>
    /// Formats per-location damage status for a frame
    /// </summary>
    public static string FormatFrameDamageStatus(CombatFrame frame)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine($"{frame.CustomName} ({frame.ChassisDesignation} {frame.ChassisName})");
        output.AppendLine($"  Reactor: {frame.CurrentEnergy}/{frame.EffectiveReactorOutput} energy | Stress: {frame.ReactorStress}");
        output.AppendLine($"  Position: {frame.HexPosition} | AP: {frame.ActionPoints}/{frame.MaxActionPoints}");

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
