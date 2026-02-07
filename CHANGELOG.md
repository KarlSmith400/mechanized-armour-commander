# Changelog - Mechanized Armour Commander

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added - Faction System & QoL (v0.4.0)

- **3-Faction System**
  - Crucible Industries (cyan, corporate megacorp, energy weapons, heavy armor)
  - Terran Directorate (gold, military authority, balanced doctrine)
  - Outer Reach Collective (orange, frontier pirates, ballistic/missile, fast frames)
  - Faction standing levels: Hostile (<-50), Neutral (0+), Friendly (100+), Allied (200+), Trusted (400+)
  - Standing-based price discounts: Friendly 5%, Allied 10%, Trusted 20%
  - Standing changes after missions: victory boosts employer, penalizes rivals; defeat/withdrawal penalizes employer

- **Faction Markets**
  - Market UI with faction filter buttons (ALL + 3 factions)
  - Each faction sells its own chassis/weapons plus universal items
  - Price modifier applied from faction standing
  - 3 exclusive high-tier weapons locked behind Allied standing (200+):
    - Fusion Lance (Crucible, Large Energy, Dmg 35, $120K)
    - Precision Gauss (Directorate, Large Ballistic, Dmg 28, Acc 90, $110K)
    - Swarm Launcher (Outer Reach, Large Missile, Dmg 22, $90K)

- **Faction-Driven Missions**
  - Each contract has an employer faction and opponent faction
  - Mission titles and descriptions tailored per employer faction (7 titles, 4 descriptions each)
  - Employer faction color on mission card left border
  - Enemy force uses opponent faction's chassis (70% bias) and weapons
  - Enemy frames named with faction prefix (CRI-1, TDR-2, ORC-1) instead of Hostile-N
  - Higher standing with employer = pay multiplier (Trusted 1.3x, Allied 1.2x, Friendly 1.1x)
  - Hostile factions (-50 standing) excluded from employer pool

- **Faction Standing Report** (PostCombatWindow)
  - After combat, shows per-faction standing changes in faction colors

- **Frame Renaming**
  - RENAME button on roster panel for each owned frame
  - Inline rename dialog matching terminal aesthetic
  - Custom names displayed across roster, deploy, combat, and refit

- **Salvage Value Display**
  - Post-combat salvage items now show their credit value

- **Faction Status Bar**
  - Management HQ status bar shows all 3 faction standings with colored text

### Changed

- **Database Schema** (SchemaVersion 5)
  - Added Faction table (FactionId, Name, ShortName, Description, Color, WeaponPreference, ChassisPreference, EnemyPrefix)
  - Added FactionStanding table (FactionId FK, Standing)
  - Added FactionId column to Chassis and Weapon tables (nullable FK, null = universal)
  - Schema auto-migrates (drops and recreates on version mismatch)

- **Data Seeder** — assigns FactionId to all chassis and weapons, seeds 3 factions and standings
- **MissionService** — faction-aware contract generation, enemy building, standing rewards
- **ManagementService** — faction market methods, price modifiers, frame rename support
- **Weapon/Chassis Repositories** — FactionId support, GetByFaction queries

- **New Data Layer Files**
  - Faction.cs — faction data model
  - FactionStanding.cs — per-faction reputation with computed StandingLevel and PriceModifier
  - FactionRepository.cs — faction CRUD
  - FactionStandingRepository.cs — standing persistence with faction JOIN

---

### Added - Management & Campaign System (v0.3.0)

- **Campaign Game Loop**
  - Full loop: Management Hub → Select Mission → Deploy Lance → Combat → Post-Combat Results → Management Hub
  - Persistent player state across sessions (credits, reputation, missions completed)
  - Day advancement system ticking injury recovery and repair timers
  - Starting conditions: 500,000 credits, 2 frames (Medium + Light), 4 pilots

- **Management Hub** (ManagementWindow)
  - Terminal-style headquarters UI with 7 navigation sections
  - **Roster**: View all owned frames with status, assigned pilot, loadout summary, repair/sell options
  - **Pilots**: View pilot roster with stats, assignment status, hire new pilots (30,000 credits)
  - **Market**: Browse and purchase chassis and weapons; weapon purchases go to company inventory
  - **Inventory**: View stored weapons with sell option; populated by salvage and purchases
  - **Refit**: Equip/unequip weapons between frames and inventory with slot/space validation
  - **Missions**: Browse 3 generated contracts with difficulty, rewards, enemy composition
  - **Deploy**: Select 1-4 frames with pilots for mission deployment

