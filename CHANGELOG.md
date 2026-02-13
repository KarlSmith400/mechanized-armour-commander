# Changelog - Mechanized Armour Commander

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added - Audio System & Station Terrain Art (v0.9.1)

- **Audio System**
  - New AudioService with 9 sound effects: UI click, weapon fire, hit confirm, miss, mech destroyed, turn start, error, victory, defeat
  - Button click sounds across all 8 windows via WPF routed ButtonBase.ClickEvent (one handler per window)
  - Combat sounds: weapon fire on player attack, hit/miss on attack resolution, mech destroyed on frame kill
  - Turn flow sounds: ascending chime on player turn start, victory/defeat stings on combat end
  - Sounds generated programmatically (sine waves, square waves, frequency sweeps, noise bursts)
  - Mute support via AudioService.IsMuted property (for future Settings screen)

- **Station Terrain Tiles**
  - 3 new hex tile images for Station/Industrial landscapes: station_open (metal floor panels), station_rocks (cargo crates), station_rough (grating with hazard stripes)
  - Terrain tile system now landscape-aware — different tile sets loaded per landscape type
  - Station/Industrial landscapes use gray-blue metal aesthetic instead of green nature tiles
  - Polygon fallback colors also landscape-aware (dark metal palette for Station/Industrial)
  - Tiles generated programmatically with hex clipping masks matching Kenney tile dimensions (120x140)

### Changed

- **MainWindow.xaml.cs** — Landscape-aware tile sets (`_tileSets` dictionary), `GetTilesForLandscape()`, `GetTerrainColors()` accepts landscape parameter, combat sound hooks
- **MechanizedArmourCommander.UI.csproj** — Added `Resources\Sounds\*.wav` resource glob

### New Files
- AudioService.cs — Static sound manager with pre-loaded WAV playback
- Resources/Sounds/ui_click.wav — Button click blip
- Resources/Sounds/weapon_fire.wav — Descending frequency sweep + noise
- Resources/Sounds/hit_confirm.wav — Low square wave impact
- Resources/Sounds/miss.wav — Rising sweep whoosh
- Resources/Sounds/mech_destroyed.wav — Noise burst + descending rumble
- Resources/Sounds/turn_start.wav — Two-tone ascending beep
- Resources/Sounds/error.wav — Low double-buzz
- Resources/Sounds/victory.wav — Ascending C-E-G arpeggio
- Resources/Sounds/defeat.wav — Descending G-Eb-C
- Resources/Hex/station_open.png — Metal floor panel hex tile
- Resources/Hex/station_rocks.png — Cargo crate hex tile
- Resources/Hex/station_rough.png — Grating/hazard hex tile

---

### Added - Galaxy Travel System (v0.9.0)

- **Star System Map**
  - 11 star systems across 3 faction territories and 2 contested regions
  - Directorate (Core): Sol, Terra Nova, Centauri Gate
  - Crucible Industries (Mid-rim): Avalon, Forge, Meridian
  - Outer Reach Collective (Fringe): Haven, The Drift, Rimward
  - Contested: Crossroads (player start), Deadlight (pirate haven)
  - X/Y coordinates stored per system for future graphical star map rendering

- **Planets & Stations**
  - 25 planets/stations across all systems (2-4 per system)
  - Location types: Habitable, Industrial, Mining, Station, Outpost
  - Per-location services: Market (buy/sell), Hiring (recruit pilots), Contract difficulty range
  - Unique descriptions for every location grounded in faction lore

- **Jump Route Network**
  - 16 bidirectional routes connecting the galaxy
  - Variable fuel cost (10-20 units) and travel time (2-4 days) per route
  - Multiple paths between faction territories via contested space

- **Travel Mechanics**
  - Fuel system: 100 unit capacity, $500/unit at any market
  - Intra-system travel: 5 fuel, 1 day (move between planets)
  - Inter-system jump: route-specific fuel and days
  - Travel advances time (triggers maintenance, repair ticks, injury recovery)

- **Location-Based Contracts**
  - Mission generation now biased to current system's controlling faction (80%)
  - Contested systems offer contracts from all factions equally
  - Difficulty range bounded by current planet (core capitals: 3-5, frontier outposts: 1-3)
  - Contract descriptions include location context

