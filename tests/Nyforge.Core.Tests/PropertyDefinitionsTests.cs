using Nyforge.Core.Nui;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The metadata-driven Inspector's contract (NFS-006): every component's
/// property definitions come from the Nyrqis API Registry (generated
/// into PropertyDefinitions.cs), typed enough that the Inspector can pick
/// the right editor without hardcoding anything.
/// </summary>
public class PropertyDefinitionsTests
{
    [Fact]
    public void Unknown_type_has_no_properties()
    {
        Assert.Empty(PropertyDefinitions.For("NotARealComponent"));
    }

    [Fact]
    public void Button_definitions_come_from_the_registry()
    {
        var defs = PropertyDefinitions.For("Button");
        Assert.NotEmpty(defs);
        Assert.Contains(defs, d => d.Name == "text" && d.Type == "string");
        Assert.Contains(defs, d => d.Name == "enabled" && d.Type == "boolean");
        Assert.Contains(defs, d => d.Name == "visible" && d.Type == "boolean");
    }

    [Fact]
    public void Slider_value_is_a_bounded_number()
    {
        var value = PropertyDefinitions.For("Slider").Single(d => d.Name == "value");
        Assert.Equal("number", value.Type);
        Assert.Equal(0, value.Min);
        Assert.Equal(100, value.Max);
    }

    [Fact]
    public void Taskbar_position_is_an_enum()
    {
        var position = PropertyDefinitions.For("Taskbar").Single(d => d.Name == "position");
        Assert.Equal("enum", position.Type);
        Assert.Contains("bottom", position.EnumValues);
        Assert.Contains("top", position.EnumValues);
    }

    [Fact]
    public void MediaPlayer_position_is_a_number_not_an_enum()
    {
        // Same name, different meaning per component: the registry keeps
        // component-specific typing, so the Inspector renders a numeric
        // editor for playback position.
        var position = PropertyDefinitions.For("MediaPlayer").Single(d => d.Name == "position");
        Assert.Equal("number", position.Type);
        Assert.Equal(0, position.Min);
    }

    [Fact]
    public void Shell_vocabulary_is_typed()
    {
        var open = PropertyDefinitions.For("StartMenu").Single(d => d.Name == "open");
        Assert.Equal("boolean", open.Type);
        var clock = PropertyDefinitions.For("Taskbar").Single(d => d.Name == "showClock");
        Assert.Equal("boolean", clock.Type);
    }

    [Fact]
    public void Every_registry_property_has_a_definition()
    {
        // The generated table is the registry; every component's property
        // name that the contract tables know must resolve to metadata.
        foreach (var contract in ComponentContracts.All)
        {
            foreach (var property in contract.Properties)
            {
                var defs = PropertyDefinitions.For(contract.Type);
                Assert.Contains(defs, d => d.Name == property);
            }
        }
    }
}
