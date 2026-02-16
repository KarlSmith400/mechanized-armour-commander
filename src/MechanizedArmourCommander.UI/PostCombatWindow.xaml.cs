using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.UI;

public partial class PostCombatWindow : Window
{
    private readonly MissionResults _results;
    private readonly Mission? _mission;
    private readonly MissionService? _missionService;
    private readonly HashSet<int> _selectedSalvageIndices = new();
    private bool _scavengePhaseComplete;

    public PostCombatWindow(MissionResults results, Mission? mission, DatabaseContext dbContext)
    {
        InitializeComponent();
        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));
        _results = results;
        _mission = mission;
        _missionService = new MissionService(dbContext);
        DisplayResults(results, mission, dbContext);
    }

    private void DisplayResults(MissionResults results, Mission? mission, DatabaseContext dbContext)
    {
        // Outcome header
        string outcomeText = results.Outcome switch
        {
            CombatResult.Victory => "VICTORY",
            CombatResult.Defeat => "DEFEAT",
            CombatResult.Withdrawal => "WITHDRAWAL",
            _ => "COMBAT COMPLETE"
        };
        string outcomeColor = results.Outcome switch
        {
            CombatResult.Victory => "#00FF00",
            CombatResult.Defeat => "#FF3333",
            _ => "#FFAA00"
        };
        OutcomeHeader.Text = outcomeText;
        OutcomeHeader.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(outcomeColor));

        if (mission != null)
        {
            AddLine($"Mission: {mission.Title}", "#00AA00", 12, true);
            AddLine("", "#000000");
        }

        // Credits
        AddLine("=== FINANCIAL REPORT ===", "#006600", 11, true);
        AddLine($"Credits Earned: ${results.CreditsEarned:N0}", "#FFAA00");
        if (results.BonusCredits > 0)
            AddLine($"Performance Bonus: ${results.BonusCredits:N0}", "#FFCC00");
        AddLine($"Total: ${results.CreditsEarned + results.BonusCredits:N0}", "#FFAA00", 11, true);
        AddLine("", "#000000");

        // Reputation
        if (results.ReputationGained != 0)
        {
            string repSign = results.ReputationGained > 0 ? "+" : "";
            string repColor = results.ReputationGained > 0 ? "#00AAFF" : "#FF6600";
            AddLine($"Reputation: {repSign}{results.ReputationGained}", repColor);
            AddLine("", "#000000");
        }

        // Faction standing changes
        if (results.FactionStandingChanges.Any())
        {
            AddLine("=== FACTION STANDING ===", "#006600", 11, true);
            var factionRepo = new FactionRepository(dbContext);
            foreach (var (factionId, delta) in results.FactionStandingChanges)
            {
                var faction = factionRepo.GetById(factionId);
                if (faction == null) continue;

                string sign = delta >= 0 ? "+" : "";
                string color = delta >= 0 ? faction.Color : "#FF6600";
                AddLine($"{faction.Name}: {sign}{delta}", color);
            }
            AddLine("", "#000000");
        }

        // Frame damage reports
        AddLine("=== FRAME STATUS ===", "#006600", 11, true);
        foreach (var report in results.FrameDamageReports)
        {
            if (report.IsDestroyed)
            {
                AddLine($"{report.FrameName}: DESTROYED", "#FF3333", 10, true);
            }
            else
            {
                string armorColor = report.ArmorPercentRemaining > 60 ? "#00FF00" :
                    report.ArmorPercentRemaining > 30 ? "#FFAA00" : "#FF6600";
                AddLine($"{report.FrameName}: Armor {report.ArmorPercentRemaining:F0}%  " +
                    $"Repair: ${report.RepairCost:N0} ({report.RepairDays} days)", armorColor);

                if (report.DestroyedLocations.Any())
                    AddLine($"  Destroyed: {string.Join(", ", report.DestroyedLocations)}", "#FF6600");
            }
        }
        AddLine("", "#000000");

        // Pilot reports
        if (results.PilotsKIA.Any() || results.PilotsInjured.Any() || results.PilotXPGained.Any())
        {
            AddLine("=== PILOT REPORT ===", "#006600", 11, true);

            var pilotRepo = new PilotRepository(dbContext);
            foreach (var (pilotId, xp) in results.PilotXPGained)
            {
                var pilot = pilotRepo.GetById(pilotId);
                if (pilot == null) continue;

                if (results.PilotsKIA.Contains(pilotId))
                {
                    AddLine($"\"{pilot.Callsign}\": KILLED IN ACTION", "#FF3333", 10, true);
                }
                else if (results.PilotsInjured.Contains(pilotId))
                {
                    AddLine($"\"{pilot.Callsign}\": Injured ({pilot.InjuryDays} days recovery)  +{xp} XP", "#FFAA00");
                }
                else
                {
                    AddLine($"\"{pilot.Callsign}\": +{xp} XP", "#00CC00");
                }
            }
            AddLine("", "#000000");
        }

        // Salvage selection
        if (results.SalvagePool.Any() && results.SalvageAllowance > 0)
        {
            AddLine("=== SALVAGE ===", "#006600", 11, true);
            AddLine($"Select up to {results.SalvageAllowance} item(s) from destroyed enemy wreckage:", "#FFCC00");
            AddLine("", "#000000");

            for (int i = 0; i < results.SalvagePool.Count; i++)
            {
                var item = results.SalvagePool[i];
                int capturedIndex = i;

                var salvagePanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333300")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A00")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var sContent = new StackPanel { Orientation = Orientation.Horizontal };
                sContent.Children.Add(new TextBlock
                {
                    Text = $"{item.WeaponName} ({item.HardpointSize}) - ${item.SalvageValue:N0} - from {item.SourceFrame}  ",
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var pickBtn = new Button
                {
                    Content = "TAKE",
                    Tag = capturedIndex,
                    Style = null,
                    Height = 22,
                    Width = 60,
                    Padding = new Thickness(4, 0, 4, 0),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003300")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006600")),
                    BorderThickness = new Thickness(1)
                };
                pickBtn.Click += SalvagePick_Click;
                sContent.Children.Add(pickBtn);

                salvagePanel.Child = sContent;
                ReportPanel.Children.Add(salvagePanel);
            }

            AddLine("", "#000000");
            UpdateSalvageCounter();
        }
        else if (results.Outcome == CombatResult.Defeat)
        {
            AddLine("=== SALVAGE ===", "#006600", 11, true);
            AddLine("No salvage available - mission failed.", "#555555");
        }
        else if (!results.SalvagePool.Any())
        {
            AddLine("=== SALVAGE ===", "#006600", 11, true);
            AddLine("No enemy wreckage to salvage.", "#555555");
        }

        // Frame salvage from head kills
        if (results.SalvageFrames.Any())
        {
            AddLine("", "#000000");
            AddLine("=== FRAME SALVAGE ===", "#FFAA00", 11, true);
            AddLine("Enemy frames recovered from head kills — purchase with credits:", "#CC8800");
            AddLine("", "#000000");

            for (int i = 0; i < results.SalvageFrames.Count; i++)
            {
                var frame = results.SalvageFrames[i];
                int capturedIndex = i;

                var framePanel = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#332200")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A00")),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var fContent = new StackPanel();
                fContent.Children.Add(new TextBlock
                {
                    Text = $"{frame.ChassisDesignation} {frame.ChassisName} ({frame.ChassisClass}) — from {frame.SourceFrame}",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Consolas")
                });
                fContent.Children.Add(new TextBlock
                {
                    Text = $"Armor: H:{frame.ArmorHead} CT:{frame.ArmorCenterTorso} " +
                           $"LT:{frame.ArmorLeftTorso} RT:{frame.ArmorRightTorso} " +
                           $"LA:{frame.ArmorLeftArm} RA:{frame.ArmorRightArm} L:{frame.ArmorLegs}",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AA8800")),
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas")
                });

                var buyBtn = new Button
                {
                    Content = $"PURCHASE ${frame.SalvagePrice:N0}",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#332200")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCC00")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#664400")),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 3, 0, 0),
                    Width = 200,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Tag = capturedIndex
                };
                buyBtn.Click += SalvageFramePurchase_Click;
                fContent.Children.Add(buyBtn);

                framePanel.Child = fContent;
                ReportPanel.Children.Add(framePanel);
            }
        }
    }

    private void SalvagePick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int index) return;

        if (_selectedSalvageIndices.Contains(index))
        {
            // Deselect
            _selectedSalvageIndices.Remove(index);
            btn.Content = "TAKE";
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003300"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));

            // Remove from results
            var item = _results.SalvagePool[index];
            _results.SalvagedWeaponIds.Remove(item.WeaponId);
        }
        else if (_selectedSalvageIndices.Count < _results.SalvageAllowance)
        {
            // Select
            _selectedSalvageIndices.Add(index);
            btn.Content = "TAKEN";
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#332200"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFAA00"));

            // Add to results
            var item = _results.SalvagePool[index];
            _results.SalvagedWeaponIds.Add(item.WeaponId);
        }

        UpdateSalvageCounter();
    }

    private void SalvageFramePurchase_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int index) return;

        var frame = _results.SalvageFrames[index];
        if (!_results.PurchasedSalvageFrames.Contains(frame))
        {
            _results.PurchasedSalvageFrames.Add(frame);
            btn.Content = "PURCHASED";
            btn.IsEnabled = false;
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222200"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#887700"));
        }
    }

    private void UpdateSalvageCounter()
    {
        ContinueButton.Content = _selectedSalvageIndices.Count > 0
            ? $"CONTINUE ({_selectedSalvageIndices.Count}/{_results.SalvageAllowance} salvaged)"
            : "CONTINUE";
    }

    private void AddLine(string text, string colorHex, int fontSize = 10, bool bold = false)
    {
        ReportPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 1, 0, 1),
            TextWrapping = TextWrapping.Wrap
        });
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        // Two-phase: first click runs scavenge rolls, second click closes
        if (!_scavengePhaseComplete && _mission != null && _missionService != null
            && _results.SalvagePool.Any())
        {
            _scavengePhaseComplete = true;
            _missionService.ProcessScavengeAndBonus(_results, _mission);
            DisplayScavengeResults();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void DisplayScavengeResults()
    {
        var scavenged = _results.ScavengedItems;
        var bonus = _results.BonusLootItems;

        if (!scavenged.Any() && !bonus.Any())
        {
            AddLine("", "#000000");
            AddLine("=== SCAVENGE RESULTS ===", "#FFAA00", 11, true);
            AddLine("Your salvage crew found nothing additional in the wreckage.", "#555555");
        }
        else
        {
            AddLine("", "#000000");
            AddLine("=== SCAVENGE RESULTS ===", "#FFAA00", 11, true);
            AddLine("Your salvage crew recovered additional items from the wreckage:", "#CC8800");
            AddLine("", "#000000");

            foreach (var item in scavenged)
            {
                AddLine($"[Scavenged] {item.WeaponName} ({item.HardpointSize}) — ${item.SalvageValue:N0} — from {item.SourceFrame}",
                    "#CCAA00");
            }

            foreach (var item in bonus)
            {
                AddLine($"[Bonus Find] {item.WeaponName} ({item.HardpointSize}) — ${item.SalvageValue:N0} — lucky find from {item.SourceFrame}",
                    "#00CCFF");
            }

            int totalItems = scavenged.Count + bonus.Count;
            int totalValue = scavenged.Sum(s => s.SalvageValue) + bonus.Sum(b => b.SalvageValue);
            AddLine("", "#000000");
            AddLine($"Total recovered: {totalItems} item(s) (${totalValue:N0} value)", "#FFCC00", 10, true);
        }

        ContinueButton.Content = "CONFIRM & EXIT";
    }
}
