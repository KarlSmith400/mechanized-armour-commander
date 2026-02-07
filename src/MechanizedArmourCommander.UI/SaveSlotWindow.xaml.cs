using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MechanizedArmourCommander.UI;

public partial class SaveSlotWindow : Window
{
    private readonly SaveSlotMode _mode;
    private List<SaveSlotInfo> _slots;
    private int? _selectedSlotNumber;

    public int SelectedSlot { get; private set; }
    public string CompanyName { get; private set; } = "Iron Wolves";

    public SaveSlotWindow(SaveSlotMode mode, List<SaveSlotInfo> slots)
    {
        InitializeComponent();
        _mode = mode;
        _slots = slots;

        HeaderText.Text = mode == SaveSlotMode.NewGame
            ? "NEW GAME - SELECT SLOT"
            : "LOAD GAME - SELECT SLOT";

        if (mode == SaveSlotMode.NewGame)
            CompanyNamePanel.Visibility = Visibility.Visible;

        BuildSlotDisplay();
    }

    private void BuildSlotDisplay()
    {
        SlotPanel.Children.Clear();

        foreach (var slot in _slots)
        {
            bool isSelected = _selectedSlotNumber == slot.SlotNumber;
            bool isClickable = _mode == SaveSlotMode.NewGame || slot.IsOccupied;

            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isSelected ? "#00FF00" : (isClickable ? "#004400" : "#1A1A1A"))),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isSelected ? "#001A00" : "#0A0A0A")),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = isClickable ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow,
                Tag = slot.SlotNumber
            };

            if (isClickable)
                border.MouseLeftButtonDown += Slot_Click;

            var content = new DockPanel();

            // Slot info text
            var infoPanel = new StackPanel();

            if (slot.IsOccupied)
            {
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"SLOT {slot.SlotNumber}: {slot.CompanyName}",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                        isClickable ? "#00FF00" : "#555555")),
                    FontFamily = new FontFamily("Consolas")
                });
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"Day {slot.CurrentDay}  |  ${slot.Credits:N0}  |  {slot.MissionsCompleted} Missions",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA00")),
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }
            else
            {
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"SLOT {slot.SlotNumber}: ---- EMPTY ----",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                        _mode == SaveSlotMode.NewGame ? "#006600" : "#333333")),
                    FontFamily = new FontFamily("Consolas")
                });
            }

            DockPanel.SetDock(infoPanel, Dock.Left);
            content.Children.Add(infoPanel);

            // Delete button for occupied slots
            if (slot.IsOccupied)
            {
                var deleteBtn = new Button
                {
                    Content = "DELETE",
                    Tag = slot.SlotNumber,
                    Style = null,
                    Width = 70,
                    Height = 24,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#330000")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3333")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#660000")),
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                deleteBtn.Click += Delete_Click;
                DockPanel.SetDock(deleteBtn, Dock.Right);
                content.Children.Add(deleteBtn);
            }

            border.Child = content;
            SlotPanel.Children.Add(border);
        }
    }

    private void Slot_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int slotNumber)
        {
            _selectedSlotNumber = slotNumber;
            ConfirmButton.IsEnabled = true;
            BuildSlotDisplay();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int slotNumber)
        {
            var slot = _slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            if (slot == null || !slot.IsOccupied) return;

            var result = MessageBox.Show(
                $"Delete save slot {slotNumber} ({slot.CompanyName})?\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (File.Exists(slot.FilePath))
                    File.Delete(slot.FilePath);

                // Refresh slot data
                slot.IsOccupied = false;
                slot.CompanyName = null;
                slot.CurrentDay = 0;
                slot.Credits = 0;
                slot.MissionsCompleted = 0;

                if (_selectedSlotNumber == slotNumber)
                {
                    if (_mode == SaveSlotMode.LoadGame)
                    {
                        _selectedSlotNumber = null;
                        ConfirmButton.IsEnabled = false;
                    }
                }

                BuildSlotDisplay();
            }
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSlotNumber == null) return;

        var slot = _slots.FirstOrDefault(s => s.SlotNumber == _selectedSlotNumber);
        if (slot == null) return;

        // For NewGame on occupied slot, confirm overwrite
        if (_mode == SaveSlotMode.NewGame && slot.IsOccupied)
        {
            var result = MessageBox.Show(
                $"Overwrite save slot {slot.SlotNumber} ({slot.CompanyName})?\nAll progress will be lost.",
                "Confirm Overwrite",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
        }

        // Validate company name for new game
        if (_mode == SaveSlotMode.NewGame)
        {
            string name = CompanyNameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a company name.", "Invalid Name",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CompanyName = name;
        }

        SelectedSlot = _selectedSlotNumber.Value;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
