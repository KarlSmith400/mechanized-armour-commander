using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MechanizedArmourCommander.Core.Combat;
using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.UI;

public partial class TacticalDecisionWindow : Window
{
    public RoundTacticalDecision Decision { get; private set; } = new();
    public bool AutoResolveRemaining { get; private set; } = false;
    public bool UseAI { get; private set; } = false;

    private readonly int _roundNumber;
    private readonly RoundSituation _situation;
    private int _selectedFrameId = -1;

    // Per-frame action plans (built from UI)
    private readonly Dictionary<int, List<PlannedAction>> _framePlans = new();
    private readonly Dictionary<int, int?> _frameFocusTargets = new();

    public TacticalDecisionWindow(int roundNumber, RoundSituation situation)
    {
        InitializeComponent();

        _roundNumber = roundNumber;
        _situation = situation;

        RoundNumberText.Text = $"ROUND {roundNumber} - TACTICAL DECISION REQUIRED";
        DisplaySituationReport();
        BuildFrameTabs();
    }

    private void DisplaySituationReport()
    {
        var report = $@"RANGE: {PositioningSystem.FormatRangeBand(_situation.CurrentRangeBand)}

═══════════════════════════════════
PLAYER FORCES
═══════════════════════════════════

{FormatFrameSituations(_situation.PlayerFrames)}

═══════════════════════════════════
ENEMY FORCES
═══════════════════════════════════

{FormatFrameSituations(_situation.EnemyFrames)}

═══════════════════════════════════
LAST ROUND
═══════════════════════════════════

{_situation.LastRoundSummary}

Player Losses: {_situation.PlayerLosses}
Enemy Losses: {_situation.EnemyLosses}
";
        SituationReportText.Text = report;
    }

