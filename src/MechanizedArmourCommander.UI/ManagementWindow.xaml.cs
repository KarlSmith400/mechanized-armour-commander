using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.UI;

public partial class ManagementWindow : Window
{
    private readonly DatabaseContext _dbContext;
    private readonly ManagementService _management;
    private readonly MissionService _missionService;

    private string _currentSection = "Roster";
    private List<Mission> _availableMissions = new();
    private Mission? _selectedMission;
    private HashSet<int> _selectedDeployFrames = new();
    private int? _marketFactionId = null;

    public bool LaunchCombat { get; private set; }
    public bool ReturnToMainMenu { get; private set; }
    public List<int> DeployedFrameIds { get; private set; } = new();
    public Mission? SelectedMission => _selectedMission;

    public ManagementWindow(DatabaseContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _management = new ManagementService(dbContext);
        _missionService = new MissionService(dbContext);

        RefreshStatusBar();
        GenerateMissions();
        ShowSection("Roster");
    }

    private void RefreshStatusBar()
    {
        var state = _management.GetPlayerState();
        if (state == null) return;

        CompanyNameText.Text = state.CompanyName;
        DayText.Text = $"Day {state.CurrentDay}";
        CreditsText.Text = $"${state.Credits:N0}";
        ReputationText.Text = $"Rep: {state.Reputation}";
        MissionsText.Text = $"Missions: {state.MissionsWon}/{state.MissionsCompleted}";

        // Faction standings in status bar
        var standings = _management.GetAllStandings();
        var standingParts = standings.Select(s => $"{s.FactionName.Split(' ')[0]}: {s.Standing}");
        FactionStandingText.Text = string.Join("  |  ", standingParts);
    }

    private void GenerateMissions()
    {
        var state = _management.GetPlayerState();
        if (state == null) return;
        var standings = _management.GetAllStandings();
        _availableMissions = _missionService.GenerateContracts(3, state.Reputation, standings);
    }

