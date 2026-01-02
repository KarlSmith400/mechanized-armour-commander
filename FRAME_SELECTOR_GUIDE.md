# Frame Selector - User Guide

## Overview

The Frame Selector is a comprehensive UI for browsing and configuring combat frames from the database. It allows you to select chassis, equip weapons, and add frames to your player lance.

---

## Accessing the Frame Selector

From the main combat prototype window, click the **"FRAME SELECTOR"** button (blue button in the bottom control panel).

---

## UI Layout

The Frame Selector has three main panels:

### **Left Panel: Chassis List**
- Browse all available chassis from the database
- Filter by class: All Classes, Light, Medium, Heavy, Assault
- Chassis displayed as: `[Designation] [Name] ([Class])`
- Example: `SC-20 Scout (Light)`

### **Center Panel: Chassis Details**
- Complete specifications for selected chassis
- Hardpoint configuration (Small, Medium, Large)
- Capacities (Heat, Ammo, Armor)
- Performance stats (Speed, Evasion)
- Tactical assessment based on chassis class and stats

### **Right Panel: Weapon Configuration**
- **Top Section**: Loadout Configuration
  - Dynamic hardpoint slots based on selected chassis
  - Dropdown for each hardpoint slot
  - Only shows weapons matching the hardpoint size

- **Bottom Section**: Available Weapons Browser
  - All 13 weapons from database
  - Organized by hardpoint size and name
  - Format: `[Size] Name - Damage Range`

---

## Step-by-Step Usage

### 1. **Select a Chassis**
1. Optionally filter by class using the dropdown
2. Click on a chassis in the list
3. View detailed specifications in the center panel

### 2. **Configure Loadout**
1. For each hardpoint slot, select a weapon from the dropdown
2. Or leave it as "(Empty)" if you want an empty slot
3. Weapons show: Name (Damage, Range Class)
4. Example: `Light Laser (5dmg, Medium)`

### 3. **Confirm Selection**
1. Check the "Selected Frame" text at bottom shows your configuration
2. Click **"CONFIRM SELECTION"** to add to your lance
3. Or click **"CANCEL"** to close without adding

---

## Tactical Assessment

The Frame Selector provides automatic tactical assessments:

### **Class-Based Roles**

| Class | Role | Strengths | Weaknesses |
|-------|------|-----------|------------|
| **Light** | Scout, Fast Attack | High speed, good evasion | Low armor, limited firepower |
| **Medium** | Versatile Combatant | Balanced capabilities | Jack of all trades |
| **Heavy** | Main Battle Frame | Heavy armor, strong firepower | Slower movement |
| **Assault** | Heavy Assault | Maximum armor and firepower | Slow speed, low evasion |

### **Loadout Recommendations**

The tactical assessment analyzes heat/ammo ratios:

- **Heat Capacity > Ammo Capacity**: "Favors energy weapons"
  - Best with: Lasers, Plasma Lance

- **Ammo Capacity > Heat Capacity**: "Favors ballistic weapons"
  - Best with: Autocannons, Missiles, Gauss

- **Balanced**: "Balanced for mixed loadouts"
  - Good for: Mix of energy and ballistic weapons

---

## Lance Management

- **Max Lance Size**: 4 frames
- **Adding Frames**: Frames are added sequentially (Frame-1, Frame-2, etc.)
- **Lance Full**: If you have 4 frames, selecting a new one replaces the last frame
- **Instance IDs**: Automatically generated and incremented

### After Adding a Frame:
- Frame appears in the Player Forces list on main window
- Shows: Frame name, chassis designation, armor status, pilot callsign
- Combat feed displays equipped weapons
- Default pilot stats: Gunnery 5, Piloting 5, Tactics 5

---

## Database Integration

The Frame Selector loads data directly from the SQLite database:

