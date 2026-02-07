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
│   │   ├── MainWindow.xaml                    # Combat view (battlefield, tactical decisions)
│   │   ├── TacticalDecisionWindow.xaml        # Per-frame action planning
│   │   ├── FrameSelectorWindow.xaml           # Frame/loadout browser
│   │   ├── PostCombatWindow.xaml              # Results, salvage selection
│   │   └── SettingsWindow.xaml                # Settings (placeholder)
│   │
│   ├── MechanizedArmourCommander.Core/        # Game Logic & Systems
│   │   ├── Models/                            # Game models (CombatFrame, Mission, etc.)
│   │   ├── Combat/                            # Combat subsystems (damage, reactor, actions, AI)
│   │   └── Services/                          # CombatService, ManagementService, MissionService
│   │
│   └── MechanizedArmourCommander.Data/        # Data Access Layer
│       ├── Models/                            # Database entities (Chassis, Weapon, Pilot, etc.)
│       ├── Repositories/                      # Data access (11 repositories)
│       └── DatabaseContext.cs                 # SQLite management & schema versioning
│
├── CORE_RULES.md                              # Authoritative game rules reference
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
- **Market**: Faction-filtered marketplace with standing-based discounts and exclusive items
- **Inventory**: Company weapon storage — buy, sell, salvage flows through here
- **Refit**: Equip/unequip weapons between frames and inventory
- **Missions**: Browse 3 faction-driven contracts with employer/opponent factions
- **Deploy**: Select 1-4 frames with pilots for mission deployment

### Tactical Combat
- **Action Point Economy**: 2 AP per frame per round (Move, Fire, Brace, Called Shot, Overwatch, Vent, Sprint)
- **Range Bands**: Point Blank, Short, Medium, Long — weapon effectiveness varies by range
- **Layered Damage**: 7 hit locations with Armor → Structure → Component damage cascade
- **Reactor Energy**: Manage energy budget each round — overloading risks shutdown
- **Weapon Groups**: Assign weapons to groups, fire as single actions
- **AI Opponents**: Aggressive/Balanced/Defensive stances with target priority logic
- **Battlefield Map**: Canvas-based X,Y unit plotting with range band zones

### Post-Combat
- Victory/Defeat/Withdrawal outcomes with financial report
- Interactive salvage selection from destroyed enemy wreckage (with item values displayed)
- Faction standing changes based on mission outcome
- Pilot XP gain, injury tracking, KIA
- Per-frame damage reports with repair costs

### Economy
- Starting credits: 500,000
- Chassis: 100K (Light) to 650K (Assault)
- Repair costs scale with damage
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

## Game Rules

See [CORE_RULES.md](CORE_RULES.md) for the complete authoritative rules reference covering:
- Combat round structure and end conditions
- Accuracy formula with all modifiers
- Damage resolution and component damage
- Reactor energy and shutdown mechanics
- Loadout, hardpoints, and space budget
- Chassis class stats and pilot skills
- AI behavior and economy rules

## Development Status

### Completed
- Combat system with action points, range bands, layered damage, reactor energy
- Full management hub (roster, pilots, market, inventory, refit, missions, deploy)
- Mission generation and post-combat results with salvage
- Pilot system with XP, injuries, KIA
- Main menu with 5 named save slots
- Campaign loop: Menu → HQ → Deploy → Combat → Results → HQ
- 3-faction system with standings, faction markets, discounts, and exclusive weapons
- Frame renaming from roster UI
- Salvage value display on post-combat loot screen

### Planned
- Settings screen (difficulty, audio, etc.)
- Pilot skill leveling on XP thresholds
- Terrain and environmental modifiers
- Special weapon effects
- Tutorial/onboarding

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

*Last Updated: 2026-02-07*
