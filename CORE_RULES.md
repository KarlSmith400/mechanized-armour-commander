# Mechanized Armour Commander - Core Rules

This document defines the authoritative rules governing combat, loadout, and frame mechanics. All code implementations must conform to these rules.

---

## 1. Combat Round Structure

Each combat round proceeds through 5 phases in order:

1. **Energy & AP Refresh** - All active frames receive full reactor energy and action points
2. **Initiative** - Frames are ordered by Speed (descending), ties broken by PilotPiloting (descending)
3. **Action Phase** - Each frame executes planned actions in initiative order
4. **Overwatch Resolution** - Frames on overwatch fire at enemies (reaction fire, -10% accuracy)
5. **End of Round** - Reactor stress processed, natural stress dissipation, shutdown checks

### Combat End Conditions
- All enemy frames destroyed = **Victory**
- All player frames destroyed = **Defeat**
- Mutual destruction = **Victory** (player took them down)
- Withdrawal triggered = **Withdrawal** (player) or **Victory** (enemy withdraws)
- 100 rounds exceeded = forced **Withdrawal**

---

## 2. Action Economy

Each frame has **2 Action Points** per round (reduced to **1 AP** if gyro is damaged).

| Action        | AP Cost | Description                                      |
|---------------|---------|--------------------------------------------------|
| Move          | 1       | Move up to HexMovement hexes                      |
| Fire Group    | 1       | Fire all weapons in one weapon group              |
| Brace         | 1       | +20% evasion bonus until next round               |
| Called Shot   | 2       | Target a specific location (-15% accuracy penalty)|
| Overwatch     | 1       | Queue reaction fire at enemies                    |
| Vent Reactor  | 1       | Reduce reactor stress                             |
| Sprint        | 2       | Move up to 2x HexMovement hexes (costs 2x energy)|

### Action Restrictions
- **Destroyed legs** prevent Move and Sprint actions
- **Shutdown frames** have 0 AP (skip their turn)
- **Destroyed frames** are removed from initiative order

---

## 3. Accuracy & To-Hit Resolution

Hit chance is calculated as a percentage (d100 roll-under):

```
HitChance = BaseAccuracy + GunneryBonus - EvasionPenalty + RangeModifier
            - BraceBonus - SensorPenalty - CalledShotPenalty - ActuatorPenalty
            - TerrainDefenseBonus - LOSPenalty + EquipmentModifier
```

**Final hit chance is clamped to [5%, 95%]** — no attack is ever guaranteed or impossible.

### 3.1 Base Accuracy (per weapon)

Each weapon has a fixed BaseAccuracy stat:

| Weapon                  | Base Accuracy |
|-------------------------|---------------|
| Flamer                  | 95%           |
| Machine Gun             | 90%           |
| Light Laser             | 85%           |
| Gauss Rifle (Light)     | 85%           |
| Medium Laser            | 80%           |
| Autocannon-5            | 80%           |
| Gauss Cannon (Heavy)    | 80%           |
| Small Missile Rack      | 80%           |
| Missile Pod (SRM-6)     | 75%           |
| Heavy Laser             | 75%           |
| Heavy Autocannon-10     | 70%           |
| LRM-15                  | 70%           |
| Plasma Lance            | 65%           |

### 3.2 Gunnery Skill Bonus

```
GunneryBonus = PilotGunnery * 2
```

A gunnery skill of 5 provides +10% accuracy. Skill of 1 provides +2%.

### 3.3 Target Evasion Penalty

Each chassis has a BaseEvasion stat subtracted from the attacker's hit chance:

| Class   | Typical Evasion |
|---------|-----------------|
| Light   | 20-25%          |
| Medium  | 13-16%          |
| Heavy   | 10-12%          |
| Assault | 5-8%            |

### 3.4 Range Accuracy Modifiers

Weapons have a RangeClass (Short, Medium, Long) that determines bonuses/penalties at each range band:

| Weapon Range | Point Blank | Short  | Medium | Long   |
|--------------|-------------|--------|--------|--------|
| Short        | +5%         | +10%   | -10%   | -25%   |
| Medium       | -5%         | +5%    | +10%   | -10%   |
| Long         | -15%        | -5%    | +5%    | +10%   |

Short-range weapons excel at close quarters but suffer heavily at long range. Long-range weapons are poor at point blank but dominate at distance.

### 3.5 Other Modifiers

| Modifier               | Effect  | Condition                              |
|------------------------|---------|----------------------------------------|
| Brace bonus            | -20%    | Target is bracing                      |
| Sensor damage          | -10%    | Attacker has sensor hit                |
| Called shot penalty     | -15%    | Attacker is making a called shot       |
| Arm actuator damage    | -10%    | Per actuator hit on the weapon's arm   |
| Terrain defense (Forest)| -15%   | Target is on a Forest hex              |
| Terrain defense (Rocks) | -10%   | Target is on a Rocks hex               |
| Equipment modifiers    | Varies  | See Section 7.7 Equipment Combat Effects |

### 3.6 Line of Sight (LOS) Penalties

Shooting through intervening terrain reduces accuracy. The hex line between attacker and target is traced, and each **intervening hex** (excluding the attacker's and target's own hex) applies a penalty:

| Intervening Terrain | Penalty per Hex |
|---------------------|-----------------|
| Forest              | -5%             |
| Rocks               | -3%             |
| Open / Rough / Sand | 0%              |

Penalties stack: shooting through 3 Forest hexes applies -15% total LOS penalty.

**Note**: The target's own terrain defense bonus (Section 3.5) is applied separately from LOS penalties. A target standing in Forest behind 2 more Forest hexes would face -15% (cover) + -10% (LOS through 2 Forest) = -25% total terrain-related penalty.

### 3.7 Targeting Display

When selecting an attack target, the UI displays:
- **LOS line**: Yellow dashed line from attacker to target hex
- **Intervening terrain**: Orange-highlighted hexes with per-hex penalty text (-5, -3)
- **Clear hexes**: Small green dots along clear line-of-sight hexes
- **Hit chance breakdown**: Header bar shows full modifier breakdown (base, gunnery, range, evasion, cover, LOS, etc.)
- **Crosshair cursor**: Appears when hovering over a valid target

### 3.8 Critical Hits

Any successful hit has a **5% chance** of being a critical hit. Critical hits are cosmetic in the current system (the damage cascade through structure already handles component damage).

---

## 4. Damage Resolution

Damage follows a layered cascade: **Armor → Structure → Component Damage → Location Destruction**

