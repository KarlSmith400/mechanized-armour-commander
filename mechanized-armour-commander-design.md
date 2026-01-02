# Mechanized Armour Commander - Game Design Document

## Overview
A management-focused mechanized combat simulator where players build and maintain a stable of combat frames, manage pilots, and accept contracts. Combat is auto-resolved with strategic pre-mission decisions driving outcomes.

---

## Core Game Loop

1. **Home Base** - Manage frames, pilots, repairs, finances
2. **Mission Selection** - Choose contracts from mission board
3. **Pre-Mission Setup** - Assign pilots, configure loadouts, set tactical orders
4. **Combat Resolution** - Auto-resolved with round-by-round text feed
5. **Post-Mission** - Collect salvage, assess damage, pay costs, advance time

---

## Screen Flow

### Main Menu
- Continue Game
- New Game
- Settings
- Exit

### Home Base Hub (Main Screen)
- **Hangar View** - All frames, status indicators (Ready/Damaged/Deployed)
- **Pilot Barracks** - Roster, stats, injuries, experience
- **Workshop** - Repair queue, salvage inventory, weapon/equipment management
- **Mission Board** - Available contracts with briefings
- **Finance Dashboard** - Credits, monthly expenses, reputation

### Pre-Mission Screens
1. **Mission Selection** - Difficulty, pay, salvage rights, enemy intel
2. **Frame Assignment** - Select which frames to deploy (1-4 units)
3. **Pilot Assignment** - Assign pilots to frames
4. **Loadout Configuration** - Equip weapons to hardpoints, check heat/ammo capacity
5. **Tactical Orders** - Set AI behavior parameters
6. **Mission Brief** - Final confirmation

### Mission Execution
- **Combat Feed** - Round-by-round text display of combat results
- **Watch/Fast-Forward** - Option to see each round or skip to results

### Post-Mission Screens
1. **After Action Report** - Mission success/failure, pilot XP, injuries
2. **Salvage Selection** - Choose from defeated enemy frames/parts
3. **Damage Assessment** - Repair costs and time requirements
4. **Return to Hub** - Time advances, repairs begin

---

## Tactical Orders System

Players set high-level strategy before combat begins:

### Stance
- **Aggressive** - Close distance, prioritize damage output
- **Balanced** - Maintain optimal range, balanced approach
- **Defensive** - Maintain distance, prioritize survival

### Target Priority
- **Focus Fire** - All units target same enemy
- **Spread Damage** - Distribute fire across multiple targets
- **Threat Priority** - Target heaviest/most dangerous first
- **Opportunity** - Target weakest/most damaged first

### Formation
- **Tight** - Stay close for mutual support
- **Spread** - Disperse to avoid concentrated fire
- **Flanking** - Attempt to surround enemy

### Withdrawal Threshold
- **Fight to End** - No retreat
- **Retreat at 50%** - Pull back when half frames damaged
- **Retreat at 25%** - Conservative approach

---

## Combat Resolution

### Round Structure
Each combat round consists of:
1. Initiative determination (light frames first)
2. Movement phase (based on frame class and orders)
3. Attack resolution (each frame attacks once)
4. Status checks (heat, ammo, pilot condition)

### Text Feed Example
```
=== ROUND 1 ===
Your VG-45 Vanguard (Pilot: "Razor") advances to medium range
Enemy BR-70 Bruiser takes defensive position
Your SC-20 Scout flanks at high speed
> Light Laser HIT - 5 damage to enemy BR-70 CT

Enemy BR-70 returns fire
> Heavy Cannon HIT - 20 damage to your VG-45 RT, armor holding!
Pilot "Razor" maintains composure (Gunnery check: SUCCESS)

Heat Status: VG-45 at 8/30, SC-20 at 4/20
Ammo Status: VG-45 at 142/150

=== ROUND 2 ===
...
```

### Combat Calculations
- **To-Hit Roll** = Base Accuracy + Pilot Gunnery + Range Modifier + Movement Modifier + Stance Modifier
- **Damage** = Weapon Damage + Random Variance (±10%)
- **Hit Location** = Randomized (Center Torso, Left/Right Torso, Left/Right Arm, Legs)
- **Critical Hit** = 5% chance for double damage or component damage

---

## Database Schema

### Chassis Table (Frame Templates)
```
chassis_id         | INT PRIMARY KEY
designation        | VARCHAR(10)    # e.g., "VG-45"
name               | VARCHAR(50)    # e.g., "Vanguard"
class              | VARCHAR(20)    # Light/Medium/Heavy/Assault
hp_small           | INT            # Number of small hardpoints
hp_medium          | INT            # Number of medium hardpoints
hp_large           | INT            # Number of large hardpoints
heat_capacity      | INT            # Maximum heat before penalties
ammo_capacity      | INT            # Maximum ammunition storage
armor_points       | INT            # Total armor/structure points
base_speed         | INT            # Movement rating
base_evasion       | INT            # Base chance to avoid hits
```