- **Mission System** (MissionService)
  - Procedural mission generation scaled to player reputation
  - 5 difficulty levels with scaling enemy composition (2-6 enemy frames)
  - Credit rewards: 40,000-300,000 based on difficulty
  - Reputation rewards and performance bonuses
  - Enemy force builder creates CombatFrames from chassis/weapon database

- **Post-Combat Results** (PostCombatWindow)
  - Victory/Defeat/Withdrawal outcome display
  - Financial report: credits earned, performance bonus
  - Reputation changes
  - Per-frame damage report with armor %, repair cost, destroyed locations
  - Pilot report: XP gained, injuries, KIA
  - Interactive salvage selection from destroyed enemy wreckage

- **Salvage System**
  - Full weapon pool from all destroyed enemy frames
  - Salvage allowance: 1 + difficulty/2 picks for victory, 1 for withdrawal
  - TAKE/TAKEN toggle buttons for each salvage item
  - Selected weapons added to company inventory

- **Company Inventory System**
  - Inventory table stores company-owned weapons not currently equipped
  - Weapons flow: Market purchase → Inventory → Equip on frame
  - Weapons flow: Unequip from frame → Inventory → Sell or re-equip
  - Selling a frame returns its equipped weapons to inventory
  - Salvaged weapons added to inventory after combat

- **Refit System**
  - Select frame → view equipped weapons → unequip to inventory
  - View inventory weapons → equip onto frame
  - Auto-assigns slot name, weapon group, and mount location
  - Validates hardpoint size, available slots, and space budget

- **Economy**
  - Chassis pricing: Light 100K, Medium 200K, Heavy 375K, Assault 650K
  - Sell frames at 50% purchase price
  - Repair costs: 30% of chassis price scaled by damage ratio
  - Destroyed frame repair = full chassis purchase price
  - Pilot hiring: 30,000 credits
  - Weapon buy/sell at seeded prices

- **Pilot System**
  - 4 starting pilots with varied Gunnery/Piloting/Tactics skills
  - Pilot assignment to frames (one pilot per frame)
  - Pilot XP gain after combat
  - Injury system: 3-10 days recovery, unavailable during recovery
  - KIA: permanent pilot loss
  - Hire new pilots with randomized stats

- **Core Rules Documentation** (CORE_RULES.md)
  - Comprehensive 13-section rules reference
  - Combat round structure and end conditions
  - Action economy with AP costs
  - Accuracy formula with all modifiers
  - Damage resolution cascade
  - Reactor and energy system
  - Positioning and range bands
  - Loadout and hardpoint system
  - Chassis class stats
  - Pilot skills and status
  - AI decision making
  - Economy rules
  - Campaign loop

- **New Data Layer Files**
  - FrameInstanceRepository.cs - CRUD for player-owned frames
  - LoadoutRepository.cs - weapon loadout management
  - PilotRepository.cs - pilot CRUD and status management
  - PlayerStateRepository.cs - persistent player state
  - InventoryRepository.cs - company weapon inventory
  - PlayerState.cs - credits, reputation, missions, company name, day counter
  - InventoryItem.cs - inventory entry model

- **New Core Files**
  - ManagementService.cs - roster, economy, refit, inventory operations
  - MissionService.cs - mission generation, enemy building, results processing
  - Mission.cs - mission contract model
  - MissionResults.cs - post-combat results with salvage pool

### Changed

- **Database Schema** (SchemaVersion 4)
  - Added PlayerState table (Credits, Reputation, MissionsCompleted, MissionsWon, CompanyName, CurrentDay)
  - Added PilotId column to FrameInstance (nullable FK to Pilot)
  - Added Inventory table (InventoryId, WeaponId FK)
  - Schema auto-migrates (drops and recreates on version mismatch)

- **MainWindow** - integrated campaign flow: management → combat → post-combat → management
- **DataSeeder** - seeds 4 starting pilots, 2 starting frames with loadouts, initial player state
- **CombatFrame** - added InstanceId for linking combat frames to persistent data
- **Combat flow** - ApplyResults now runs after PostCombatWindow closes (ensures salvage selection is captured)

---

### Added - Combat System Redesign (v0.2.0)

- **Reactor Energy System** (replaces Heat)
  - Each frame has a reactor with output that refreshes each round
  - Energy weapons consume reactor energy per shot (renewable resource)
  - Ballistic/missile weapons are energy-free but consume limited ammo (finite resource)
  - Reactor stress accumulates from overuse (spending > 100% output)
  - Stress > 150% triggers automatic shutdown with permanent reactor damage
  - Stress > 100% has 25% shutdown chance per round
  - VentReactor action reduces stress by output/4
  - Natural dissipation of output/10 per round
  - ReactorSystem.cs handles all energy/stress logic