### 4.1 Hit Location Table

When a hit lands, the target location is determined by weighted random roll (unless called shot):

| Location      | Weight | Probability |
|---------------|--------|-------------|
| Head          | 5      | 5%          |
| Center Torso  | 20     | 20%         |
| Left Torso    | 15     | 15%         |
| Right Torso   | 15     | 15%         |
| Left Arm      | 10     | 10%         |
| Right Arm     | 10     | 10%         |
| Legs          | 25     | 25%         |

### 4.2 Damage Cascade

1. **Armor absorbs first** — damage reduces location armor, excess passes through
2. **Structure absorbs overflow** — structure damage triggers component checks
3. **Structure reaches 0** = location destroyed

### 4.3 Component Damage (on structure hit)

When structure takes damage, there is a **43% chance** of a component effect:

| Roll Range | Effect             | Probability | Description                                    |
|------------|--------------------|-------------|------------------------------------------------|
| 0-14       | Weapon Destroyed   | 15%         | One functional weapon at that location is lost  |
| 15-24      | Actuator Damaged   | 10%         | Arm: -10% weapon accuracy. Legs: +2 move energy |
| 25-29      | Ammo Explosion     | 5%          | 10-24 internal damage if ammo present          |
| 30-34      | Reactor Hit        | 5%          | +25% reactor stress, -3 effective reactor output|
| 35-37      | Gyro Hit           | 3%          | Max AP reduced to 1                            |
| 38-39      | Sensor Hit         | 2%          | -10% accuracy on all weapons                   |
| 40-42      | Cockpit Hit        | 3%          | Pilot injured                                  |
| 43-99      | No effect          | 57%         | —                                              |

### 4.4 Damage Transfer

When a location is destroyed, overflow damage transfers to an adjacent location:

| Destroyed Location | Transfers To   |
|--------------------|----------------|
| Left Torso         | Center Torso   |
| Right Torso        | Center Torso   |
| Left Arm           | Left Torso     |
| Right Arm          | Right Torso    |
| Head / CT / Legs   | No transfer    |

### 4.5 Frame Destruction

A frame is **destroyed** when:
- **Center Torso** structure reaches 0 (reactor breached), OR
- **Head** is destroyed and pilot fails survival roll (no pilot to control frame)

When a location is destroyed, **all weapons mounted at that location are also destroyed**.

### 4.6 Head Destruction & Pilot Survival

The Head houses the cockpit, sensors, and pilot. Head destruction (5% hit chance, low armor/structure) triggers a special sequence:

1. **Cockpit breached** — all sensors destroyed (permanent -10% accuracy if not already damaged)
2. **Pilot survival roll** — `50% + (PilotPiloting * 5%)` chance to survive
   - Piloting 1 = 55% survival, Piloting 3 = 65%, Piloting 5 = 75%
3. **If pilot survives**:
   - Pilot is injured (3-10 day recovery post-combat)
   - Gunnery bonus zeroed (cockpit targeting offline — `PilotGunnery * 2` becomes 0)
   - Sensor hit applied (-10% all weapons)
   - Frame continues fighting with heavy accuracy penalties
4. **If pilot dies**:
   - Pilot is KIA (permanent loss)
   - Frame immediately shuts down (out of combat)
   - Frame is recoverable post-combat (not destroyed like CT breach)

**Called shot to Head** (2 AP, -15% accuracy) is a high-risk assassination play — if the Head goes down, there's a real chance of killing the pilot outright.

---

## 5. Reactor & Energy System

### 5.1 Energy Budget

Each round, a frame receives energy equal to its **Effective Reactor Output**:

```
EffectiveReactorOutput = ReactorOutput + ReactorBoost - (ReactorHits * 3)
```

Where `ReactorBoost` comes from Cooling Vents equipment (see Section 7.7). Minimum effective output is 1.

### 5.2 Energy Consumers

| Action           | Energy Cost                                |
|------------------|--------------------------------------------|
| Move (advance)   | MovementEnergyCost (chassis-specific)      |
| Move (pull back) | MovementEnergyCost * 1.5 (retreating penalty) |
| Sprint           | MovementEnergyCost * 2                     |
| Fire weapon      | Weapon's EnergyCost (per weapon in group)  |
| Leg actuator dmg | +2 per hit to movement cost                |

### 5.3 Overloading

Frames can spend up to **150% of effective reactor output** per round. Energy used beyond 100% generates **reactor stress** equal to the overuse amount.

### 5.4 Reactor Stress & Shutdown

| Stress Level               | Effect                                    |
|----------------------------|-------------------------------------------|
| Stress < Output            | Normal operation                          |
| Stress >= Output           | 25% chance of shutdown                    |
| Stress >= Output * 1.5     | Automatic shutdown + permanent reactor hit|

**Shutdown recovery**: Frame skips one full round (0 AP, 0 energy). Stress reduced by 50%.

**Natural dissipation**: Each round, stress decreases by max(1, EffectiveOutput / 10).

### 5.5 Vent Reactor Action

Costs 1 AP. Reduces reactor stress by max(2, EffectiveOutput / 4).

---

## 6. Hex Grid & Positioning

Combat takes place on a pointy-top hex grid using axial coordinates (q, r).

### 6.1 Map Sizes

| Mission Difficulty | Map Size | Dimensions |
|--------------------|----------|------------|
| 1-2 (Easy)         | Small    | 12 x 10   |
| 3 (Medium)         | Medium   | 16 x 12   |
| 4-5 (Hard)         | Large    | 20 x 14   |

### 6.2 Terrain Types

The map is procedurally generated with mixed terrain:

| Terrain | Move Cost | Defense Bonus | LOS Penalty | Description                          |
|---------|-----------|---------------|-------------|--------------------------------------|
| Open    | 1         | 0%            | 0%          | Standard — no modifiers              |
| Forest  | 2         | +15%          | -5%/hex     | Trees — cover, slow, blocks sight    |
| Rocks   | 2         | +10%          | -3%/hex     | Boulders — partial cover, blocks LOS |
| Rough   | 2         | 0%            | 0%          | Broken ground — slow only            |
| Sand    | 1         | 0%            | 0%          | Cosmetic variant, no effect          |

Terrain is rendered using **Kenney hex tile assets** (pointy-top hex PNGs) for visual clarity.

Terrain is scattered procedurally (~12% Forest in clusters, ~10% Rocks, ~8% Rough). Deployment zones (first/last 2 columns) are always kept as Open terrain for fair starts.