- **Visual Galaxy Map**
  - Canvas-based star map with faction-colored system nodes
  - Jump routes drawn as dashed lines with fuel cost labels
  - Radial gradient system circles with system-type icons (C/O/F/X)
  - Faction territory ellipses as subtle background regions
  - "YOU" marker and green glow ring on current system
  - Color legend showing all faction colors

- **Galaxy UI Tab**
  - New "GALAXY" navigation tab in Company HQ
  - Visual galaxy map at the top of the tab
  - Current location display with system info, faction control, and planet services
  - Fuel gauge with buy buttons (10/25/50 units)
  - Local destination list with TRAVEL buttons
  - Jump route list with destination info, fuel cost, and JUMP buttons
  - Travel log showing day events during transit

- **Terrain Generation by Planet Type**
  - Combat terrain now matches the planet type where the mission takes place
  - Habitable: forests, rocks, rough terrain (green worlds)
  - Industrial: rocks (structures), rough (debris), sand — no vegetation
  - Mining: heavy rocks, sand, rough — barren rock
  - Station: rocks (bulkheads), rough (debris) — metal corridors
  - Outpost: sand, rough, scattered rocks — sparse frontier
  - Landscape type displayed on mission cards and combat feed

- **Database**
  - Schema version bumped to v7
  - New tables: StarSystem, Planet, JumpRoute
  - PlayerState extended with CurrentSystemId, CurrentPlanetId, Fuel
  - 3 new repositories: StarSystemRepository, PlanetRepository, JumpRouteRepository

---

### Added - Head Destruction & Pilot Survival (v0.8.1)

- **Head Destruction System**
  - Head destroyed (5% hit chance) now triggers pilot survival roll instead of being ignored
  - Survival chance: 50% base + 5% per Piloting skill point (Piloting 5 = 75% survival)
  - Sensors automatically destroyed on head breach (permanent -10% accuracy)
  - Cockpit targeting offline: pilot gunnery bonus zeroed on survival

- **Pilot Survival Outcomes**
  - **Survives**: pilot injured post-combat, frame fights on with no gunnery bonus and sensor hit
  - **Dies**: pilot KIA (permanent loss), frame shuts down immediately, frame recoverable post-combat
  - Called shot to Head is now a meaningful assassination play with real consequences

- **Post-Combat Integration**
  - Pilot killed by head destruction = guaranteed KIA (no random survival roll)
  - Pilot survived head breach = guaranteed injured (3-10 day recovery)
  - Frame with dead pilot treated as destroyed for combat but recoverable post-combat

### Changed

- **DamageSystem.cs** — Added ResolveHeadDestruction() with pilot survival roll, sensor damage, gunnery zeroing
- **CombatFrame.cs** — Added IsPilotDead flag, HasHeadDestroyed query, IsDestroyed now includes IsPilotDead
- **CombatLog.cs** — Added HeadDestroyed, PilotKilledInCombat, PilotSurvivedHeadHit event types
- **MissionService.cs** — Head destruction KIA/injury handling in post-combat results (before random roll)
- **CORE_RULES.md** — Added Section 4.6 (Head Destruction & Pilot Survival), updated Piloting skill description

---

### Added - Equipment System (v0.8.0)

- **Equipment System**
  - 12 equipment items across 3 categories: Passive (4), Active (4), Slot (4)
  - Equipment competes with weapons for chassis TotalSpace budget
  - Slot-type equipment occupies hardpoint slots (Small, Medium, Large) like weapons
  - Equipment stored in company inventory, bought/sold from market, installed via refit bay

- **Passive Equipment** (always-on, no hardpoint)
  - Cooling Vents: +3 effective reactor output (Space 3, $15K)
  - Reactive Armor: 15% structure damage reduction (Space 5, $25K)
  - Ammo Bin: +4 bonus reloads per ammo type at combat start (Space 4, $12K)
  - Gyro Stabilizer: -10% target evasion penalty (Space 3, $18K)

- **Active Equipment** (AP + energy cost, no hardpoint)
  - Thrust Pack: Jump 3 hexes ignoring terrain (Space 4, Energy 3, $20K)
  - Countermeasure Suite: -20% incoming fire accuracy (Space 3, Energy 2, $22K)
  - Targeting Uplink: +15% accuracy for allies near target (Space 2, Energy 2, $28K)
  - Barrier Projector: +20 temporary armor to adjacent ally (Space 5, Energy 4, $35K)

