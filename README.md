# Mechanized Armour Commander

A management-focused mechanized combat simulator where players build and maintain a stable of combat frames, manage pilots, and accept contracts. Combat is auto-resolved with strategic pre-mission decisions driving outcomes.

## Project Structure

```
MechanizedArmourCommander/
├── src/
│   ├── MechanizedArmourCommander.UI/          # WPF Desktop Application
│   │   ├── Views/                             # XAML views
│   │   ├── ViewModels/                        # MVVM view models
│   │   ├── Controls/                          # Custom controls
│   │   └── MainWindow.xaml                    # Main application window
│   │
│   ├── MechanizedArmourCommander.Core/        # Game Logic & Systems
│   │   ├── Models/                            # Core game models (CombatFrame, TacticalOrders, etc.)
│   │   ├── Combat/                            # Combat resolution engine
│   │   └── Services/                          # High-level services (CombatService, etc.)
│   │
│   └── MechanizedArmourCommander.Data/        # Data Access Layer
│       ├── Models/                            # Database entity models (Chassis, Weapon, Pilot, etc.)
│       ├── Repositories/                      # Data access repositories
│       └── DatabaseContext.cs                 # SQLite database management
│
├── mechanized-armour-commander-design.md      # Complete design document
├── MechanizedArmourCommander.sln              # Visual Studio solution
└── README.md                                  # This file
```

## Technology Stack

- **Framework**: .NET 9
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Language**: C# 13

## Current Status: Combat Prototype

The project is currently at **Milestone 1: Combat Prototype** phase with the following features implemented:

### Completed Features
- ✅ Project structure with 3 layered projects (UI, Core, Data)
- ✅ Core data models (Chassis, Weapon, Pilot, FrameInstance, Loadout)
- ✅ Combat runtime models (CombatFrame, TacticalOrders, CombatLog)
- ✅ SQLite database setup with schema
- ✅ Repository pattern for data access
- ✅ Combat resolution engine with:
  - Initiative system (speed-based)
  - To-hit calculations (accuracy, gunnery, evasion)
  - Damage resolution with hit locations
  - Critical hits (5% chance, 2x damage)
  - Heat and ammo management
  - Combat logging system
- ✅ Basic WPF UI for combat testing
  - Player and enemy force displays
  - Combat feed with round-by-round results
  - Test scenario with pre-configured frames

### Next Steps
- [ ] Seed initial data (chassis and weapons from design doc)
- [ ] Add tactical orders UI controls
- [ ] Implement real-time combat feed animation
- [ ] Add post-combat damage assessment
- [ ] Begin Milestone 2: Management Core

## How to Run

### Prerequisites
- .NET 9 SDK or later
- Windows 10/11 (for WPF)
- Visual Studio 2022 or Visual Studio Code

### Build and Run

**Option 1: Using the rename script (Recommended)**
1. Close Visual Studio if open
2. Run the rename script: `powershell -ExecutionPolicy Bypass -File rename-project.ps1`
3. Open `MechanizedArmourCommander.sln` in Visual Studio
4. Set `MechanizedArmourCommander.UI` as the startup project
5. Press F5 to build and run

**Option 2: Command line (after running rename script)**
```bash
dotnet build MechanizedArmourCommander.sln
dotnet run --project src/MechanizedArmourCommander.UI/MechanizedArmourCommander.UI.csproj
```

## Combat Prototype Testing

The current build includes a test scenario with:
- **Player Forces**:
  - Alpha (VG-45 Vanguard, Medium) - Pilot: Razor
  - Bravo (SC-20 Scout, Light) - Pilot: Ghost
- **Enemy Forces**:
  - Enemy-1 (BR-70 Bruiser, Heavy)
  - Enemy-2 (RD-30 Raider, Light)

Click "START COMBAT" to run the auto-resolved combat simulation and see the results in the combat feed.

## Development Roadmap

See [mechanized-armour-commander-design.md](mechanized-armour-commander-design.md) for complete design specifications.

### Milestone 1: Combat Prototype (Current)
- Implement combat resolution system ✅
- Basic weapon and frame models ✅
- Text-based combat feed ✅
- Test balance with simple scenarios (In Progress)

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

## Design Philosophy

- **Management over twitch** - Player wins through preparation, not execution
- **Meaningful choices** - Every decision should have tradeoffs
- **Resource scarcity** - Can't have/do everything, forces prioritization
- **Emergent narratives** - Pilots and frames develop history through play
- **Clear feedback** - Player always knows why they won or lost
- **Modular design** - Easy to expand content without code changes

## License

**Proprietary Software - All Rights Reserved**

Copyright © 2026. This software is proprietary and confidential. Unauthorized copying, distribution, modification, or use of this software, via any medium, is strictly prohibited without explicit written permission from the copyright holder.

This code is provided for **viewing and portfolio purposes only**. No license is granted for any use whatsoever.

See [LICENSE](LICENSE) file for complete terms.

## Contact

For licensing inquiries or permission requests, please contact the repository owner.

---

*Last Updated: 2026-01-02*
