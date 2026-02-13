using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.UI;

public partial class PostCombatWindow : Window
{
    private readonly MissionResults _results;
    private readonly HashSet<int> _selectedSalvageIndices = new();

    public PostCombatWindow(MissionResults results, Mission? mission, DatabaseContext dbContext)
    {
        InitializeComponent();
        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));
        _results = results;
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
        DialogResult = true;
        Close();
    }
}