### 6.3 Deployment Phase

Before combat begins, a **Deployment Phase** allows unit placement:

1. **Enemy auto-deploy**: AI forces are placed automatically in the rightmost 2 columns
2. **Player manual deploy**: Player selects each frame and clicks a hex in the leftmost 2 columns (deployment zone, highlighted in blue)
3. **Start combat**: Once all player frames are placed, the START COMBAT button activates
4. Player may RESET DEPLOYMENT to reposition all frames

### 6.4 Hex Movement

Each frame class has a fixed hex movement range per Move action:

| Class   | HexMovement | Sprint Range | Move Energy | Sprint Energy |
|---------|-------------|--------------|-------------|---------------|
| Light   | 4           | 8            | 2-3         | 4-6           |
| Medium  | 3           | 6            | 4-5         | 8-10          |
| Heavy   | 2           | 4            | 7-8         | 14-16         |
| Assault | 1           | 2            | 10-12       | 20-24         |

- **Move** (1 AP): Move up to HexMovement hexes (terrain costs deducted per hex entered)
- **Sprint** (2 AP): Move up to 2x HexMovement hexes (costs 2x movement energy)
- Destroyed legs prevent all movement
- Terrain move cost is deducted when **entering** a hex (Forest/Rocks/Rough cost 2 movement instead of 1)

### 6.5 Distance-Based Accuracy

Weapon effectiveness scales with hex distance between attacker and target:

| Weapon Range | Optimal Hexes | Max Range | Modifier at Optimal | Modifier at Max |
|--------------|---------------|-----------|---------------------|-----------------|
| Short        | 2-4           | 7         | +10%                | -25%            |
| Medium       | 4-7           | 10        | +10%                | -10%            |
| Long         | 7-10          | 14        | +10%                | -5%             |

### 6.6 Movement Energy Costs by Class

The energy trade-off is core to class identity:
- **Lights** can move and fire freely (2-3 energy to move, 10-12 total energy)
- **Mediums** can move and fire one group comfortably (4-5 to move, 15-17 total)
- **Heavies** spend ~30-35% of energy to move (7-8 to move, 22-24 total)
- **Assaults** spend ~35-40% of energy to move (10-12 to move, 26-30 total)

---

## 7. Loadout & Hardpoint System

### 7.1 Hardpoints

Each chassis has a fixed number of weapon mount points in three sizes:

| Size   | Accepts         |
|--------|-----------------|
| Small  | Small weapons   |
| Medium | Medium weapons  |
| Large  | Large weapons   |

Weapons can only be equipped in a matching hardpoint size. There is no cross-fitting.

### 7.2 Weapon Groups

Weapons are organized into **numbered groups** (1–4). Firing a group costs 1 AP and fires all weapons in that group sequentially. Each weapon in the group independently consumes energy from the reactor — if a weapon cannot draw enough energy, it is skipped. Each group can only be fired once per round.

Group assignment is managed in the Refit Bay using the **◄/►** buttons next to each weapon. The reactor budget panel shows per-group energy and damage totals, with warnings for groups that exceed available energy. Reassigning weapon groups is free (no cost or time).

### 7.3 Weapon Types

| Type      | Energy Cost | Ammo  | Notes                                  |
|-----------|-------------|-------|----------------------------------------|
| Energy    | High        | None  | No ammo, draws from reactor            |
| Ballistic | Low (1-2)   | Yes   | Low energy cost but limited ammo       |
| Missile   | Low (1-2)   | Yes   | Low energy cost but limited ammo       |

**Ammo**: Ballistic and Missile weapons have limited shots. Each weapon starts combat with **8 reloads** worth of ammo (AmmoPerShot * 8), plus bonus reloads from Ammo Bin equipment. Running dry means the weapon can no longer fire.

### 7.4 Complete Weapon Reference

#### Small Hardpoint Weapons

| Weapon | Type | Damage | Energy | Ammo/Shot | Space | Range | Accuracy | Price | Faction |
|--------|------|--------|--------|-----------|-------|-------|----------|-------|---------|
| Light Laser | Energy | 5 | 4 | — | 2 | Medium | 85% | 10,000 | Universal |
| Machine Gun | Ballistic | 3 | 0 | 10 | 2 | Short | 90% | 6,000 | Universal |
| Flamer | Energy | 2 | 6 | — | 2 | Short | 95% | 8,000 | Crucible |
| Small Missile Rack | Missile | 6 | 1 | 8 | 3 | Short | 80% | 12,000 | Outer Reach |

#### Medium Hardpoint Weapons

| Weapon | Type | Damage | Energy | Ammo/Shot | Space | Range | Accuracy | Price | Faction |
|--------|------|--------|--------|-----------|-------|-------|----------|-------|---------|
| Medium Laser | Energy | 10 | 8 | — | 5 | Medium | 80% | 20,000 | Universal |
| Autocannon-5 | Ballistic | 8 | 1 | 5 | 6 | Long | 80% | 24,000 | Universal |
| Missile Pod (SRM-6) | Missile | 12 | 2 | 10 | 6 | Short | 75% | 30,000 | Outer Reach |
| Gauss Rifle (Light) | Ballistic | 15 | 2 | 8 | 7 | Long | 85% | 50,000 | Directorate |

#### Large Hardpoint Weapons

| Weapon | Type | Damage | Energy | Ammo/Shot | Space | Range | Accuracy | Price | Faction |
|--------|------|--------|--------|-----------|-------|-------|----------|-------|---------|
| Heavy Laser | Energy | 20 | 14 | — | 10 | Long | 75% | 50,000 | Crucible |
| Heavy Autocannon-10 | Ballistic | 20 | 1 | 8 | 12 | Long | 70% | 60,000 | Outer Reach |
| Plasma Lance | Energy | 25 | 18 | — | 11 | Medium | 65% | 80,000 | Crucible |
| LRM-15 (Long Range Missiles) | Missile | 15 | 2 | 12 | 12 | Long | 70% | 70,000 | Directorate |
| Gauss Cannon (Heavy) | Ballistic | 30 | 2 | 6 | 14 | Long | 80% | 100,000 | Directorate |

#### Faction Exclusive Weapons (Allied Standing 200+ Required)

| Weapon | Type | Damage | Energy | Ammo/Shot | Space | Range | Accuracy | Price | Faction |
|--------|------|--------|--------|-----------|-------|-------|----------|-------|---------|
| Fusion Lance | Energy | 35 | 22 | — | 13 | Medium | 60% | 120,000 | Crucible |
| Precision Gauss | Ballistic | 28 | 3 | 4 | 15 | Long | 90% | 110,000 | Directorate |
| Swarm Launcher | Missile | 22 | 3 | 15 | 14 | Medium | 75% | 90,000 | Outer Reach |