- **Slot Equipment** (requires matching hardpoint)
  - Sensor Array (Small): +10% accuracy for Long-range weapons (Space 2, $16K)
  - Point Defense System (Small): 50% chance to intercept incoming missiles (Space 3, $24K)
  - Phantom Emitter (Medium): -25% accuracy for attackers beyond 5 hexes (Space 4, $30K)
  - Stealth Plating (Large): -20% accuracy for all attackers, -1 hex movement (Space 8, $40K)

- **Equipment Combat Integration**
  - Accuracy modifiers: Gyro Stabilizer, Sensor Array, Stealth Plating, Phantom Emitter, ECM all modify hit chance
  - Point Defense: 50% missile intercept check on each missile hit (negates damage entirely)
  - Reactive Armor: 15% structure damage reduction (after armor is breached, min 1 damage)
  - Cooling Vents: Increases EffectiveReactorOutput (more energy per round)
  - Ammo Bin: Extra reloads per ammo-consuming weapon type at combat start
  - Stealth Plating: Reduces hex movement by 1

- **Equipment in Market**
  - Equipment section in Market UI grouped by category (Passive, Active, Slot)
  - Blue color scheme distinguishes equipment from green weapons
  - Buy equipment to add to company inventory

- **Equipment in Inventory**
  - Equipment section in Inventory UI with category grouping and sell buttons
  - Equipment flows: Market → Inventory → Refit onto frame

- **Equipment in Refit Bay**
  - Equipment staging alongside weapons — install/remove with cost tracking
  - Passive/Active equipment: click Install to equip directly
  - Slot equipment: click Select then click a matching hardpoint slot
  - Equipment shown on mech diagram in blue (vs green for weapons)
  - Equipment changes cost 500 credits + 1 day each (same as weapons)
  - Space budget accounts for both weapons and equipment

### Changed

- **DatabaseContext.cs** — Schema version 5 → 6, added Equipment, EquipmentLoadout, EquipmentInventory tables
- **DataSeeder.cs** — Added SeedEquipment() seeding 12 equipment items with effect keys
- **CombatFrame.cs** — Added EquippedEquipment class, Equipment list, HasEquipment/GetEquipmentValue helpers, ReactorBoost in EffectiveReactorOutput
- **CombatEngine.cs** — Added GetEquipmentAccuracyModifiers() for equipment hit chance effects, Point Defense missile intercept in ExecuteHexFireGroup
- **DamageSystem.cs** — Added Reactive Armor (DamageReduction) percentage reduction before structure damage
- **CombatState.cs** — Added EquipmentModifier field to HitChanceBreakdown
- **ManagementService.cs** — Equipment repos, market/inventory methods, BuildCombatFrame loads equipment (AmmoBonus, StealthPlating speed), SellFrame returns equipment to inventory
- **RefitWindow.xaml.cs** — Full equipment staging (StagedEquipment/StagedEquipmentInv), install/remove/select handlers, equipment in mech diagram and inventory panel
- **ManagementWindow.xaml.cs** — Equipment in Market (buy) and Inventory (sell) sections

### New Files
- Data/Models/Equipment.cs — Equipment definition model
- Data/Models/EquipmentLoadout.cs — Equipment-to-frame-instance mapping
- Data/Models/EquipmentInventoryItem.cs — Equipment in company storage
- Data/Repositories/EquipmentRepository.cs — Equipment CRUD
- Data/Repositories/EquipmentLoadoutRepository.cs — Equipment loadout management with transaction support
- Data/Repositories/EquipmentInventoryRepository.cs — Equipment inventory CRUD

---

### Added - LOS, Hex Tiles & Targeting (v0.7.0)

- **Kenney Hex Tile Terrain**
  - Replaced solid-color hex polygons with Kenney hexagon-pack terrain tile images
  - 5 terrain tiles embedded as resources: terrain_open (grass), terrain_forest (trees), terrain_rocks (rock formations), terrain_rough (dirt), terrain_sand (dunes)
  - Pointy-top hex PNG images scaled to match hex grid dimensions
  - Fallback to colored polygons if tile images fail to load

