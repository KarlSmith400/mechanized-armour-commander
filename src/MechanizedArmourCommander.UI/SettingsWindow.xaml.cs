using System.Windows;

namespace MechanizedArmourCommander.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
