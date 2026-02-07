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
/// Range bands for combat positioning
/// </summary>
public enum RangeBand
{
    PointBlank = 0,
    Short = 1,
    Medium = 2,
    Long = 3
}

/// <summary>
/// Actions a frame can perform during combat
/// </summary>
public enum CombatAction
{
    Move,           // 1 AP - move one range band closer or farther
    FireGroup,      // 1 AP - fire one weapon group
    Brace,          // 1 AP - defensive bonus until next round
    CalledShot,     // 2 AP - target specific location with accuracy penalty
    Overwatch,      // 1 AP - interrupt fire on enemy movement
    VentReactor,    // 1 AP - reduce reactor stress
    Sprint          // 2 AP - move two range bands
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
/// Movement direction within range bands
/// </summary>
public enum MovementDirection
{
    Close,      // Move toward enemy (decrease range band number)
    Hold,       // Stay at current range
    PullBack    // Move away from enemy (increase range band number)
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
