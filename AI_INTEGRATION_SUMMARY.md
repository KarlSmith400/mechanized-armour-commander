# AI Integration Summary

## Overview

The combat AI system has been fully integrated into the Mechanized Armour Commander combat engine. The AI handles tactical decision-making, target selection, and withdrawal logic based on pre-mission tactical orders.

---

## Key Components

### 1. **CombatAI.cs** ([src/MechanizedArmourCommander.Core/Combat/CombatAI.cs](src/MechanizedArmourCommander.Core/Combat/CombatAI.cs))

The central AI decision-making system with the following capabilities:

#### Target Selection
- **Focus Fire**: Targets most damaged enemy first (finish kills)
- **Spread Damage**: Targets least damaged enemy (spread damage across all enemies)
- **Threat Priority**: Uses threat scoring algorithm to target biggest threats
- **Opportunity**: Scores targets based on kill potential and vulnerability

#### Threat Scoring Algorithm
```csharp
Score = Class Weight + (Current Armor / 10) + Total Weapon Damage

Class Weights:
- Assault: 40 points
- Heavy: 30 points
- Medium: 20 points
- Light: 10 points
```

#### Opportunity Scoring Algorithm
```csharp
Score = (100 - Armor%) + One-Shot Bonus + Overheating Bonus

Bonuses:
- Can kill in one volley: +50 points
- Target is overheating: +20 points
```

#### Withdrawal Logic
Automatically triggers withdrawal based on tactical orders:
- **Fight to End**: Never withdraws
- **Retreat at 50%**: Withdraws when 50% frames destroyed OR 50% frames damaged
- **Retreat at 25%**: Withdraws when 25% frames destroyed OR 75% frames damaged

---

### 2. **PositioningSystem.cs** ([src/MechanizedArmourCommander.Core/Combat/PositioningSystem.cs](src/MechanizedArmourCommander.Core/Combat/PositioningSystem.cs))

Handles all positional tracking and range calculations:

#### Deployment
- Team 1 (Player) starts at position **-10** (left)
- Team 2 (Enemy) starts at position **+10** (right)
- Formation offsets applied to each frame

#### Formation Offsets
| Formation | Effect |
|-----------|--------|
| **Tight** | All frames at same position (0 offset) |
| **Spread** | Frames deployed 2 units apart |
| **Flanking** | Lights ±4, Mediums ±3, Heavies/Assaults 0 |

#### Movement Per Round
| Stance | Movement Speed |
|--------|---------------|
| **Aggressive** | 50% of frame speed |
| **Balanced** | 25% of frame speed |
| **Defensive** | 33% of frame speed |

#### Range Bands
| Range | Distance | Accuracy Modifier |
|-------|----------|-------------------|
| **Short** | 0-5 units | +10% (optimal) / -5% (minor mismatch) / -15% (major mismatch) |
| **Medium** | 6-15 units | +10% (optimal) / -5% (minor mismatch) |
| **Long** | 16+ units | +10% (optimal) / -15% (major mismatch) |

---

### 3. **CombatEngine.cs** Integration

The combat engine now uses both systems:

#### Initialization
```csharp
// Line 24: Initialize positions before combat
_positioning.InitializePositions(playerFrames, enemyFrames,
    playerOrders.Formation, enemyOrders.Formation);
```

#### Withdrawal Checks
```csharp
// Lines 30-64: Check withdrawal conditions before each round
if (_ai.ShouldWithdraw(playerFrames, playerOrders.WithdrawalThreshold))
{
    log.Result = CombatResult.Withdrawal;
    // Log withdrawal event
    break;
}
```

#### Movement Processing
```csharp
// Line 56: Process movement each round
_positioning.ProcessMovement(playerFrames, enemyFrames,
    playerOrders, enemyOrders);
```

#### Target Selection
```csharp
// Line 248: AI selects targets based on priority
return _ai.SelectTarget(attacker, targets, orders.TargetPriority);
```

#### Range-Based Accuracy
```csharp
// Lines 98-99: Calculate distance and range band
int distance = positioning.GetDistance(attacker, target);
string rangeBand = positioning.GetRangeBand(distance);

// Line 175: Apply range modifier to accuracy
int rangeModifier = positioning.GetRangeAccuracyModifier(weapon, rangeBand);
```

---

## Tactical Implications

### Formation Strategy
- **Tight Formation**: Concentrated firepower, easier to hit
- **Spread Formation**: +1 evasion, harder to damage entire lance
- **Flanking Formation**: +2 evasion, light frames get positioning advantage

### Stance Strategy
- **Aggressive + Short-Range Weapons**: Close quickly to optimal range
- **Defensive + Long-Range Weapons**: Maintain distance, maximize range advantage
- **Balanced**: Safe default for mixed loadouts

### Target Priority Strategy
- **Focus Fire**: Best for eliminating threats quickly
- **Spread Damage**: Best for attrition warfare, overwhelming repairs
- **Threat Priority**: Best for defensive battles
- **Opportunity**: Best for maximizing kill count with minimal risk

---

## Combat Flow Example

### Round 1
```
Initial Positions:
- Player Scout (Speed 8): Position -10
- Player Assault (Speed 4): Position -10
- Enemy Medium (Speed 6): Position +10
- Enemy Heavy (Speed 5): Position +10

Distance: 20 units (Long Range)
```

### Round 2 (Aggressive Stance)
```
After Movement:
- Player Scout: -10 + (8 × 0.50) = -6 (moved 4 units)
- Player Assault: -10 + (4 × 0.50) = -8 (moved 2 units)
- Enemy Medium: +10 - (6 × 0.50) = +7 (moved 3 units)
- Enemy Heavy: +10 - (5 × 0.50) = +7.5 (moved 2.5 units)

Scout to Medium distance: |-6 - 7| = 13 units (Medium Range)
Assault to Heavy distance: |-8 - 7.5| = 15.5 units (Long Range)
```

### Round 5
```
Scout reaches Short Range (~5 units)
- Short-range weapons get +10% accuracy bonus
- Deals maximum damage

Assault still at Medium/Long Range
- Long-range weapons effective
- Continues advancing slowly
```

---

## Testing Status

✅ **Build Status**: Clean build, 0 warnings, 0 errors
✅ **AI Target Selection**: Implemented and tested
✅ **Positioning System**: Implemented and tested
✅ **Withdrawal Logic**: Implemented and tested
⏳ **UI Integration**: Pending (currently text-based output)
⏳ **Round-by-Round Intervention**: Models created, UI pending

---

## Next Steps

1. **Test Combat Scenarios**: Run various tactical combinations
2. **UI Enhancement**: Add position visualization to combat feed
3. **Round Intervention UI**: Implement player decision interface
4. **Database Loading**: Replace hardcoded frames with database queries
5. **Balance Tuning**: Adjust range bands, modifiers, and movement speeds

---

*Last Updated: 2026-01-02*
*See also: [COMBAT_AI_DESIGN.md](COMBAT_AI_DESIGN.md)*