    private string FormatFrameSituations(List<FrameSituation> frames)
    {
        var lines = new List<string>();

        foreach (var frame in frames)
        {
            string status = frame.Status;
            lines.Add($"{frame.Name} ({frame.Class}) [{status}]");

            if (frame.IsDestroyed) { lines.Add(""); continue; }

            // Per-location damage summary
            int totalArmor = frame.Armor.Values.Sum();
            int totalMaxArmor = frame.MaxArmor.Values.Sum();
            int armorPercent = totalMaxArmor > 0 ? (int)((float)totalArmor / totalMaxArmor * 100) : 0;
            var armorBar = CreateBar(armorPercent, 10);
            lines.Add($"  Armor: [{armorBar}] {totalArmor}/{totalMaxArmor}");

            // Show destroyed/critical locations
            if (frame.DestroyedLocations.Count > 0)
            {
                var locs = string.Join(", ", frame.DestroyedLocations.Select(l => DamageSystem.FormatLocation(l)));
                lines.Add($"  DESTROYED: {locs}");
            }

            // Reactor
            int stressPercent = frame.ReactorOutput > 0 ? (int)((float)frame.ReactorStress / frame.ReactorOutput * 100) : 0;
            string stressWarning = stressPercent > 100 ? " OVERLOAD" : stressPercent > 75 ? " HIGH" : "";
            lines.Add($"  Reactor: {frame.CurrentEnergy}/{frame.ReactorOutput} energy | Stress: {frame.ReactorStress}{stressWarning}");

            if (frame.IsShutDown)
                lines.Add($"  ** REACTOR SHUTDOWN **");

            // Range & AP
            lines.Add($"  Range: {PositioningSystem.FormatRangeBand(frame.CurrentRange)} | AP: {frame.ActionPoints}");

            // Weapon groups
            foreach (var (groupId, weapons) in frame.WeaponGroups)
            {
                var weaponNames = string.Join("+", weapons.Where(w => !w.IsDestroyed).Select(w => w.Name));
                int groupEnergy = weapons.Where(w => !w.IsDestroyed).Sum(w => w.EnergyCost);
                if (weaponNames.Length > 0)
                    lines.Add($"  G{groupId}: {weaponNames} ({groupEnergy}E)");
            }

            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    private string CreateBar(int percent, int length)
    {
        percent = Math.Clamp(percent, 0, 100);
        var filled = (int)(percent / 100.0 * length);
        var empty = length - filled;
        return new string('#', filled) + new string('-', empty);
    }

    private void BuildFrameTabs()
    {
        FrameTabsPanel.Children.Clear();

        foreach (var frame in _situation.PlayerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
        {
            _framePlans[frame.InstanceId] = new List<PlannedAction>();
            _frameFocusTargets[frame.InstanceId] = null;

            var btn = new Button
            {
                Content = $" {frame.Name} ({frame.Class}) ",
                Tag = frame.InstanceId,
                Style = (Style)FindResource("TerminalButton"),
                Margin = new Thickness(0, 0, 5, 0)
            };
            btn.Click += FrameTab_Click;
            FrameTabsPanel.Children.Add(btn);
        }

        // Select first frame by default
        var firstFrame = _situation.PlayerFrames.FirstOrDefault(f => !f.IsDestroyed && !f.IsShutDown);
        if (firstFrame != null)
        {
            _selectedFrameId = firstFrame.InstanceId;
            ShowFrameActionPlanner(firstFrame);
        }
    }

    private void FrameTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int frameId)
        {
            _selectedFrameId = frameId;
            var frame = _situation.PlayerFrames.First(f => f.InstanceId == frameId);
            ShowFrameActionPlanner(frame);
        }
    }

    private void ShowFrameActionPlanner(FrameSituation frame)
    {
        ActionPlannerPanel.Children.Clear();

        // Frame header
        AddText($"=== {frame.Name} ({frame.Class}) ===", "#FF00FF00", 13, FontWeights.Bold);
        AddText($"Reactor: {frame.CurrentEnergy}/{frame.ReactorOutput} | Move Cost: {frame.MovementEnergyCost}E", "#FF00AA00", 11);
        AddText($"AP: {frame.ActionPoints} | Range: {PositioningSystem.FormatRangeBand(frame.CurrentRange)}", "#FF00AA00", 11);
        AddText("", "#FF00AA00", 8);

        // Focus target selection
        AddText("Focus Target:", "#FF00AA00", 11);
        var focusCombo = new ComboBox
        {
            Background = Brushes.Black,
            Foreground = Brushes.LimeGreen,
            BorderBrush = Brushes.LimeGreen,
            Tag = "FocusTarget",
            Margin = new Thickness(0, 2, 0, 10)
        };
        focusCombo.Items.Add("(Auto - AI picks target)");
        foreach (var enemy in _situation.EnemyFrames.Where(e => !e.IsDestroyed))
        {
            int eArmor = enemy.Armor.Values.Sum();
            int eMaxArmor = enemy.MaxArmor.Values.Sum();
            focusCombo.Items.Add($"{enemy.Name} ({eArmor}/{eMaxArmor} armor)");
        }
        focusCombo.SelectedIndex = 0;
        if (_frameFocusTargets.TryGetValue(frame.InstanceId, out var existingTarget) && existingTarget.HasValue)
        {
            var idx = _situation.EnemyFrames.Where(e => !e.IsDestroyed).ToList()
                .FindIndex(e => e.InstanceId == existingTarget.Value);
            if (idx >= 0) focusCombo.SelectedIndex = idx + 1;
        }
        focusCombo.SelectionChanged += (s, e) =>
        {
            if (focusCombo.SelectedIndex <= 0)
                _frameFocusTargets[frame.InstanceId] = null;
            else
            {
                var enemies = _situation.EnemyFrames.Where(en => !en.IsDestroyed).ToList();
                if (focusCombo.SelectedIndex - 1 < enemies.Count)
                    _frameFocusTargets[frame.InstanceId] = enemies[focusCombo.SelectedIndex - 1].InstanceId;
            }
        };
        ActionPlannerPanel.Children.Add(focusCombo);

        // Action slots
        AddText("ACTIONS (2 AP available):", "#FF00FF00", 12, FontWeights.Bold);
        AddText("Each action costs AP. Plan up to 2 AP of actions.", "#FF008800", 10);
        AddText("", "#FF00AA00", 5);

        var existingPlan = _framePlans.GetValueOrDefault(frame.InstanceId) ?? new List<PlannedAction>();

        // Action Slot 1
        AddText("Action Slot 1 (1 AP):", "#FF00AA00", 11);
        var action1Combo = CreateActionCombo(frame, 1);
        if (existingPlan.Count > 0) SelectActionInCombo(action1Combo, existingPlan[0]);
        ActionPlannerPanel.Children.Add(action1Combo);

        // Sub-options for action 1
        var subPanel1 = new StackPanel { Tag = "Sub1", Margin = new Thickness(15, 2, 0, 10) };
        ActionPlannerPanel.Children.Add(subPanel1);
        action1Combo.SelectionChanged += (s, e) => UpdateSubOptions(subPanel1, action1Combo, frame);
        UpdateSubOptions(subPanel1, action1Combo, frame);

        // Action Slot 2 (1 AP or nothing if slot 1 used 2 AP)
        AddText("Action Slot 2 (1 AP):", "#FF00AA00", 11);
        var action2Combo = CreateActionCombo(frame, 2);
        if (existingPlan.Count > 1) SelectActionInCombo(action2Combo, existingPlan[1]);
        ActionPlannerPanel.Children.Add(action2Combo);

        var subPanel2 = new StackPanel { Tag = "Sub2", Margin = new Thickness(15, 2, 0, 10) };
        ActionPlannerPanel.Children.Add(subPanel2);
        action2Combo.SelectionChanged += (s, e) => UpdateSubOptions(subPanel2, action2Combo, frame);
        UpdateSubOptions(subPanel2, action2Combo, frame);

        // Weapon groups reference
        AddText("", "#FF00AA00", 5);
        AddText("WEAPON GROUPS:", "#FF00FF00", 12, FontWeights.Bold);
        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            foreach (var w in weapons)
            {
                string ammoInfo = w.AmmoPerShot > 0 ? $", {w.AmmoPerShot} ammo/shot" : "";
                string destroyedTag = w.IsDestroyed ? " [DESTROYED]" : "";
                AddText($"  G{groupId}: {w.Name} - {w.Damage}dmg {w.RangeClass} ({w.EnergyCost}E{ammoInfo}){destroyedTag}",
                    w.IsDestroyed ? "#FF880000" : "#FF00AA00", 10);
            }
        }

        // Ammo status
        if (frame.AmmoByType.Any())
        {
            AddText("", "#FF00AA00", 5);
            AddText("AMMO:", "#FF00FF00", 11);
            foreach (var (type, count) in frame.AmmoByType)
            {
                AddText($"  {type}: {count} rounds", "#FF00AA00", 10);
            }
        }

        UpdateStatusBar();
    }

