# Mechanized Armour Commander - Project Summary

## What We've Built

A WPF C# tactical mech combat game with full campaign management, a 3-faction ecosystem, and a complete resource-based combat system built on three core layers:

### 1. **MechanizedArmourCommander.Data** - Data Access Layer
- **Database Models**: Chassis, Weapon, FrameInstance, Loadout, Pilot, PlayerState, InventoryItem, Faction, FactionStanding
- **Repositories**: 11 repositories (Chassis, Weapon, FrameInstance, Loadout, Pilot, PlayerState, Inventory, Faction, FactionStanding)
- **DatabaseContext**: SQLite database initialization, schema versioning (v5), auto-migration
- **DataSeeder**: Seeds 3 factions, 12 chassis, 16 weapons (incl. 3 faction exclusives), 4 pilots, starting frames
- **Schema**: 9 tables + SchemaVersion table for automatic migration detection

### 2. **MechanizedArmourCommander.Core** - Game Logic & Combat Engine
- **Combat Models**:
  - CombatFrame - Per-location armor/structure, reactor energy, weapon groups, action points
  - CombatEnums - HitLocation, RangeBand, CombatAction, ComponentDamageType, MovementDirection
  - CombatLog - Comprehensive event logging with 14 event types
  - TacticalOrders - Stance, TargetPriority, WithdrawalThreshold, PreferredRange
  - RoundTacticalDecision - Per-frame action orders with PlannedAction lists
- **Combat Subsystems**:
  - CombatEngine - Orchestrates all subsystems, resolves rounds and full combats
  - DamageSystem - Hit locations, layered damage (armor/structure), component damage, damage transfer
  - ReactorSystem - Energy refresh, consumption, overload stress, shutdown mechanics
  - ActionSystem - AP management, action validation, cost enforcement
  - PositioningSystem - Range band management, movement costs, range accuracy modifiers
  - CombatAI - Stance-based decision generation, target selection, weapon group scoring
- **Services**:
  - CombatService - High-level combat management, round execution, log formatting
  - ManagementService - Roster, economy, refit, inventory, faction market operations
  - MissionService - Faction-aware mission generation, enemy building, results processing

### 3. **MechanizedArmourCommander.UI** - WPF Desktop Application
- **MainMenuWindow** - Title screen with NEW GAME, LOAD GAME, SETTINGS, EXIT
- **SaveSlotWindow** - 5 named save slots, each stored as a separate .db file
- **ManagementWindow** - Company HQ with 7 sections:
  - Roster (view/rename/repair/sell frames), Pilots, Market (faction-filtered with discounts), Inventory, Refit, Missions (faction-driven contracts), Deploy
  - Status bar with credits, reputation, day counter, faction standings
- **MainWindow** - Combat view with:
  - Battlefield map (Canvas-based X,Y unit plotting with grid overlay)
  - Three-column layout (Player Forces | Combat Feed | Enemy Forces)
  - Terminal-style green-on-black aesthetic
  - Frame status with reactor energy, armor %, pilot info
  - Tactical mode toggle for round-by-round or auto-resolve combat
- **TacticalDecisionWindow** - Per-frame action planning:
  - Situation report with per-location damage, reactor status, weapon groups
  - Frame tab switching for multi-frame planning
  - Action slot selection with sub-options (weapon groups, movement direction, called shot locations)
  - Focus target selection, withdrawal option, auto-resolve toggle
- **PostCombatWindow** - Results, salvage selection (with values), faction standing changes
- **FrameSelectorWindow** - Frame configuration:
  - Browse 12 chassis with class filtering
  - Detailed specs (reactor output, movement cost, structure, space budget)
  - Dynamic weapon loadout with space budget tracking
  - Tactical assessment per chassis class
- **SettingsWindow** - Settings (placeholder)

## File Structure