**Salvage Values**: Weapons salvaged from destroyed enemies are worth 50% of purchase price.

**Special Effects**: Flamer increases target reactor stress on hit. LRM-15 is indirect fire capable.

### 7.5 Space Budget

Each chassis has a TotalSpace value. Weapons and equipment both have a SpaceCost. The sum of all equipped weapon and equipment space costs must not exceed the chassis TotalSpace.

### 7.6 Weapon Mount Locations

Weapons are mounted at specific body locations (LeftArm, RightArm, LeftTorso, RightTorso, CenterTorso, Head). If that location is destroyed, the weapon is destroyed with it.

### 7.7 Equipment System

Equipment provides passive bonuses, active abilities, or slot-based augments that compete with weapons for space budget.

#### 7.7.1 Equipment Categories

| Category | Hardpoint | Activation | Description |
|----------|-----------|------------|-------------|
| Passive  | None      | Always on  | Permanent bonuses, no energy cost |
| Active   | None      | Manual     | Costs AP + energy to activate (future) |
| Slot     | Required  | Always on  | Occupies a hardpoint slot like a weapon |

#### 7.7.2 Equipment List

**Passive Equipment** (no hardpoint required):

| Equipment       | Space | Effect                                       | Cost    |
|-----------------|-------|----------------------------------------------|---------|
| Cooling Vents   | 4     | +3 effective reactor output                  | 25,000  |
| Reactive Armor  | 6     | 15% structure damage reduction               | 35,000  |
| Ammo Bin        | 3     | +4 bonus reloads per ammo type               | 15,000  |
| Gyro Stabilizer | 5     | -10% target evasion (attacker benefit)       | 30,000  |

**Active Equipment** (no hardpoint required):

| Equipment           | Space | Energy | Effect                               | Cost    |
|---------------------|-------|--------|--------------------------------------|---------|
| Thrust Pack         | 5     | 4      | Jump 3 hexes (ignores terrain)       | 40,000  |
| Countermeasure Suite | 4    | 3      | -20% accuracy for incoming fire      | 45,000  |
| Targeting Uplink    | 3     | 5      | +15% accuracy for allies near target | 50,000  |
| Barrier Projector   | 6     | 6      | +20 temporary armor to adjacent ally | 55,000  |

**Slot Equipment** (requires matching hardpoint):

| Equipment            | Size   | Space | Effect                                           | Cost    |
|----------------------|--------|-------|--------------------------------------------------|---------|
| Sensor Array         | Small  | 2     | +10% accuracy at Long range                      | 20,000  |
| Point Defense System | Small  | 3     | 50% chance to intercept incoming missiles        | 35,000  |
| Phantom Emitter      | Medium | 5     | -25% accuracy for attackers beyond 5 hexes       | 60,000  |
| Stealth Plating      | Large  | 10    | -20% accuracy for all attackers, -1 hex movement | 75,000  |

#### 7.7.3 Equipment in Combat

Equipment modifiers are applied as a net accuracy bonus/penalty:

| Effect          | Source           | Modifier | Condition                              |
|-----------------|------------------|----------|----------------------------------------|
| EvasionReduction| Gyro Stabilizer  | +10%     | Always (attacker has equipment)        |
| LongRangeBonus  | Sensor Array     | +10%     | Attacker fires Long-range weapon       |
| StealthPlating  | Stealth Plating  | -20%     | Target has equipment                   |
| RangedECM       | Phantom Emitter  | -25%     | Target has equipment, attacker > 5 hex |
| ECM             | Countermeasure   | -20%     | Target has activated ECM               |
| MissileDefense  | Point Defense    | 50% miss | Target has equipment, missile weapon hit|
| DamageReduction | Reactive Armor   | -15% dmg | Structure damage reduced (min 1)       |

**Point Defense System**: When a missile weapon scores a hit, the target rolls a 50% intercept chance. If intercepted, the hit is negated entirely (no damage applied).

**Reactive Armor**: When damage penetrates armor and hits structure, structure damage is reduced by 15% (minimum 1 damage).

**Stealth Plating**: Also reduces frame hex movement by 1 (e.g., Light moves 3 instead of 4).

**Ammo Bin**: Adds 4 extra reloads per ammo-consuming weapon type at combat start (Ballistic and Missile weapons benefit).

### 7.8 Refit Costs

Changing a frame's loadout in the Refit Bay costs time and credits:

- **500 credits** per weapon or equipment change (install or removal)
- **1 day** per weapon or equipment change
- **Weapon group reassignment is free** — no cost or time
- Changes are staged and previewed before confirmation (CONFIRM REFIT / RESET)
- The visual Refit Bay shows a mech diagram with body locations — click a location to equip a weapon or slot equipment from inventory
- The reactor budget panel displays effective reactor output, movement energy cost, and per-group energy/damage summaries with over-budget warnings

---

## 8. Frame Classes & Chassis Stats

### 8.1 Class Overview

| Class   | Tonnage  | Armor    | Reactor  | Speed | Evasion | Role                    |
|---------|----------|----------|----------|-------|---------|-------------------------|
| Light   | 20-35t   | 60-75    | 10-12    | 7-9   | 20-25   | Scout, flanker          |
| Medium  | 40-55t   | 95-110   | 15-17    | 5-6   | 13-16   | Versatile workhorse     |
| Heavy   | 60-75t   | 140-150  | 22-24    | 4     | 10-12   | Fire support, brawler   |
| Assault | 80-100t  | 180-220  | 26-30    | 2-3   | 5-8     | Anchor, siege platform  |

### 8.2 Armor Distribution Formula

Total armor is distributed across locations using fixed percentages:

| Location      | Percentage |
|---------------|------------|
| Head          | 7%         |
| Center Torso  | 20%        |
| Left Torso    | 14.5%      |
| Right Torso   | 14.5%      |
| Left Arm      | 11%        |
| Right Arm     | 11%        |
| Legs          | 22%        |

### 8.3 Chassis Stat Blocks

#### Light Class (20–35 tons)