- **Line of Sight (LOS) System**
  - Intervening terrain between attacker and target now penalizes accuracy
  - Forest: -5% per intervening hex, Rocks: -3% per intervening hex
  - Uses `HexCoord.LineDraw()` to trace hex line from attacker to target
  - Attacker's own hex and target's hex excluded (target already has terrain defense bonus)
  - Penalties stack: shooting through 3 Forest hexes = -15% LOS penalty
  - Integrated into `RollToHit` — affects both player and AI attacks
  - `GetHitChanceBreakdown()` API for UI to query full modifier breakdown without rolling dice

- **Target Selection Cursor**
  - MouseMove tracking on hex canvas during FireGroup targeting mode
  - Hovering over a valid enemy target shows:
    - Yellow dashed LOS line from attacker to target hex center
    - Orange-tinted overlays on intervening blocking hexes with penalty text (-5, -3)
    - Small green dots on clear LOS hexes
  - Crosshair cursor when hovering over valid targets
  - Hit chance breakdown displayed in map header: `TARGET: Name | 5 hex | 62% hit | Base 70% +6 gun -10 rng -15 cover -5 LOS`
  - Auto-clears when mouse leaves target or canvas, or action is cancelled

- **Hardpoint Slot System (Refit Bay)**
  - Fixed hardpoint positions per chassis — body locations have deterministic slot assignments
  - Hardpoint distribution: Large→CT,RT,LT; Medium→RA,LA,RT,LT,CT; Small→Head,RA,LA,RT,LT
  - Body parts without hardpoints are dimmed/grayed out
  - Individual slots drawn with size-coded colors (Large=red, Medium=yellow, Small=green)
  - Slot-by-slot click targeting instead of body-part clicks
  - Smart cost calculation: slot-by-slot diff detects no-change swaps (unequip+re-equip same weapon = 0 cost)

### Changed

- **HexGrid.cs** — Added `GetInterveningTerrainPenalty()`, `GetLOSPenalty()` methods
- **CombatEngine.cs** — `RollToHit` now includes LOS penalty; added `GetHitChanceBreakdown()` for UI
- **CombatService.cs** — Exposed `GetHitChanceBreakdown()` wrapper
- **CombatState.cs** — Added `HitChanceBreakdown` class with all modifier fields
- **MainWindow.xaml** — Added `MouseMove` and `MouseLeave` events on HexCanvas
- **MainWindow.xaml.cs** — Kenney tile image loading/caching, terrain Image rendering, LOS line visualization, targeting cursor with hit chance display
- **RefitWindow.xaml.cs** — Complete rewrite with `HardpointDef` system, slot-based equipping, smart cost diff
- **MechanizedArmourCommander.UI.csproj** — Added `<Resource Include="Resources\Hex\*.png" />`

### New Files
- Resources/Hex/terrain_open.png — Kenney grass tile
- Resources/Hex/terrain_forest.png — Kenney grass+trees tile
- Resources/Hex/terrain_rocks.png — Kenney grass+rocks tile
- Resources/Hex/terrain_rough.png — Kenney dirt tile
- Resources/Hex/terrain_sand.png — Kenney sand tile

---

### Added - Terrain, Deployment & Visual Refit (v0.6.0)

- **Terrain System**
  - 5 terrain types: Open, Forest, Rocks, Rough, Sand
  - Procedural terrain generation with clustered forests (~12%), scattered rocks (~10%), rough patches (~8%)
  - Terrain movement costs: Forest/Rocks/Rough cost 2 movement to enter (vs 1 for Open/Sand)
  - Terrain defense bonuses: Forest +15%, Rocks +10% — subtracted from attacker accuracy
  - Terrain-colored hex polygons (green=Open, dark green=Forest, brown=Rocks, tan=Rough, yellow=Sand)
  - Deployment zones kept clear of terrain for fair starts

- **Pre-Combat Deployment Phase**
  - New `Deployment` turn phase before combat begins
  - Enemy forces auto-deployed in rightmost 2 columns
  - Player manually places each frame by clicking hexes in leftmost 2 columns (deployment zone highlighted in blue)
  - Frame selection buttons to pick which unit to place
  - RESET DEPLOYMENT button to reposition all frames
  - START COMBAT disabled until all player frames are placed
  - Deployment zone visualized with semi-transparent blue overlay