- **Range Band Positioning** (replaces numeric positions)
  - Four range bands: Point Blank, Short, Medium, Long
  - All frames start at Long range
  - Movement shifts one band per action (Sprint moves two)
  - Pulling back costs 150% energy vs closing
  - Weapon accuracy varies by range band and weapon class:
    - Short weapons: best at PB/Short, penalised at Medium/Long
    - Medium weapons: best at Short/Medium
    - Long weapons: best at Medium/Long, penalised at PB
  - PositioningSystem.cs manages all range logic

- **Layered Damage Model** (replaces single armor pool)
  - 7 hit locations: Head, Center Torso, Left/Right Torso, Left/Right Arm, Legs
  - Per-location Armor (ablative, loadout decision) and Structure (fixed by chassis)
  - Damage cascade: Armor absorbs first, excess hits Structure
  - Component damage rolls (~40% chance when structure is hit)
  - Component damage types: WeaponDestroyed, ActuatorDamaged, AmmoExplosion, ReactorHit, GyroHit, SensorHit, CockpitHit
  - Damage transfer: destroyed side torso overflows to center torso, destroyed arm to side torso
  - Frame destroyed when Center Torso structure reaches 0
  - DamageSystem.cs handles hit location rolls, damage application, component damage

- **Action Point Economy** (replaces auto-resolution per frame)
  - 2 AP per frame per round (1 AP if gyro damaged)
  - Available actions:
    - Move (1 AP) - move one range band
    - Fire Weapon Group (1 AP) - fire all weapons in a group
    - Brace (1 AP) - +20 defense until next round
    - Called Shot (2 AP) - target specific location with accuracy penalty
    - Overwatch (1 AP) - interrupt fire when enemy moves
    - Vent Reactor (1 AP) - reduce reactor stress
    - Sprint (2 AP) - move two range bands
  - ActionSystem.cs validates actions and manages AP costs

- **Weapon Groups**
  - Weapons assigned to numbered groups (1-4)
  - Fire entire group as one action
  - Energy budget forces choices: can't fire everything every round
  - Each weapon has WeaponType (Energy/Ballistic/Missile), EnergyCost, AmmoPerShot, SpaceCost

- **Space Budget System**
  - Each chassis has TotalSpace defining payload capacity
  - Weapons consume space based on SpaceCost
  - Armor is a loadout decision (more armor = less weapon space)
  - MaxArmorTotal sets per-chassis armor cap
  - FrameSelectorWindow shows space budget tracking

- **Battlefield Map**
  - Canvas-based X,Y plotted map in MainWindow
  - Grid overlay with regularly spaced lines
  - Range band zones shown as dashed vertical dividers (PB, Short, Medium, Long)
  - Player frames plotted in top half, enemy frames in bottom half
  - Unit markers show class tag, name, armor %, and energy
  - Color coding: green (player), red (enemy), orange (critical), yellow (shutdown), grey (destroyed)
  - Position indicator dot at exact coordinates
  - Map updates each round during tactical combat

- **Per-Frame Tactical Decision UI**
  - TacticalDecisionWindow completely rebuilt for action planning
  - Frame tabs to switch between player frames
  - Per-frame action slots (Action 1, Action 2) with sub-options
  - Weapon group selection for fire actions
  - Movement direction selection (close/pull back)
  - Called shot location targeting
  - Focus target selection per frame
  - Weapon groups reference panel with energy/ammo costs
  - Ammo status display

- **Weight Class Identity**
  - Light: reactor 10-12, move cost 2-3E, space 35-45 (fast, cheap to move, small payload)
  - Medium: reactor 15-18, move cost 4-5E, space 50-65 (balanced)
  - Heavy: reactor 20-24, move cost 6-8E, space 70-85 (big reactor, expensive to move)
  - Assault: reactor 25-30, move cost 9-12E, space 85-110 (massive firepower, nearly static)

- **New Core Files**
  - CombatEnums.cs - HitLocation, RangeBand, CombatAction, ComponentDamageType, MovementDirection, ComponentDamage
  - DamageSystem.cs - layered damage, hit locations, component damage, damage transfer
  - ReactorSystem.cs - energy refresh, consumption, overload, stress, shutdown
  - ActionSystem.cs - AP management, action validation, costs

### Changed

