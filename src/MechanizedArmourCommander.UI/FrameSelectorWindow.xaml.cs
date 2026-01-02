using System.Windows;
using System.Windows.Controls;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.UI;

public partial class FrameSelectorWindow : Window
{
    private readonly DatabaseContext _dbContext;
    private readonly ChassisRepository _chassisRepo;
    private readonly WeaponRepository _weaponRepo;

    private List<Chassis> _allChassis = new();
    private List<Weapon> _allWeapons = new();
    private Chassis? _selectedChassis;
    private Dictionary<string, Weapon?> _selectedLoadout = new();

    public Chassis? SelectedChassis => _selectedChassis;
    public Dictionary<string, Weapon?> SelectedLoadout => _selectedLoadout;

    public FrameSelectorWindow(DatabaseContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _chassisRepo = new ChassisRepository(_dbContext);
        _weaponRepo = new WeaponRepository(_dbContext);

        LoadData();
    }

    private void LoadData()
    {
        // Load all chassis and weapons
        _allChassis = _chassisRepo.GetAll();
        _allWeapons = _weaponRepo.GetAll();

        // Populate chassis list
        RefreshChassisList("All Classes");

        // Populate weapons list
        RefreshWeaponsList();
    }

    private void RefreshChassisList(string classFilter)
    {
        ChassisListBox.Items.Clear();

        var filteredChassis = classFilter == "All Classes"
            ? _allChassis
            : _allChassis.Where(c => c.Class == classFilter).ToList();

        foreach (var chassis in filteredChassis.OrderBy(c => c.Class).ThenBy(c => c.Designation))
        {
            ChassisListBox.Items.Add($"{chassis.Designation} {chassis.Name} ({chassis.Class})");
        }
    }

    private void RefreshWeaponsList()
    {
        WeaponsListBox.Items.Clear();

        foreach (var weapon in _allWeapons.OrderBy(w => w.HardpointSize).ThenBy(w => w.Name))
        {
            WeaponsListBox.Items.Add($"[{weapon.HardpointSize}] {weapon.Name} - {weapon.Damage}dmg {weapon.RangeClass}");
        }
    }

    private void ClassFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Avoid running during initialization before controls are loaded
        if (ChassisListBox == null || ClassFilterComboBox.SelectedItem is not ComboBoxItem item)
            return;

