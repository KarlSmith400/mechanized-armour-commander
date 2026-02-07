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
        _allChassis = _chassisRepo.GetAll();
        _allWeapons = _weaponRepo.GetAll();

        RefreshChassisList("All Classes");
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
            string typeTag = weapon.WeaponType switch
            {
                "Energy" => "E",
                "Ballistic" => "B",
                "Missile" => "M",
                _ => "?"
            };
            string energyInfo = weapon.EnergyCost > 0 ? $"{weapon.EnergyCost}E" : "";
            string ammoInfo = weapon.AmmoPerShot > 0 ? $"{weapon.AmmoPerShot}ammo" : "";
            string costInfo = string.Join("/", new[] { energyInfo, ammoInfo }.Where(s => s.Length > 0));

            WeaponsListBox.Items.Add($"[{weapon.HardpointSize}][{typeTag}] {weapon.Name} - {weapon.Damage}dmg {weapon.RangeClass} ({costInfo}) {weapon.SpaceCost}sp");
        }
    }

    private void ClassFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChassisListBox == null || ClassFilterComboBox.SelectedItem is not ComboBoxItem item)
            return;

        RefreshChassisList(item.Content?.ToString() ?? "All Classes");
    }

    private void ChassisListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChassisListBox.SelectedItem is string selection)
        {
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
        var details = $@"=======================================

DESIGNATION: {chassis.Designation}
NAME: {chassis.Name}
CLASS: {chassis.Class}

=======================================
SPECIFICATIONS
=======================================

HARDPOINTS:
  Small:  {chassis.HardpointSmall}
  Medium: {chassis.HardpointMedium}
  Large:  {chassis.HardpointLarge}

REACTOR & MOVEMENT:
  Reactor Output:     {chassis.ReactorOutput}
  Movement Cost:      {chassis.MovementEnergyCost}E/band

SPACE & ARMOR:
  Total Space:        {chassis.TotalSpace}
  Max Armor Total:    {chassis.MaxArmorTotal}

STRUCTURE:
  Head:               {chassis.StructureHead}
  Center Torso:       {chassis.StructureCenterTorso}
  Side Torso:         {chassis.StructureSideTorso}
  Arms:               {chassis.StructureArm}
  Legs:               {chassis.StructureLegs}

PERFORMANCE:
  Base Speed:         {chassis.BaseSpeed}
  Base Evasion:       {chassis.BaseEvasion}

=======================================
TACTICAL ASSESSMENT
=======================================

{GetTacticalAssessment(chassis)}

=======================================";

        ChassisDetailsText.Text = details;
    }

    private string GetTacticalAssessment(Chassis chassis)
    {
        var assessment = "";

        assessment += chassis.Class switch
        {
            "Light" => "Role: Scout, Fast Attack\nStrengths: Low move cost, high evasion\nWeaknesses: Low structure, small reactor\n",
            "Medium" => "Role: Versatile Combatant\nStrengths: Balanced reactor/mobility\nWeaknesses: Jack of all trades\n",
            "Heavy" => "Role: Main Battle Frame\nStrengths: Large reactor, strong structure\nWeaknesses: High movement cost\n",
            "Assault" => "Role: Heavy Assault\nStrengths: Maximum reactor and firepower\nWeaknesses: Very high move cost, low evasion\n",
            _ => ""
        };

        var totalHardpoints = chassis.HardpointSmall + chassis.HardpointMedium + chassis.HardpointLarge;
        assessment += $"\nTotal Hardpoints: {totalHardpoints}\n";

        if (chassis.HardpointLarge > 0)
            assessment += "Can mount heavy weapons\n";

        // Energy budget analysis
        int energyAfterMove = chassis.ReactorOutput - chassis.MovementEnergyCost;
        assessment += $"\nEnergy after one move: {energyAfterMove}E\n";

        if (energyAfterMove > 15)
            assessment += "Excellent energy surplus for weapons\n";
        else if (energyAfterMove > 8)
            assessment += "Good energy for mixed loadouts\n";
        else if (energyAfterMove > 3)
            assessment += "Tight energy budget - consider ballistics\n";
        else
            assessment += "Very tight energy - rely on ammo weapons\n";

        return assessment;
    }

    private void BuildLoadoutConfiguration(Chassis chassis)
    {
        LoadoutPanel.Children.Clear();
        _selectedLoadout.Clear();

        // Show space budget
        var spaceBudgetText = new TextBlock
        {
            Text = $"Space Budget: 0/{chassis.TotalSpace} used",
            Foreground = System.Windows.Media.Brushes.LimeGreen,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10),
            Tag = "SpaceBudget"
        };
        LoadoutPanel.Children.Add(spaceBudgetText);

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

                weaponCombo.Items.Add("(Empty)");

                var matchingWeapons = _allWeapons
                    .Where(w => w.HardpointSize == size)
                    .OrderBy(w => w.Name);

                foreach (var weapon in matchingWeapons)
                {
                    string typeTag = weapon.WeaponType == "Energy" ? "E" : weapon.WeaponType == "Ballistic" ? "B" : "M";
                    weaponCombo.Items.Add($"{weapon.Name} ({weapon.Damage}dmg, {weapon.RangeClass}, {typeTag}, {weapon.SpaceCost}sp)");
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
                var weaponName = selection.Split('(')[0].Trim();
                var weapon = _allWeapons.FirstOrDefault(w => w.Name == weaponName);
                _selectedLoadout[slotKey] = weapon;
            }
            else
            {
                _selectedLoadout[slotKey] = null;
            }

            UpdateSelectedFrameText();
            UpdateSpaceBudget();
        }
    }

    private void UpdateSpaceBudget()
    {
        if (_selectedChassis == null) return;

        int usedSpace = _selectedLoadout.Values
            .Where(w => w != null)
            .Sum(w => w!.SpaceCost);

        // Find the space budget text block
        foreach (var child in LoadoutPanel.Children)
        {
            if (child is TextBlock tb && tb.Tag is string tag && tag == "SpaceBudget")
            {
                tb.Text = $"Space Budget: {usedSpace}/{_selectedChassis.TotalSpace} used";
                tb.Foreground = usedSpace > _selectedChassis.TotalSpace
                    ? System.Windows.Media.Brushes.Red
                    : System.Windows.Media.Brushes.LimeGreen;
                break;
            }
        }
    }

    private void UpdateSelectedFrameText()
    {
        if (_selectedChassis != null)
        {
            var weaponCount = _selectedLoadout.Values.Count(w => w != null);
            int usedSpace = _selectedLoadout.Values.Where(w => w != null).Sum(w => w!.SpaceCost);
            SelectedFrameText.Text = $"{_selectedChassis.Designation} {_selectedChassis.Name} ({weaponCount} weapons, {usedSpace}/{_selectedChassis.TotalSpace} space)";
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

        // Warn if over space budget
        int usedSpace = _selectedLoadout.Values.Where(w => w != null).Sum(w => w!.SpaceCost);
        if (usedSpace > _selectedChassis.TotalSpace)
        {
            var result = MessageBox.Show(
                $"Loadout exceeds space budget ({usedSpace}/{_selectedChassis.TotalSpace}). Confirm anyway?",
                "Over Budget", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
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