### Weapon Table
```
weapon_id          | INT PRIMARY KEY
name               | VARCHAR(50)    # e.g., "Light Laser"
hardpoint_size     | VARCHAR(10)    # Small/Medium/Large
heat_generation    | INT            # Heat per shot
ammo_consumption   | INT            # Ammo per shot (0 for energy)
damage             | INT            # Base damage
range_class        | VARCHAR(10)    # Short/Medium/Long
base_accuracy      | INT            # Base to-hit modifier
salvage_value      | INT            # Credits when salvaged
purchase_cost      | INT            # Credits to buy (if market exists)
```

### Frame Instance Table (Player's Frames)
```
instance_id        | INT PRIMARY KEY
chassis_id         | INT FOREIGN KEY
custom_name        | VARCHAR(50)    # Player-assigned name
current_armor      | INT            # Current armor/structure
status             | VARCHAR(20)    # Ready/Damaged/Destroyed/Deployed
repair_cost        | INT            # Cost to fully repair
repair_time        | INT            # Days to complete repair
acquisition_date   | DATE
```

### Loadout Table (What's equipped on each frame)
```
loadout_id         | INT PRIMARY KEY
instance_id        | INT FOREIGN KEY
hardpoint_slot     | VARCHAR(20)    # small_1, medium_2, large_1, etc.
weapon_id          | INT FOREIGN KEY
```

### Pilot Table
```
pilot_id           | INT PRIMARY KEY
callsign           | VARCHAR(50)
gunnery_skill      | INT            # Affects accuracy
piloting_skill     | INT            # Affects evasion
tactics_skill      | INT            # Affects tactical order effectiveness
experience_points  | INT
missions_completed | INT
kills              | INT
status             | VARCHAR(20)    # Active/Injured/KIA
injury_days        | INT            # Days until recovery
morale             | INT            # Affects performance
```

### Mission Table
```
mission_id         | INT PRIMARY KEY
title              | VARCHAR(100)
description        | TEXT
difficulty         | INT            # 1-10
base_payment       | INT            # Credits
salvage_rights     | INT            # % of salvage allowed (0-100)
enemy_composition  | TEXT           # JSON of enemy frames
terrain_type       | VARCHAR(50)
objectives         | TEXT           # JSON of mission objectives
time_limit         | INT            # Rounds until mission ends (0 = no limit)
reputation_change  | INT            # Impact on player reputation
```

### Salvage Table (Available after missions)
```
salvage_id         | INT PRIMARY KEY
mission_id         | INT FOREIGN KEY
item_type          | VARCHAR(20)    # Frame/Weapon/Part
chassis_id         | INT NULLABLE   # If frame
weapon_id          | INT NULLABLE   # If weapon
condition          | INT            # 0-100%
salvage_value      | INT            # Credits if sold
```

---

## Frame Classes & Examples

### Light Class (20-35 tons)
**SC-20 Scout**
- Small HP: 4 | Medium HP: 2 | Large HP: 0
- Heat Cap: 20 | Ammo Cap: 100
- Role: Fast reconnaissance, harassment

**RD-30 Raider**
- Small HP: 3 | Medium HP: 3 | Large HP: 0
- Heat Cap: 25 | Ammo Cap: 120
- Role: Hit-and-run striker

**HR-35 Harrier**
- Small HP: 2 | Medium HP: 3 | Large HP: 1
- Heat Cap: 28 | Ammo Cap: 130
- Role: Medium-range skirmisher

### Medium Class (40-55 tons)
**VG-45 Vanguard**
- Small HP: 3 | Medium HP: 3 | Large HP: 1
- Heat Cap: 30 | Ammo Cap: 150
- Role: Versatile frontline combatant

**EN-50 Enforcer**
- Small HP: 2 | Medium HP: 2 | Large HP: 2
- Heat Cap: 32 | Ammo Cap: 140
- Role: Close-combat brawler

**RG-55 Ranger**
- Small HP: 4 | Medium HP: 4 | Large HP: 0
- Heat Cap: 35 | Ammo Cap: 180
- Role: Support/fire support

### Heavy Class (60-75 tons)
**WD-60 Warden**
- Small HP: 2 | Medium HP: 3 | Large HP: 2
- Heat Cap: 40 | Ammo Cap: 200
- Role: Fire support/artillery

**BR-70 Bruiser**
- Small HP: 1 | Medium HP: 3 | Large HP: 3
- Heat Cap: 38 | Ammo Cap: 180
- Role: Heavy assault

