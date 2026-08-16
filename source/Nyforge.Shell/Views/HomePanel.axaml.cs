using Avalonia.Controls;
using Avalonia.Interactivity;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class HomePanel : UserControl
{
    private HomeViewModel? Vm => DataContext as HomeViewModel;

    public HomePanel()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PreviewElementViewModel element } && Vm is not null)
        {
            Vm.InvokeIfCommand(element.Id);
        }
    }
}