    private ComboBox CreateActionCombo(FrameSituation frame, int slot)
    {
        var combo = new ComboBox
        {
            Background = Brushes.Black,
            Foreground = Brushes.LimeGreen,
            BorderBrush = Brushes.LimeGreen,
            Tag = $"Action{slot}",
            Margin = new Thickness(0, 2, 0, 0)
        };

        combo.Items.Add("(No Action)");
        combo.Items.Add($"Move (1 AP, {frame.MovementEnergyCost}E)");
        combo.Items.Add("Fire Weapon Group (1 AP)");
        combo.Items.Add("Brace (1 AP, +20 defense)");
        combo.Items.Add("Overwatch (1 AP, interrupt fire)");
        combo.Items.Add("Vent Reactor (1 AP, reduce stress)");
        combo.Items.Add($"Called Shot (2 AP, target location)");
        combo.Items.Add($"Sprint (2 AP, {frame.MovementEnergyCost * 2}E, move 2 bands)");

        combo.SelectedIndex = 0;
        return combo;
    }

    private void SelectActionInCombo(ComboBox combo, PlannedAction action)
    {
        int idx = action.Action switch
        {
            CombatAction.Move => 1,
            CombatAction.FireGroup => 2,
            CombatAction.Brace => 3,
            CombatAction.Overwatch => 4,
            CombatAction.VentReactor => 5,
            CombatAction.CalledShot => 6,
            CombatAction.Sprint => 7,
            _ => 0
        };
        if (idx < combo.Items.Count) combo.SelectedIndex = idx;
    }

