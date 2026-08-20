using Avalonia.Controls;
using Avalonia.Input;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class CommandPalette : UserControl
{
    public CommandPalette()
    {
        InitializeComponent();
    }

    private CommandPaletteViewModel? Vm => DataContext as CommandPaletteViewModel;

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null) return;

        switch (e.Key)
        {
            case Key.Enter:
                Vm.ExecuteSelected();
                e.Handled = true;
                break;
            case Key.Escape:
                Vm.IsOpen = false;
                e.Handled = true;
                break;
            case Key.Up:
                Vm.MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                Vm.MoveSelection(1);
                e.Handled = true;
                break;
        }
    }

    private void OnOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is not null) Vm.IsOpen = false;
    }

    private void OnCommandHover(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Tag is PaletteCommand cmd && Vm is not null)
        {
            Vm.SelectedCommand = cmd;
        }
    }

    private void OnCommandClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is PaletteCommand cmd)
        {
            cmd.Execute();
            if (Vm is not null)
            {
                Vm.IsOpen = false;
                Vm.CloseRequested?.Invoke(Vm, EventArgs.Empty);
            }
        }
    }
}