- **Visual Refit Bay**
  - New RefitWindow with mech body diagram (7 locations: Head, CT, LT, RT, LA, RA, Legs)
  - Click body location to equip selected weapon from inventory
  - Equipped weapons shown on diagram with color coding (green=equipped, blue=selected, gray=empty)
  - REMOVE buttons on equipped weapons to unequip back to inventory
  - Staged change system — nothing saved to database until CONFIRM
  - Running cost display: 500 credits + 1 day per weapon change (install or removal)
  - RESET button restores original loadout, CANCEL exits without changes
  - Hardpoint validation (size matching, slot availability, space budget)

- **Pointy-Top Hex Orientation**
  - Switched hex grid from flat-top to pointy-top orientation
  - Fixed rectangular grid generation using odd-r offset-to-axial coordinate conversion
  - Updated all pixel conversion, polygon rendering, and sizing calculations

### Changed

- **HexCoord.cs** — ToPixel/FromPixel switched to pointy-top formulas
- **HexGrid.cs** — Added OffsetCol() helper, rectangular grid generation, GetFullDeploymentZone(), terrain properties (GetTerrainMoveCost, GetTerrainDefenseBonus)
- **HexPathfinding.cs** — Terrain-aware movement costs in BFS and A* pathfinding
- **PositioningSystem.cs** — Added InitializeEnemyPositions(), terrain defense bonus in accuracy calculation
- **CombatEngine.cs** — Added InitializeCombatForDeployment() method
- **CombatService.cs** — Added InitializeCombatForDeployment() wrapper
- **CombatEnums.cs** — Added Deployment to TurnPhase enum
- **MainWindow.xaml.cs** — Deployment phase UI, terrain-colored hex rendering, pointy-top polygon generation, deployment zone highlighting
- **ManagementWindow.xaml.cs** — Simplified refit section to open RefitWindow as modal dialog

### New Files
- RefitWindow.xaml — Visual refit bay layout
- RefitWindow.xaml.cs — Refit bay logic with staged changes, mech diagram, cost tracking

---

### Added - Hex Grid Combat & Economy Overhaul (v0.5.0)