### **12 Chassis Available**
- 3 Light: SC-20 Scout, RD-30 Raider, HR-35 Harrier
- 3 Medium: VG-45 Vanguard, EN-50 Enforcer, RG-55 Ranger
- 3 Heavy: WD-60 Warden, BR-70 Bruiser, SN-75 Sentinel
- 3 Assault: TN-85 Titan, JG-95 Juggernaut, CL-100 Colossus

### **13 Weapons Available**
- 4 Small: Light Laser, Machine Gun, Flamer, Small Missile Rack
- 4 Medium: Medium Laser, Autocannon-5, SRM-6, Light Gauss Rifle
- 5 Large: Heavy Laser, Heavy Autocannon-10, Plasma Lance, LRM-15, Heavy Gauss Cannon

---

## Example Loadout Configurations

### **Light Scout Build (SC-20)**
- **Hardpoints**: 3 Small
- **Loadout**:
  - 2× Light Laser (energy-based, no ammo)
  - 1× Machine Gun (suppressive fire)
- **Role**: Fast reconnaissance, harassment

### **Medium Brawler (VG-45)**
- **Hardpoints**: 2 Small, 2 Medium
- **Loadout**:
  - 2× Light Laser (Small hardpoints)
  - 2× Medium Laser (Medium hardpoints)
- **Role**: Close-range energy combat

### **Heavy Fire Support (BR-70)**
- **Hardpoints**: 1 Medium, 2 Large
- **Loadout**:
  - 1× Autocannon-5 (Medium)
  - 1× Heavy Autocannon-10 (Large)
  - 1× LRM-15 (Large)
- **Role**: Long-range fire support

### **Assault Tank (CL-100)**
- **Hardpoints**: 1 Small, 1 Medium, 3 Large
- **Loadout**:
  - 1× Flamer (Small) - heat management
  - 1× Light Gauss Rifle (Medium)
  - 2× Heavy Gauss Cannon (Large)
  - 1× Plasma Lance (Large)
- **Role**: Maximum firepower platform

---

## Tips and Strategies

### **Heat Management**
- Energy weapons generate heat but don't use ammo
- Balance energy weapons with heat capacity
- High heat capacity chassis can run more lasers

### **Ammo Consideration**
- Ballistic weapons need ammo
- High ammo capacity = more sustained fire
- Mix energy weapons to avoid running dry

### **Range Synergy**
- Group weapons by range class for optimal positioning
- Short-range builds benefit from Aggressive stance
- Long-range builds benefit from Defensive stance

### **Hardpoint Utilization**
- Don't leave hardpoints empty unless intentional
- Large hardpoints are valuable - use heavy hitters
- Small hardpoints perfect for supplementary weapons

---

## Technical Notes

### **Frame Creation Process**
1. User selects chassis and configures weapons
2. Clicks "Confirm Selection"
3. System creates `CombatFrame` instance with:
   - Unique InstanceId
   - Full chassis stats (armor, heat, ammo, speed, evasion)
   - Equipped weapons list
   - Default pilot with stats of 5
   - Auto-generated name: "Frame-[ID]"
4. Frame added to player lance
5. Immediately available for combat

### **Data Persistence**
- Currently: Frames exist only in session memory
- Future: Save/load lance configurations
- Database contains template data (Chassis, Weapons)
- Runtime frames stored in `_playerFrames` list

---

## Future Enhancements

Planned features for the Frame Selector:

1. **Frame Customization**
   - Rename frames
   - Assign specific pilots from roster
   - Custom paint schemes / colors

2. **Lance Management**
   - Save lance configurations
   - Load preset lances
   - Multiple lance slots

3. **Advanced Loadout Features**
   - Loadout templates
   - Quick-swap weapon configurations
   - Visual hardpoint map

4. **Validation & Warnings**
   - Heat/ammo efficiency warnings
   - Weapon synergy recommendations
   - Optimal range analysis

---

*Last Updated: 2026-01-02*
*See also: [CHANGELOG.md](CHANGELOG.md), [mechanized-armour-commander-design.md](mechanized-armour-commander-design.md)*