    private void UpdateSubOptions(StackPanel subPanel, ComboBox actionCombo, FrameSituation frame)
    {
        subPanel.Children.Clear();

        if (actionCombo.SelectedIndex <= 0) return;

        var action = GetActionFromIndex(actionCombo.SelectedIndex);

        switch (action)
        {
            case CombatAction.Move:
            case CombatAction.Sprint:
                var dirCombo = new ComboBox
                {
                    Background = Brushes.Black,
                    Foreground = Brushes.LimeGreen,
                    BorderBrush = Brushes.LimeGreen,
                    Tag = "Direction"
                };
                dirCombo.Items.Add("Close (toward enemy)");
                dirCombo.Items.Add("Pull Back (away from enemy)");
                dirCombo.SelectedIndex = 0;
                var dirLabel = new TextBlock { Text = "Direction:", Foreground = Brushes.LimeGreen, FontSize = 10 };
                subPanel.Children.Add(dirLabel);
                subPanel.Children.Add(dirCombo);
                break;

            case CombatAction.FireGroup:
                var groupCombo = new ComboBox
                {
                    Background = Brushes.Black,
                    Foreground = Brushes.LimeGreen,
                    BorderBrush = Brushes.LimeGreen,
                    Tag = "WeaponGroup"
                };
                foreach (var (groupId, weapons) in frame.WeaponGroups)
                {
                    var activeWeapons = weapons.Where(w => !w.IsDestroyed).ToList();
                    if (activeWeapons.Count == 0) continue;
                    var names = string.Join("+", activeWeapons.Select(w => w.Name));
                    int totalEnergy = activeWeapons.Sum(w => w.EnergyCost);
                    int totalDmg = activeWeapons.Sum(w => w.Damage);
                    groupCombo.Items.Add($"G{groupId}: {names} ({totalDmg}dmg, {totalEnergy}E)");
                }
                if (groupCombo.Items.Count == 0)
                    groupCombo.Items.Add("(No weapons available)");
                groupCombo.SelectedIndex = 0;
                var grpLabel = new TextBlock { Text = "Weapon Group:", Foreground = Brushes.LimeGreen, FontSize = 10 };
                subPanel.Children.Add(grpLabel);
                subPanel.Children.Add(groupCombo);
                break;

            case CombatAction.CalledShot:
                // Weapon group selection
                var csGroupCombo = new ComboBox
                {
                    Background = Brushes.Black,
                    Foreground = Brushes.LimeGreen,
                    BorderBrush = Brushes.LimeGreen,
                    Tag = "WeaponGroup"
                };
                foreach (var (groupId, weapons) in frame.WeaponGroups)
                {
                    var activeWeapons = weapons.Where(w => !w.IsDestroyed).ToList();
                    if (activeWeapons.Count == 0) continue;
                    var names = string.Join("+", activeWeapons.Select(w => w.Name));
                    csGroupCombo.Items.Add($"G{groupId}: {names}");
                }
                if (csGroupCombo.Items.Count == 0)
                    csGroupCombo.Items.Add("(No weapons available)");
                csGroupCombo.SelectedIndex = 0;

                // Location selection
                var locCombo = new ComboBox
                {
                    Background = Brushes.Black,
                    Foreground = Brushes.LimeGreen,
                    BorderBrush = Brushes.LimeGreen,
                    Tag = "CalledLocation"
                };
                foreach (HitLocation loc in Enum.GetValues<HitLocation>())
                {
                    locCombo.Items.Add(DamageSystem.FormatLocation(loc));
                }
                locCombo.SelectedIndex = 0;

                subPanel.Children.Add(new TextBlock { Text = "Weapon Group:", Foreground = Brushes.LimeGreen, FontSize = 10 });
                subPanel.Children.Add(csGroupCombo);
                subPanel.Children.Add(new TextBlock { Text = "Target Location:", Foreground = Brushes.LimeGreen, FontSize = 10, Margin = new Thickness(0, 5, 0, 0) });
                subPanel.Children.Add(locCombo);
                break;
        }
    }

    private CombatAction GetActionFromIndex(int index)
    {
        return index switch
        {
            1 => CombatAction.Move,
            2 => CombatAction.FireGroup,
            3 => CombatAction.Brace,
            4 => CombatAction.Overwatch,
            5 => CombatAction.VentReactor,
            6 => CombatAction.CalledShot,
            7 => CombatAction.Sprint,
            _ => CombatAction.Move
        };
    }

