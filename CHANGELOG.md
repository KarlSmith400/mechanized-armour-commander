# Changelog - Mechanized Armour Commander

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added
- **Tactical Mode (Round-by-Round Combat)** ✨ NEW
  - TacticalDecisionWindow.xaml - Situation report and decision interface
  - Pause at start of each combat round for player decisions
  - Detailed situation report with frame status, positions, and damage
  - Global tactical overrides (Stance, Target Priority)
  - Focus fire on specific targets
  - Attempt withdrawal option
  - Auto-resolve remaining rounds option
  - "AUTO (AI DECIDES)" button to let AI handle specific rounds
  - Visual health/heat/ammo bars for all frames
  - Integration toggle in MainWindow ("Tactical Mode" checkbox)

- **Frame Selector UI**
  - FrameSelectorWindow.xaml - Full terminal-style UI for frame configuration
  - Browse all 12 chassis from database with class filtering
  - Detailed chassis specifications display
  - Tactical assessment for each chassis type
  - Dynamic weapon loadout configuration based on hardpoints
  - Real-time weapon selection per hardpoint slot
  - Integration with MainWindow - adds frames to player lance
  - Supports up to 4 frames per lance

- **Positional Combat System**
  - Individual frame position tracking (Team 1 at -10, Team 2 at +10)
  - Distance-based range bands (Short 0-5, Medium 6-15, Long 16+)
  - Range accuracy modifiers (+10% optimal, -5%/-15% penalties)
  - Formation-based deployment (Tight, Spread, Flanking)
  - Movement based on stance and frame speed
  - Realistic positioning without graphics

- **AI Integration**
  - Sophisticated target selection via CombatAI
  - Automatic withdrawal logic based on tactical orders
  - Threat scoring for intelligent targeting
  - Opportunity targeting for finishing weakened enemies
  - Focus fire and spread damage strategies

- **Enhanced Combat Mechanics**
  - PositioningSystem.cs - handles all positional logic
  - Range-based weapon effectiveness
  - Movement messages showing position changes
  - Formation actually affects combat positioning

### Planned Features
- ~~Load frames from database instead of hardcoded test data~~ ✅ DONE (Frame Selector)
- ~~Frame and weapon browser UI~~ ✅ DONE (Frame Selector)
- ~~Round-by-round tactical intervention UI~~ ✅ DONE (Tactical Mode)
- Full multi-round tactical combat loop (currently proof-of-concept)
- Lance management screen (save/load lance configurations)
- Pilot roster system with pilot data seeding
- Save/load game state
- Mission generation system
- Full tactical orders UI controls (stance, formation, withdrawal)
- Frame customization (rename frames, assign pilots)
- Frame-specific commands in tactical mode (Hold Fire, Evasive, All-Out)

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
  - Damage resolution with 6 hit locations (CT, RT, LT, RA, LA, LEGS)
  - Critical hit system (5% chance, 2x damage)
  - Heat generation and dissipation mechanics
  - Ammunition tracking and consumption
  - Frame destruction detection
  - Victory/defeat condition checking
  - `CombatService` - High-level combat management and log formatting

- **User Interface**
  - WPF main window with terminal-style aesthetic (green-on-black)
  - Three-column layout:
    - Left: Player forces display
    - Center: Combat feed with scrolling text output
    - Right: Enemy forces display
  - Combat control buttons (Start Combat, Reset)
  - Database statistics display on startup
  - Test combat scenario (2v2 engagement)

- **Documentation**
  - `mechanized-armour-commander-design.md` - Complete game design document
  - `README.md` - Project overview and setup instructions
  - `PROJECT_SUMMARY.md` - Technical implementation summary
  - `RENAME_INSTRUCTIONS.md` - Project renaming guide
  - `CHANGELOG.md` - This file

- **Development Tools**
  - `rename-project.ps1` - PowerShell script for automated renaming
  - `test-database.ps1` - Database verification script
  - `test/DatabaseTest` - Console application for database testing
  - `.gitignore` - Git ignore configuration

#### Changed
- Project renamed from "MechCommander" to "Mechanized Armour Commander"
- All namespaces updated to `MechanizedArmourCommander.*`
- Window title updated to "MECHANIZED ARMOUR COMMANDER - COMBAT PROTOTYPE"

#### Fixed
- Database connection management in repositories (removed improper `using` statements)
- Accidental `nul` file creation removed
- Repository connection disposal conflicts resolved

#### Technical Details
- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite 3 with Microsoft.Data.Sqlite
- **Language**: C# 13
- **Build Status**: Clean build, 0 warnings, 0 errors

---

## Milestone Progress

### Milestone 1: Combat Prototype (Current) - 75% Complete
- ✅ Combat resolution system implemented
- ✅ Basic weapon and frame models created
- ✅ Database schema and seeding complete
- ✅ Text-based combat feed working
- ⏳ Balance testing in progress
- ⏳ Tactical orders UI controls pending

### Milestone 2: Management Core - Not Started
- Hangar/workshop screens
- Repair and salvage systems
- Economy tracking
- Save/load functionality

### Milestone 3: Mission System - Not Started
- Mission generation
- Pre-mission loadout screen
- Tactical orders implementation
- Post-mission flows

### Milestone 4: Pilot System - Not Started
- Pilot roster management
- Skill progression
- Injury/fatigue mechanics
- Performance tracking

### Milestone 5: Polish & Balance - Not Started
- UI improvements
- Balance tuning
- Bug fixes
- Tutorial/onboarding

---

## Development Statistics

- **Total Lines of Code**: ~3,500+
- **Source Files**: 24 (.cs files)
- **XAML Files**: 2
- **Projects**: 3 (+ 1 test project)
- **Database Tables**: 5
- **Seeded Records**: 25 (12 chassis + 13 weapons)
- **Development Time**: 1 session
- **Commits**: Initial development

---

## Known Issues

None currently - all systems operational.

---

## Notes

- Database file (`MechanizedArmourCommander.db`) is created on first run
- Only seeds data once (checks for existing data)
- Test scenario uses hardcoded frames (will be replaced with database loading)
- Combat is fully auto-resolved based on pre-mission tactical orders

---

*This changelog follows [Semantic Versioning](https://semver.org/).*
*For the complete design specification, see [mechanized-armour-commander-design.md](mechanized-armour-commander-design.md).*
