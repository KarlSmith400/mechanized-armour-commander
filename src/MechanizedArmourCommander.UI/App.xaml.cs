using System.Windows;

namespace MechanizedArmourCommander.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainMenu = new MainMenuWindow();
        mainMenu.Show();
    }
}