| Stat | SC-20 "Scout" | RD-30 "Raider" | HR-35 "Harrier" |
|------|---------------|----------------|-----------------|
| Faction | Universal | Directorate | Outer Reach |
| Tonnage | 20t | 30t | 35t |
| Total Armor | 60 | 70 | 75 |
| Reactor Output | 10 | 11 | 12 |
| Move Energy | 2 | 2 | 3 |
| Speed | 9 | 8 | 7 |
| Evasion | 25% | 22% | 20% |
| Total Space | 35 | 40 | 45 |
| Hardpoints | 4S/2M/0L | 3S/3M/0L | 2S/3M/1L |
| Price | 100,000 | 100,000 | 100,000 |

#### Medium Class (40–55 tons)

| Stat | VG-45 "Vanguard" | EN-50 "Enforcer" | RG-55 "Ranger" |
|------|------------------|------------------|----------------|
| Faction | Universal | Universal | Directorate |
| Tonnage | 45t | 50t | 55t |
| Total Armor | 100 | 110 | 95 |
| Reactor Output | 15 | 17 | 16 |
| Move Energy | 5 | 5 | 4 |
| Speed | 6 | 5 | 6 |
| Evasion | 15% | 13% | 16% |
| Total Space | 55 | 60 | 55 |
| Hardpoints | 3S/3M/1L | 2S/2M/2L | 4S/4M/0L |
| Price | 200,000 | 200,000 | 200,000 |

#### Heavy Class (60–75 tons)

| Stat | WD-60 "Warden" | BR-70 "Bruiser" | SN-75 "Sentinel" |
|------|----------------|-----------------|------------------|
| Faction | Directorate | Outer Reach | Crucible |
| Tonnage | 60t | 70t | 75t |
| Total Armor | 140 | 150 | 145 |
| Reactor Output | 22 | 24 | 23 |
| Move Energy | 7 | 8 | 7 |
| Speed | 4 | 4 | 4 |
| Evasion | 11% | 10% | 12% |
| Total Space | 75 | 80 | 78 |
| Hardpoints | 2S/3M/2L | 1S/3M/3L | 3S/4M/2L |
| Price | 375,000 | 375,000 | 375,000 |

#### Assault Class (80–100 tons)

| Stat | TN-85 "Titan" | JG-95 "Juggernaut" | CL-100 "Colossus" |
|------|---------------|--------------------|--------------------|
| Faction | Crucible | Crucible | Outer Reach |
| Tonnage | 85t | 95t | 100t |
| Total Armor | 180 | 200 | 220 |
| Reactor Output | 26 | 28 | 30 |
| Move Energy | 10 | 11 | 12 |
| Speed | 3 | 2 | 2 |
| Evasion | 8% | 6% | 5% |
| Total Space | 90 | 100 | 110 |
| Hardpoints | 2S/4M/3L | 1S/3M/4L | 2S/5M/4L |
| Price | 650,000 | 650,000 | 650,000 |

### 8.4 Structure Per Location

Each chassis has fixed internal structure values. When structure reaches 0 at a location, that location is destroyed.

| Chassis | Head | CT | Side Torso | Arm | Legs |
|---------|------|----|------------|-----|------|
| SC-20 Scout | 2 | 6 | 4 | 3 | 5 |
| RD-30 Raider | 2 | 7 | 5 | 3 | 6 |
| HR-35 Harrier | 3 | 8 | 5 | 4 | 6 |
| VG-45 Vanguard | 3 | 10 | 7 | 5 | 8 |
| EN-50 Enforcer | 4 | 11 | 7 | 5 | 9 |
| RG-55 Ranger | 3 | 10 | 6 | 5 | 8 |
| WD-60 Warden | 4 | 14 | 10 | 7 | 11 |
| BR-70 Bruiser | 5 | 15 | 10 | 7 | 12 |
| SN-75 Sentinel | 5 | 14 | 10 | 7 | 12 |
| TN-85 Titan | 5 | 18 | 12 | 8 | 14 |
| JG-95 Juggernaut | 6 | 20 | 14 | 9 | 16 |
| CL-100 Colossus | 6 | 22 | 15 | 10 | 17 |

Structure cannot be repaired mid-mission — it is restored when the frame is fully repaired between missions.

---

## 9. Pilot System

### 9.1 Pilot Skills

| Skill    | Effect                                         | Range |
|----------|-------------------------------------------------|-------|
| Gunnery  | +2% accuracy per point                          | 1-5   |
| Piloting | Initiative tiebreaker, +5% head survival per pt | 1-5   |
| Tactics  | (Reserved for future use)                       | 1-5   |

### 9.2 Pilot Status

| Status   | Effect                                          |
|----------|--------------------------------------------------|
| Active   | Available for deployment                         |
| Injured  | Unavailable for 3-10 days (3 + random 0-7)      |
| KIA      | Permanently lost                                 |

**KIA Chance**: When a frame is destroyed in combat, the pilot has a **30% chance** of being killed. Otherwise the pilot ejects and is automatically unassigned.

**Injury Recovery**: Injury days tick down each game day. Pilot becomes Active when days reach 0.

### 9.3 Hiring

New pilots cost **30,000 credits**. Stats are rolled randomly on hire:

| Stat | Roll Range | Method (tabletop) |
|------|-----------|---------------------|
| Gunnery | 2–4 | 1d6: 1-2=2, 3-4=3, 5-6=4 |
| Piloting | 2–4 | 1d6: 1-2=2, 3-4=3, 5-6=4 |
| Tactics | 1–3 | 1d6: 1-2=1, 3-4=2, 5-6=3 |
| Morale | 70–90 | 70 + 1d20 |

**Callsign**: Assigned from a pool of 14 names — Ghost, Falcon, Thunder, Shadow, Blade, Phoenix, Wolf, Iron, Storm, Razor, Fang, Ember, Frost, Havoc. No duplicates; overflow generates "Merc-XXX".

### 9.5 Starting Pilots

The company begins with 4 pilots at fixed stats:

| Callsign | Gunnery | Piloting | Tactics | Morale | Notes |
|----------|---------|----------|---------|--------|-------|
| Ghost | 4 | 4 | 3 | 90 | Well-rounded veteran |
| Falcon | 5 | 3 | 3 | 85 | Elite marksman |
| Thunder | 3 | 5 | 4 | 95 | Ace pilot, high morale |
| Shadow | 2 | 2 | 1 | 80 | Green rookie |

### 9.4 Experience Points

Pilots earn XP after each mission:

| Condition | XP Earned |
|-----------|-----------|
| Base XP | Difficulty × 25 |
| Victory bonus | +50 |
| Defeat/Withdrawal | Base XP only |

