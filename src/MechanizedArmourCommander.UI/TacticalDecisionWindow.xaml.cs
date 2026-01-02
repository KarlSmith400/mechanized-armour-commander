using System.Windows;
using System.Windows.Controls;
using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.UI;

public partial class TacticalDecisionWindow : Window
{
    public RoundTacticalDecision Decision { get; private set; } = new();
    public bool AutoResolveRemaining { get; private set; } = false;
    public bool UseAI { get; private set; } = false;

    private readonly int _roundNumber;
    private readonly RoundSituation _situation;

    public TacticalDecisionWindow(int roundNumber, RoundSituation situation)
    {
        InitializeComponent();

        _roundNumber = roundNumber;
        _situation = situation;

        RoundNumberText.Text = $"ROUND {roundNumber} - TACTICAL DECISION REQUIRED";
        DisplaySituationReport();
        PopulateFocusTargets();
    }

    private void DisplaySituationReport()
    {
        var report = $@"═══════════════════════════════════
PLAYER FORCES
═══════════════════════════════════

{FormatFrameSituations(_situation.PlayerFrames)}

═══════════════════════════════════
ENEMY FORCES
═══════════════════════════════════

{FormatFrameSituations(_situation.EnemyFrames)}

═══════════════════════════════════
LAST ROUND SUMMARY
═══════════════════════════════════

{_situation.LastRoundSummary}

═══════════════════════════════════
TACTICAL ASSESSMENT
═══════════════════════════════════

Average Distance: {_situation.AverageDistance} units
Range Band: {_situation.RangeBand}

Player Losses: {_situation.PlayerLosses} destroyed
Enemy Losses: {_situation.EnemyLosses} destroyed
";

        SituationReportText.Text = report;
    }

    private string FormatFrameSituations(List<FrameSituation> frames)
    {
        var lines = new List<string>();

        foreach (var frame in frames)
        {
            var armorPercent = (int)((float)frame.CurrentArmor / frame.MaxArmor * 100);
            var heatPercent = (int)((float)frame.CurrentHeat / frame.MaxHeat * 100);
            var ammoPercent = (int)((float)frame.CurrentAmmo / frame.MaxAmmo * 100);

            var armorBar = CreateBar(armorPercent, 10);
            var heatBar = CreateBar(heatPercent, 10);

            var status = "";
            if (frame.CurrentArmor < frame.MaxArmor * 0.25f)
                status = " ⚠ CRITICAL";
            else if (frame.CurrentArmor < frame.MaxArmor * 0.5f)
                status = " ⚠ DAMAGED";

            if (frame.IsOverheating)
                status += " 🔥 HEAT";

            lines.Add($"{frame.Name} ({frame.Class}):");
            lines.Add($"  Armor: [{armorBar}] {frame.CurrentArmor}/{frame.MaxArmor}{status}");
            lines.Add($"  Heat:  [{heatBar}] {frame.CurrentHeat}/{frame.MaxHeat}");
            lines.Add($"  Ammo:  {frame.CurrentAmmo}/{frame.MaxAmmo} ({ammoPercent}%)");
            lines.Add($"  Pos:   {frame.Position}");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    private string CreateBar(int percent, int length)
    {
        var filled = (int)(percent / 100.0 * length);
        var empty = length - filled;
        return new string('█', filled) + new string('░', empty);
    }

    private void PopulateFocusTargets()
    {
        FocusTargetComboBox.Items.Clear();
        FocusTargetComboBox.Items.Add("(None)");

        foreach (var enemy in _situation.EnemyFrames)
        {
            var item = new ComboBoxItem
            {
                Content = $"{enemy.Name} ({enemy.CurrentArmor}/{enemy.MaxArmor} HP)",
                Tag = enemy.InstanceId
            };
            FocusTargetComboBox.Items.Add(item);
        }

        FocusTargetComboBox.SelectedIndex = 0;
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        // Build decision from UI selections
        Decision = new RoundTacticalDecision();

        // Stance override
        if (StanceComboBox.SelectedItem is ComboBoxItem stanceItem && stanceItem.Content.ToString() != "(No Override)")
        {
            Decision.StanceOverride = stanceItem.Content.ToString() switch
            {
                "Aggressive" => Stance.Aggressive,
                "Balanced" => Stance.Balanced,
                "Defensive" => Stance.Defensive,
                _ => null
            };
        }

        // Target priority override
        if (TargetPriorityComboBox.SelectedItem is ComboBoxItem targetItem && targetItem.Content.ToString() != "(No Override)")
        {
            Decision.TargetPriorityOverride = targetItem.Content.ToString() switch
            {
                "Focus Fire" => TargetPriority.FocusFire,
                "Spread Damage" => TargetPriority.SpreadDamage,
                "Threat Priority" => TargetPriority.ThreatPriority,
                "Opportunity" => TargetPriority.Opportunity,
                _ => null
            };
        }

        // Focus target
        if (FocusTargetComboBox.SelectedItem is ComboBoxItem focusItem && focusItem.Tag is int targetId)
        {
            Decision.FocusTargetId = targetId;
        }

        // Withdrawal attempt
        Decision.AttemptWithdrawal = AttemptWithdrawalCheckBox.IsChecked == true;

        // Auto remaining
        AutoResolveRemaining = AutoRemainingCheckBox.IsChecked == true;

        UseAI = false;
        DialogResult = true;
        Close();
    }

    private void AutoButton_Click(object sender, RoutedEventArgs e)
    {
        UseAI = true;
        DialogResult = true;
        Close();
    }
}
