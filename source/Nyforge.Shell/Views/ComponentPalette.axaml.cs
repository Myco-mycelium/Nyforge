using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class ComponentPalette : UserControl
{
    public ComponentPalette()
    {
        InitializeComponent();
    }

    private void OnPaletteItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: string componentType } && DataContext is MainWindowViewModel vm)
        {
            vm.AddComponentCommand.Execute(componentType);
        }
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchBox is null) return;
        var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

        // Walk all palette items and show/hide based on search query
        foreach (var border in this.GetVisualDescendants().OfType<Border>())
        {
            if (border.Tag is string tag && border.Classes.Contains("palette-item"))
            {
                var matches = string.IsNullOrEmpty(query) ||
                              tag.Contains(query, StringComparison.OrdinalIgnoreCase);
                border.IsVisible = matches;
            }
        }

        // Show/hide category headers based on whether they have visible children
        foreach (var textBlock in this.GetVisualDescendants().OfType<TextBlock>())
        {
            if (textBlock.Classes.Contains("category-header"))
            {
                // Find the next sibling StackPanel and check if it has visible items
                var parent = textBlock.Parent as Panel;
                if (parent is not null)
                {
                    var index = parent.Children.IndexOf(textBlock);
                    if (index >= 0 && index + 1 < parent.Children.Count)
                    {
                        var nextStack = parent.Children[index + 1] as StackPanel;
                        if (nextStack is not null)
                        {
                            var hasVisible = nextStack.Children
                                .OfType<Border>()
                                .Any(b => b.IsVisible);
                            textBlock.IsVisible = hasVisible;
                        }
                    }
                }
            }
        }
    }
}