**Example**: Difficulty 3 Victory = 75 + 50 = 125 XP. Difficulty 5 Defeat = 125 XP (no bonus).

*Note: XP is tracked but skill progression thresholds are not yet implemented.*

---

## 10. AI Decision Making

### 10.1 Stances

| Stance      | Behavior                                                |
|-------------|---------------------------------------------------------|
| Aggressive  | Close to optimal range, maximize fire output            |
| Balanced    | Move to optimal range, vent if stressed, fire remainder |
| Defensive   | Brace if heavily damaged, pull back, vent if stressed   |

### 10.2 Optimal Range Determination

The AI calculates optimal range based on the frame's weapon loadout damage distribution across range classes. The stance then shifts the preference:
- **Aggressive** shifts one band closer
- **Defensive** shifts one band farther

### 10.3 Target Priority

| Priority       | Logic                                          |
|----------------|-------------------------------------------------|
| Focus Fire     | Target with lowest remaining armor              |
| Spread Damage  | Target with highest remaining armor (%)         |
| Threat Priority | Heaviest class + most weapon damage             |
| Opportunity    | Most damaged + shutdown bonus                   |

### 10.4 Withdrawal

| Threshold      | Triggers When                                   |
|----------------|-------------------------------------------------|
| Fight to End   | Never withdraws                                 |
| Retreat at 50% | 50% frames lost OR 50% of surviving frames < 50% armor |
| Retreat at 25% | 25% frames lost OR 75% of surviving frames damaged |

---

## 11. Economy

### 11.1 Chassis Pricing

| Class   | Purchase Price | Sell Price (50%) |
|---------|----------------|------------------|
| Light   | 100,000        | 50,000           |
| Medium  | 200,000        | 100,000           |
| Heavy   | 375,000        | 187,500           |
| Assault | 650,000        | 325,000           |

### 11.2 Repair Costs

```
RepairCost = (ChassisPrice * 0.30 * DamageRatio) + (ComponentDamageCount * 5,000)
```

Where `DamageRatio = 1.0 - (ArmorPercent / 100)`.

Frames with less than 5% damage ratio are marked Ready without needing repair.

### 11.3 Repair Time

```
RepairTime = DamageRatio * 5 days  (minimum 1 day)
```

**Rush Repair**: Costs 2× the normal repair cost but takes half the time (rounded up).

**Destroyed Frames**: 7 days for full rebuild at full chassis purchase price. The pilot is unassigned (ejected or KIA).

### 11.4 Daily Maintenance (Upkeep)

Every game day, each frame incurs a maintenance cost based on class:

| Class   | Daily Cost | Monthly (30 days) |
|---------|------------|--------------------|
| Light   | 500        | 15,000             |
| Medium  | 1,000      | 30,000             |
| Heavy   | 2,000      | 60,000             |
| Assault | 3,500      | 105,000            |

Maintenance applies to all frames with Status other than "Destroyed". Time advancement (Advance Day, travel, repair ticks) all deduct maintenance.

### 11.5 Deployment Costs

Each frame deployed to a mission incurs a one-time deployment cost:

| Class   | Deployment Cost |
|---------|-----------------|
| Light   | 2,000           |
| Medium  | 4,000           |
| Heavy   | 7,500           |
| Assault | 12,000          |

Deployment costs are charged before the mission begins. A full 4-frame lance of Assaults costs 48,000 just to deploy.

### 11.6 Mission Rewards

Base credit rewards scale with mission difficulty:

| Difficulty | Base Reward | Random Bonus | Total Range |
|------------|-------------|--------------|-------------|
| 1 (Easy) | 50,000 | +0–20,000 | 50,000–70,000 |
| 2 | 80,000 | +0–30,000 | 80,000–110,000 |
| 3 (Medium) | 120,000 | +0–50,000 | 120,000–170,000 |
| 4 | 200,000 | +0–60,000 | 200,000–260,000 |
| 5 (Hard) | 300,000 | +0–80,000 | 300,000–380,000 |

**Standing Bonus Multiplier** (based on standing with the employer faction):

| Standing Level | Multiplier |
|----------------|------------|
| Trusted (400+) | 1.30× |
| Allied (200-399) | 1.20× |
| Friendly (100-199) | 1.10× |
| Neutral / Hostile | 1.00× |

**Outcome Modifiers**:
- **Victory**: Full reward × standing multiplier
- **Victory + Zero Losses**: Full reward + 25% bonus (no frames destroyed)
- **Withdrawal**: 50% of base reward
- **Defeat**: 25% of base reward

### 11.7 Price Modifiers (Faction Standing)

Market prices are modified by faction standing with the planet's controlling faction:

| Standing Level | Discount |
|----------------|----------|
| Trusted (400+) | 20% off (0.80×) |
| Allied (200-399) | 10% off (0.90×) |
| Friendly (100-199) | 5% off (0.95×) |
| Neutral / Hostile | Full price (1.00×) |

---

### 11.8 Market Stock System

Market inventory is **persistent per planet** and regenerates every **7 game days**. Stock availability is determined by a roll system weighted by faction standing with the controlling faction.

**Availability by rarity:**

| Item Type | Threshold | Base Chance | Stock Qty |
|-----------|-----------|-------------|-----------|
| Small weapons | Always | 100% | 2–4 |
| Medium weapons | roll ≥ 30 | ~70% | 1–2 |
| Large weapons | roll ≥ 70 | ~30% | 1 |
| Light chassis | Always | 100% (min 1) | 1–2 |
| Medium chassis | roll ≥ 55 | ~45% | 1 |
| Heavy chassis | roll ≥ 80 | ~20% | 1 |
| Assault chassis | roll ≥ 92 | ~8% | 1 |
| Passive equipment | roll ≥ 20 | ~80% | 1–3 |
| Active equipment | roll ≥ 50 | ~50% | 1–2 |
| Slot equipment | roll ≥ 60 | ~40% | 1 |

**How rolls work**: Every item definition in the database is rolled individually. When stock regenerates, the system iterates through all weapons, chassis, and equipment available at that planet's controlling faction (universal items plus faction-specific items). Each item gets its own independent roll:

```
roll = random(1–100) + standingBonus
standingBonus = factionStanding / 50  (ranges 0–10)
```

If the roll meets or exceeds the item's threshold, it appears in stock with a random quantity for its tier. Items that fail the roll simply don't appear that week.

