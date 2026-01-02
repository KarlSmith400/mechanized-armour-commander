# GitHub Repository Setup Guide

## Step 1: Create Repository on GitHub

1. Go to https://github.com/new
2. Fill in repository details:
   - **Repository name**: `mechanized-armour-commander`
   - **Description**: `Turn-based mech combat management game with tactical AI and positional combat system`
   - **Visibility**: Choose Public or Private
   - **DO NOT** initialize with README, .gitignore, or license (we already have these)
3. Click "Create repository"

## Step 2: Push to GitHub

After creating the repository, run these commands:

```bash
cd "e:\Onedrive\Project M"

# Add the remote repository (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/mechanized-armour-commander.git

# Push to GitHub
git push -u origin main
```

## Step 3: Verify

Visit your repository at:
`https://github.com/YOUR_USERNAME/mechanized-armour-commander`

---

## Alternative: Using GitHub CLI (if installed)

If you have GitHub CLI installed, you can create and push in one command:

```bash
cd "e:\Onedrive\Project M"

# Create repository and push
gh repo create mechanized-armour-commander --public --source=. --push
```

---

## Repository Features to Enable

After pushing, consider enabling these GitHub features:

### Issues
- Enable Issues for bug tracking and feature requests
- Use labels: `bug`, `enhancement`, `documentation`, `AI`, `combat-system`, `UI`

### Projects
- Create project board for tracking development milestones
- Columns: To Do, In Progress, Done

### Releases
- Create v0.1.0 release with current state
- Use tag: `v0.1.0`
- Include release notes from CHANGELOG.md

### Topics
Add repository topics for discoverability:
- `game-development`
- `csharp`
- `wpf`
- `turn-based-strategy`
- `mech-combat`
- `tactical-game`
- `ai-combat`
- `sqlite`
- `dotnet`

---

## Suggested GitHub Description

**Short description:**
```
Turn-based mech combat management game with tactical AI and positional combat system
```

**Long description:**
```
Mechanized Armour Commander is a turn-based tactical mech combat game featuring:
• AI-driven combat with sophisticated target selection
• Positional combat system with range-based accuracy
• SQLite database with 12 chassis types and 13 weapons
• Round-by-round tactical intervention mode
• Frame customization and loadout configuration
• Terminal-style WPF interface

Built with .NET 9, C# 13, and WPF
```

---

## Repository Settings

### Branch Protection (Optional)
For `main` branch:
- ✅ Require pull request reviews before merging
- ✅ Require status checks to pass before merging
- ✅ Require conversation resolution before merging

### GitHub Actions (Future)
Consider adding workflows for:
- Automated builds on push
- Unit test execution
- Release automation

---

## README Badge Suggestions

Add these to your README.md:

```markdown
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
```

---

## Next Steps After Push

1. Create `LICENSE` file (if desired)
   - MIT License recommended for open source
   - Or keep proprietary/closed source

2. Add screenshots to README
   - Frame Selector UI
   - Tactical Decision window
   - Combat feed

3. Create Wiki pages for:
   - Getting Started
   - Combat Mechanics
   - AI System Documentation
   - Contributing Guide

4. Set up GitHub Discussions for community

---

*Created: 2026-01-02*