- **Database Schema** (SchemaVersion 2 - auto-migrates)
  - Chassis: removed HeatCapacity, AmmoCapacity, ArmorPoints; added ReactorOutput, MovementEnergyCost, TotalSpace, MaxArmorTotal, per-location structure fields
  - Weapon: removed HeatGeneration, AmmoConsumption; added WeaponType, EnergyCost, AmmoPerShot, SpaceCost
  - FrameInstance: removed CurrentArmor; added per-location armor fields, ReactorStress
  - Loadout: added WeaponGroup, MountLocation
  - SchemaVersion table added for automatic migration detection
  - Database drops and recreates all tables on version mismatch

- **All 12 chassis rebalanced** for reactor/movement/space system
- **All 13 weapons rebalanced** for energy/ammo/space costs
- **CombatFrame** completely rewritten with per-location tracking, reactor system, weapon groups, AP
- **CombatEngine** rewritten to orchestrate subsystems (damage, reactor, actions, positioning)
- **CombatAI** rewritten for action-based decisions per stance (Aggressive/Balanced/Defensive)
- **CombatService** updated with ExecuteRound for tactical mode, FormatRoundEvents, FormatFrameDamageStatus
- **CombatLog** extended with new event types (ComponentDamage, AmmoExplosion, ReactorOverload, ReactorShutdown, LocationDestroyed, DamageTransfer, Brace, Overwatch)
- **TacticalOrders** simplified (removed Formation enum, added PreferredRange)
- **RoundTacticalDecision** rewritten with per-frame FrameActions containing PlannedAction lists
- **Repositories** updated for new columns, switched to named column reading (GetOrdinal)
- **FrameSelectorWindow** updated to show reactor, movement cost, structure, space budget, weapon types
- **MainWindow** updated with battlefield map, reactor/range status in frame lists, round-by-round tactical loop

### Removed
- Heat system (replaced by reactor energy)
- Numeric position system (replaced by range bands)
- Single armor pool (replaced by per-location armor/structure)
- Formation enum (range bands handle positioning)
- Automatic per-frame attack resolution (replaced by action point economy)

### Planned Features
- Save/load game state
- Terrain/environmental modifiers
- Special weapon effects
- Pilot morale/stress checks
- Pilot skill leveling on XP thresholds
- Tutorial/onboarding experience

---

## [0.1.0] - 2026-01-02

### Project Initialization

#### Added
- **Project Structure**
  - Created solution file `MechanizedArmourCommander.sln`
  - Created three-layer architecture:
    - `MechanizedArmourCommander.UI` - WPF desktop application
    - `MechanizedArmourCommander.Core` - Game logic and combat engine
    - `MechanizedArmourCommander.Data` - SQLite data access layer
  - Set up project references and dependencies
  - Added Microsoft.Data.Sqlite (v9.0.0) for database access

- **Data Models**
  - `Chassis` - Frame template definitions
  - `Weapon` - Weapon specifications
  - `FrameInstance` - Player-owned frames
  - `Loadout` - Equipped weapons on frames
  - `Pilot` - Pilot statistics and progression
  - `CombatFrame` - Runtime combat state model
  - `TacticalOrders` - Pre-combat strategic decisions
  - `CombatLog` - Combat event logging system

- **Database Layer**
  - SQLite schema with 5 tables (Chassis, Weapon, FrameInstance, Loadout, Pilot)
  - `DatabaseContext` - Connection management and initialization
  - `ChassisRepository` - CRUD operations for chassis data
  - `WeaponRepository` - CRUD operations for weapon data
  - `DataSeeder` - Initial data population system

- **Database Content**
  - **12 Chassis** seeded from design document:
    - 3 Light class (SC-20 Scout, RD-30 Raider, HR-35 Harrier)
    - 3 Medium class (VG-45 Vanguard, EN-50 Enforcer, RG-55 Ranger)
    - 3 Heavy class (WD-60 Warden, BR-70 Bruiser, SN-75 Sentinel)
    - 3 Assault class (TN-85 Titan, JG-95 Juggernaut, CL-100 Colossus)
  - **13 Weapons** seeded from design document:
    - 4 Small hardpoint (Light Laser, Machine Gun, Flamer, Small Missile Rack)
    - 4 Medium hardpoint (Medium Laser, Autocannon-5, SRM-6, Light Gauss Rifle)
    - 5 Large hardpoint (Heavy Laser, Heavy Autocannon-10, Plasma Lance, LRM-15, Heavy Gauss Cannon)

