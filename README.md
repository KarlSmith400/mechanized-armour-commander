# Mechanized Armour Commander

A tactical mech combat game with full campaign management. Build and maintain a company of combat frames, hire and train pilots, accept mission contracts, and lead your lance into turn-based tactical combat. Salvage enemy wreckage, refit your frames, and grow your mercenary company.

## Project Structure

```
MechanizedArmourCommander/
├── src/
│   ├── MechanizedArmourCommander.UI/          # WPF Desktop Application
│   │   ├── MainMenuWindow.xaml                # Title screen & profile selection
│   │   ├── SaveSlotWindow.xaml                # Save slot management (5 slots)
│   │   ├── ManagementWindow.xaml              # Company HQ (roster, market, refit, missions)
│   │   ├── MainWindow.xaml                    # Hex grid combat view
│   │   ├── RefitWindow.xaml                   # Visual refit bay with mech diagram
│   │   ├── FrameSelectorWindow.xaml           # Frame/loadout browser
│   │   ├── PostCombatWindow.xaml              # Results, salvage selection
│   │   ├── SettingsWindow.xaml                # Settings (placeholder)
│   │   ├── AudioService.cs                    # Sound manager (9 effects)
│   │   ├── Resources/Hex/                     # Terrain hex tiles (5 nature + 3 station)
│   │   └── Resources/Sounds/                  # WAV sound effects
│   │
│   ├── MechanizedArmourCommander.Core/        # Game Logic & Systems
│   │   ├── Models/                            # Game models (CombatFrame, HexCoord, HexGrid, etc.)
│   │   ├── Combat/                            # Combat subsystems (engine, AI, pathfinding, damage)
│   │   └── Services/                          # CombatService, ManagementService, MissionService, GalaxyService
│   │
│   └── MechanizedArmourCommander.Data/        # Data Access Layer
│       ├── Models/                            # Database entities (Chassis, Weapon, Pilot, StarSystem, Planet, etc.)
│       ├── Repositories/                      # Data access (17 repositories)
│       └── DatabaseContext.cs                 # SQLite management & schema versioning
│
├── CORE_RULES.md                              # Authoritative game rules reference
├── LORE.md                                    # Faction backstories & universe setting
├── CHANGELOG.md                               # Detailed change history
├── MechanizedArmourCommander.sln              # Visual Studio solution
└── README.md                                  # This file
```

## Technology Stack

- **Framework**: .NET 9
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Language**: C# 13
- **Architecture**: 3-layer (UI → Core → Data), repository pattern, no EF

## Features

### Main Menu & Save System
- Title screen with NEW GAME, LOAD GAME, SETTINGS, EXIT
- 5 named save slots, each stored as a separate `.db` file
- Custom company name on new game creation
- Delete and overwrite existing saves

### Faction System
- **3 Factions**: Crucible Industries (corporate/energy), Terran Directorate (military/balanced), Outer Reach Collective (frontier/ballistic)
- **Standing Levels**: Hostile → Neutral → Friendly → Allied → Trusted
- **Faction Markets**: Each faction sells its own chassis and weapons, plus universal items
- **Standing-Based Discounts**: Friendly 5%, Allied 10%, Trusted 20% off
- **Exclusive Weapons**: High-tier faction weapons unlocked at Allied standing (200+)
- **Faction-Biased Enemies**: Enemy forces use their faction's chassis and weapons (70% bias)
- **Standing Consequences**: Winning missions raises employer standing, lowers rival standings

### Company Management (HQ)
- **Roster**: View owned frames with status, assigned pilot, loadout, repair/sell/rename
- **Pilots**: Hire, assign, and track pilot stats (Gunnery, Piloting, Tactics)
- **Market**: Persistent per-planet stock (refreshes weekly), roll-based availability weighted by faction standing — small weapons always available, heavy/assault chassis rare, standing-based discounts and exclusive items
- **Inventory**: Company weapon and equipment storage — buy, sell, salvage flows through here
- **Refit Bay**: Visual mech diagram with click-to-equip weapons and equipment, weapon group management (◄/► cycling with reactor budget display), staged changes with running cost display, confirm/reset
- **Galaxy**: Star map with 11 systems, travel between planets and jump between systems, buy fuel, faction territory overview
- **Missions**: Browse 3 location-based contracts biased to current system's controlling faction
- **Deploy**: Select 1-4 frames with pilots for mission deployment

