namespace MechanizedArmourCommander.Core.Models;

/// <summary>
/// Hit locations on a frame
/// </summary>
public enum HitLocation
{
    Head,
    CenterTorso,
    LeftTorso,
    RightTorso,
    LeftArm,
    RightArm,
    Legs
}

/// <summary>
/// Actions a frame can perform during combat
/// </summary>
public enum CombatAction
{
    Move,           // 1 AP - move up to HexMovement hexes
    FireGroup,      // 1 AP - fire one weapon group
    Brace,          // 1 AP - defensive bonus until next round
    CalledShot,     // 2 AP - target specific location with accuracy penalty
    Overwatch,      // 1 AP - interrupt fire on enemy movement
    VentReactor,    // 1 AP - reduce reactor stress
    Sprint          // 2 AP - move up to 2x HexMovement hexes
}

/// <summary>
/// Types of component damage from structure hits
/// </summary>
public enum ComponentDamageType
{
    WeaponDestroyed,
    AmmoExplosion,
    ActuatorDamaged,
    ReactorHit,
    GyroHit,
    SensorHit,
    CockpitHit
}

/// <summary>
/// Map size for hex grid battlefields
/// </summary>
public enum MapSize
{
    Small,      // 12x10 hexes, difficulty 1-2
    Medium,     // 16x12 hexes, difficulty 3
    Large       // 20x14 hexes, difficulty 4-5
}

/// <summary>
/// Turn phases for individual activation combat
/// </summary>
public enum TurnPhase
{
    Deployment,
    RoundStart,
    AwaitingActivation,
    PlayerInput,
    AIActing,
    RoundEnd,
    CombatOver
}

/// <summary>
/// Tracks a component damage effect on a frame
/// </summary>
public class ComponentDamage
{
    public HitLocation Location { get; set; }
    public ComponentDamageType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? AffectedWeaponGroup { get; set; }
}