**Example**: A planet controlled by Crucible Industries, player has 150 standing (Friendly):
- Standing bonus: 150 / 50 = 3
- Medium Laser (Small): auto-stocked, qty 2–4
- AC-5 (Medium): roll 45 + 3 = 48 ≥ 30 → in stock, qty 1–2
- SRM-6 (Medium): roll 22 + 3 = 25 < 30 → not available this week
- Heavy Laser (Large): roll 64 + 3 = 67 < 70 → not available this week
- Sentinel (Heavy chassis): roll 79 + 3 = 82 ≥ 80 → in stock, qty 1

**Contested systems** (no controlling faction) have 0 standing bonus and only stock universal items (no faction-specific gear). **Exclusive weapons** are excluded from stock entirely and remain behind the Allied standing gate (200+).

Purchasing an item decrements its stock quantity. When stock reaches 0, the item is no longer available until the next weekly refresh.

---

## 12. Salvage

After combat, weapons from destroyed enemy frames can be salvaged. The salvage pool consists of all weapons equipped on destroyed enemies.

### 12.1 Salvage Allowance

The number of salvage picks depends on mission outcome:

| Outcome | Picks Allowed |
|---------|---------------|
| Victory | 1 + (Difficulty / 2), rounded down |
| Withdrawal | 1 |
| Defeat | 0 (no salvage) |

**Examples**: Difficulty 1 Victory = 1 pick. Difficulty 3 Victory = 2 picks. Difficulty 5 Victory = 3 picks.

Picks are capped by the available pool size — you cannot take more items than exist in the wreckage. Each weapon has a **15% + (Difficulty × 5)%** chance of being intact enough to salvage (Difficulty 1 = 20%, Difficulty 5 = 40%).

### 12.2 Payout Slider

Before deployment, choose a **Payout Level** that trades credit reward for salvage picks:

| Level | Label | Credit Modifier | Salvage Picks Modifier |
|-------|-------|----------------|----------------------|
| 0 | Full Pay | 100% | 0× (no manual picks) |
| 1 | Mostly Pay | 85% | 0.5× (round down, min 0) |
| 2 | Balanced | 70% | 1× (normal) |
| 3 | Mostly Salvage | 50% | 1.5× (round up) |
| 4 | Full Salvage | 25% | 2× |

**Default**: Balanced (70% credits, normal salvage). The credit modifier applies to all credit earnings (base reward, bonus, and outcome-reduced amounts). The salvage modifier scales the base allowance from Section 12.1.

**Examples**: Difficulty 3 Victory (base 2 picks):
- Full Pay → full credits, 0 picks
- Balanced → 70% credits, 2 picks
- Full Salvage → 25% credits, 4 picks

### 12.3 Scavenge Rolls

After the player makes their manual salvage picks, two additional loot sources are rolled:

**Scavenge**: Each **unpicked** item in the salvage pool has a chance equal to the mission's **Salvage Chance** (15% + Difficulty × 5%) to be auto-recovered by the salvage crew. These items are added to inventory automatically.

**Bonus Finds**: Each destroyed enemy has a **15% flat chance** to yield a bonus weapon — a random weapon from the opponent faction's arsenal (excluding exclusive weapons). This represents lucky finds in the wreckage beyond normal salvage.

Both scavenge and bonus results are revealed after the player confirms their manual picks. Even at **Full Pay** (0 manual picks), scavenge rolls still occur on the entire pool.

### 12.4 Frame Salvage (Head Kills)

Enemy frames destroyed via **head kill** (pilot killed by head destruction) are structurally intact enough to recover. These frames appear in a separate "Frame Salvage" section after combat.

- **Eligibility**: Enemy must have `IsPilotDead` AND `HasHeadDestroyed` (CT-breached frames are too damaged)
- **Price**: 40% of the chassis base purchase price
- **Condition**: Frame arrives with combat-damaged armor values (head armor = 0, other locations reflect actual combat damage)
- **Status**: "Damaged" with repair cost (30% of salvage price) and 3 days repair time
- **Purchase**: Uses credits (not salvage allowance) — separate from weapon salvage picks

---

## 13. Campaign Loop

```
Management Hub → Galaxy Travel → Select Mission → Deploy Lance (1-4 frames) → Deployment Phase → Combat → Post-Combat Results → Management Hub
```

### 13.1 Between Missions