```
Project M/
├── src/
│   ├── MechanizedArmourCommander.UI/
│   │   ├── MainMenuWindow.xaml          [Title screen & profile selection]
│   │   ├── SaveSlotWindow.xaml          [Save slot management (5 slots)]
│   │   ├── ManagementWindow.xaml        [Company HQ — roster, market, refit, missions]
│   │   ├── MainWindow.xaml              [Combat UI + Battlefield Map]
│   │   ├── TacticalDecisionWindow.xaml  [Per-frame action planner]
│   │   ├── FrameSelectorWindow.xaml     [Frame configuration]
│   │   ├── PostCombatWindow.xaml        [Results, salvage, faction standings]
│   │   └── SettingsWindow.xaml          [Settings (placeholder)]
│   │
│   ├── MechanizedArmourCommander.Core/
│   │   ├── Models/
│   │   │   ├── CombatFrame.cs           [Runtime frame state + EquippedWeapon]
│   │   │   ├── CombatEnums.cs           [HitLocation, RangeBand, CombatAction, etc.]
│   │   │   ├── CombatLog.cs             [Combat events + 14 event types]
│   │   │   ├── TacticalOrders.cs        [Stance, TargetPriority, WithdrawalThreshold]
│   │   │   ├── RoundTacticalDecision.cs [Per-frame actions, FrameSituation, WeaponGroupInfo]
│   │   │   ├── Mission.cs              [Mission contract + EnemySpec]
│   │   │   └── MissionResults.cs       [Post-combat results, salvage, faction changes]
│   │   ├── Combat/
│   │   │   ├── CombatEngine.cs          [Round resolution, to-hit, initiative]
│   │   │   ├── CombatAI.cs              [Stance-based AI action generation]
│   │   │   ├── DamageSystem.cs          [Layered damage, hit locations, components]
│   │   │   ├── ReactorSystem.cs         [Energy, stress, overload, shutdown]
│   │   │   ├── ActionSystem.cs          [AP costs, action validation]
│   │   │   └── PositioningSystem.cs     [Range bands, movement, accuracy modifiers]
│   │   └── Services/
│   │       ├── CombatService.cs         [High-level combat API]
│   │       ├── ManagementService.cs     [Roster, economy, refit, faction market]
│   │       └── MissionService.cs        [Faction-aware mission generation]
│   │
│   └── MechanizedArmourCommander.Data/
│       ├── Models/
│       │   ├── Chassis.cs               [ReactorOutput, MovementEnergyCost, FactionId]
│       │   ├── Weapon.cs                [WeaponType, EnergyCost, AmmoPerShot, FactionId]
│       │   ├── FrameInstance.cs          [Per-location armor, ReactorStress, CustomName]
│       │   ├── Loadout.cs               [WeaponGroup, MountLocation]
│       │   ├── Pilot.cs
│       │   ├── PlayerState.cs           [Credits, reputation, company name, day]
│       │   ├── InventoryItem.cs         [Company weapon inventory]
│       │   ├── Faction.cs               [Name, color, weapon/chassis preference]
│       │   └── FactionStanding.cs       [Standing, StandingLevel, PriceModifier]
│       ├── Repositories/
│       │   ├── ChassisRepository.cs     [+ GetByFaction]
│       │   ├── WeaponRepository.cs      [+ GetByFaction]
│       │   ├── FrameInstanceRepository.cs
│       │   ├── LoadoutRepository.cs
│       │   ├── PilotRepository.cs
│       │   ├── PlayerStateRepository.cs
│       │   ├── InventoryRepository.cs
│       │   ├── FactionRepository.cs
│       │   └── FactionStandingRepository.cs
│       ├── DatabaseContext.cs            [Schema v5, auto-migration]
│       └── DataSeeder.cs                [3 factions + 12 chassis + 16 weapons]
│
├── CORE_RULES.md                        [Authoritative game rules reference]
├── README.md
├── PROJECT_SUMMARY.md                   [This File]
├── CHANGELOG.md
└── MechanizedArmourCommander.sln
```

## Combat System Architecture

### Resource Model
The combat system is built around two competing resource types:

- **Reactor Energy** (renewable): Refreshes each round to reactor output. Energy weapons consume it. Moving costs it. The key per-round bandwidth constraint.
- **Ammunition** (finite): Ballistic and missile weapons consume ammo. Free in terms of energy but runs out permanently. The long-term constraint.

