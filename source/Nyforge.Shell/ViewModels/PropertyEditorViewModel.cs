using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// One row of the metadata-driven Inspector: wraps a
/// <see cref="PropertyDefinition"/> (from the Nyrqis API Registry, via
/// <see cref="PropertyDefinitions.For"/>) and the selected component's
/// live value.  The editor kind is chosen from
/// <see cref="PropertyDefinition.Type"/> (string → text box, boolean →
/// checkbox, number → numeric up-down, enum → combo box); the ViewModel
/// exposes the four Is* flags so the XAML can pick the right control.
///
/// Writes are routed through <see cref="Commit"/> so MainWindowViewModel
/// can wrap them in an undoable <c>ChangePropertyCommand</c> — Inspector
/// edits land on the same history as canvas edits.
/// </summary>
public sealed class PropertyEditorViewModel : ViewModelBase
{
    private readonly NuiComponent _component;
    private readonly PropertyDefinition _definition;
    private readonly Action<NuiComponent, string, object?, object?> _commit;

    public PropertyEditorViewModel(
        NuiComponent component,
        PropertyDefinition definition,
        Action<NuiComponent, string, object?, object?> commit)
    {
        _component = component;
        _definition = definition;
        _commit = commit;
    }

    public string Name => _definition.Name;
    public string Type => _definition.Type;

    public bool IsStringEditor => Type == "string";
    public bool IsBooleanEditor => Type == "boolean";
    public bool IsNumberEditor => Type == "number";
    public bool IsEnumEditor => Type == "enum";
    public bool IsArrayEditor => Type == "array";

    public double? Min => _definition.Min;
    public double? Max => _definition.Max;
    public string? Units => _definition.Units;
    public IReadOnlyList<string> EnumValues => _definition.EnumValues;

    /// <summary>The component's current value for this property (falling
    /// back to the registry default when unset).</summary>
    public object? Value
    {
        get
        {
            if (_component.Properties.TryGetValue(Name, out var v)) return v;
            return _definition.DefaultValue;
        }
        set
        {
            var old = Value;
            _commit(_component, Name, old, value);
        }
    }
}
