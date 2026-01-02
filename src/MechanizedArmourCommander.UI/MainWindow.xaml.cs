using System.Windows;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;

namespace MechanizedArmourCommander.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly CombatService _combatService;
    private readonly DatabaseContext _dbContext;
    private List<CombatFrame> _playerFrames = new();
    private List<CombatFrame> _enemyFrames = new();
    private TacticalOrders _playerOrders = new();

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database and seed data
        _dbContext = new DatabaseContext();
        _dbContext.Initialize();

        _combatService = new CombatService();
        InitializeTestScenario();
        DisplayDatabaseStats();
    }

    private void DisplayDatabaseStats()
    {
        var chassisRepo = new MechanizedArmourCommander.Data.Repositories.ChassisRepository(_dbContext);
        var weaponRepo = new MechanizedArmourCommander.Data.Repositories.WeaponRepository(_dbContext);

        var allChassis = chassisRepo.GetAll();
        var allWeapons = weaponRepo.GetAll();

        var stats = $"DATABASE INITIALIZED\n" +
                   $"==================\n" +
                   $"Chassis loaded: {allChassis.Count}\n" +
                   $"Weapons loaded: {allWeapons.Count}\n\n" +
                   $"Chassis by class:\n" +
                   $"  Light: {allChassis.Count(c => c.Class == "Light")}\n" +
                   $"  Medium: {allChassis.Count(c => c.Class == "Medium")}\n" +
                   $"  Heavy: {allChassis.Count(c => c.Class == "Heavy")}\n" +
                   $"  Assault: {allChassis.Count(c => c.Class == "Assault")}\n\n" +
                   $"Weapons by size:\n" +
                   $"  Small: {allWeapons.Count(w => w.HardpointSize == "Small")}\n" +
                   $"  Medium: {allWeapons.Count(w => w.HardpointSize == "Medium")}\n" +
                   $"  Large: {allWeapons.Count(w => w.HardpointSize == "Large")}\n\n" +
                   $"Ready for combat testing.";

        CombatFeedText.Text = stats;
    }

    private void InitializeTestScenario()
    {
        // Create test player frames
        _playerFrames = new List<CombatFrame>
        {
            new CombatFrame
            {
                InstanceId = 1,
                CustomName = "Alpha",
                ChassisDesignation = "VG-45",
                ChassisName = "Vanguard",
                Class = "Medium",
                CurrentArmor = 100,
                MaxArmor = 100,
                CurrentHeat = 0,
                MaxHeat = 30,
                CurrentAmmo = 150,
                MaxAmmo = 150,
                Speed = 6,
                Evasion = 15,
                PilotCallsign = "Razor",
                PilotGunnery = 5,
                PilotPiloting = 4,
                Weapons = new List<EquippedWeapon>
                {
                    new EquippedWeapon { Name = "Medium Laser", Damage = 10, HeatGeneration = 4, AmmoConsumption = 0, BaseAccuracy = 80, RangeClass = "Medium" },
                    new EquippedWeapon { Name = "Autocannon-5", Damage = 8, HeatGeneration = 1, AmmoConsumption = 5, BaseAccuracy = 80, RangeClass = "Long" }
                }
            },
            new CombatFrame
            {
                InstanceId = 2,
                CustomName = "Bravo",
                ChassisDesignation = "SC-20",
                ChassisName = "Scout",
                Class = "Light",
                CurrentArmor = 60,
                MaxArmor = 60,
                CurrentHeat = 0,
                MaxHeat = 20,
                CurrentAmmo = 100,
                MaxAmmo = 100,
                Speed = 9,
                Evasion = 25,
                PilotCallsign = "Ghost",
                PilotGunnery = 6,
                PilotPiloting = 7,
                Weapons = new List<EquippedWeapon>
                {
                    new EquippedWeapon { Name = "Light Laser", Damage = 5, HeatGeneration = 2, AmmoConsumption = 0, BaseAccuracy = 85, RangeClass = "Medium" },
                    new EquippedWeapon { Name = "Machine Gun", Damage = 3, HeatGeneration = 0, AmmoConsumption = 10, BaseAccuracy = 90, RangeClass = "Short" }
                }
            }
        };

        // Create test enemy frames
        _enemyFrames = new List<CombatFrame>
        {
            new CombatFrame
            {
                InstanceId = 101,
                CustomName = "Enemy-1",
                ChassisDesignation = "BR-70",
                ChassisName = "Bruiser",
                Class = "Heavy",
                CurrentArmor = 150,
                MaxArmor = 150,
                CurrentHeat = 0,
                MaxHeat = 38,
                CurrentAmmo = 180,
                MaxAmmo = 180,
                Speed = 4,
                Evasion = 10,
                PilotGunnery = 4,
                PilotPiloting = 3,
                Weapons = new List<EquippedWeapon>
                {
                    new EquippedWeapon { Name = "Heavy Autocannon", Damage = 20, HeatGeneration = 2, AmmoConsumption = 8, BaseAccuracy = 70, RangeClass = "Long" },
                    new EquippedWeapon { Name = "Medium Laser", Damage = 10, HeatGeneration = 4, AmmoConsumption = 0, BaseAccuracy = 80, RangeClass = "Medium" }
                }
            },
            new CombatFrame
            {
                InstanceId = 102,
                CustomName = "Enemy-2",
                ChassisDesignation = "RD-30",
                ChassisName = "Raider",
                Class = "Light",
                CurrentArmor = 55,
                MaxArmor = 55,
                CurrentHeat = 0,
                MaxHeat = 25,
                CurrentAmmo = 120,
                MaxAmmo = 120,
                Speed = 8,
                Evasion = 22,
                PilotGunnery = 3,
                PilotPiloting = 5,
                Weapons = new List<EquippedWeapon>
                {
                    new EquippedWeapon { Name = "Small Missile Rack", Damage = 6, HeatGeneration = 2, AmmoConsumption = 8, BaseAccuracy = 80, RangeClass = "Short" },
                    new EquippedWeapon { Name = "Light Laser", Damage = 5, HeatGeneration = 2, AmmoConsumption = 0, BaseAccuracy = 85, RangeClass = "Medium" }
                }
            }
        };

        UpdateFrameLists();
    }

    private void UpdateFrameLists()
    {
        PlayerFramesList.Items.Clear();
        foreach (var frame in _playerFrames)
        {
            PlayerFramesList.Items.Add($"{frame.CustomName} - {frame.ChassisDesignation} {frame.ChassisName}");
            PlayerFramesList.Items.Add($"  Armor: {frame.CurrentArmor}/{frame.MaxArmor}");
            PlayerFramesList.Items.Add($"  Pilot: {frame.PilotCallsign}");
            PlayerFramesList.Items.Add("");
        }

        EnemyFramesList.Items.Clear();
        foreach (var frame in _enemyFrames)
        {
            EnemyFramesList.Items.Add($"{frame.CustomName} - {frame.ChassisDesignation} {frame.ChassisName}");
            EnemyFramesList.Items.Add($"  Armor: {frame.CurrentArmor}/{frame.MaxArmor}");
            EnemyFramesList.Items.Add("");
        }
    }

    private void StartCombatButton_Click(object sender, RoutedEventArgs e)
    {
        CombatFeedText.Text = "INITIATING COMBAT...\n\n";

        if (TacticalModeCheckBox.IsChecked == true)
        {
            // Tactical mode - round by round with player intervention
            ExecuteTacticalCombat();
        }
        else
        {
            // Auto-resolve mode
            var log = _combatService.ExecuteCombat(_playerFrames, _enemyFrames, _playerOrders);
            var formattedLog = _combatService.FormatCombatLog(log);

            CombatFeedText.Text = formattedLog;
            UpdateFrameLists();
        }
    }

    private void ExecuteTacticalCombat()
    {
        // For now, just show the tactical decision window as a proof of concept
        var situation = CreateRoundSituation(1);
        var decisionWindow = new TacticalDecisionWindow(1, situation);

        if (decisionWindow.ShowDialog() == true)
        {
            if (decisionWindow.UseAI)
            {
                CombatFeedText.Text += "Player chose to let AI handle this round.\n";
            }
            else
            {
                CombatFeedText.Text += "Player made tactical decisions.\n";
                if (decisionWindow.Decision.AttemptWithdrawal)
                {
                    CombatFeedText.Text += "⚠ Attempting withdrawal...\n";
                }
            }

            // Run one round of combat
            var log = _combatService.ExecuteCombat(_playerFrames, _enemyFrames, _playerOrders);
            var formattedLog = _combatService.FormatCombatLog(log);
            CombatFeedText.Text += "\n" + formattedLog;
            UpdateFrameLists();
        }
    }

    private RoundSituation CreateRoundSituation(int roundNumber)
    {
        var situation = new RoundSituation
        {
            RoundNumber = roundNumber,
            PlayerFrames = _playerFrames.Select(f => new FrameSituation
            {
                InstanceId = f.InstanceId,
                Name = f.CustomName,
                Class = f.Class,
                CurrentArmor = f.CurrentArmor,
                MaxArmor = f.MaxArmor,
                CurrentHeat = f.CurrentHeat,
                MaxHeat = f.MaxHeat,
                CurrentAmmo = f.CurrentAmmo,
                MaxAmmo = f.MaxAmmo,
                Position = f.Position,
                IsDestroyed = f.IsDestroyed,
                IsOverheating = f.IsOverheating
            }).ToList(),
            EnemyFrames = _enemyFrames.Select(f => new FrameSituation
            {
                InstanceId = f.InstanceId,
                Name = f.CustomName,
                Class = f.Class,
                CurrentArmor = f.CurrentArmor,
                MaxArmor = f.MaxArmor,
                CurrentHeat = f.CurrentHeat,
                MaxHeat = f.MaxHeat,
                CurrentAmmo = f.CurrentAmmo,
                MaxAmmo = f.MaxAmmo,
                Position = f.Position,
                IsDestroyed = f.IsDestroyed,
                IsOverheating = f.IsOverheating
            }).ToList(),
            LastRoundSummary = roundNumber == 1 ? "Combat initiating. Forces deploying to battle positions." : "Previous round events...",
            PlayerLosses = _playerFrames.Count(f => f.IsDestroyed),
            EnemyLosses = _enemyFrames.Count(f => f.IsDestroyed)
        };

        // Calculate average distance
        var distances = new List<int>();
        foreach (var pf in _playerFrames.Where(f => !f.IsDestroyed))
        {
            foreach (var ef in _enemyFrames.Where(f => !f.IsDestroyed))
            {
                distances.Add(Math.Abs(pf.Position - ef.Position));
            }
        }

        if (distances.Any())
        {
            situation.AverageDistance = (int)distances.Average();
            situation.RangeBand = situation.AverageDistance switch
            {
                <= 5 => "Short",
                <= 15 => "Medium",
                _ => "Long"
            };
        }
        else
        {
            situation.AverageDistance = 20;
            situation.RangeBand = "Long";
        }

        return situation;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        InitializeTestScenario();
        CombatFeedText.Text = "Combat reset. Ready for engagement.";
    }

    private void FrameSelectorButton_Click(object sender, RoutedEventArgs e)
    {
        var frameSelectorWindow = new FrameSelectorWindow(_dbContext);

        if (frameSelectorWindow.ShowDialog() == true)
        {
            var selectedChassis = frameSelectorWindow.SelectedChassis;
            var selectedLoadout = frameSelectorWindow.SelectedLoadout;

            if (selectedChassis != null)
            {
                // Create a new combat frame from the selection
                var newFrame = CreateCombatFrameFromSelection(selectedChassis, selectedLoadout);

                // Add to player frames (or replace if desired)
                if (_playerFrames.Count < 4) // Max 4 frames per lance
                {
                    _playerFrames.Add(newFrame);
                }
                else
                {
                    // Replace last frame if lance is full
                    _playerFrames[_playerFrames.Count - 1] = newFrame;
                }

                UpdateFrameLists();
                CombatFeedText.Text = $"Frame added to lance: {newFrame.CustomName} ({selectedChassis.Designation} {selectedChassis.Name})\n\n" +
                                     $"Equipped Weapons:\n" +
                                     string.Join("\n", newFrame.Weapons.Select(w => $"  - {w.Name} ({w.Damage} dmg, {w.RangeClass} range)"));
            }
        }
    }

    private CombatFrame CreateCombatFrameFromSelection(
        MechanizedArmourCommander.Data.Models.Chassis chassis,
        Dictionary<string, MechanizedArmourCommander.Data.Models.Weapon?> loadout)
    {
        // Generate instance ID
        var instanceId = _playerFrames.Any() ? _playerFrames.Max(f => f.InstanceId) + 1 : 1;

        // Create equipped weapons from loadout
        var weapons = loadout.Values
            .Where(w => w != null)
            .Select(w => new EquippedWeapon
            {
                WeaponId = w!.WeaponId,
                Name = w.Name,
                HardpointSize = w.HardpointSize,
                HeatGeneration = w.HeatGeneration,
                AmmoConsumption = w.AmmoConsumption,
                Damage = w.Damage,
                RangeClass = w.RangeClass,
                BaseAccuracy = w.BaseAccuracy,
                SpecialEffect = w.SpecialEffect
            })
            .ToList();

        return new CombatFrame
        {
            InstanceId = instanceId,
            CustomName = $"Frame-{instanceId}",
            ChassisDesignation = chassis.Designation,
            ChassisName = chassis.Name,
            Class = chassis.Class,
            CurrentArmor = chassis.ArmorPoints,
            MaxArmor = chassis.ArmorPoints,
            CurrentHeat = 0,
            MaxHeat = chassis.HeatCapacity,
            CurrentAmmo = chassis.AmmoCapacity,
            MaxAmmo = chassis.AmmoCapacity,
            Speed = chassis.BaseSpeed,
            Evasion = chassis.BaseEvasion,
            PilotCallsign = "Pilot-" + instanceId,
            PilotGunnery = 5, // Default pilot stats
            PilotPiloting = 5,
            PilotTactics = 5,
            Weapons = weapons
        };
    }
}