### Hex Grid Tactical Combat
- **Hex Grid Battlefield**: Pointy-top hex map with varying sizes per mission difficulty (Small 12x10, Medium 16x12, Large 20x14)
- **Terrain System**: 5 terrain types (Open, Forest, Rocks, Rough, Sand) rendered with Kenney hex tile assets — Forest gives +15% defense and +1 move cost, Rocks give +10% defense and +1 move cost. Landscape-aware tile sets: nature tiles for planetary surfaces, metal/industrial tiles for station interiors
- **Pre-Combat Deployment**: Manual unit placement in deployment zone (leftmost 2 columns) while AI auto-deploys on the opposite side
- **Individual Unit Activation**: Turn-based initiative order — each unit acts in sequence (XCOM/Fire Emblem style)
- **Hex Movement**: Units move across hexes based on weight class (Light=4, Medium=3, Heavy=2, Assault=1 hexes per move), terrain-aware pathfinding
- **Distance-Based Accuracy**: Weapon effectiveness scales with hex distance — Short weapons optimal at 2-4 hexes, Medium at 4-7, Long at 7-10
- **Action Point Economy**: 2 AP per frame per turn (Move, Fire, Brace, Called Shot, Overwatch, Vent, Sprint)
- **Equipment System**: 12 equipment items across 3 categories (Passive, Active, Slot) — Cooling Vents, Reactive Armor, Stealth Plating, Point Defense, and more with combat-integrated effects
- **Head Destruction**: Cockpit breach triggers pilot survival roll (50% + Piloting skill) — survive with heavy penalties or pilot KIA and frame shuts down
- **Layered Damage**: 7 hit locations with Armor → Structure → Component damage cascade, Reactive Armor reduces structure damage
- **Reactor Energy**: Manage energy budget each turn — Cooling Vents boost output, overloading risks shutdown
- **Weapon Groups**: Assign weapons to groups (1–4) via Refit Bay, fire as single actions — reactor budget panel shows per-group energy/damage with over-budget warnings, group reassignment is free
- **Line of Sight**: Intervening terrain (Forest -5%/hex, Rocks -3%/hex) penalizes shots fired through blocking hexes
- **Target Cursor**: Hover over enemies when targeting to see LOS line, intervening terrain penalties, and full hit chance breakdown
- **AI Opponents**: Hex pathfinding with Aggressive/Balanced/Defensive stances and optimal distance targeting
- **Graphical Map**: Kenney hex tile terrain with geometric unit shapes per weight class (diamond/pentagon/hexagon/octagon)

### Post-Combat
- Victory/Defeat/Withdrawal outcomes with financial report
- Interactive salvage selection from destroyed enemy wreckage (with item values displayed)
- **Frame salvage from head kills** — purchase structurally intact enemy frames at 40% price with combat damage applied
- Faction standing changes based on mission outcome
- Pilot XP gain, injury tracking, KIA
- Per-frame damage reports with repair costs

### Galaxy Travel
- **11 Star Systems**: 3 Directorate (core), 3 Crucible (mid-rim), 3 Outer Reach (fringe), 2 contested
- **25+ Planets/Stations**: Each with market, hiring, and contract difficulty range
- **16 Jump Routes**: Bidirectional connections between systems (10-20 fuel, 2-4 days)
- **Fuel System**: 100 unit capacity, $500/unit, intra-system travel costs 5 fuel/1 day
- **Location-Based Contracts**: Faction-controlled systems bias 80% of contracts to controlling faction
- **Player starts at Crossroads** — contested neutral space, gateway to all faction territories

### Audio System
- **9 Sound Effects**: UI clicks, weapon fire, hit confirm, miss, mech destroyed, turn start, error, victory fanfare, defeat sting
- **Universal Button Clicks**: Every button across all 8 windows plays a click sound via routed event handlers
- **Combat Audio**: Weapon fire, hit/miss feedback, frame destruction, turn start alerts, victory/defeat outcomes
- **Mute Support**: Global mute toggle via AudioService

### Economy & Upkeep
- Starting credits: 500,000
- Chassis: 100K (Light) to 650K (Assault)
- **Daily maintenance**: Light $500, Medium $1,000, Heavy $2,000, Assault $3,500 per day
- **Deployment costs**: Light $2K, Medium $4K, Heavy $7.5K, Assault $12K per mission per frame
- **Repair time**: Damaged frames require days to repair (1-5 days based on damage, 7 days if destroyed)
- **Rush repairs**: Pay 2x cost to halve repair time
- Repair costs scale with damage (30% of chassis price x damage ratio)
- Mission rewards: 40K-300K based on difficulty
- Pilot hiring: 30,000 credits

## How to Run

### Prerequisites
- .NET 9 SDK or later
- Windows 10/11 (required for WPF)

### Build and Run

```bash
dotnet build MechanizedArmourCommander.sln
dotnet run --project src/MechanizedArmourCommander.UI/MechanizedArmourCommander.UI.csproj
```

Or open `MechanizedArmourCommander.sln` in Visual Studio 2022, set `MechanizedArmourCommander.UI` as startup project, and press F5.

### First Launch
1. Main menu appears — click **NEW GAME**
2. Select a save slot and enter your company name
3. You start with 500,000 credits, 2 frames, and 4 pilots
4. Browse **MISSIONS**, **DEPLOY** your lance, and fight
5. After combat, collect salvage and manage your company
6. **SAVE & EXIT** returns to main menu

## Lore & Setting