    private PlannedAction? BuildPlannedActionFromSlot(StackPanel actionPanel, ComboBox actionCombo, FrameSituation frame)
    {
        if (actionCombo.SelectedIndex <= 0) return null;

        var action = GetActionFromIndex(actionCombo.SelectedIndex);
        var planned = new PlannedAction { Action = action };

        switch (action)
        {
            case CombatAction.Move:
            case CombatAction.Sprint:
                var dirCombo = FindChildByTag<ComboBox>(actionPanel, "Direction");
                if (dirCombo != null)
                {
                    planned.MoveDirection = dirCombo.SelectedIndex == 0
                        ? MovementDirection.Close
                        : MovementDirection.PullBack;
                }
                break;

            case CombatAction.FireGroup:
                var groupCombo = FindChildByTag<ComboBox>(actionPanel, "WeaponGroup");
                if (groupCombo != null && groupCombo.SelectedItem is string groupText)
                {
                    // Extract group ID from "G1: ..." format
                    if (groupText.StartsWith("G") && groupText.Length > 1)
                    {
                        var numEnd = groupText.IndexOf(':');
                        if (numEnd > 1 && int.TryParse(groupText.Substring(1, numEnd - 1), out int gid))
                            planned.WeaponGroupId = gid;
                    }
                }
                break;

            case CombatAction.CalledShot:
                var csGroupCombo = FindChildByTag<ComboBox>(actionPanel, "WeaponGroup");
                if (csGroupCombo != null && csGroupCombo.SelectedItem is string csGroupText)
                {
                    if (csGroupText.StartsWith("G") && csGroupText.Length > 1)
                    {
                        var numEnd = csGroupText.IndexOf(':');
                        if (numEnd > 1 && int.TryParse(csGroupText.Substring(1, numEnd - 1), out int gid))
                            planned.WeaponGroupId = gid;
                    }
                }
                var locCombo = FindChildByTag<ComboBox>(actionPanel, "CalledLocation");
                if (locCombo != null)
                {
                    planned.CalledShotLocation = (HitLocation)locCombo.SelectedIndex;
                }
                break;
        }

        return planned;
    }

    private T? FindChildByTag<T>(StackPanel panel, string tag) where T : FrameworkElement
    {
        foreach (var child in panel.Children)
        {
            if (child is T element && element.Tag is string t && t == tag)
                return element;
        }
        return null;
    }

    private void AddText(string text, string colorHex, double fontSize, FontWeight? weight = null)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap
        };
        ActionPlannerPanel.Children.Add(tb);
    }

    private void UpdateStatusBar()
    {
        int planned = _framePlans.Count(kv => kv.Value.Count > 0);
        int total = _situation.PlayerFrames.Count(f => !f.IsDestroyed && !f.IsShutDown);
        StatusBarText.Text = $"Orders planned for {planned}/{total} frames. Select a frame tab to plan actions.";
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        Decision = new RoundTacticalDecision();
        Decision.AttemptWithdrawal = AttemptWithdrawalCheckBox.IsChecked == true;
        AutoResolveRemaining = AutoRemainingCheckBox.IsChecked == true;

        // Build decisions from the current UI state for the currently selected frame
        SaveCurrentFramePlan();

        // Build frame orders from stored plans
        foreach (var (frameId, actions) in _framePlans)
        {
            if (actions.Count == 0) continue;

            Decision.FrameOrders[frameId] = new FrameActions
            {
                Actions = new List<PlannedAction>(actions),
                FocusTargetId = _frameFocusTargets.GetValueOrDefault(frameId)
            };
        }

        UseAI = false;
        DialogResult = true;
        Close();
    }

    private void SaveCurrentFramePlan()
    {
        if (_selectedFrameId < 0) return;

        var frame = _situation.PlayerFrames.FirstOrDefault(f => f.InstanceId == _selectedFrameId);
        if (frame == null) return;

        var actions = new List<PlannedAction>();

        // Find Action1 and Action2 combos in the panel
        var allCombos = new List<(ComboBox combo, StackPanel sub)>();
        ComboBox? currentCombo = null;

        foreach (var child in ActionPlannerPanel.Children)
        {
            if (child is ComboBox cb && cb.Tag is string tag)
            {
                if (tag == "Action1" || tag == "Action2")
                    currentCombo = cb;
                else if (tag == "FocusTarget")
                {
                    // Handle focus target
                    if (cb.SelectedIndex > 0)
                    {
                        var enemies = _situation.EnemyFrames.Where(en => !en.IsDestroyed).ToList();
                        if (cb.SelectedIndex - 1 < enemies.Count)
                            _frameFocusTargets[_selectedFrameId] = enemies[cb.SelectedIndex - 1].InstanceId;
                    }
                    else
                    {
                        _frameFocusTargets[_selectedFrameId] = null;
                    }
                }
            }
            else if (child is StackPanel sp && currentCombo != null)
            {
                var planned = BuildPlannedActionFromSlot(sp, currentCombo, frame);
                if (planned != null) actions.Add(planned);
                currentCombo = null;
            }
        }

        _framePlans[_selectedFrameId] = actions;
    }

    private void AutoButton_Click(object sender, RoutedEventArgs e)
    {
        UseAI = true;
        AutoResolveRemaining = AutoRemainingCheckBox.IsChecked == true;
        DialogResult = true;
        Close();
    }
}
