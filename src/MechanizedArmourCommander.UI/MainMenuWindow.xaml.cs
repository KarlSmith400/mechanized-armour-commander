using System.ComponentModel;
using System.IO;
using System.Windows;
using MechanizedArmourCommander.Data;

namespace MechanizedArmourCommander.UI;

public partial class MainMenuWindow : Window
{
    private const int MaxSaveSlots = 5;
    private const string SaveDirectory = "saves";

    public MainMenuWindow()
    {
        InitializeComponent();
        EnsureSaveDirectory();
    }

    private void EnsureSaveDirectory()
    {
        var savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SaveDirectory);
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
    }

    private string GetSlotPath(int slotNumber)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SaveDirectory, $"save_slot_{slotNumber}.db");
    }

    private List<SaveSlotInfo> GetSlotInfos()
    {
        var infos = new List<SaveSlotInfo>();
        for (int i = 1; i <= MaxSaveSlots; i++)
        {
            string path = GetSlotPath(i);
            var state = DatabaseContext.PeekPlayerState(path);
            infos.Add(new SaveSlotInfo
            {
                SlotNumber = i,
                FilePath = path,
                IsOccupied = state != null,
                CompanyName = state?.CompanyName,
                CurrentDay = state?.CurrentDay ?? 0,
                Credits = state?.Credits ?? 0,
                MissionsCompleted = state?.MissionsCompleted ?? 0
            });
        }
        return infos;
    }

    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        var slotWindow = new SaveSlotWindow(SaveSlotMode.NewGame, GetSlotInfos());
        slotWindow.Owner = this;
        if (slotWindow.ShowDialog() == true)
        {
            int slotNumber = slotWindow.SelectedSlot;
            string companyName = slotWindow.CompanyName;
            string dbPath = GetSlotPath(slotNumber);

            // Delete existing file if overwriting
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            // Create and initialize fresh database
            using (var dbContext = new DatabaseContext(dbPath))
            {
                dbContext.Initialize(companyName);
            }

            LaunchGame(dbPath);
        }
    }

    private void LoadGame_Click(object sender, RoutedEventArgs e)
    {
        var slotInfos = GetSlotInfos();
        if (!slotInfos.Any(s => s.IsOccupied))
        {
            MessageBox.Show("No saved games found. Start a new game first.",
                "No Saves", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var slotWindow = new SaveSlotWindow(SaveSlotMode.LoadGame, slotInfos);
        slotWindow.Owner = this;
        if (slotWindow.ShowDialog() == true)
        {
            string dbPath = GetSlotPath(slotWindow.SelectedSlot);
            LaunchGame(dbPath);
        }
    }

    private void LaunchGame(string dbPath)
    {
        this.Hide();

        var mainWindow = new MainWindow(dbPath);
        mainWindow.ShowDialog();

        this.Show();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Application.Current.Shutdown();
        base.OnClosing(e);
    }
}

public class SaveSlotInfo
{
    public int SlotNumber { get; set; }
    public string FilePath { get; set; } = "";
    public bool IsOccupied { get; set; }
    public string? CompanyName { get; set; }
    public int CurrentDay { get; set; }
    public int Credits { get; set; }
    public int MissionsCompleted { get; set; }
}

public enum SaveSlotMode
{
    NewGame,
    LoadGame
}