        RefreshChassisList(item.Content?.ToString() ?? "All Classes");
    }

    private void ChassisListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChassisListBox.SelectedItem is string selection)
        {
            // Extract designation from selection
            var designation = selection.Split(' ')[0];
            _selectedChassis = _allChassis.FirstOrDefault(c => c.Designation == designation);

            if (_selectedChassis != null)
            {
                DisplayChassisDetails(_selectedChassis);
                BuildLoadoutConfiguration(_selectedChassis);
                UpdateSelectedFrameText();
                ConfirmButton.IsEnabled = true;
            }
        }
    }

    private void DisplayChassisDetails(Chassis chassis)
    {
        var details = $@"═══════════════════════════════════════

DESIGNATION: {chassis.Designation}
NAME: {chassis.Name}
CLASS: {chassis.Class}

═══════════════════════════════════════
SPECIFICATIONS
═══════════════════════════════════════

HARDPOINTS:
  Small:  {chassis.HardpointSmall}
  Medium: {chassis.HardpointMedium}
  Large:  {chassis.HardpointLarge}

CAPACITIES:
  Heat Capacity:  {chassis.HeatCapacity}
  Ammo Capacity:  {chassis.AmmoCapacity}
  Armor Points:   {chassis.ArmorPoints}

PERFORMANCE:
  Base Speed:     {chassis.BaseSpeed}
  Base Evasion:   {chassis.BaseEvasion}

═══════════════════════════════════════
TACTICAL ASSESSMENT
═══════════════════════════════════════

{GetTacticalAssessment(chassis)}

═══════════════════════════════════════";

        ChassisDetailsText.Text = details;
    }

    private string GetTacticalAssessment(Chassis chassis)
    {
        var assessment = "";

        // Class-based assessment
        assessment += chassis.Class switch
        {
            "Light" => "Role: Scout, Fast Attack\nStrengths: High speed, good evasion\nWeaknesses: Low armor, limited firepower\n",
            "Medium" => "Role: Versatile Combatant\nStrengths: Balanced capabilities\nWeaknesses: Jack of all trades\n",
            "Heavy" => "Role: Main Battle Frame\nStrengths: Heavy armor, strong firepower\nWeaknesses: Slower movement\n",
            "Assault" => "Role: Heavy Assault\nStrengths: Maximum armor and firepower\nWeaknesses: Slow speed, low evasion\n",
            _ => ""
        };

        // Hardpoint analysis
        var totalHardpoints = chassis.HardpointSmall + chassis.HardpointMedium + chassis.HardpointLarge;
        assessment += $"\nTotal Hardpoints: {totalHardpoints}\n";

        if (chassis.HardpointLarge > 0)
            assessment += "Can mount heavy weapons\n";

        // Heat/Ammo ratio
        var heatAmmoRatio = (float)chassis.HeatCapacity / chassis.AmmoCapacity;
        if (heatAmmoRatio > 1.5f)
            assessment += "Favors energy weapons\n";
        else if (heatAmmoRatio < 0.7f)
            assessment += "Favors ballistic weapons\n";
        else
            assessment += "Balanced for mixed loadouts\n";

        return assessment;
    }

    private void BuildLoadoutConfiguration(Chassis chassis)
    {
        LoadoutPanel.Children.Clear();
        _selectedLoadout.Clear();

        var slots = new List<(string Size, int Count)>
        {
            ("Small", chassis.HardpointSmall),
            ("Medium", chassis.HardpointMedium),
            ("Large", chassis.HardpointLarge)
        };

        foreach (var (size, count) in slots)
        {
            for (int i = 0; i < count; i++)
            {
                var slotKey = $"{size}-{i}";
                _selectedLoadout[slotKey] = null;

                var slotPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };

                var headerText = new TextBlock
                {
                    Text = $"{size} Hardpoint {i + 1}:",
                    Foreground = System.Windows.Media.Brushes.LimeGreen,
                    FontSize = 10,
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var weaponCombo = new ComboBox
                {
                    Background = System.Windows.Media.Brushes.Black,
                    Foreground = System.Windows.Media.Brushes.LimeGreen,
                    BorderBrush = System.Windows.Media.Brushes.LimeGreen,
                    Tag = slotKey
                };

                // Add empty option
                weaponCombo.Items.Add("(Empty)");

                // Add weapons matching this hardpoint size
                var matchingWeapons = _allWeapons
                    .Where(w => w.HardpointSize == size)
                    .OrderBy(w => w.Name);

                foreach (var weapon in matchingWeapons)
                {
                    weaponCombo.Items.Add($"{weapon.Name} ({weapon.Damage}dmg, {weapon.RangeClass})");
                }

                weaponCombo.SelectedIndex = 0;
                weaponCombo.SelectionChanged += WeaponCombo_SelectionChanged;

                slotPanel.Children.Add(headerText);
                slotPanel.Children.Add(weaponCombo);

                LoadoutPanel.Children.Add(slotPanel);
            }
        }
    }

    private void WeaponCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.Tag is string slotKey)
        {
            if (combo.SelectedItem is string selection && selection != "(Empty)")
            {
                // Extract weapon name from selection
                var weaponName = selection.Split('(')[0].Trim();
                var weapon = _allWeapons.FirstOrDefault(w => w.Name == weaponName);
                _selectedLoadout[slotKey] = weapon;
            }
            else
            {
                _selectedLoadout[slotKey] = null;
            }

            UpdateSelectedFrameText();
        }
    }

    private void UpdateSelectedFrameText()
    {
        if (_selectedChassis != null)
        {
            var weaponCount = _selectedLoadout.Values.Count(w => w != null);
            SelectedFrameText.Text = $"{_selectedChassis.Designation} {_selectedChassis.Name} ({weaponCount} weapons)";
        }
        else
        {
            SelectedFrameText.Text = "None";
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedChassis == null)
        {
            MessageBox.Show("Please select a chassis first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
