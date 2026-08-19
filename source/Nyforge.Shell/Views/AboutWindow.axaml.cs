using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nyforge.Shell.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently ignore — URL opening is best-effort.
        }
    }

    private void OnGitHub(object? sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Myco-mycelium/Nyforge");

    private void OnDocumentation(object? sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Myco-mycelium/Nythera/tree/main/docs");

    private void OnLicense(object? sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Myco-mycelium/Nyforge/blob/main/LICENSE");
}