- **Hex Grid Tactical Combat**
  - Replaced abstract range bands with a full hex grid battlefield using axial coordinates (q, r)
  - Flat-top hex rendering with colored polygons (#0A1A0A fill, #1A3A1A borders)
  - Variable map sizes per mission difficulty: Small 12x10 (diff 1-2), Medium 16x12 (diff 3), Large 20x14 (diff 4-5)
  - BFS-based reachable hex calculation and A* pathfinding for movement
  - Player deployment zone (left 2 columns) and enemy deployment zone (right 2 columns)

- **Individual Unit Activation**
  - Replaced simultaneous round planning with per-unit turn-based activation
  - Initiative order based on Speed (descending), then PilotPiloting (descending)
  - Turn phases: RoundStart → AwaitingActivation → PlayerInput/AIActing → RoundEnd → CombatOver
  - Turn order sidebar shows activation sequence with active unit highlight

- **Hex Movement System**
  - Per-class hex movement: Light=4, Medium=3, Heavy=2, Assault=1 hexes per move action
  - Sprint doubles movement range (2 AP)
  - Movement energy cost preserved from previous system (per action, not per hex)
  - Click-to-move: select action, click highlighted hex to execute

- **Distance-Based Accuracy**
  - Replaced range band accuracy table with continuous hex-distance curve
  - Short weapons: optimal 2-4 hexes, max 7 hexes
  - Medium weapons: optimal 4-7 hexes, max 10 hexes
  - Long weapons: optimal 7-10 hexes, max 14 hexes
  - Accuracy bonus at optimal range, penalties at extremes

- **Graphical Unit Rendering**
  - Weight-class unit shapes: Light=diamond(4), Medium=pentagon(5), Heavy=hexagon(6), Assault=octagon(8)
  - Player frames in green, enemy frames in red, destroyed in gray
  - Class letter overlay (L/M/H/A) with unit name labels
  - Active unit glow effect during their turn

- **Combat UI Overhaul**
  - New 3-column layout: left sidebar (unit info + actions), center hex canvas, right sidebar (turn order + combat feed)
  - Action buttons: Move, Sprint, Fire, Brace, Overwatch, Vent, End Turn
  - Hex highlighting: green for movement range, red for attack range
  - Click-to-act: click action button → click target hex
  - AI turn animation with 150ms event streaming and Space key to skip

- **Hex AI System**
  - AI uses BFS pathfinding to choose movement destinations
  - Optimal distance targeting: Short-focused→3 hexes, Medium→5, Long→8
  - Stance modifiers: Aggressive -2 distance, Defensive +2
  - AI evaluates all reachable hexes and scores by proximity to optimal distance

- **Repair Time System**
  - Repairs now take time instead of being instant
  - Damaged frames: 1-5 repair days based on damage ratio
  - Destroyed frames: 7 repair days (full rebuild)
  - Pay repair cost upfront, frame enters "Repairing" status
  - AdvanceDay() ticks repair timers and restores armor when complete
  - Roster shows "Repairing... X days remaining" for frames in repair

- **Rush Repairs**
  - Pay 2x repair cost to halve repair time (rounded up, min 1 day)
  - RUSH button alongside standard REPAIR in roster UI

- **Daily Maintenance Costs**
  - All owned non-destroyed frames incur daily upkeep:
    - Light: $500/day, Medium: $1,000/day, Heavy: $2,000/day, Assault: $3,500/day
  - Deducted automatically on AdvanceDay()
  - Status bar shows total daily upkeep
  - Day report popup shows maintenance cost and completed repairs/recoveries

- **Deployment Costs**
  - Per-mission cost to deploy each frame:
    - Light: $2,000, Medium: $4,000, Heavy: $7,500, Assault: $12,000
  - Deployment cost shown per frame and as total on deploy button
  - Insufficient credits blocks deployment

### Changed

- **CombatEngine** — Complete rewrite for individual activation: InitializeCombat, StartRound, AdvanceActivation, ExecuteAction, ExecuteAITurn, EndRound
- **CombatAI** — Complete rewrite for hex pathfinding: DetermineOptimalDistance, ChooseMoveDestination, GenerateHexActions
- **PositioningSystem** — Complete rewrite: hex-based accuracy modifiers, InitializeHexPositions, GetHexRangeAccuracyModifier
- **ActionSystem** — Simplified for hex: CanPerformAction validates hex movement range
- **CombatService** — Incremental combat API wrapping engine methods
- **ManagementService.RepairFrame()** — Now queues repair (sets "Repairing" status) instead of instant fix
- **ManagementService.AdvanceDay()** — Returns DayReport, restores armor on repair completion, deducts daily maintenance
- **MainWindow.xaml** — Complete layout restructure: hex canvas with sidebars replacing old canvas strip
- **MainWindow.xaml.cs** — Complete rewrite (~650 lines): hex rendering, turn interaction, click handling, AI animation

- **New Core Files**
  - HexCoord.cs — Axial hex coordinate math (distance, neighbors, pixel conversion, line-of-sight)
  - HexGrid.cs — Grid storage, cell management, deployment zones
  - HexPathfinding.cs — BFS reachable hexes, A* pathfinding
  - CombatState.cs — Mutable combat session state

- **New Model Fields**
  - CombatFrame: HexPosition, HexMovement, HasActedThisRound
  - Mission: MapSize
  - PlannedAction: TargetHex, TargetFrameId
  - CombatLog: Stalemate result

### Removed
- TacticalDecisionWindow.xaml/.cs (replaced by inline hex click interaction)
- RangeBand enum (replaced by hex distance)
- MovementDirection enum (replaced by hex coordinate movement)
- Old range band accuracy tables
- Old canvas strip battlefield map

---

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
- ~~Graphical star map UI~~ — Done (v0.9.0)
- ~~Audio system~~ — Done (v0.9.1)
- Active equipment actions (Thrust Pack, ECM, Barrier, Targeting Uplink)
- Special weapon effects
- Pilot skill leveling on XP thresholds
- Pilot morale/stress checks
- Settings screen (difficulty, audio volume)
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

### Milestone 6: Hex Grid Combat - Complete
- ✅ Hex grid battlefield with pointy-top hexes and axial coordinates
- ✅ Individual unit activation with initiative order
- ✅ Click-to-move and click-to-fire hex interaction
- ✅ Distance-based weapon accuracy curves
- ✅ Per-class hex movement (Light=4, Medium=3, Heavy=2, Assault=1)
- ✅ Graphical unit shapes per weight class
- ✅ Variable map sizes (Small/Medium/Large) per mission difficulty
- ✅ Hex-aware AI with pathfinding and optimal distance targeting

### Milestone 7: Economy & Repair Overhaul - Complete
- ✅ Time-based repair system (1-7 days based on damage)
- ✅ Rush repair option (2x cost, half time)
- ✅ Daily frame maintenance costs (Light $500 to Assault $3,500/day)
- ✅ Per-mission deployment costs (Light $2K to Assault $12K)
- ✅ Day report with maintenance and repair completion summaries
- ✅ Upkeep display in status bar

### Milestone 8: Terrain, Deployment & Refit - Complete
- ✅ 5 terrain types with movement costs and defense bonuses
- ✅ Procedural terrain generation with deployment zone protection
- ✅ Terrain-aware pathfinding (BFS and A*)
- ✅ Pre-combat deployment phase with manual player placement
- ✅ Visual refit bay with mech diagram and staged changes
- ✅ Refit costs (500 credits + 1 day per weapon change)
- ✅ Pointy-top hex orientation with rectangular grid

### Milestone 9: LOS, Hex Tiles & Targeting - Complete
- ✅ Kenney hex tile terrain rendering (5 terrain tile images)
- ✅ Line of sight system with intervening terrain penalties (Forest -5%, Rocks -3% per hex)
- ✅ Target selection cursor with LOS line visualization and hit chance breakdown
- ✅ Hardpoint slot system in refit bay (fixed positions, size-coded, smart cost diff)

### Milestone 10: Equipment System - Complete
- ✅ 12 equipment items across 3 categories (Passive, Active, Slot)
- ✅ Equipment competes with weapons for chassis space budget
- ✅ Slot equipment occupies hardpoints (Small, Medium, Large)
- ✅ Combat effects: accuracy modifiers, missile intercept, damage reduction, reactor boost, ammo bonus
- ✅ Equipment in Market (buy), Inventory (sell), and Refit Bay (install/remove)
- ✅ Database schema v6 with Equipment, EquipmentLoadout, EquipmentInventory tables

### Milestone 11: Galaxy Travel & Faction Territories - Complete
- ✅ 11 star systems with faction control (3 Directorate, 3 Crucible, 3 Outer Reach, 2 contested)
- ✅ 25 planets/stations with types, services, and contract difficulty ranges
- ✅ 16 bidirectional jump routes with variable fuel cost and travel time
- ✅ Fuel system (100 capacity, $500/unit, intra-system 5 fuel/1 day)
- ✅ Location-based contract generation (80% bias to controlling faction)
- ✅ Galaxy UI tab with travel, fuel purchase, and system overview
- ✅ Database schema v7 with StarSystem, Planet, JumpRoute tables
- ✅ Player starts at Crossroads (contested, gateway to all territories)
- ✅ X/Y coordinates stored for future graphical star map

### Milestone 12: Audio & Visual Polish - In Progress
- ✅ Audio system with 9 sound effects (UI clicks, combat, outcomes)
- ✅ Station terrain tile art (metal floor panels, cargo crates, hazard grating)
- ✅ Landscape-aware terrain rendering (Station/Industrial use metal tiles)
- ⏳ Balance testing
- Active equipment actions (Thrust Pack, ECM, Barrier, Targeting Uplink)
- Pilot skill leveling on XP thresholds
- Settings screen (audio volume, difficulty)
- Tutorial/onboarding

---

## Development Statistics

- **Source Files**: ~71 (.cs files)
- **XAML Files**: 8
- **Sound Effects**: 9 (.wav files)
- **Terrain Tiles**: 8 (.png hex tiles — 5 nature + 3 station)
- **Projects**: 3
- **Database Tables**: 15 + SchemaVersion (Chassis, Weapon, Equipment, FrameInstance, Loadout, EquipmentLoadout, Pilot, PlayerState, Inventory, EquipmentInventory, Faction, FactionStanding, StarSystem, Planet, JumpRoute)
- **Seeded Records**: 101 (12 chassis + 16 weapons + 12 equipment + 4 pilots + 2 frames + 3 factions + 11 systems + 25 planets + 16 routes)

---

## Known Issues

- Database file auto-recreates on schema version mismatch (drops all player data)
- Hex map recalculates on window resize (may flash briefly)
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