See [LORE.md](LORE.md) for the full backstory covering:
- The Shattered Accord — how humanity's unified government collapsed
- Combat Frames — the dominant weapons platform of the era
- Crucible Industries — corporate megacorp, energy weapons, attrition warfare
- Terran Directorate — military authority, balanced doctrine, claims legitimacy
- Outer Reach Collective — frontier alliance, fast frames, ballistic pragmatism
- The mercenary life and faction standing system
- Galaxy geography — core, mid-rim, and fringe systems

## Game Rules

See [CORE_RULES.md](CORE_RULES.md) for the complete authoritative rules reference (17 sections, tabletop-ready). Covers:
- Combat round structure, action economy, and end conditions
- Full accuracy formula with all modifiers (d100 roll-under)
- Damage resolution, hit locations, component damage, and damage transfer
- Reactor energy, overloading, and shutdown mechanics
- Complete weapon reference (16 weapons with full stat blocks)
- All 12 chassis stat blocks with structure values per location
- Equipment system (12 items across 3 categories)
- Faction standing system (5 tiers, standing change formulas)
- Mission generation (enemy composition, difficulty scaling, reward tables)
- Economy (upkeep, deployment costs, repair, market stock, salvage allowance)
- Galaxy travel and location-based contracts
- Tabletop quick reference (dice, turn sequence, attack resolution, economy cheat sheet)

## Development Status

### Completed
- Hex grid tactical combat with individual unit activation and graphical hex map
- Combat system with action points, hex movement, distance-based accuracy, layered damage, reactor energy
- Terrain system with 5 types rendered with Kenney hex tiles, landscape-aware tile sets (nature + station)
- Line of sight system with intervening terrain penalties and targeting cursor with hit chance breakdown
- Pre-combat deployment phase with manual player placement and AI auto-deploy
- Visual refit bay with mech diagram, click-to-equip, staged changes, and cost preview
- Full management hub (roster, pilots, market, inventory, refit, missions, deploy)
- Mission generation with variable map sizes and post-combat results with salvage
- Pilot system with XP, injuries, KIA
- Main menu with 5 named save slots
- Campaign loop: Menu → HQ → Galaxy Travel → Deploy → Deployment → Combat → Results → HQ
- 3-faction system with standings, faction markets, discounts, and exclusive weapons
- Equipment system: 12 items (Passive/Active/Slot) with combat effects, market, inventory, and refit integration
- Galaxy travel: 11 star systems, 25+ planets/stations, 16 jump routes, fuel economy, location-based contracts
- Visual galaxy map with faction-colored star systems, jump route connections, and interactive travel
- Audio system: 9 sound effects (UI clicks, combat sounds, outcome stings) across all windows
- Weapon group management in Refit Bay with reactor budget display and free group reassignment
- Persistent market stock system with roll-based availability weighted by faction standing (weekly refresh per planet)
- Frame salvage from head kills — purchase enemy frames at 40% cost with combat damage applied
- Tabletop-ready CORE_RULES.md (17 sections) with complete weapon/chassis stat blocks, faction standing, mission generation, and quick reference
- Frame renaming from roster UI
- Salvage value display on post-combat loot screen
- Repair time system (repairs take days, rush option at 2x cost)
- Upkeep economy (daily maintenance per frame, per-mission deployment costs)

### Planned
- Pilot skill leveling on XP thresholds
- Special weapon effects
- Active equipment actions (Thrust Pack jump, ECM activation, Barrier deploy)
- Settings screen (difficulty, audio volume controls)
- Tutorial/onboarding

### Planned Audio Additions
Priority sounds to improve game feel, organized by tier:

**Tier 2 — Atmosphere**
- Mech movement (servo/footstep), brace (shield charge), overwatch set/trigger (targeting lock)
- Critical hit alarm, purchase/sell sounds, repair completion

**Tier 3 — Polish**
- Fuel purchase, jump travel FTL whoosh, day advance tick, deploy confirmation
- Nav tab switch, window transitions

**Tier 4 — Ambient (Future)**
- Combat background ambience (industrial hum, wind)
- Station interior ambience
- Management screen background music
- Main menu theme music

## Design Philosophy

- **Management over twitch** — Player wins through preparation, not execution
- **Meaningful choices** — Every decision has tradeoffs
- **Resource scarcity** — Can't have everything, forces prioritization
- **Emergent narratives** — Pilots and frames develop history through play
- **Clear feedback** — Player always knows why they won or lost

## License

**Proprietary Software - All Rights Reserved**

Copyright 2026. This software is proprietary and confidential. Unauthorized copying, distribution, modification, or use of this software, via any medium, is strictly prohibited without explicit written permission from the copyright holder.

This code is provided for **viewing and portfolio purposes only**. No license is granted for any use whatsoever.

See [LICENSE](LICENSE) file for complete terms.

## Contact

For licensing inquiries or permission requests, please contact the repository owner.

---

*Last Updated: 2026-02-16*