**SN-75 Sentinel**
- Small HP: 3 | Medium HP: 4 | Large HP: 2
- Heat Cap: 45 | Ammo Cap: 220
- Role: Defensive anchor

### Assault Class (80-100 tons)
**TN-85 Titan**
- Small HP: 2 | Medium HP: 4 | Large HP: 3
- Heat Cap: 50 | Ammo Cap: 250
- Role: Heavy firepower platform

**JG-95 Juggernaut**
- Small HP: 1 | Medium HP: 3 | Large HP: 4
- Heat Cap: 48 | Ammo Cap: 240
- Role: Devastating but slow

**CL-100 Colossus**
- Small HP: 2 | Medium HP: 5 | Large HP: 4
- Heat Cap: 55 | Ammo Cap: 280
- Role: Ultimate battlefield presence

---

## Weapon Types

### Small Hardpoint Weapons
**Light Laser**
- Heat: 2 | Ammo: 0 | Damage: 5 | Range: Medium | Accuracy: 85

**Machine Gun**
- Heat: 0 | Ammo: 10 | Damage: 3 | Range: Short | Accuracy: 90

**Flamer**
- Heat: 4 | Ammo: 0 | Damage: 2 | Range: Short | Accuracy: 95
- Special: Increases enemy heat

**Small Missile Rack**
- Heat: 2 | Ammo: 8 | Damage: 6 | Range: Short | Accuracy: 80

### Medium Hardpoint Weapons
**Medium Laser**
- Heat: 4 | Ammo: 0 | Damage: 10 | Range: Medium | Accuracy: 80

**Autocannon-5**
- Heat: 1 | Ammo: 5 | Damage: 8 | Range: Long | Accuracy: 80

**Missile Pod (SRM-6)**
- Heat: 3 | Ammo: 10 | Damage: 12 | Range: Short | Accuracy: 75

**Gauss Rifle (Light)**
- Heat: 1 | Ammo: 8 | Damage: 15 | Range: Long | Accuracy: 85

### Large Hardpoint Weapons
**Heavy Laser**
- Heat: 8 | Ammo: 0 | Damage: 20 | Range: Long | Accuracy: 75

**Heavy Autocannon-10**
- Heat: 2 | Ammo: 8 | Damage: 20 | Range: Long | Accuracy: 70

**Plasma Lance**
- Heat: 10 | Ammo: 0 | Damage: 25 | Range: Medium | Accuracy: 65

**LRM-15 (Long Range Missiles)**
- Heat: 5 | Ammo: 12 | Damage: 15 | Range: Long | Accuracy: 70
- Special: Indirect fire capable

**Gauss Cannon (Heavy)**
- Heat: 1 | Ammo: 6 | Damage: 30 | Range: Long | Accuracy: 80

---

## Variant System

Variants use letter suffixes to indicate specialization:

**VG-45 Vanguard** (Base)
- 3 Small, 3 Medium, 1 Large
- Balanced loadout

**VG-45B Vanguard-Ballistic**
- Modified hardpoints for autocannons
- Increased ammo capacity (+20%)

**VG-45E Vanguard-Energy**
- Enhanced heat sinks
- Increased heat capacity (+30%)

**VG-45M Vanguard-Missile**
- Missile-optimized hardpoints
- Increased ammo capacity (+25%)

---

## Economy System

### Income Sources
- Mission base payment
- Salvage sales
- Reputation bonuses

### Expenses
- Frame repairs
- Ammunition restocking
- Pilot salaries
- Monthly maintenance costs

### Salvage Rights
Missions offer different salvage percentages:
- **0-25%** - High pay, minimal salvage
- **50%** - Balanced contracts
- **75-100%** - Low pay, excellent salvage potential

### Difficulty vs Reward
- **Easy (1-3)** - Low risk, low reward, light enemies
- **Medium (4-6)** - Moderate challenge, good balance
- **Hard (7-9)** - High risk, high reward, heavy enemies
- **Elite (10)** - Extreme danger, massive payouts

---

## Pilot Progression

### Skills
**Gunnery** - Improves weapon accuracy
- Level 1-5: +2% accuracy per level
- Level 6-10: +3% accuracy per level

**Piloting** - Improves evasion and stability
- Level 1-5: +2% evasion per level
- Level 6-10: +3% evasion per level

**Tactics** - Improves tactical order effectiveness
- Level 1-5: +5% order bonus per level
- Level 6-10: +8% order bonus per level

### Experience Gain
- Mission completion: 50 XP
- Enemy frame destroyed: 25 XP per class tier
- Survival bonus: 25 XP
- Performance bonus: 0-50 XP based on damage dealt

### Level Thresholds
- Level 2: 100 XP
- Level 3: 250 XP
- Level 4: 500 XP
- Level 5: 1000 XP
- Level 6+: +750 XP per level

