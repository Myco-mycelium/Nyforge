using Nyforge.Core.Nui;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The extended Shell vocabulary — AppGrid, Clock, Dock, TitleBar
/// (NUI-SCHEMA §2 component table, doc #15 "desktop-specific
/// primitives"): the registry carries typed semantic contracts, not
/// generic rectangles, and the Inspector's property definitions follow.
/// </summary>
public class ShellComponentsTests
{
    [Theory]
    [InlineData("AppGrid")]
    [InlineData("Clock")]
    [InlineData("Dock")]
    [InlineData("TitleBar")]
    public void Shell_components_have_registry_contracts(string type)
    {
        Assert.True(ComponentContracts.TryGet(type, out var contract));
        Assert.Equal("Shell", contract!.Category);
        Assert.NotEmpty(contract.Properties);
    }

    [Fact]
    public void Dock_carries_a_semantic_contract()
    {
        Assert.True(ComponentContracts.TryGet("Dock", out var dock));
        Assert.Equal(new[] { "position", "pinnedApps", "runningApps", "autoHide", "iconSize", "magnify" }, dock!.Properties);
        Assert.Equal(new[] { "appClicked" }, dock.Events);
        Assert.Equal(new[] { "Launch" }, dock.Actions);
    }

    [Fact]
    public void AppGrid_launches_apps()
    {
        Assert.True(ComponentContracts.TryGet("AppGrid", out var grid));
        Assert.Equal(new[] { "apps", "columns", "iconSize" }, grid!.Properties);
        Assert.Equal(new[] { "appClicked" }, grid.Events);
        Assert.Equal(new[] { "Launch" }, grid.Actions);
    }

    [Fact]
    public void Clock_and_TitleBar_are_leaf_components()
    {
        Assert.True(ComponentContracts.TryGet("Clock", out var clock));
        Assert.Empty(clock!.Events);
        Assert.Empty(clock.Actions);
        Assert.True(ComponentContracts.TryGet("TitleBar", out var titlebar));
        Assert.Equal(new[] { "doubleClicked" }, titlebar!.Events);
        Assert.Empty(titlebar.Actions);
    }

    [Fact]
    public void Inspector_definitions_cover_the_new_vocabulary()
    {
        var dock = PropertyDefinitions.For("Dock");
        Assert.Contains(dock, d => d.Name == "position" && d.Type == "enum");
        Assert.Contains(dock, d => d.Name == "autoHide" && d.Type == "boolean");
        Assert.Contains(dock, d => d.Name == "iconSize" && d.Units == "px");
        var clock = PropertyDefinitions.For("Clock");
        Assert.Contains(clock, d => d.Name == "format" && d.Type == "enum");
        Assert.Contains(clock, d => d.Name == "showSeconds" && d.Type == "boolean");
        var grid = PropertyDefinitions.For("AppGrid");
        Assert.Contains(grid, d => d.Name == "columns" && d.Type == "number");
    }
}
