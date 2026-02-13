using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
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
    private readonly GalaxyService _galaxyService;

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
        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));
        _dbContext = dbContext;
        _management = new ManagementService(dbContext);
        _missionService = new MissionService(dbContext);
        _galaxyService = new GalaxyService(dbContext);

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
        int upkeep = _management.GetDailyUpkeep();
        ReputationText.Text = $"Rep: {state.Reputation}  |  Upkeep: ${upkeep:N0}/day  |  Fuel: {state.Fuel}/{GalaxyService.MaxFuel}";
        var currentSystem = _galaxyService.GetCurrentSystem();
        var currentPlanet = _galaxyService.GetCurrentPlanet();
        string locationText = currentPlanet != null && currentSystem != null
            ? $"{currentPlanet.Name}, {currentSystem.Name}"
            : "Unknown";
        MissionsText.Text = $"Missions: {state.MissionsWon}/{state.MissionsCompleted}  |  Location: {locationText}";

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
        _availableMissions = _missionService.GenerateContracts(3, state.Reputation, standings,
            state.CurrentSystemId, state.CurrentPlanetId);
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
            case "Galaxy": ShowGalaxy(); break;
            case "Missions": ShowMissions(); break;
            case "Deploy": ShowDeploy(); break;
        }
    }

    private void UpdateNavHighlight(string activeSection)
    {
        var buttons = new[] { NavRoster, NavPilots, NavMarket, NavInventory, NavRefit, NavGalaxy, NavMissions, NavDeploy };
        var tags = new[] { "Roster", "Pilots", "Market", "Inventory", "Refit", "Galaxy", "Missions", "Deploy" };

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

            // Repair time info
            if (frame.Status == "Repairing")
            {
                content.Children.Add(MakeText(
                    $"Repairing... {frame.RepairTime} day{(frame.RepairTime != 1 ? "s" : "")} remaining",
                    "#4488FF", 10));
            }

            // Action buttons
            var btnPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };

            if (frame.Status == "Damaged" || frame.Status == "Destroyed")
            {
                int rushCost = frame.RepairCost * 2;
                int rushDays = Math.Max(1, (int)Math.Ceiling(frame.RepairTime / 2.0));

                var repairBtn = MakeButton(
                    $"REPAIR ${frame.RepairCost:N0} ({frame.RepairTime}d)",
                    "#332200", "#FFAA00", "#664400");
                repairBtn.Tag = frame.InstanceId;
                repairBtn.Click += RepairFrame_Click;
                btnPanel.Children.Add(repairBtn);

                var rushBtn = MakeButton(
                    $"RUSH ${rushCost:N0} ({rushDays}d)",
                    "#331100", "#FF6600", "#663300");
                rushBtn.Tag = frame.InstanceId;
                rushBtn.Click += RushRepairFrame_Click;
                btnPanel.Children.Add(rushBtn);
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

    private void RushRepairFrame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            if (_management.RushRepairFrame(instanceId))
            {
                RefreshStatusBar();
                ShowSection("Roster");
            }
            else
            {
                MessageBox.Show("Insufficient credits for rush repair.", "Cannot Rush Repair",
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

        // === Equipment Section ===
        AddInfoLine("", "#000000");
        AddInfoLine("=== EQUIPMENT ===", "#0066AA");
        AddInfoLine("Purchased equipment is added to company INVENTORY.", "#004488");
        AddInfoLine("", "#000000");

        var allEquipment = _management.GetAllEquipment();

        foreach (var category in new[] { "Passive", "Active", "Slot" })
        {
            var catItems = allEquipment.Where(e => e.Category == category).ToList();
            if (!catItems.Any()) continue;

            AddInfoLine($"--- {category.ToUpper()} ---", "#004488");

            foreach (var eq in catItems)
            {
                int eqPrice = eq.PurchaseCost;
                bool canAffordEq = state != null && state.Credits >= eqPrice;

                var eqPanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003366")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A14")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var eqContent = new StackPanel();
                string sizeTag = eq.HardpointSize != null ? $" [{eq.HardpointSize[0]}]" : "";
                eqContent.Children.Add(MakeText($"{eq.Name}{sizeTag}", "#5599DD", 11, true));
                eqContent.Children.Add(MakeText(
                    $"Space: {eq.SpaceCost}  Energy: {eq.EnergyCost}  |  {eq.Description ?? eq.Effect}",
                    "#4488BB", 10));

                var eqBuyBtn = MakeButton($"BUY ${eqPrice:N0}",
                    canAffordEq ? "#002244" : "#1A0000",
                    canAffordEq ? "#5599DD" : "#663333",
                    canAffordEq ? "#004488" : "#330000");
                eqBuyBtn.Tag = eq.EquipmentId;
                eqBuyBtn.IsEnabled = canAffordEq;
                eqBuyBtn.Click += PurchaseEquipment_Click;
                eqBuyBtn.Width = 180;
                eqBuyBtn.Margin = new Thickness(0, 3, 0, 0);
                eqContent.Children.Add(eqBuyBtn);

                eqPanel.Child = eqContent;
                ContentPanel.Children.Add(eqPanel);
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

    private void PurchaseEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int equipmentId)
        {
            if (_management.PurchaseEquipment(equipmentId))
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
        var equipmentInventory = _management.GetEquipmentInventory();

        if (!inventory.Any() && !equipmentInventory.Any())
        {
            AddInfoLine("Inventory is empty. Buy weapons and equipment from MARKET or collect SALVAGE after combat.", "#888888");
            return;
        }

        // --- Weapons Section ---
        if (inventory.Any())
        {
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

        // --- Equipment Section ---
        if (equipmentInventory.Any())
        {
            AddInfoLine("", "#000000");
            AddInfoLine($"Equipment in storage: {equipmentInventory.Count}", "#003366");
            AddInfoLine("", "#000000");

            foreach (var category in new[] { "Passive", "Active", "Slot" })
            {
                var items = equipmentInventory.Where(i => i.Equipment?.Category == category).ToList();
                if (!items.Any()) continue;

                AddInfoLine($"--- {category.ToUpper()} EQUIPMENT ---", "#004488");

                foreach (var item in items)
                {
                    if (item.Equipment == null) continue;

                    var panel = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003366")),
                        BorderThickness = new Thickness(1),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                        Padding = new Thickness(6),
                        Margin = new Thickness(0, 0, 0, 3)
                    };

                    var content = new StackPanel();
                    content.Children.Add(MakeText(item.Equipment.Name, "#5599DD", 11, true));

                    string slotInfo = item.Equipment.HardpointSize != null ? $"  Slot: {item.Equipment.HardpointSize}" : "";
                    content.Children.Add(MakeText(
                        $"Category: {item.Equipment.Category}{slotInfo}  Space: {item.Equipment.SpaceCost}  Energy: {item.Equipment.EnergyCost}",
                        "#4488BB", 10));

                    if (!string.IsNullOrEmpty(item.Equipment.Description))
                        content.Children.Add(MakeText(item.Equipment.Description, "#336699", 9));

                    var sellBtn = MakeButton($"SELL ${item.Equipment.SalvageValue:N0}", "#330000", "#FF6600", "#662200");
                    sellBtn.Tag = item.EquipmentInventoryId;
                    sellBtn.Click += SellEquipment_Click;
                    sellBtn.Width = 140;
                    sellBtn.Margin = new Thickness(0, 3, 0, 0);
                    content.Children.Add(sellBtn);

                    panel.Child = content;
                    ContentPanel.Children.Add(panel);
                }
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

    private void SellEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int equipmentInventoryId)
        {
            if (_management.SellEquipment(equipmentInventoryId))
            {
                RefreshStatusBar();
                ShowSection("Inventory");
            }
        }
    }

    #endregion

    #region Refit Section

    private void ShowRefit()
    {
        AddSectionHeader("REFIT BAY");
        AddInfoLine("Visual mech configuration — equip weapons by location.", "#006600");
        AddInfoLine("", "#000000");

        var roster = _management.GetRoster();
        var readyFrames = roster.Where(f => f.Status == "Ready").ToList();

        if (!readyFrames.Any())
        {
            AddInfoLine("No frames available for refit. Frames must be in Ready status.", "#888888");
            return;
        }

        AddInfoLine("Select frame to open refit bay:", "#00AA00");
        var selectPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };
        foreach (var frame in readyFrames)
        {
            var loadout = _management.GetLoadout(frame.InstanceId);
            int weaponCount = loadout.Count(l => l.Weapon != null);
            string label = $"{frame.CustomName} ({frame.Chassis?.Name}) [{weaponCount}W]";

            var btn = MakeButton(label, "#001A00", "#00CC00", "#004400");
            btn.Tag = frame.InstanceId;
            btn.Click += OpenRefitWindow_Click;
            btn.Width = 220;
            selectPanel.Children.Add(btn);
        }
        ContentPanel.Children.Add(selectPanel);
    }

    private void OpenRefitWindow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int instanceId)
        {
            var refitWindow = new RefitWindow(_dbContext, instanceId);
            refitWindow.Owner = this;
            refitWindow.ShowDialog();

            if (refitWindow.RefitApplied)
            {
                RefreshStatusBar();
                ShowSection("Refit");
            }
        }
    }

    #endregion

    #region Galaxy Section

    private void ShowGalaxy()
    {
        var state = _management.GetPlayerState();
        if (state == null) return;

        var currentSystem = _galaxyService.GetCurrentSystem();
        var currentPlanet = _galaxyService.GetCurrentPlanet();
        if (currentSystem == null || currentPlanet == null) return;

        // === Visual Galaxy Map ===
        DrawGalaxyMap(currentSystem.SystemId);

        // === Current Location ===
        string factionName = currentSystem.ControllingFaction?.Name ?? "Contested";
        string factionColor = currentSystem.ControllingFaction?.Color ?? "#FF4444";

        AddSectionHeader($"CURRENT LOCATION — {currentSystem.Name.ToUpper()} SYSTEM");
        AddInfoLine($"System: {currentSystem.Name}  |  Type: {currentSystem.SystemType}  |  Control: {factionName}", factionColor);
        AddInfoLine(currentSystem.Description, "#888888");
        AddInfoLine("", "#000000");
        AddInfoLine($"Docked at: {currentPlanet.Name} ({currentPlanet.PlanetType})", "#00FF00");
        AddInfoLine(currentPlanet.Description, "#888888");
        string services = "";
        if (currentPlanet.HasMarket) services += "Market  ";
        if (currentPlanet.HasHiring) services += "Hiring  ";
        services += $"Contracts: Difficulty {currentPlanet.ContractDifficultyMin}-{currentPlanet.ContractDifficultyMax}";
        AddInfoLine($"Services: {services}", "#AAAAAA");

        // === Fuel ===
        AddInfoLine("", "#000000");
        var fuelPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
        fuelPanel.Children.Add(new TextBlock
        {
            Text = $"FUEL: {state.Fuel} / {GalaxyService.MaxFuel}",
            FontFamily = new FontFamily("Consolas"), FontSize = 13, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(state.Fuel < 15
                ? (Color)ColorConverter.ConvertFromString("#FF4444")
                : (Color)ColorConverter.ConvertFromString("#00AAFF")),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0)
        });

        if (currentPlanet.HasMarket && state.Fuel < GalaxyService.MaxFuel)
        {
            foreach (int amount in new[] { 10, 25, 50 })
            {
                int actualAmount = Math.Min(amount, GalaxyService.MaxFuel - state.Fuel);
                if (actualAmount <= 0) continue;
                int cost = actualAmount * GalaxyService.FuelPricePerUnit;

                var buyBtn = new Button
                {
                    Content = $"BUY {actualAmount} (${cost:N0})",
                    Tag = actualAmount,
                    Style = null,
                    Width = 140, Height = 26,
                    FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#001133")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4488FF")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003388")),
                    Margin = new Thickness(4, 0, 0, 0)
                };
                buyBtn.Click += BuyFuel_Click;
                fuelPanel.Children.Add(buyBtn);
            }
        }
        ContentPanel.Children.Add(fuelPanel);

        // === Local Destinations (same system) ===
        var localPlanets = _galaxyService.GetSystemPlanets(currentSystem.SystemId);
        var otherPlanets = localPlanets.Where(p => p.PlanetId != currentPlanet.PlanetId).ToList();

        if (otherPlanets.Any())
        {
            AddSectionHeader("LOCAL DESTINATIONS (In-System)");
            AddInfoLine($"Travel cost: {GalaxyService.IntraSystemFuelCost} fuel, {GalaxyService.IntraSystemTravelDays} day", "#888888");

            foreach (var planet in otherPlanets)
            {
                var planetPanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003333")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A1A1A")),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var innerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                innerPanel.Children.Add(new TextBlock
                {
                    Text = $"{planet.Name} ({planet.PlanetType})",
                    FontFamily = new FontFamily("Consolas"), FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(factionColor)),
                    Width = 280, VerticalAlignment = VerticalAlignment.Center
                });
                innerPanel.Children.Add(new TextBlock
                {
                    Text = planet.Description,
                    FontFamily = new FontFamily("Consolas"), FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                    Width = 400, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
                });

                bool canTravel = state.Fuel >= GalaxyService.IntraSystemFuelCost;
                var travelBtn = new Button
                {
                    Content = canTravel ? "TRAVEL" : "NO FUEL",
                    Tag = planet.PlanetId,
                    Style = null,
                    Width = 80, Height = 26,
                    FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canTravel ? "#002200" : "#1A0000")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canTravel ? "#00FF00" : "#FF4444")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canTravel ? "#004400" : "#440000")),
                    IsEnabled = canTravel,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                travelBtn.Click += TravelToPlanet_Click;
                innerPanel.Children.Add(travelBtn);

                planetPanel.Child = innerPanel;
                ContentPanel.Children.Add(planetPanel);
            }
        }

        // === Jump Routes ===
        var jumps = _galaxyService.GetAvailableJumps();
        if (jumps.Any())
        {
            AddSectionHeader("JUMP ROUTES (Inter-System)");

            foreach (var (route, destination) in jumps)
            {
                string destFactionName = destination.ControllingFaction?.Name ?? "Contested";
                string destFactionColor = destination.ControllingFaction?.Color ?? "#FF4444";
                bool canJump = state.Fuel >= route.Distance;

                var routePanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A0A")),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var innerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                innerPanel.Children.Add(new TextBlock
                {
                    Text = $"{destination.Name} ({destination.SystemType})",
                    FontFamily = new FontFamily("Consolas"), FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(destFactionColor)),
                    Width = 200, VerticalAlignment = VerticalAlignment.Center
                });
                innerPanel.Children.Add(new TextBlock
                {
                    Text = $"{destFactionName}",
                    FontFamily = new FontFamily("Consolas"), FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(destFactionColor)),
                    Width = 160, VerticalAlignment = VerticalAlignment.Center
                });
                innerPanel.Children.Add(new TextBlock
                {
                    Text = $"Fuel: {route.Distance}  |  {route.TravelDays} days",
                    FontFamily = new FontFamily("Consolas"), FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canJump ? "#AAAAAA" : "#FF4444")),
                    Width = 180, VerticalAlignment = VerticalAlignment.Center
                });

                var jumpBtn = new Button
                {
                    Content = canJump ? "JUMP" : "NO FUEL",
                    Tag = new int[] { route.RouteId, destination.SystemId },
                    Style = null,
                    Width = 80, Height = 26,
                    FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canJump ? "#1A1A00" : "#1A0000")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canJump ? "#FFAA00" : "#FF4444")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canJump ? "#444400" : "#440000")),
                    IsEnabled = canJump,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                jumpBtn.Click += JumpToSystem_Click;
                innerPanel.Children.Add(jumpBtn);

                routePanel.Child = innerPanel;
                ContentPanel.Children.Add(routePanel);
            }
        }
    }

    private void DrawGalaxyMap(int currentSystemId)
    {
        var allSystems = _galaxyService.GetAllSystems();
        var allRoutes = _galaxyService.GetAllRoutes();

        if (!allSystems.Any()) return;

        // Map canvas dimensions
        const double mapWidth = 860;
        const double mapHeight = 440;
        const double margin = 60;
        const double nodeRadius = 14;

        // Build coordinate lookup (SystemId → scaled position)
        float minX = allSystems.Min(s => s.X);
        float maxX = allSystems.Max(s => s.X);
        float minY = allSystems.Min(s => s.Y);
        float maxY = allSystems.Max(s => s.Y);
        float rangeX = Math.Max(maxX - minX, 1);
        float rangeY = Math.Max(maxY - minY, 1);

        var positions = new Dictionary<int, (double x, double y)>();
        foreach (var sys in allSystems)
        {
            double sx = margin + (sys.X - minX) / rangeX * (mapWidth - margin * 2);
            double sy = margin + (sys.Y - minY) / rangeY * (mapHeight - margin * 2);
            positions[sys.SystemId] = (sx, sy);
        }

        // Create the canvas
        var mapCanvas = new Canvas
        {
            Width = mapWidth,
            Height = mapHeight,
            Background = new SolidColorBrush(Color.FromRgb(5, 5, 12)),
            ClipToBounds = true
        };

        // Draw subtle grid dots for atmosphere
        for (double gx = 20; gx < mapWidth; gx += 40)
        {
            for (double gy = 20; gy < mapHeight; gy += 40)
            {
                var dot = new Ellipse
                {
                    Width = 1, Height = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 60, 60, 80))
                };
                Canvas.SetLeft(dot, gx);
                Canvas.SetTop(dot, gy);
                mapCanvas.Children.Add(dot);
            }
        }

        // Draw faction territory backgrounds (subtle tinted regions)
        DrawFactionTerritory(mapCanvas, allSystems, positions, 1, Color.FromArgb(15, 0, 180, 220));  // Crucible - cyan
        DrawFactionTerritory(mapCanvas, allSystems, positions, 2, Color.FromArgb(15, 220, 180, 0));   // Directorate - gold
        DrawFactionTerritory(mapCanvas, allSystems, positions, 3, Color.FromArgb(15, 220, 90, 40));    // ORC - orange

        // Draw route lines
        var drawnRoutes = new HashSet<string>();
        foreach (var route in allRoutes)
        {
            string routeKey = $"{Math.Min(route.FromSystemId, route.ToSystemId)}-{Math.Max(route.FromSystemId, route.ToSystemId)}";
            if (drawnRoutes.Contains(routeKey)) continue;
            drawnRoutes.Add(routeKey);

            if (!positions.ContainsKey(route.FromSystemId) || !positions.ContainsKey(route.ToSystemId)) continue;

            var (x1, y1) = positions[route.FromSystemId];
            var (x2, y2) = positions[route.ToSystemId];

            // Route line
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 100, 100, 140)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 6, 3 }
            };
            mapCanvas.Children.Add(line);

            // Fuel cost label at midpoint
            double mx = (x1 + x2) / 2;
            double my = (y1 + y2) / 2;
            var costLabel = new TextBlock
            {
                Text = $"{route.Distance}f",
                FontSize = 8,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromArgb(100, 140, 140, 170))
            };
            costLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(costLabel, mx - costLabel.DesiredSize.Width / 2);
            Canvas.SetTop(costLabel, my - costLabel.DesiredSize.Height / 2);
            mapCanvas.Children.Add(costLabel);
        }

        // Draw system nodes
        foreach (var sys in allSystems)
        {
            if (!positions.ContainsKey(sys.SystemId)) continue;
            var (cx, cy) = positions[sys.SystemId];

            bool isCurrent = sys.SystemId == currentSystemId;
            string colorHex = sys.ControllingFaction?.Color ?? "#FF4444";
            var nodeColor = (Color)ColorConverter.ConvertFromString(colorHex);

            // Glow ring for current system
            if (isCurrent)
            {
                var glow = new Ellipse
                {
                    Width = nodeRadius * 2 + 16, Height = nodeRadius * 2 + 16,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromArgb(120, 0, 255, 0)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(glow, cx - nodeRadius - 8);
                Canvas.SetTop(glow, cy - nodeRadius - 8);
                mapCanvas.Children.Add(glow);

                // Pulsing inner ring
                var innerGlow = new Ellipse
                {
                    Width = nodeRadius * 2 + 8, Height = nodeRadius * 2 + 8,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 0, 255, 0)),
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(innerGlow, cx - nodeRadius - 4);
                Canvas.SetTop(innerGlow, cy - nodeRadius - 4);
                mapCanvas.Children.Add(innerGlow);
            }

            // Node background (darker circle behind)
            var nodeBg = new Ellipse
            {
                Width = nodeRadius * 2 + 2, Height = nodeRadius * 2 + 2,
                Fill = new SolidColorBrush(Color.FromRgb(10, 10, 15)),
                Stroke = new SolidColorBrush(Color.FromArgb(180, nodeColor.R, nodeColor.G, nodeColor.B)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(nodeBg, cx - nodeRadius - 1);
            Canvas.SetTop(nodeBg, cy - nodeRadius - 1);
            mapCanvas.Children.Add(nodeBg);

            // Node circle
            var node = new Ellipse
            {
                Width = nodeRadius * 2, Height = nodeRadius * 2,
                Fill = new RadialGradientBrush(
                    Color.FromArgb(200, nodeColor.R, nodeColor.G, nodeColor.B),
                    Color.FromArgb(80, (byte)(nodeColor.R / 3), (byte)(nodeColor.G / 3), (byte)(nodeColor.B / 3)))
            };
            Canvas.SetLeft(node, cx - nodeRadius);
            Canvas.SetTop(node, cy - nodeRadius);
            mapCanvas.Children.Add(node);

            // System type icon letter (C=Core, F=Frontier, etc.)
            string typeIcon = sys.SystemType switch
            {
                "Core" => "C",
                "Colony" => "O",
                "Frontier" => "F",
                "Contested" => "X",
                _ => "?"
            };
            var typeLabel = new TextBlock
            {
                Text = typeIcon,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center
            };
            typeLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(typeLabel, cx - typeLabel.DesiredSize.Width / 2);
            Canvas.SetTop(typeLabel, cy - typeLabel.DesiredSize.Height / 2);
            mapCanvas.Children.Add(typeLabel);

            // System name label below node
            var nameLabel = new TextBlock
            {
                Text = sys.Name,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(isCurrent
                    ? Color.FromRgb(0, 255, 0)
                    : Color.FromArgb(220, nodeColor.R, nodeColor.G, nodeColor.B)),
                TextAlignment = TextAlignment.Center
            };
            nameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(nameLabel, cx - nameLabel.DesiredSize.Width / 2);
            Canvas.SetTop(nameLabel, cy + nodeRadius + 3);
            mapCanvas.Children.Add(nameLabel);

            // "YOU" marker for current system
            if (isCurrent)
            {
                var youLabel = new TextBlock
                {
                    Text = "YOU",
                    FontSize = 8,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    TextAlignment = TextAlignment.Center
                };
                youLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(youLabel, cx - youLabel.DesiredSize.Width / 2);
                Canvas.SetTop(youLabel, cy - nodeRadius - 14);
                mapCanvas.Children.Add(youLabel);
            }
        }

        // Legend
        double legendY = mapHeight - 28;
        var legendItems = new (string label, string color)[]
        {
            ("Crucible Industries", "#00CCFF"),
            ("Terran Directorate", "#FFCC00"),
            ("Outer Reach Collective", "#FF6633"),
            ("Contested", "#FF4444")
        };
        double legendX = 10;
        foreach (var (label, color) in legendItems)
        {
            var legendDot = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
            };
            Canvas.SetLeft(legendDot, legendX);
            Canvas.SetTop(legendDot, legendY + 3);
            mapCanvas.Children.Add(legendDot);

            var legendText = new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
            };
            Canvas.SetLeft(legendText, legendX + 12);
            Canvas.SetTop(legendText, legendY);
            mapCanvas.Children.Add(legendText);

            legendText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            legendX += legendText.DesiredSize.Width + 24;
        }

        // Wrap in border and add to content
        var mapBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 60, 0)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(5, 5, 12)),
            Margin = new Thickness(0, 0, 0, 10),
            Child = mapCanvas
        };

        ContentPanel.Children.Add(mapBorder);
    }

    private static void DrawFactionTerritory(Canvas canvas, List<StarSystem> systems,
        Dictionary<int, (double x, double y)> positions, int factionId, Color fillColor)
    {
        var factionSystems = systems.Where(s => s.ControllingFactionId == factionId).ToList();
        if (factionSystems.Count < 2) return;

        // Draw a subtle ellipse encompassing faction systems
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var sys in factionSystems)
        {
            if (!positions.ContainsKey(sys.SystemId)) continue;
            var (x, y) = positions[sys.SystemId];
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }

        double padX = 50, padY = 50;
        var region = new Ellipse
        {
            Width = (maxX - minX) + padX * 2,
            Height = (maxY - minY) + padY * 2,
            Fill = new SolidColorBrush(fillColor),
            Stroke = new SolidColorBrush(Color.FromArgb(25, fillColor.R, fillColor.G, fillColor.B)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(region, minX - padX);
        Canvas.SetTop(region, minY - padY);
        canvas.Children.Add(region);
    }

    private void BuyFuel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int amount)
        {
            if (_galaxyService.PurchaseFuel(amount))
            {
                RefreshStatusBar();
                ShowSection("Galaxy");
            }
        }
    }

    private void TravelToPlanet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int planetId)
        {
            var result = _galaxyService.TravelToPlanet(planetId, _management);
            if (result.Success)
            {
                // Regenerate missions at new location
                GenerateMissions();
                RefreshStatusBar();
                ShowSection("Galaxy");

                // Show travel report
                ShowTravelReport(result);
            }
        }
    }

    private void JumpToSystem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int[] ids)
        {
            int routeId = ids[0];
            int destSystemId = ids[1];

            // Pick the first planet in destination system
            var destPlanets = _galaxyService.GetSystemPlanets(destSystemId);
            if (!destPlanets.Any()) return;
            int targetPlanetId = destPlanets.First().PlanetId;

            var result = _galaxyService.JumpToSystem(routeId, targetPlanetId, _management);
            if (result.Success)
            {
                // Regenerate missions at new location
                GenerateMissions();
                RefreshStatusBar();
                ShowSection("Galaxy");

                // Show travel report
                ShowTravelReport(result);
            }
        }
    }

    private void ShowTravelReport(TravelResult result)
    {
        // Insert travel report at top of content
        var reportBorder = new Border
        {
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#004400")),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#001100")),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var reportPanel = new StackPanel();
        reportPanel.Children.Add(new TextBlock
        {
            Text = $"TRAVEL LOG: {result.Message}",
            FontFamily = new FontFamily("Consolas"), FontSize = 11, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00")),
            TextWrapping = TextWrapping.Wrap
        });

        // Show day events from travel
        foreach (var dayReport in result.DayReports)
        {
            if (dayReport.MaintenanceCost > 0)
            {
                reportPanel.Children.Add(new TextBlock
                {
                    Text = $"  Day {dayReport.Day}: Maintenance ${dayReport.MaintenanceCost:N0}",
                    FontFamily = new FontFamily("Consolas"), FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"))
                });
            }
            foreach (var evt in dayReport.Events)
            {
                reportPanel.Children.Add(new TextBlock
                {
                    Text = $"  Day {dayReport.Day}: {evt}",
                    FontFamily = new FontFamily("Consolas"), FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AAFF"))
                });
            }
        }

        reportBorder.Child = reportPanel;
        ContentPanel.Children.Insert(0, reportBorder);
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

            content.Children.Add(MakeText(
                $"Map: {mission.MapSize}  |  Terrain: {mission.Landscape}", "#888888", 10));

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

            string frameClass = frame.Chassis?.Class ?? "Medium";
            int deployCost = ManagementService.GetDeploymentCostPerFrame(frameClass);
            content.Children.Add(MakeText($"Deployment cost: ${deployCost:N0}", "#FFAA00", 10));

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

        // Deploy button with cost
        if (_selectedDeployFrames.Count > 0 && _selectedDeployFrames.Count <= 4)
        {
            int totalDeployCost = _management.GetDeploymentCost(_selectedDeployFrames);
            AddInfoLine("", "#000000");
            AddInfoLine($"Total deployment cost: ${totalDeployCost:N0}", "#FFAA00");
            AddInfoLine("", "#000000");
            var deployBtn = MakeButton($"DEPLOY LANCE ({_selectedDeployFrames.Count} FRAMES — ${totalDeployCost:N0})",
                "#003300", "#00FF00", "#006600");
            deployBtn.Click += DeployLance_Click;
            deployBtn.Width = 360;
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

        if (!_management.DeductDeploymentCost(_selectedDeployFrames))
        {
            MessageBox.Show("Insufficient credits for deployment costs.", "Cannot Deploy",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LaunchCombat = true;
        DeployedFrameIds = _selectedDeployFrames.ToList();
        DialogResult = true;
        Close();
    }

    #endregion

    #region Bottom Controls

    private void AdvanceDay_Click(object sender, RoutedEventArgs e)
    {
        var report = _management.AdvanceDay();
        GenerateMissions();
        RefreshStatusBar();

        // Show day report summary
        string summary = $"Day {report.Day}";
        if (report.MaintenanceCost > 0)
            summary += $"  |  Maintenance: -${report.MaintenanceCost:N0}";
        foreach (var evt in report.Events)
            summary += $"\n  {evt}";

        ShowSection(_currentSection);

        if (report.MaintenanceCost > 0 || report.Events.Count > 0)
        {
            MessageBox.Show(summary, "Day Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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