---

## Mission Types

### Skirmish
- Objective: Defeat all enemies
- Standard combat mission

### Assassination
- Objective: Destroy specific target frame
- High-value target, usually heavily defended

### Escort
- Objective: Protect friendly unit(s)
- Defensive mission, mission fails if escort destroyed

### Base Defense
- Objective: Prevent enemies from destroying your position
- Wave-based assault, time limit

### Capture Zone
- Objective: Control specific map area for X rounds
- Positioning and survival focused

### Raid
- Objective: Destroy specific objective, then extract
- Hit-and-run style mission

### Rescue
- Objective: Recover downed pilot/frame and extract
- Time-sensitive

---

## Future Expansion Ideas

### Phase 1 (Core Game)
- Basic combat system
- 10-12 frame chassis
- 20-25 weapon types
- Single-player career mode
- Save/load system

### Phase 2 (Enhanced Management)
- Tech/R&D tree
- Market system for buying/selling
- Pilot recruitment and training
- Reputation system with factions
- Campaign/story missions

### Phase 3 (Advanced Features)
- Weight/tonnage system
- Component damage (gyros, sensors, etc.)
- Weather/terrain effects
- Frame customization (colors, decals)
- Workshop upgrades

### Phase 4 (Extended Content)
- Additional frame classes
- Specialized equipment (ECM, AMS, etc.)
- Legendary/unique frames and pilots
- Faction warfare
- Endless/roguelike mode

---

## Technical Stack Considerations

### Recommended for Visual Studio:
- **C# with Unity** - Best for eventual visual polish
- **C# WinForms/WPF** - If staying purely desktop/management focused
- **C# Console App** - Simplest to start prototyping combat system

### Data Storage:
- **SQLite** - Lightweight, perfect for single-player
- **JSON files** - Simple for prototyping
- **XML** - Good for configuration/mod support

### UI Framework:
- **Unity UI** - If using Unity
- **WPF/XAML** - Native Windows, good data binding
- **ImGui** - Fast prototyping for debug/testing

---

## Development Roadmap

### Milestone 1: Combat Prototype
- Implement combat resolution system
- Basic weapon and frame tables
- Text-based combat feed
- Test balance with simple scenarios

### Milestone 2: Management Core
- Build hangar/workshop screens
- Implement repair and salvage systems
- Basic economy tracking
- Save/load functionality

### Milestone 3: Mission System
- Mission generation
- Pre-mission loadout screen
- Tactical orders implementation
- Post-mission flows

### Milestone 4: Pilot System
- Pilot roster management
- Skill progression
- Injury/fatigue mechanics
- Performance tracking

### Milestone 5: Polish & Balance
- UI improvements
- Balance tuning
- Bug fixes
- Tutorial/onboarding

---

## Resources & Tools

### 3D Space Map Visualization

If you want to add a strategic star map or campaign map with a space theme:

**Bright Star Catalog JSON XYZ**
- **Repository**: `BSC5P-JSON-XYZ` on GitHub
- **What it is**: Cleaned-up Bright Star Catalog with:
  - Real star positions and distances
  - Precomputed 3D x,y,z coordinates
  - Additional data: color, luminosity, star names
  - Optimized for game developers and visualization
- **Why it's useful**:
  - Already in JSON format (easy Unity/Godot/C# integration)
  - Contains only naked-eye visible stars (manageable dataset)
  - Real astronomical data adds authenticity
- **Implementation**:
  - Parse JSON and use `x`, `y`, `z` coordinates to place stars in 3D space
  - Use star data for mission locations, territories, or campaign map
  - Can generate sectors/regions based on star clusters

**Potential Uses in Game**:
- **Campaign Map** - Stars represent mission locations or territories
- **Strategic Layer** - Travel between star systems for contracts
- **Faction Control** - Color-code stars by controlling faction
- **Mission Generation** - Star properties influence mission types/difficulty
- **Visual Polish** - Background starfield for mission briefings

**Example Integration**:
```csharp
// Parse star data
public class StarData {
    public string name;
    public float x, y, z;
    public string color;
    public float luminosity;
}

// Use for mission locations
public class MissionLocation {
    public StarData star;
    public Vector3 position;
    public string sector;
}
```

---

## Notes & Design Philosophy

- **Management over twitch** - Player wins through preparation, not execution
- **Meaningful choices** - Every decision should have tradeoffs
- **Resource scarcity** - Can't have/do everything, forces prioritization
- **Emergent narratives** - Pilots and frames develop history through play
- **Clear feedback** - Player always knows why they won or lost
- **Modular design** - Easy to expand content without code changes

---

*Last Updated: 2026-01-02*
