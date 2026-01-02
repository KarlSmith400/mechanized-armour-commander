# Combat AI & Tactical Decision System

## Overview

The combat system in Mechanized Armour Commander features **two modes**:
1. **Auto-Resolve**: Fully automated combat based on pre-mission tactical orders
2. **Tactical Mode**: Round-by-round player intervention with AI handling execution

---

## AI Decision Making

### Target Selection

The AI uses sophisticated target selection based on tactical priorities:

#### **Focus Fire**
- Prioritizes most damaged enemy
- Finish off weakened targets
- Concentrates fire for guaranteed kills
- **Best for**: Eliminating threats quickly

#### **Spread Damage**
- Engages least-damaged targets
- Distributes fire across multiple enemies
- Prevents any single enemy from full effectiveness
- **Best for**: Attrition warfare, overwhelming enemy repairs

#### **Threat Priority**
- Calculates threat score based on:
  - Frame class (Assault=40, Heavy=30, Medium=20, Light=10)
  - Current armor (every 10 points = +1 score)
  - Total weapon damage
- Targets biggest threats first
- **Best for**: Defensive battles, protecting objectives

#### **Opportunity**
- Calculates opportunity score based on:
  - Damage percentage (100 points max)
  - One-shot potential (+50 bonus)
  - Overheating status (+20 bonus)
- Finishes off easy kills
- **Best for**: Opportunistic play, maximizing kill count

---

## Stance Modifiers

### **Aggressive**
- Shifts preferred range closer (Long→Medium→Short)
- Movement speed: **50% of frame speed** per round
- Close quickly to maximize damage
- **Risk**: Takes more fire, higher damage received
- **Effect**: Advances rapidly toward enemy

### **Balanced**
- Uses optimal weapon range
- Movement speed: **25% of frame speed** per round
- Maintains tactical positioning
- **Best for**: Standard combat
- **Effect**: Cautious advance

### **Defensive**
- Shifts preferred range farther (Short→Medium→Long)
- Movement speed: **33% of frame speed** per round
- Maximizes survival at cost of damage
- **Risk**: Lower damage output
- **Effect**: Slow advance, tries to maintain distance

---

## Positional Combat System

### **Deployment**
- **Team 1** (Player): Deploys at position **-10** (left side)
- **Team 2** (Enemy): Deploys at position **+10** (right side)
- **Center**: Position **0**
- Each frame tracks its position relative to center

### **Distance Measurement**
- Distance = |Frame1.Position - Frame2.Position|
- Starting distance: 20 units (10 + 10)
- Distance affects weapon accuracy via range bands

### **Range Bands**
- **Short Range**: 0-5 units
- **Medium Range**: 6-15 units
- **Long Range**: 16+ units

### **Range Accuracy Modifiers**
- **Optimal Range**: +10% accuracy
- **Minor Mismatch**: -5% accuracy
- **Major Mismatch**: -15% accuracy

Examples:
- Short-range weapon at Short range: +10%
- Short-range weapon at Medium range: -5%
- Short-range weapon at Long range: -15%

### **Movement Each Round**
- Teams advance toward each other
- Movement amount = Frame Speed × Stance Multiplier
- Fast frames close distance quickly
- Slow frames lag behind

Example:
- Scout (Speed 8) with Aggressive stance: Moves 4 units/round (8 × 0.50)
- Assault (Speed 4) with Balanced stance: Moves 1 unit/round (4 × 0.25)
- Starting at distance 20, they'll be in short range (~5 units) after:
  - Scout: ~4 rounds
  - Assault: ~15 rounds

### **Tactical Implications**
- **Fast frames reach optimal range first**
- **Formation spreads out during advance** (different speeds)
- **Aggressive stance** brings long-range combat to short range quickly
- **Defensive stance** keeps combat at range longer
- **Weapon loadout matters** - short-range builds want aggressive stance

---

## Formation Effects

### **Tight**
- Position: All frames at same position (0 offset)
- +0 evasion modifier
- Concentrated firepower
- **Best for**: Maximum damage output

### **Spread**
- Position: Frames spread 2 units apart
- +1 evasion modifier
- Harder to hit with area weapons
- **Best for**: Avoiding concentrated fire

### **Flanking**
- Light frames: ±4 units from center
- Medium frames: ±3 units from center
- Heavy/Assault: 0 offset (center)
- +2 evasion modifier
- **Best for**: Envelopment tactics, multi-angle attacks

---

## Withdrawal Logic

### **Fight to End**
- Never withdraws
- Combat until victory or defeat

### **Retreat at 50%**
- Withdraws when:
  - 50% of frames destroyed OR
  - 50% of active frames damaged (<50% armor)

### **Retreat at 25%**
- Conservative withdrawal
- Withdraws when:
  - 25% of frames destroyed OR
  - 75% of active frames damaged

---

## Round-by-Round Tactical Decisions

### Available Each Round

**Global Commands**:
- **Stance Override**: Change stance for one round
  - Examples: "Go defensive", "All-out attack"
- **Target Priority Override**: Change targeting for one round
  - Examples: "Focus fire", "Spread damage"
- **Focus Target**: Entire lance targets specific enemy
  - Useful for eliminating priority threats