- Advance Day: ticks injury recovery timers, repair timers, deducts daily maintenance
- Repair damaged frames (costs credits, takes 1-7 days; rush at 2x cost for half time)
- Buy/sell frames
- Hire pilots, assign pilots to frames
- Refit weapons and equipment in the Refit Bay (costs 500 credits + 1 day per change)
- Buy/sell equipment from Market and Inventory
- Travel between planets and star systems via the Galaxy tab
- Select next mission contract (biased to current system's controlling faction)

### 13.2 Calendar

The game uses a standard Earth calendar set in the year **2847**, matching the lore setting:

- **Day 1** = 1 January 2847
- Standard 12-month calendar (365 days per year)
- Dates display in military format: `15 Jan 2847`
- All game systems (repair, market refresh, travel) still operate on day counts internally

For tabletop play, track dates on a calendar sheet or simply count days from 1 Jan 2847.

### 13.3 Starting Conditions

- Credits: 500,000
- Company: Player-chosen name
- Starting date: 1 January 2847
- Starting location: Crossroads system, Junction Station
- Starting fuel: 50 / 100
- Starting frames: 2 (1 Medium Enforcer, 1 Light Raider)
- Starting pilots: 4 (Ghost, Falcon, Thunder, Shadow — see Section 9.5)

---

## 14. Galaxy Travel

### 14.1 Star Systems

Human space spans 11 star systems connected by jump gates. Each system is controlled by a faction or contested.

| System | Faction | Type | Description |
|--------|---------|------|-------------|
| Sol | Directorate | Core | Capital, High Command |
| Terra Nova | Directorate | Core | Military shipyards |
| Centauri Gate | Directorate | Colony | Border garrison |
| Avalon | Crucible | Colony | Crucible HQ, Foundry Station |
| Forge | Crucible | Colony | Weapons manufacturing |
| Meridian | Crucible | Colony | R&D, backdoor to Sol |
| Haven | Outer Reach | Frontier | Diplomatic hub |
| The Drift | Outer Reach | Frontier | Mobile capital |
| Rimward | Outer Reach | Frontier | Deep fringe, salvage |
| Crossroads | Contested | Contested | Border nexus, player start |
| Deadlight | Contested | Contested | Pirate haven, black market |

### 14.2 Planets & Stations

Each system contains 2-4 planets or stations. Locations offer:
- **Market**: Buy/sell weapons, equipment, chassis, and fuel
- **Hiring**: Recruit new pilots
- **Contracts**: Mission difficulty range varies by location (e.g., core capitals offer Difficulty 3-5)

### 14.3 Travel Mechanics

| Travel Type | Fuel Cost | Time | Notes |
|-------------|-----------|------|-------|
| Intra-system | 5 fuel | 1 day | Move between planets in same system |
| Inter-system jump | 10-20 fuel | 2-4 days | Varies by jump route distance |

- **Fuel capacity**: 100 units maximum
- **Fuel price**: $500 per unit at any market
- **Travel advances time**: triggers daily maintenance, repair ticks, injury recovery

### 14.4 Location-Based Contracts

- Faction-controlled systems: 80% chance employer is the controlling faction
- Contested systems: any faction can post contracts
- Deeper in faction territory = higher difficulty range available
- Missions regenerate when you arrive at a new location

---

## 15. Faction Standing

### 15.1 Standing Scale

Standing with each faction is tracked independently on a scale from **-100 to 500**:

| Range | Level | Effects |
|-------|-------|---------|
| < -50 | Hostile | Full prices, no exclusive gear |
| -50 to 99 | Neutral | Full prices, no exclusive gear |
| 100 to 199 | Friendly | 5% market discount, 1.10× mission rewards |
| 200 to 399 | Allied | 10% market discount, 1.20× rewards, exclusive weapons unlocked |
| 400 to 500 | Trusted | 20% market discount, 1.30× rewards, exclusive weapons |

**Starting Standing**: 0 (Neutral) with all three factions.

### 15.2 Standing Changes

Standing shifts after every mission based on outcome:

| Outcome | Employer Faction | All Other Factions |
|---------|------------------|--------------------|
| Victory | +10 + (Difficulty × 2) | -(2 + Difficulty) |
| Defeat | -(5 + Difficulty) | No change |
| Withdrawal | -3 | No change |

**Example**: Completing a Difficulty 3 mission for Crucible grants +16 Crucible standing and -5 to both Directorate and Outer Reach.

Working exclusively for one faction will eventually make the others hostile, limiting market access in their territory.

### 15.3 The Three Factions

| Faction | Territory | Identity | Exclusive Gear |
|---------|-----------|----------|----------------|
| **Crucible Industries** | Avalon, Forge, Meridian | Corporate military-industrial conglomerate | Flamer, Heavy Laser, Plasma Lance, Fusion Lance, Sentinel, Titan, Juggernaut |
| **Terran Directorate** | Sol, Terra Nova, Centauri Gate | Central human government and military | Raider, Ranger, Warden, Gauss Rifle, LRM-15, Gauss Cannon, Precision Gauss |
| **Outer Reach Collective** | Haven, The Drift, Rimward | Frontier alliance of independent colonies | Harrier, Bruiser, Colossus, Small Missile Rack, SRM-6, HAC-10, Swarm Launcher |

---

## 16. Mission Generation

### 16.1 Enemy Force Composition

Enemy lance composition scales with mission difficulty:

| Difficulty | Enemy Force | Enemy Pilot Skills |
|------------|-------------|--------------------|
| 1 (Easy) | 2× Light | Gunnery 3, Piloting 2, Tactics 2 |
| 2 | 1× Light + 1× Medium | Gunnery 4, Piloting 3, Tactics 3 |
| 3 (Medium) | 2× Medium | Gunnery 5, Piloting 4, Tactics 4 |
| 4 | 1× Medium + 1× Heavy | Gunnery 6, Piloting 5, Tactics 5 |
| 5 (Hard) | 1× Heavy + 1× Assault | Gunnery 7, Piloting 6, Tactics 6 |

**Faction Bias**: 70% chance enemy frames use the controlling faction's chassis (if available). Otherwise random universal chassis.

### 16.2 Difficulty Determination

Mission difficulty is based on company reputation:

```
BaseDifficulty = 1 + (Reputation / 5), clamped to 1–4
FinalDifficulty = BaseDifficulty + random(-1, +2), clamped to planet min/max
```

Higher reputation unlocks harder (and more rewarding) contracts. Each planet has a difficulty range — core worlds offer Difficulty 3-5, frontier planets offer 1-3.

---

## 17. Tabletop Quick Reference

### 17.1 Dice Required

- **2d10** (read as percentile / d100) — hit rolls, component damage, market stock rolls, hit location
- **1d6** — pilot stat generation, salvage chance, random quantities
- **1d20** — pilot morale generation

No unusual dice needed. All rolls use d6, d10 (percentile), or d20.

### 17.2 Turn Sequence Summary

1. **Refresh**: All frames receive full reactor energy and 2 AP
2. **Initiative**: Order by Speed (desc), then Piloting (desc)
3. **Actions**: Each frame spends AP (Move, Fire Group, Brace, Called Shot, Overwatch, Vent, Sprint)
4. **Overwatch**: Queued reaction shots resolve (-10% accuracy)
5. **End of Round**: Process reactor stress, dissipate stress, check shutdowns

### 17.3 Attack Resolution (d100 roll-under)

1. Calculate hit chance: `Base Accuracy + (Gunnery × 2) - Evasion + Range Mod - Cover - LOS - Other Mods`
2. Clamp to 5%–95%
3. Roll d100: if roll ≤ hit chance, the attack hits
4. Roll hit location (d100 weighted — see Section 4.1)
5. Apply damage: Armor → Structure → Component check (43% chance, d100 — see Section 4.3)
6. Check location destruction and damage transfer

### 17.4 Economy Cheat Sheet

| Action | Cost |
|--------|------|
| Hire pilot | 30,000 |
| Buy Light frame | 100,000 |
| Buy Medium frame | 200,000 |
| Buy Heavy frame | 375,000 |
| Buy Assault frame | 650,000 |
| Refit (per change) | 500 + 1 day |
| Weapon group change | Free |
| Daily upkeep (Light) | 500/day |
| Daily upkeep (Medium) | 1,000/day |
| Daily upkeep (Heavy) | 2,000/day |
| Daily upkeep (Assault) | 3,500/day |
| Deploy (Light) | 2,000 |
| Deploy (Medium) | 4,000 |
| Deploy (Heavy) | 7,500 |
| Deploy (Assault) | 12,000 |
| Fuel (per unit) | 500 |
| Intra-system travel | 5 fuel, 1 day |
| Inter-system jump | 10-20 fuel, 2-4 days |
