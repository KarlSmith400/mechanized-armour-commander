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
| Move          | 1       | Move one range band closer or farther             |
| Fire Group    | 1       | Fire all weapons in one weapon group              |
| Brace         | 1       | +20% evasion bonus until next round               |
| Called Shot   | 2       | Target a specific location (-15% accuracy penalty)|
| Overwatch     | 1       | Queue reaction fire at enemies                    |
| Vent Reactor  | 1       | Reduce reactor stress                             |
| Sprint        | 2       | Move two range bands (costs 2x movement energy)   |

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

### 3.6 Critical Hits

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

A frame is **destroyed** when its Center Torso structure reaches 0.

When a location is destroyed, **all weapons mounted at that location are also destroyed**.

---

## 5. Reactor & Energy System

### 5.1 Energy Budget

Each round, a frame receives energy equal to its **Effective Reactor Output**:

```
EffectiveReactorOutput = ReactorOutput - (ReactorHits * 3)
```

Minimum effective output is 1.

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

## 6. Positioning & Range Bands

Combat uses 4 range bands (abstract, not grid-based):

```
Point Blank (0) ←→ Short (1) ←→ Medium (2) ←→ Long (3)
```

### 6.1 Movement Rules

- **Move** (1 AP): Shift one band toward or away from enemies
- **Sprint** (2 AP): Shift two bands in one direction
- **Hold** (0 AP): Stay at current range (free, no action cost)
- All frames start at **Long** range
- Pulling back costs **50% more energy** than advancing
- Destroyed legs prevent all movement

### 6.2 Movement Energy Costs by Class

| Class   | Typical Move Cost | Sprint Cost | Pull Back Cost |
|---------|-------------------|-------------|----------------|
| Light   | 2-3               | 4-6         | 3-5            |
| Medium  | 4-5               | 8-10        | 6-8            |
| Heavy   | 7-8               | 14-16       | 11-12          |
| Assault | 10-12             | 20-24       | 15-18          |

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

Weapons are organized into **numbered groups** (1, 2, 3...). Firing a group costs 1 AP and fires all weapons in that group simultaneously. Each group can only be fired once per round.

Group assignment is part of the loadout configuration. Players decide which weapons fire together.

### 7.3 Weapon Types

| Type      | Energy Cost | Ammo  | Notes                                  |
|-----------|-------------|-------|----------------------------------------|
| Energy    | High        | None  | No ammo, draws from reactor            |
| Ballistic | Low (1-2)   | Yes   | Low energy cost but limited ammo       |
| Missile   | Low (1-2)   | Yes   | Low energy cost but limited ammo       |

**Ammo**: Ballistic and Missile weapons have limited shots. Each weapon starts combat with **8 reloads** worth of ammo (AmmoPerShot * 8). Running dry means the weapon can no longer fire.

### 7.4 Space Budget

Each chassis has a TotalSpace value. Each weapon has a SpaceCost. The sum of equipped weapon space costs must not exceed the chassis TotalSpace.

### 7.5 Weapon Mount Locations

Weapons are mounted at specific body locations (LeftArm, RightArm, LeftTorso, RightTorso, CenterTorso, Head). If that location is destroyed, the weapon is destroyed with it.

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

### 8.3 Structure

Each chassis has fixed internal structure per location. Structure cannot be repaired — it is restored when the frame is fully repaired between missions. Structure is the "last line" before location destruction.

---

## 9. Pilot System

### 9.1 Pilot Skills

| Skill    | Effect                                         | Range |
|----------|-------------------------------------------------|-------|
| Gunnery  | +2% accuracy per point                          | 1-5   |
| Piloting | Tiebreaker for initiative order                 | 1-5   |
| Tactics  | (Reserved for future use)                       | 1-5   |

### 9.2 Pilot Status

| Status   | Effect                                          |
|----------|--------------------------------------------------|
| Active   | Available for deployment                         |
| Injured  | Unavailable for InjuryDays days                  |
| KIA      | Permanently lost                                 |

### 9.3 Hiring

New pilots cost **30,000 credits**. Stats are randomized: Gunnery 2-4, Piloting 2-4, Tactics 1-3.

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

### 11.3 Destroyed Frames

Destroyed frames have a repair cost equal to the **full chassis purchase price**. The pilot is unassigned (ejected or KIA).

---

## 12. Salvage

After combat, weapons from destroyed enemy frames can be salvaged. The salvage pool consists of all weapons equipped on destroyed enemies. The player may select from available salvage to add to their company inventory.

---

## 13. Campaign Loop

```
Management Hub → Select Mission → Deploy Lance (1-4 frames) → Combat → Post-Combat Results → Management Hub
```

### 13.1 Between Missions

- Advance Day: ticks injury recovery timers and repair timers
- Repair damaged frames (costs credits, instant)
- Buy/sell frames
- Hire pilots, assign pilots to frames
- Refit weapons from inventory onto frames
- Select next mission contract

### 13.2 Starting Conditions

- Credits: 500,000
- Company: "Iron Wolves"
- Starting frames: 2 (1 Medium, 1 Light)
- Starting pilots: 4
