# Renaming Project to "Mechanized Armour Commander"

## Quick Start

The project has been partially renamed. To complete the rename:

### Option 1: Automated (Recommended)

1. **Close Visual Studio** (important - files must not be locked)
2. Run the PowerShell rename script:
   ```powershell
   powershell -ExecutionPolicy Bypass -File rename-project.ps1
   ```
3. Open `MechanizedArmourCommander.sln` in Visual Studio
4. Rebuild the solution

### Option 2: Manual

If the script doesn't work, follow these steps:

#### 1. Rename Directories
```
src/MechCommander.UI → src/MechanizedArmourCommander.UI
src/MechCommander.Core → src/MechanizedArmourCommander.Core
src/MechCommander.Data → src/MechanizedArmourCommander.Data
```

#### 2. Rename Project Files
```
src/MechanizedArmourCommander.UI/MechCommander.UI.csproj → MechanizedArmourCommander.UI.csproj
src/MechanizedArmourCommander.Core/MechCommander.Core.csproj → MechanizedArmourCommander.Core.csproj
src/MechanizedArmourCommander.Data/MechCommander.Data.csproj → MechanizedArmourCommander.Data.csproj
```

#### 3. Update All References

Use Find & Replace in Visual Studio or VS Code:
- Find: `MechCommander`
- Replace with: `MechanizedArmourCommander`
- In files: `*.cs`, `*.csproj`, `*.xaml`

#### 4. Clean Build Artifacts
Delete all `bin` and `obj` folders:
```powershell
Get-ChildItem -Path src -Directory -Recurse | Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } | Remove-Item -Recurse -Force
```

#### 5. Rebuild
```bash
dotnet build MechanizedArmourCommander.sln
```

## What's Already Done

✅ Solution file renamed to `MechanizedArmourCommander.sln`
✅ Solution file content updated with new project paths
✅ Design document renamed to `mechanized-armour-commander-design.md`
✅ README.md updated with new project name
✅ PowerShell rename script created

## What Still Needs Doing

⏳ Rename project directories (requires no file locks)
⏳ Rename .csproj files
⏳ Update namespaces in all C# files
⏳ Update XAML namespace references
⏳ Clean build artifacts
⏳ Rebuild solution

## Verification

After renaming, verify:
1. Solution opens without errors
2. All projects load correctly
3. Solution builds successfully
4. Application runs and displays "MECHANIZED ARMOUR COMMANDER" in title
5. No references to "MechCommander" remain (search solution)

## Troubleshooting

**Error: "Project could not be loaded"**
- Ensure the .sln file references match the actual directory names
- Check that .csproj files have been renamed

**Error: "Type or namespace could not be found"**
- Clean solution and delete bin/obj folders
- Rebuild from scratch
- Check namespace declarations in C# files

**Files are locked**
- Close Visual Studio completely
- Close any file explorer windows in the project directory
- Check Task Manager for any lingering MSBuild or VBCSCompiler processes

---

*Created: 2026-01-02*