- **Combat System**
  - `CombatEngine` - Complete auto-resolution combat engine
  - Initiative system based on frame speed
  - To-hit calculations with accuracy, gunnery skill, and evasion
  - Damage resolution with hit locations
  - Critical hit system (5% chance, 2x damage)
  - Heat generation and dissipation mechanics
  - Ammunition tracking and consumption
  - Frame destruction detection
  - Victory/defeat condition checking
  - `CombatService` - High-level combat management and log formatting

- **User Interface**
  - WPF main window with terminal-style aesthetic (green-on-black)
  - Three-column layout (Player Forces | Combat Feed | Enemy Forces)
  - Combat control buttons (Start Combat, Reset)
  - Database statistics display on startup
  - Test combat scenario (2v2 engagement)
  - Frame Selector UI with chassis browsing, filtering, loadout configuration
  - Tactical Mode with round-by-round decision window

- **Documentation**
  - `mechanized-armour-commander-design.md` - Complete game design document
  - `README.md` - Project overview and setup instructions
  - `PROJECT_SUMMARY.md` - Technical implementation summary
  - `CHANGELOG.md` - This file

#### Technical Details
- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite 3 with Microsoft.Data.Sqlite
- **Language**: C# 13
- **Build Status**: Clean build, 0 warnings, 0 errors

---

## Milestone Progress

### Milestone 1: Combat Prototype - Complete
- ✅ Combat resolution system implemented
- ✅ Basic weapon and frame models created
- ✅ Database schema and seeding complete
- ✅ Text-based combat feed working
- ✅ Reactor energy system (replacing heat)
- ✅ Range band positioning (replacing numeric positions)
- ✅ Per-location layered damage model
- ✅ Action point economy with meaningful choices
- ✅ Weapon groups and energy budget
- ✅ Space budget system for loadout decisions
- ✅ Battlefield map with X,Y unit plotting
- ✅ Per-frame tactical decision UI
- ✅ AI decision-making for all action types

### Milestone 2: Management Core - Complete
- ✅ Management Hub with 7 sections (Roster, Pilots, Market, Inventory, Refit, Missions, Deploy)
- ✅ Repair system with cost scaling
- ✅ Salvage system with player choice from destroyed enemies
- ✅ Company inventory for weapon storage
- ✅ Refit system for equip/unequip weapons
- ✅ Economy tracking (credits, reputation)
- ✅ Frame buy/sell at market

### Milestone 3: Mission System - Complete
- ✅ Procedural mission generation scaled to reputation
- ✅ Pre-mission deployment screen with lance selection
- ✅ Post-combat results with salvage, XP, damage reports
- ✅ Full campaign loop: Management → Deploy → Combat → Results → Management

### Milestone 4: Pilot System - Complete
- ✅ Pilot roster management with hire/assign
- ✅ XP gain after combat
- ✅ Injury/KIA mechanics
- ✅ Pilot stats affect combat (Gunnery → accuracy, Piloting → initiative)

### Milestone 5: Faction System - Complete
- ✅ 3-faction ecosystem (Crucible Industries, Terran Directorate, Outer Reach Collective)
- ✅ Faction standing system with 5 levels and price discounts
- ✅ Faction-filtered market with exclusive weapons
- ✅ Faction-driven mission generation with employer/opponent identity
- ✅ Faction-biased enemy composition (70% faction chassis/weapons)
- ✅ Post-combat standing changes
- ✅ Frame renaming
- ✅ Salvage value display

### Milestone 6: Polish & Balance (Current) - Not Started
- ⏳ Balance testing
- UI improvements
- Bug fixes
- Tutorial/onboarding

---

## Development Statistics

- **Source Files**: ~55 (.cs files)
- **XAML Files**: 8
- **Projects**: 3
- **Database Tables**: 9 + SchemaVersion (Chassis, Weapon, FrameInstance, Loadout, Pilot, PlayerState, Inventory, Faction, FactionStanding)
- **Seeded Records**: 37 (12 chassis + 16 weapons + 4 pilots + 2 frames + 3 factions)

---

## Known Issues

- Database file auto-recreates on schema version mismatch (drops all player data)
- Canvas map uses fallback dimensions before first layout pass
- ~~No save/load~~ — Resolved: 5 named save slots implemented

---

## Notes

- Database file (`MechanizedArmourCommander.db`) is created on first run in the executable directory
- Schema versioning auto-detects changes and rebuilds tables
- Delete the .db file to force a clean database rebuild
- Campaign mode deploys from persistent roster; test scenario mode still available for quick testing

---

*This changelog follows [Semantic Versioning](https://semver.org/).*
*For the complete design specification, see [mechanized-armour-commander-design.md](mechanized-armour-commander-design.md).*