This creates meaningful decisions: energy weapons are sustainable but compete with movement for reactor budget. Ballistic weapons are energy-efficient but will eventually run dry.

### Weight Class Identity
Weight classes emerge naturally from physics rather than arbitrary rules:

| Class   | Reactor | Move Cost | Space  | Identity                           |
|---------|---------|-----------|--------|------------------------------------|
| Light   | 10-12   | 2-3E      | 35-45  | Fast, cheap to move, small payload |
| Medium  | 15-18   | 4-5E      | 50-65  | Balanced reactor and mobility      |
| Heavy   | 20-24   | 6-8E      | 70-85  | Big reactor, expensive to move     |
| Assault | 25-30   | 9-12E     | 85-110 | Massive firepower, nearly static   |

### Damage Model
```
Incoming Damage
      ↓
Hit Location Roll (Head 5%, CT 20%, LT 15%, RT 15%, LA 10%, RA 10%, Legs 25%)
      ↓
Armor absorbs damage (ablative, loadout choice)
      ↓
Excess goes to Structure (fixed by chassis)
      ↓
~40% chance of Component Damage when structure hit
  - Weapon destroyed, actuator damaged, ammo explosion
  - Reactor hit, gyro hit, sensor hit, cockpit hit
      ↓
Location destroyed → damage transfers (arm→torso, side torso→CT)
      ↓
Center Torso structure ≤ 0 → Frame Destroyed
```

### Combat Round Flow
```
1. Refresh Phase
   - Reactor energy resets to effective output
   - Action points reset to 2 (1 if gyro damaged)

2. Initiative Phase
   - Sort by Speed + PilotPiloting (highest first)

3. Action Phase (per frame in initiative order)
   - Execute planned actions from player/AI decisions
   - Each action costs AP and possibly energy
   - Fire actions: to-hit roll → damage → location → cascade

4. Overwatch Resolution
   - Frames on overwatch fire at enemies who moved

5. End of Round
   - Process reactor stress (overload check, shutdown risk)
   - Clear combat flags (bracing, overwatch)
   - Natural stress dissipation
```

### To-Hit Calculation
```
Hit Chance = Base Weapon Accuracy
           + (Pilot Gunnery × 2)
           - Target Evasion
           + Range Modifier (weapon class vs range band)
           - Brace Bonus (if target bracing)
           - Sensor Penalty (if attacker has sensor damage)
           - Called Shot Penalty (-20 for called shots)
           - Actuator Penalty (-10 for arm actuator damage)

Clamped to 5-95% range.
Critical hit at 5% chance → triggers component damage.
```

## How to Play

1. Open the solution in Visual Studio or run `dotnet run --project src/MechanizedArmourCommander.UI`
2. Main menu appears — click **NEW GAME**
3. Select a save slot and enter your company name
4. You start with 500,000 credits, 2 frames, and 4 pilots
5. Browse **MISSIONS** — each contract shows employer/opponent factions
6. **DEPLOY** your lance (1-4 frames with pilots)
7. Fight in tactical or auto-resolve mode
8. After combat: collect salvage, view faction standing changes
9. Visit faction **MARKET** for discounted equipment as standings improve
10. **SAVE & EXIT** returns to main menu

## Architecture Highlights

### Separation of Concerns
- **Data Layer**: Pure data access, schema management, no business logic
- **Core Layer**: All game logic, independent of UI, testable in isolation
- **UI Layer**: Presentation and user interaction, delegates to Core services

### Design Patterns
- **Repository Pattern**: Clean data access abstraction
- **Service Layer**: High-level operations (CombatService)
- **Subsystem Architecture**: Combat engine delegates to specialized systems (Damage, Reactor, Action, Positioning)
- **Event Logging**: Immutable combat history via CombatLog/CombatRound/CombatEvent

### Schema Versioning
The database uses a SchemaVersion table. When the code schema version doesn't match the database, all tables are dropped and recreated with fresh seed data. This handles development iteration without manual migration.

## Technical Details
- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite 3 with Microsoft.Data.Sqlite
- **Language**: C# 13
- **Build Status**: Clean build, 0 warnings, 0 errors

---

*Last Updated: 2026-02-07*
