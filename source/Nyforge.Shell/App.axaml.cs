using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Nyforge.Shell.Services;
using Nyforge.Shell.ViewModels;
using Nyforge.Shell.Views;

namespace Nyforge.Shell;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var preferences = new PreferencesService();
            var themeManager = new ThemeManager(this, preferences);
            var projectService = new ProjectService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(projectService, themeManager, preferences)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
