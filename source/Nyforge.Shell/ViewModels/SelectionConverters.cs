using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nyforge.Shell.ViewModels;

public sealed class SelectionBrushConverter : IValueConverter
{
    public static readonly SelectionBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Brushes.DeepSkyBlue : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SelectionThicknessConverter : IValueConverter
{
    public static readonly SelectionThicknessConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new Avalonia.Thickness(2) : new Avalonia.Thickness(1);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