- **Attempt Withdrawal**: Try to disengage
  - Success based on frame speed vs enemy

**Frame-Specific Commands**:
- **Hold Fire**: Conserve ammo/reduce heat
  - When: Low on ammo, overheating, repositioning
- **Evasive Maneuvers**: +2 evasion, -10% accuracy
  - When: Frame critically damaged, avoiding fire
- **All-Out Attack**: +10% accuracy, -2 evasion
  - When: Finishing off weakened target
- **Target Assignment**: Specific frame targets specific enemy
  - Useful for coordinating attacks

---

## Tactical Decision Flow

```
┌─────────────────────────────┐
│   Round Start               │
│   Show Situation Report     │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Display:                  │
│   - Player frame status     │
│   - Enemy frame status      │
│   - Previous round summary  │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Player Decision Point     │
│   Options:                  │
│   1. Auto (use AI)          │
│   2. Override stance        │
│   3. Override targets       │
│   4. Frame commands         │
│   5. Withdraw attempt       │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   AI Executes Round         │
│   - Apply player decisions  │
│   - AI fills in gaps        │
│   - Resolve combat          │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Display Round Results     │
│   - Attacks and hits        │
│   - Damage dealt            │
│   - Status changes          │
└─────────────────────────────┘
```

---

## Situation Report Format

```
=== ROUND 5 ===

PLAYER FORCES:
  [█████████░] Alpha (VG-45 Vanguard)    - 90/100 HP, Heat: 12/30
  [██████░░░░] Bravo (SC-20 Scout)       - 38/60 HP, Heat: 8/20 ⚠ DAMAGED

ENEMY FORCES:
  [███████░░░] Enemy-1 (BR-70 Bruiser)   - 105/150 HP
  [████░░░░░░] Enemy-2 (RD-30 Raider)    - 22/55 HP ⚠ CRITICAL

LAST ROUND:
  > Alpha hit Enemy-1 for 18 damage (CT)
  > Bravo hit Enemy-2 for 8 damage (RT)
  > Enemy-1 CRITICAL HIT on Bravo for 40 damage! (RT)

TACTICAL DECISION:
[1] Auto (use default orders)
[2] Change stance
[3] Focus fire on target
[4] Frame commands
[5] Attempt withdrawal
```

---

## AI Advantages

### Consistency
- No mistakes due to fatigue
- Follows tactical doctrine perfectly
- Calculates optimal moves instantly

### Efficiency
- Considers all variables simultaneously
- Weapon effectiveness at current range
- Heat/ammo management
- Threat assessment

### Adaptability
- Responds to changing battlefield conditions
- Adjusts to damage, heat, ammo levels
- Evaluates withdrawal conditions each round

---

## Player Advantages (Tactical Mode)

### Flexibility
- Override AI when needed
- React to unexpected situations
- Apply human intuition

### Strategic Focus
- Focus fire on specific threats
- Coordinate lance actions
- Time special maneuvers

### Risk Management
- Conservative when needed
- Aggressive when opportunity presents
- Controlled withdrawal

---

## Example Tactical Scenarios

### Scenario 1: Damaged Scout
```
Bravo (Scout) at 35% armor
Player Decision:
  - Command: Evasive Maneuvers
  - Effect: +2 evasion, -10% accuracy
  - Rationale: Preserve valuable unit
```

### Scenario 2: Enemy Almost Destroyed
```
Enemy-2 at 15% armor
Player Decision:
  - Global: Focus Fire on Enemy-2
  - Effect: All frames target Enemy-2
  - Rationale: Guarantee kill, reduce enemy firepower
```

### Scenario 3: Overheating
```
Alpha at 28/30 heat
Player Decision:
  - Frame Command: Hold Fire
  - Effect: No attacks, heat dissipates
  - Rationale: Avoid shutdown, maintain combat effectiveness
```

### Scenario 4: Tactical Withdrawal
```
2 of 4 frames destroyed, others damaged
Player Decision:
  - Attempt Withdrawal
  - Effect: Disengage if speed check passes
  - Rationale: Preserve remaining forces
```

---

## Implementation Notes

### Auto-Resolve Mode
- Entire combat runs without player input
- Fast, cinematic
- Good for minor encounters
- AI uses pre-mission tactical orders throughout

### Tactical Mode
- Pause at start of each round
- Player can intervene or continue auto
- Good for major missions
- Boss fights, critical missions

### Hybrid Mode (Recommended)
- Runs auto until player wants to intervene
- "Pause" button available anytime
- Player can give orders, then resume auto
- Best of both worlds

---

## Future Enhancements

### Advanced AI
- Learn from player patterns
- Difficulty levels (Easy/Normal/Hard/Elite)
- Personality profiles (Aggressive/Defensive/Balanced)

### Additional Commands
- Concentrated Fire (all weapons on one target)
- Suppress (force enemy into defensive stance)
- Cover Fire (provide evasion bonus to ally)
- Coordinated Strike (wait for ally, simultaneous attack)

### Environmental Awareness
- Use terrain for cover
- Avoid hazards
- Control key positions

---

*Last Updated: 2026-01-02*
*See also: [mechanized-armour-commander-design.md](mechanized-armour-commander-design.md)*
