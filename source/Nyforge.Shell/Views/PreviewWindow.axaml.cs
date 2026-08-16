using Avalonia.Controls;
using Avalonia.Interactivity;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class PreviewWindow : Window
{
    private PreviewViewModel? Vm => DataContext as PreviewViewModel;

    public PreviewWindow()
    {
        InitializeComponent();
    }

    private void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: PreviewElementViewModel element } && Vm is not null)
        {
            Vm.FireEvent(element, "clicked");
        }
    }

    private void OnToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: PreviewElementViewModel element } checkbox || Vm is null) return;

        var value = checkbox.IsChecked ?? false;
        element.BoolValue = value;
        Vm.OnPropertyInteraction(element, "value", value);
        Vm.FireEvent(element, "changed");
    }
}