    #region Navigation

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string section)
        {
            ShowSection(section);
        }
    }

    private void ShowSection(string section)
    {
        _currentSection = section;

        // Update nav button highlighting
        UpdateNavHighlight(section);

        ContentPanel.Children.Clear();

        switch (section)
        {
            case "Roster": ShowRoster(); break;
            case "Pilots": ShowPilots(); break;
            case "Market": ShowMarket(); break;
            case "Inventory": ShowInventory(); break;
            case "Refit": ShowRefit(); break;
            case "Missions": ShowMissions(); break;
            case "Deploy": ShowDeploy(); break;
        }
    }

    private void UpdateNavHighlight(string activeSection)
    {
        var buttons = new[] { NavRoster, NavPilots, NavMarket, NavInventory, NavRefit, NavMissions, NavDeploy };
        var tags = new[] { "Roster", "Pilots", "Market", "Inventory", "Refit", "Missions", "Deploy" };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == NavDeploy) continue; // Deploy has its own style

            bool isActive = tags[i] == activeSection;
            buttons[i].Background = new SolidColorBrush(
                isActive ? (Color)ColorConverter.ConvertFromString("#003300")
                         : (Color)ColorConverter.ConvertFromString("#001A00"));
            buttons[i].Foreground = new SolidColorBrush(
                isActive ? (Color)ColorConverter.ConvertFromString("#00FF00")
                         : (Color)ColorConverter.ConvertFromString("#00CC00"));
            buttons[i].BorderBrush = new SolidColorBrush(
                isActive ? (Color)ColorConverter.ConvertFromString("#006600")
                         : (Color)ColorConverter.ConvertFromString("#004400"));
        }

        // Deploy button has its own style
        if (activeSection == "Deploy")
        {
            NavDeploy.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#332200"));
            NavDeploy.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFAA00"));
        }
        else
        {
            NavDeploy.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A00"));
            NavDeploy.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFAA00"));
        }
    }

    #endregion

    #region Roster Section

    private void ShowRoster()
    {
        AddSectionHeader("FRAME ROSTER");

        var roster = _management.GetRoster();
        if (!roster.Any())
        {
            AddInfoLine("No frames in roster. Visit the MARKET to purchase frames.", "#888888");
            return;
        }

        foreach (var frame in roster)
        {
            var chassis = frame.Chassis;
            if (chassis == null) continue;

            var panel = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    frame.Status == "Ready" ? "#004400" :
                    frame.Status == "Damaged" ? "#444400" : "#440000")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var content = new StackPanel();

            // Frame header
            string statusColor = frame.Status switch
            {
                "Ready" => "#00FF00",
                "Damaged" => "#FFAA00",
                "Destroyed" => "#FF3333",
                "Repairing" => "#4488FF",
                _ => "#888888"
            };

            content.Children.Add(MakeText(
                $"{frame.CustomName} - {chassis.Designation} {chassis.Name} ({chassis.Class})",
                "#00FF00", 12, true));

            // Status line
            string pilotInfo = frame.PilotId.HasValue ? $"Pilot: {frame.Pilot?.Callsign ?? "?"}" : "Pilot: NONE";
            content.Children.Add(MakeText(
                $"Status: {frame.Status}  |  {pilotInfo}",
                statusColor, 10));

            // Loadout summary
            var loadout = _management.GetLoadout(frame.InstanceId);
            var weaponNames = loadout.Where(l => l.Weapon != null).Select(l => l.Weapon!.Name);
            content.Children.Add(MakeText(
                $"Weapons: {(weaponNames.Any() ? string.Join(", ", weaponNames) : "NONE")}",
                "#008800", 10));

            // Armor bar
            int totalArmor = frame.ArmorHead + frame.ArmorCenterTorso + frame.ArmorLeftTorso +
                frame.ArmorRightTorso + frame.ArmorLeftArm + frame.ArmorRightArm + frame.ArmorLegs;
            int maxArmor = chassis.MaxArmorTotal;
            float pct = maxArmor > 0 ? (float)totalArmor / maxArmor : 0;
            string armorColor = pct > 0.6f ? "#00FF00" : pct > 0.3f ? "#FFAA00" : "#FF3333";
            int filledBars = (int)(pct * 20);
            string armorBar = new string('#', filledBars) + new string('-', 20 - filledBars);
            content.Children.Add(MakeText($"Armor: [{armorBar}] {totalArmor}/{maxArmor}", armorColor, 10));

            // Action buttons
            var btnPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };

            if (frame.Status == "Damaged")
            {
                var repairBtn = MakeButton($"REPAIR (${frame.RepairCost:N0})", "#332200", "#FFAA00", "#664400");
                repairBtn.Tag = frame.InstanceId;
                repairBtn.Click += RepairFrame_Click;
                btnPanel.Children.Add(repairBtn);
            }

            var renameBtn = MakeButton("RENAME", "#001133", "#4488FF", "#003388");
            renameBtn.Tag = frame.InstanceId;
            renameBtn.Click += RenameFrame_Click;
            btnPanel.Children.Add(renameBtn);

            var sellBtn = MakeButton("SELL", "#330000", "#FF6600", "#662200");
            sellBtn.Tag = frame.InstanceId;
            sellBtn.Click += SellFrame_Click;
            btnPanel.Children.Add(sellBtn);

            content.Children.Add(btnPanel);

            panel.Child = content;
            ContentPanel.Children.Add(panel);
        }
    }

    private void RepairFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            if (_management.RepairFrame(instanceId))
            {
                RefreshStatusBar();
                ShowSection("Roster");
            }
            else
            {
                MessageBox.Show("Insufficient credits for repair.", "Cannot Repair",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void RenameFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            var frame = _management.GetRoster().FirstOrDefault(f => f.InstanceId == instanceId);
            if (frame == null) return;

            // Build a simple rename dialog
            var dialog = new Window
            {
                Title = "Rename Frame",
                Width = 400, Height = 160,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D0D")),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "ENTER NEW DESIGNATION:",
                FontFamily = new FontFamily("Consolas"), FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA00")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var input = new TextBox
            {
                Text = frame.CustomName,
                FontFamily = new FontFamily("Consolas"), FontSize = 12,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006600")),
                BorderThickness = new Thickness(1),
                CaretBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00")),
                Padding = new Thickness(4, 2, 4, 2),
                MaxLength = 30
            };
            input.SelectAll();
            Grid.SetRow(input, 1);
            grid.Children.Add(input);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okBtn = MakeButton("CONFIRM", "#003300", "#00FF00", "#006600");
            okBtn.Width = 100;
            okBtn.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
            btnPanel.Children.Add(okBtn);

            var cancelBtn = MakeButton("CANCEL", "#1A0000", "#FF3333", "#660000");
            cancelBtn.Width = 100;
            cancelBtn.Margin = new Thickness(8, 0, 0, 0);
            cancelBtn.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };
            btnPanel.Children.Add(cancelBtn);

            Grid.SetRow(btnPanel, 2);
            grid.Children.Add(btnPanel);

            dialog.Content = grid;
            input.Focus();

            if (dialog.ShowDialog() == true)
            {
                string newName = input.Text.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != frame.CustomName)
                {
                    _management.RenameFrame(instanceId, newName);
                    ShowSection("Roster");
                }
            }
        }
    }

    private void SellFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            var result = MessageBox.Show("Sell this frame? This cannot be undone.",
                "Confirm Sale", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _management.SellFrame(instanceId);
                RefreshStatusBar();
                ShowSection("Roster");
            }
        }
    }

    #endregion

    #region Pilots Section

    private void ShowPilots()
    {
        AddSectionHeader("PILOT ROSTER");

        var pilots = _management.GetAllPilots();
        var roster = _management.GetRoster();

        // Hire button
        var hireBtn = MakeButton("HIRE NEW PILOT ($30,000)", "#001A00", "#00CC00", "#004400");
        hireBtn.Click += HirePilot_Click;
        hireBtn.Width = 220;
        hireBtn.Height = 30;
        hireBtn.Margin = new Thickness(0, 0, 0, 8);
        ContentPanel.Children.Add(hireBtn);

        foreach (var pilot in pilots)
        {
            string statusColor = pilot.Status switch
            {
                "Active" => "#00FF00",
                "Injured" => "#FFAA00",
                "KIA" => "#FF3333",
                _ => "#888888"
            };

            var panel = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    pilot.Status == "Active" ? "#004400" : "#444400")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var content = new StackPanel();
            content.Children.Add(MakeText($"\"{pilot.Callsign}\"", "#00FF00", 12, true));
            content.Children.Add(MakeText(
                $"Gunnery: {pilot.GunnerySkill}  Piloting: {pilot.PilotingSkill}  Tactics: {pilot.TacticsSkill}",
                "#00CC00", 10));
            content.Children.Add(MakeText(
                $"XP: {pilot.ExperiencePoints}  Missions: {pilot.MissionsCompleted}  Kills: {pilot.Kills}  Morale: {pilot.Morale}",
                "#008800", 10));

            string statusLine = pilot.Status;
            if (pilot.Status == "Injured")
                statusLine += $" ({pilot.InjuryDays} days remaining)";

            content.Children.Add(MakeText($"Status: {statusLine}", statusColor, 10));

            // Show assignment
            var assignedFrame = roster.FirstOrDefault(f => f.PilotId == pilot.PilotId);
            if (assignedFrame != null)
            {
                content.Children.Add(MakeText(
                    $"Assigned to: {assignedFrame.CustomName} ({assignedFrame.Chassis?.Name})",
                    "#4488FF", 10));
            }
            else if (pilot.Status == "Active")
            {
                // Show assign buttons for available frames
                var availableFrames = roster.Where(f => f.PilotId == null && f.Status == "Ready").ToList();
                if (availableFrames.Any())
                {
                    var assignPanel = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
                    foreach (var frame in availableFrames)
                    {
                        var assignBtn = MakeButton($"ASSIGN TO {frame.CustomName}", "#001133", "#4488FF", "#003388");
                        assignBtn.Tag = new int[] { frame.InstanceId, pilot.PilotId };
                        assignBtn.Click += AssignPilot_Click;
                        assignPanel.Children.Add(assignBtn);
                    }
                    content.Children.Add(assignPanel);
                }
            }

            panel.Child = content;
            ContentPanel.Children.Add(panel);
        }
    }

    private void HirePilot_Click(object sender, RoutedEventArgs e)
    {
        if (_management.HirePilot(out var newPilot))
        {
            RefreshStatusBar();
            ShowSection("Pilots");
        }
        else
        {
            MessageBox.Show("Insufficient credits to hire a pilot.", "Cannot Hire",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AssignPilot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int[] ids)
        {
            _management.AssignPilot(ids[0], ids[1]);
            ShowSection("Pilots");
        }
    }

    #endregion

    #region Market Section

    private void ShowMarket()
    {
        AddSectionHeader("MARKET");

        var factions = _management.GetAllFactions();
        var standings = _management.GetAllStandings();
        var state = _management.GetPlayerState();
        float priceModifier = 1.0f;

        // Faction filter buttons
        var filterPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

        var allBtn = MakeButton("ALL FACTIONS",
            _marketFactionId == null ? "#003300" : "#001A00",
            _marketFactionId == null ? "#00FF00" : "#00CC00",
            _marketFactionId == null ? "#006600" : "#004400");
        allBtn.Tag = -1;
        allBtn.Click += MarketFactionFilter_Click;
        allBtn.Width = 120;
        filterPanel.Children.Add(allBtn);

        foreach (var faction in factions)
        {
            var standing = standings.FirstOrDefault(s => s.FactionId == faction.FactionId);
            bool isSelected = _marketFactionId == faction.FactionId;
            string standingLabel = standing != null ? $" ({standing.StandingLevel})" : "";

            var fBtn = MakeButton(
                $"{faction.ShortName}{standingLabel}",
                isSelected ? "#002233" : "#001122",
                faction.Color,
                isSelected ? "#004466" : "#002244");
            fBtn.Tag = faction.FactionId;
            fBtn.Click += MarketFactionFilter_Click;
            fBtn.Width = 160;
            filterPanel.Children.Add(fBtn);
        }
        ContentPanel.Children.Add(filterPanel);

        // Show discount info for selected faction
        if (_marketFactionId.HasValue)
        {
            var selectedStanding = standings.FirstOrDefault(s => s.FactionId == _marketFactionId.Value);
            var selectedFaction = factions.FirstOrDefault(f => f.FactionId == _marketFactionId.Value);
            if (selectedStanding != null && selectedFaction != null)
            {
                priceModifier = selectedStanding.PriceModifier;
                float discount = (1f - priceModifier) * 100;
                string discountText = discount > 0
                    ? $"Standing: {selectedStanding.StandingLevel} ({selectedStanding.Standing}) — {discount:F0}% discount"
                    : $"Standing: {selectedStanding.StandingLevel} ({selectedStanding.Standing})";
                AddInfoLine(discountText, selectedFaction.Color);
                AddInfoLine("", "#000000");
            }
        }

        // Get filtered items
        List<Chassis> allChassis;
        List<Weapon> allWeapons;

        if (_marketFactionId.HasValue)
        {
            allChassis = _management.GetFactionMarketChassis(_marketFactionId.Value);
            allWeapons = _management.GetFactionMarketWeapons(_marketFactionId.Value);
        }
        else
        {
            allChassis = _management.GetAllChassis();
            allWeapons = _management.GetAllWeapons();
        }

        // === Chassis Section ===
        AddInfoLine("=== CHASSIS ===", "#006600");
        AddInfoLine("", "#000000");

        foreach (var chassisClass in new[] { "Light", "Medium", "Heavy", "Assault" })
        {
            var classItems = allChassis.Where(c => c.Class == chassisClass).ToList();
            if (!classItems.Any()) continue;

            AddInfoLine($"--- {chassisClass.ToUpper()} ---", "#005500");

            foreach (var chassis in classItems)
            {
                int basePrice = ManagementService.GetChassisPrice(chassis);
                int price = (int)(basePrice * priceModifier);
                bool canAfford = state != null && state.Credits >= price;

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var content = new StackPanel();
                content.Children.Add(MakeText(
                    $"{chassis.Designation} {chassis.Name}",
                    "#00FF00", 11, true));
                content.Children.Add(MakeText(
                    $"HP: {chassis.HardpointSmall}S/{chassis.HardpointMedium}M/{chassis.HardpointLarge}L  " +
                    $"Reactor: {chassis.ReactorOutput}  Move: {chassis.MovementEnergyCost}E  " +
                    $"Armor: {chassis.MaxArmorTotal}",
                    "#00AA00", 10));
                content.Children.Add(MakeText(
                    $"Speed: {chassis.BaseSpeed}  Evasion: {chassis.BaseEvasion}  Space: {chassis.TotalSpace}",
                    "#008800", 10));

                string priceLabel = price < basePrice ? $"PURCHASE ${price:N0} (was ${basePrice:N0})" : $"PURCHASE ${price:N0}";
                var buyBtn = MakeButton(priceLabel,
                    canAfford ? "#003300" : "#1A0000",
                    canAfford ? "#00FF00" : "#663333",
                    canAfford ? "#006600" : "#330000");
                buyBtn.Tag = new int[] { chassis.ChassisId, (int)(priceModifier * 100) };
                buyBtn.IsEnabled = canAfford;
                buyBtn.Click += PurchaseChassis_Click;
                buyBtn.Width = 240;
                buyBtn.Margin = new Thickness(0, 4, 0, 0);
                content.Children.Add(buyBtn);

                panel.Child = content;
                ContentPanel.Children.Add(panel);
            }
        }

        // === Weapons Section ===
        AddInfoLine("", "#000000");
        AddInfoLine("=== WEAPONS ===", "#006600");
        AddInfoLine("Purchased weapons are added to company INVENTORY.", "#005500");
        AddInfoLine("", "#000000");

        foreach (var size in new[] { "Small", "Medium", "Large" })
        {
            var sizeItems = allWeapons.Where(w => w.HardpointSize == size).ToList();
            if (!sizeItems.Any()) continue;

            AddInfoLine($"--- {size.ToUpper()} HARDPOINT ---", "#005500");

            foreach (var weapon in sizeItems)
            {
                // Check exclusive access
                FactionStanding? weaponStanding = null;
                if (weapon.FactionId.HasValue)
                    weaponStanding = standings.FirstOrDefault(s => s.FactionId == weapon.FactionId.Value);

                bool isLocked = !_management.CanAccessExclusive(weapon, weaponStanding);

                int basePrice = weapon.PurchaseCost;
                int price = (int)(basePrice * priceModifier);
                bool canAfford = state != null && state.Credits >= price && !isLocked;

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isLocked ? "#330000" : "#003300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isLocked ? "#0A0000" : "#0A0A0A")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var content = new StackPanel();
                string nameLabel = isLocked ? $"{weapon.Name} [LOCKED — Allied required]" : weapon.Name;
                content.Children.Add(MakeText(nameLabel, isLocked ? "#663333" : "#00FF00", 11, true));
                content.Children.Add(MakeText(
                    $"Type: {weapon.WeaponType}  Dmg: {weapon.Damage}  Energy: {weapon.EnergyCost}  " +
                    $"Range: {weapon.RangeClass}  Acc: {weapon.BaseAccuracy}%",
                    isLocked ? "#553333" : "#00AA00", 10));

                string ammoInfo = weapon.AmmoPerShot > 0 ? $"  Ammo: {weapon.AmmoPerShot}/shot" : "";
                string special = weapon.SpecialEffect != null ? $"  [{weapon.SpecialEffect}]" : "";
                content.Children.Add(MakeText(
                    $"Space: {weapon.SpaceCost}{ammoInfo}{special}",
                    isLocked ? "#442222" : "#008800", 10));

                if (!isLocked)
                {
                    string wpnPriceLabel = price < basePrice ? $"BUY ${price:N0} (was ${basePrice:N0})" : $"BUY ${price:N0}";
                    var buyBtn = MakeButton(wpnPriceLabel,
                        canAfford ? "#003300" : "#1A0000",
                        canAfford ? "#00FF00" : "#663333",
                        canAfford ? "#006600" : "#330000");
                    buyBtn.Tag = new int[] { weapon.WeaponId, (int)(priceModifier * 100) };
                    buyBtn.IsEnabled = canAfford;
                    buyBtn.Click += PurchaseWeapon_Click;
                    buyBtn.Width = 220;
                    buyBtn.Margin = new Thickness(0, 3, 0, 0);
                    content.Children.Add(buyBtn);
                }

                panel.Child = content;
                ContentPanel.Children.Add(panel);
            }
        }
    }

    private void MarketFactionFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int factionId)
        {
            _marketFactionId = factionId == -1 ? null : factionId;
            ShowSection("Market");
        }
    }

    private void PurchaseChassis_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int[] data)
        {
            int chassisId = data[0];
            float modifier = data[1] / 100f;

            var roster = _management.GetRoster();
            string name = $"Frame-{roster.Count + 1}";

            if (_management.PurchaseChassis(chassisId, name, modifier))
            {
                RefreshStatusBar();
                ShowSection("Market");
            }
        }
    }

    private void PurchaseWeapon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int[] data)
        {
            int weaponId = data[0];
            float modifier = data[1] / 100f;

            if (_management.PurchaseWeapon(weaponId, modifier))
            {
                RefreshStatusBar();
                ShowSection("Market");
            }
        }
    }

    #endregion

    #region Inventory Section

    private void ShowInventory()
    {
        AddSectionHeader("COMPANY INVENTORY");

        var inventory = _management.GetInventory();

        if (!inventory.Any())
        {
            AddInfoLine("Inventory is empty. Buy weapons from MARKET or collect SALVAGE after combat.", "#888888");
            return;
        }

        AddInfoLine($"Weapons in storage: {inventory.Count}", "#006600");
        AddInfoLine("", "#000000");

        foreach (var size in new[] { "Small", "Medium", "Large" })
        {
            var items = inventory.Where(i => i.Weapon?.HardpointSize == size).ToList();
            if (!items.Any()) continue;

            AddInfoLine($"--- {size.ToUpper()} HARDPOINT ---", "#005500");

            foreach (var item in items)
            {
                if (item.Weapon == null) continue;

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var content = new StackPanel();
                content.Children.Add(MakeText(item.Weapon.Name, "#00FF00", 11, true));
                content.Children.Add(MakeText(
                    $"Type: {item.Weapon.WeaponType}  Dmg: {item.Weapon.Damage}  Energy: {item.Weapon.EnergyCost}  " +
                    $"Range: {item.Weapon.RangeClass}  Acc: {item.Weapon.BaseAccuracy}%",
                    "#00AA00", 10));

                var sellBtn = MakeButton($"SELL ${item.Weapon.SalvageValue:N0}", "#330000", "#FF6600", "#662200");
                sellBtn.Tag = item.InventoryId;
                sellBtn.Click += SellWeapon_Click;
                sellBtn.Width = 140;
                sellBtn.Margin = new Thickness(0, 3, 0, 0);
                content.Children.Add(sellBtn);

                panel.Child = content;
                ContentPanel.Children.Add(panel);
            }
        }
    }

    private void SellWeapon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int inventoryId)
        {
            if (_management.SellWeapon(inventoryId))
            {
                RefreshStatusBar();
                ShowSection("Inventory");
            }
        }
    }

    #endregion

    #region Refit Section

    private int? _refitFrameId;

    private void ShowRefit()
    {
        AddSectionHeader("REFIT BAY");
        AddInfoLine("Equip and unequip weapons from company inventory.", "#006600");
        AddInfoLine("", "#000000");

        var roster = _management.GetRoster();
        var readyFrames = roster.Where(f => f.Status == "Ready").ToList();

        if (!readyFrames.Any())
        {
            AddInfoLine("No frames available for refit. Frames must be in Ready status.", "#888888");
            return;
        }

        // Frame selection buttons
        AddInfoLine("Select frame to refit:", "#00AA00");
        var selectPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };
        foreach (var frame in readyFrames)
        {
            bool isSelected = _refitFrameId == frame.InstanceId;
            var btn = MakeButton(
                $"{frame.CustomName} ({frame.Chassis?.Name})",
                isSelected ? "#003300" : "#001A00",
                isSelected ? "#00FF00" : "#00CC00",
                isSelected ? "#006600" : "#004400");
            btn.Tag = frame.InstanceId;
            btn.Click += RefitSelectFrame_Click;
            btn.Width = 180;
            selectPanel.Children.Add(btn);
        }
        ContentPanel.Children.Add(selectPanel);

        if (_refitFrameId == null) return;

        var selectedFrame = readyFrames.FirstOrDefault(f => f.InstanceId == _refitFrameId);
        if (selectedFrame?.Chassis == null) return;

        var chassis = selectedFrame.Chassis;
        var loadout = _management.GetLoadout(selectedFrame.InstanceId);

        // Show current loadout
        AddInfoLine($"=== {selectedFrame.CustomName} - {chassis.Designation} {chassis.Name} ===", "#00FF00");
        AddInfoLine($"Hardpoints: {chassis.HardpointSmall}S / {chassis.HardpointMedium}M / {chassis.HardpointLarge}L  |  Space: {chassis.TotalSpace}", "#00AA00");
        int usedSpace = loadout.Where(l => l.Weapon != null).Sum(l => l.Weapon!.SpaceCost);
        AddInfoLine($"Space used: {usedSpace}/{chassis.TotalSpace}", "#008800");
        AddInfoLine("", "#000000");

        // Current weapons
        AddInfoLine("--- EQUIPPED WEAPONS ---", "#005500");
        if (!loadout.Any())
        {
            AddInfoLine("  (none)", "#444444");
        }
        else
        {
            foreach (var slot in loadout)
            {
                if (slot.Weapon == null) continue;

                var weaponPanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A00")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var wContent = new StackPanel { Orientation = Orientation.Horizontal };
                wContent.Children.Add(MakeText(
                    $"[G{slot.WeaponGroup}] {slot.Weapon.Name} ({slot.Weapon.HardpointSize}) @ {slot.MountLocation}  ",
                    "#FFCC00", 10));

                var removeBtn = MakeButton("UNEQUIP", "#330000", "#FF6600", "#662200");
                removeBtn.Tag = slot.LoadoutId;
                removeBtn.Click += UnequipWeapon_Click;
                wContent.Children.Add(removeBtn);

                weaponPanel.Child = wContent;
                ContentPanel.Children.Add(weaponPanel);
            }
        }

        // Available inventory weapons to equip
        AddInfoLine("", "#000000");
        AddInfoLine("--- EQUIP FROM INVENTORY ---", "#005500");

        var inventory = _management.GetInventory();
        if (!inventory.Any())
        {
            AddInfoLine("  No weapons in inventory. Buy from MARKET or collect SALVAGE.", "#444444");
            return;
        }

        // Determine available hardpoint slots
        int smallUsed = loadout.Count(l => l.Weapon?.HardpointSize == "Small");
        int mediumUsed = loadout.Count(l => l.Weapon?.HardpointSize == "Medium");
        int largeUsed = loadout.Count(l => l.Weapon?.HardpointSize == "Large");

        int smallFree = chassis.HardpointSmall - smallUsed;
        int mediumFree = chassis.HardpointMedium - mediumUsed;
        int largeFree = chassis.HardpointLarge - largeUsed;
        int spaceRemaining = chassis.TotalSpace - usedSpace;

        AddInfoLine($"Free slots: {smallFree}S / {mediumFree}M / {largeFree}L  |  Space remaining: {spaceRemaining}", "#006600");

        foreach (var item in inventory)
        {
            if (item.Weapon == null) continue;

            bool hasFreeSlot = item.Weapon.HardpointSize switch
            {
                "Small" => smallFree > 0,
                "Medium" => mediumFree > 0,
                "Large" => largeFree > 0,
                _ => false
            };
            bool hasSpace = spaceRemaining >= item.Weapon.SpaceCost;
            bool canEquip = hasFreeSlot && hasSpace;

            var equipPanel = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canEquip ? "#003300" : "#1A1A1A")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var eContent = new StackPanel { Orientation = Orientation.Horizontal };
            eContent.Children.Add(MakeText(
                $"{item.Weapon.Name} ({item.Weapon.HardpointSize}) Dmg:{item.Weapon.Damage} E:{item.Weapon.EnergyCost} Sp:{item.Weapon.SpaceCost}  ",
                canEquip ? "#00CC00" : "#555555", 10));

            if (canEquip)
            {
                var equipBtn = MakeButton("EQUIP", "#003300", "#00FF00", "#006600");
                // Store inventoryId + frame info for equipping
                equipBtn.Tag = new int[] { item.InventoryId, selectedFrame.InstanceId, item.WeaponId };
                equipBtn.Click += EquipWeapon_Click;
                eContent.Children.Add(equipBtn);
            }
            else
            {
                eContent.Children.Add(MakeText(
                    !hasFreeSlot ? "(no slot)" : "(no space)",
                    "#553333", 10));
            }

            equipPanel.Child = eContent;
            ContentPanel.Children.Add(equipPanel);
        }
    }

    private void RefitSelectFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            _refitFrameId = instanceId;
            ShowSection("Refit");
        }
    }

    private void UnequipWeapon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int loadoutId && _refitFrameId.HasValue)
        {
            _management.UnequipToInventory(_refitFrameId.Value, loadoutId);
            ShowSection("Refit");
        }
    }

    private void EquipWeapon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int[] ids && ids.Length >= 3)
        {
            int inventoryId = ids[0];
            int instanceId = ids[1];
            int weaponId = ids[2];

            // Auto-determine slot name, group, and mount location
            var loadout = _management.GetLoadout(instanceId);
            var frame = _management.GetRoster().FirstOrDefault(f => f.InstanceId == instanceId);
            if (frame?.Chassis == null) return;

            var weapon = _management.GetAllWeapons().FirstOrDefault(w => w.WeaponId == weaponId);
            if (weapon == null) return;

            // Find next available slot name
            string sizePrefix = weapon.HardpointSize.ToLower();
            int slotNum = 1;
            while (loadout.Any(l => l.HardpointSlot == $"{sizePrefix}_{slotNum}"))
                slotNum++;
            string slotName = $"{sizePrefix}_{slotNum}";

            // Auto-assign weapon group (next available)
            int maxGroup = loadout.Any() ? loadout.Max(l => l.WeaponGroup) : 0;
            int weaponGroup = maxGroup + 1;

            // Auto-assign mount location
            string[] mountOrder = weapon.HardpointSize switch
            {
                "Large" => new[] { "LeftTorso", "RightTorso", "CenterTorso" },
                "Medium" => new[] { "LeftArm", "RightArm", "LeftTorso", "RightTorso" },
                "Small" => new[] { "CenterTorso", "LeftArm", "RightArm", "Head" },
                _ => new[] { "CenterTorso" }
            };

            var usedMounts = loadout.Select(l => l.MountLocation).ToHashSet();
            string mount = mountOrder.FirstOrDefault(m => !usedMounts.Contains(m)) ?? mountOrder[0];

            _management.EquipFromInventory(instanceId, inventoryId, slotName, weaponGroup, mount);
            ShowSection("Refit");
        }
    }

    #endregion

    #region Missions Section

    private void ShowMissions()
    {
        AddSectionHeader("AVAILABLE CONTRACTS");

        if (!_availableMissions.Any())
        {
            AddInfoLine("No contracts available. Advance the day to get new contracts.", "#888888");
            return;
        }

        foreach (var mission in _availableMissions)
        {
            string diffColor = mission.Difficulty switch
            {
                1 => "#00CC00",
                2 => "#00FF00",
                3 => "#FFAA00",
                4 => "#FF6600",
                5 => "#FF3333",
                _ => "#888888"
            };

            var panel = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mission.EmployerFactionColor)),
                BorderThickness = new Thickness(3, 1, 1, 1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var content = new StackPanel();
            content.Children.Add(MakeText(mission.Title, "#00FF00", 13, true));
            content.Children.Add(MakeText($"Employer: {mission.EmployerFactionName}", mission.EmployerFactionColor, 10, true));
            content.Children.Add(MakeText(mission.Description, "#008800", 10));

            string stars = new string('*', mission.Difficulty) + new string('.', 5 - mission.Difficulty);
            content.Children.Add(MakeText($"Difficulty: [{stars}]", diffColor, 10));

            content.Children.Add(MakeText(
                $"Reward: ${mission.CreditReward:N0}  Bonus: ${mission.BonusCredits:N0}  " +
                $"Rep: +{mission.ReputationReward}  Salvage: {mission.SalvageChance}%",
                "#FFAA00", 10));

            // Enemy composition with opponent faction
            string enemies = string.Join(", ",
                mission.EnemyComposition.Select(e => $"{e.Count}x {e.ChassisClass}"));
            content.Children.Add(MakeText($"Enemy Force ({mission.OpponentFactionName}): {enemies}", mission.OpponentFactionColor, 10));

            var selectBtn = MakeButton("SELECT MISSION", "#332200", "#FFAA00", "#664400");
            selectBtn.Tag = mission.MissionId;
            selectBtn.Click += SelectMission_Click;
            selectBtn.Width = 160;
            selectBtn.Margin = new Thickness(0, 4, 0, 0);
            content.Children.Add(selectBtn);

            panel.Child = content;
            ContentPanel.Children.Add(panel);
        }
    }

    private void SelectMission_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int missionId)
        {
            _selectedMission = _availableMissions.FirstOrDefault(m => m.MissionId == missionId);
            _selectedDeployFrames.Clear();
            ShowSection("Deploy");
        }
    }

    #endregion

    #region Deploy Section

    private void ShowDeploy()
    {
        AddSectionHeader("DEPLOY LANCE");

        if (_selectedMission == null)
        {
            AddInfoLine("No mission selected. Go to MISSIONS to select a contract.", "#888888");
            return;
        }

        // Mission info
        AddInfoLine($"Mission: {_selectedMission.Title} (Difficulty: {_selectedMission.Difficulty})", "#FFAA00");
        AddInfoLine($"Employer: {_selectedMission.EmployerFactionName}  |  Opponent: {_selectedMission.OpponentFactionName}", _selectedMission.EmployerFactionColor);
        string enemies = string.Join(", ",
            _selectedMission.EnemyComposition.Select(e => $"{e.Count}x {e.ChassisClass}"));
        AddInfoLine($"Enemy Force ({_selectedMission.OpponentFactionName}): {enemies}", _selectedMission.OpponentFactionColor);
        AddInfoLine("", "#000000");
        AddInfoLine("Select 1-4 frames to deploy (must have pilots and be Ready):", "#00AA00");
        AddInfoLine("", "#000000");

        var roster = _management.GetRoster();
        var readyFrames = roster.Where(f => f.Status == "Ready" && f.PilotId.HasValue).ToList();

        if (!readyFrames.Any())
        {
            AddInfoLine("No frames ready for deployment. Assign pilots and repair frames first.", "#FF6600");
            return;
        }

        foreach (var frame in readyFrames)
        {
            bool isSelected = _selectedDeployFrames.Contains(frame.InstanceId);

            var panel = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isSelected ? "#006600" : "#003300")),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isSelected ? "#001A00" : "#0A0A0A")),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var content = new StackPanel();

            string pilotName = frame.Pilot?.Callsign ?? "Unknown";
            content.Children.Add(MakeText(
                $"{(isSelected ? "[X] " : "[ ] ")}{frame.CustomName} - {frame.Chassis?.Designation} {frame.Chassis?.Name} ({frame.Chassis?.Class})",
                isSelected ? "#00FF00" : "#00AA00", 11, true));
            content.Children.Add(MakeText($"Pilot: \"{pilotName}\"", "#4488FF", 10));

            var loadout = _management.GetLoadout(frame.InstanceId);
            var weaponNames = loadout.Where(l => l.Weapon != null).Select(l => l.Weapon!.Name);
            content.Children.Add(MakeText(
                $"Weapons: {(weaponNames.Any() ? string.Join(", ", weaponNames) : "NONE")}",
                "#008800", 10));

            var toggleBtn = MakeButton(
                isSelected ? "REMOVE" : "ADD TO LANCE",
                isSelected ? "#330000" : "#003300",
                isSelected ? "#FF6600" : "#00FF00",
                isSelected ? "#662200" : "#006600");
            toggleBtn.Tag = frame.InstanceId;
            toggleBtn.Click += ToggleDeployFrame_Click;
            toggleBtn.Width = 140;
            toggleBtn.Margin = new Thickness(0, 4, 0, 0);
            content.Children.Add(toggleBtn);

            panel.Child = content;
            ContentPanel.Children.Add(panel);
        }

        // Deploy button
        if (_selectedDeployFrames.Count > 0 && _selectedDeployFrames.Count <= 4)
        {
            AddInfoLine("", "#000000");
            var deployBtn = MakeButton($"DEPLOY LANCE ({_selectedDeployFrames.Count} FRAMES)",
                "#003300", "#00FF00", "#006600");
            deployBtn.Click += DeployLance_Click;
            deployBtn.Width = 280;
            deployBtn.Height = 36;
            deployBtn.FontSize = 13;
            ContentPanel.Children.Add(deployBtn);
        }
    }

    private void ToggleDeployFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            if (_selectedDeployFrames.Contains(instanceId))
                _selectedDeployFrames.Remove(instanceId);
            else if (_selectedDeployFrames.Count < 4)
                _selectedDeployFrames.Add(instanceId);

            ShowSection("Deploy");
        }
    }

    private void DeployLance_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMission == null || _selectedDeployFrames.Count == 0) return;

        LaunchCombat = true;
        DeployedFrameIds = _selectedDeployFrames.ToList();
        DialogResult = true;
        Close();
    }

    #endregion

    #region Bottom Controls

    private void AdvanceDay_Click(object sender, RoutedEventArgs e)
    {
        _management.AdvanceDay();
        GenerateMissions();
        RefreshStatusBar();
        ShowSection(_currentSection);
    }

    private void QuickCombat_Click(object sender, RoutedEventArgs e)
    {
        // Launch combat with test scenario (bypass deployment)
        LaunchCombat = false;
        DialogResult = true;
        Close();
    }

    private void SaveAndExit_Click(object sender, RoutedEventArgs e)
    {
        ReturnToMainMenu = true;
        DialogResult = true;
        Close();
    }

    #endregion

    #region UI Helpers

    private void AddSectionHeader(string text)
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00")),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 8)
        });
    }

    private void AddInfoLine(string text, string colorHex)
    {
        ContentPanel.Children.Add(MakeText(text, colorHex, 10));
    }

    private static TextBlock MakeText(string text, string colorHex, int fontSize, bool bold = false)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 1, 0, 1),
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static Button MakeButton(string text, string bgHex, string fgHex, string borderHex)
    {
        return new Button
        {
            Content = text,
            Style = null,
            Height = 26,
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(8, 2, 8, 2),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
            BorderThickness = new Thickness(1)
        };
    }

    #endregion
}
