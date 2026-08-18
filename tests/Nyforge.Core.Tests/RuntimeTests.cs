using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The <see cref="INuiRuntime"/> contract — the seam between the editor
/// and the OS. <see cref="TestRuntime"/> records every call for
/// verification; a future Nyrqis runtime will implement the same
/// interface. These tests prove the interface is well-shaped and
/// TestRuntime behaves correctly.
/// </summary>
public class RuntimeTests
{
    private static NuiDocument Blank() => NyforgeProject.CreateBlank();

    // ---- TestRuntime --------------------------------------------------------

    [Fact]
    public void TestRuntime_records_events()
    {
        var rt = new TestRuntime();
        var comp = new NuiComponent { Id = "btn", Type = "Button" };

        rt.FireEvent(comp, "clicked");
        rt.FireEvent(comp, "hovered");

        Assert.Equal(2, rt.FiredEvents.Count);
        Assert.Equal(("btn", "clicked"), rt.FiredEvents[0]);
        Assert.Equal(("btn", "hovered"), rt.FiredEvents[1]);
        Assert.Equal(2, rt.Log.Count);
    }

    [Fact]
    public void TestRuntime_records_bindings()
    {
        var rt = new TestRuntime();
        rt.RuntimeStates["theme"] = "Eclipse";
        var binding = new NuiBinding { ComponentId = "toggle", Property = "value", State = "theme" };

        rt.ApplyBinding(binding);

        Assert.Single(rt.AppliedBindings);
        Assert.Contains("ApplyBinding", rt.Log[0]);
    }

    [Fact]
    public void TestRuntime_states_are_mutable()
    {
        var rt = new TestRuntime();
        rt.RuntimeStates["count"] = 0;

        rt.RuntimeStates["count"] = 42;
        Assert.Equal(42, rt.RuntimeStates["count"]);
    }

    [Fact]
    public void TestRuntime_seeds_from_document()
    {
        var doc = Blank();
        doc.States["dnd"] = true;
        doc.States["volume"] = 60;
        doc.StateScopes["persistent"] = new Dictionary<string, object?> { ["theme"] = "Eclipse" };

        var rt = new TestRuntime(doc);

        Assert.Equal(true, rt.RuntimeStates["dnd"]);
        Assert.Equal(60, rt.RuntimeStates["volume"]);
        Assert.Equal("Eclipse", rt.RuntimeStates["persistent.theme"]);
    }

    [Fact]
    public void TestRuntime_empty_log_initially()
    {
        var rt = new TestRuntime();
        Assert.Empty(rt.Log);
        Assert.Empty(rt.FiredEvents);
        Assert.Empty(rt.AppliedBindings);
    }

    // ---- INuiRuntime contract -----------------------------------------------

    [Fact]
    public void INuiRuntime_can_be_tested_through_interface()
    {
        INuiRuntime rt = new TestRuntime();
        var comp = new NuiComponent { Id = "x", Type = "Text" };

        rt.FireEvent(comp, "tap");
        rt.RuntimeStates["k"] = "v";
        rt.ApplyBinding(new NuiBinding { ComponentId = "y", Property = "text", State = "k" });

        Assert.Equal(2, rt.Log.Count);
        Assert.Equal("v", rt.RuntimeStates["k"]);
    }

    [Fact]
    public void INuiRuntime_binding_with_missing_state_is_safe()
    {
        var rt = new TestRuntime();
        var binding = new NuiBinding { ComponentId = "ghost", Property = "text", State = "nonexistent" };

        rt.ApplyBinding(binding); // should not throw

        Assert.Single(rt.AppliedBindings);
    }

    [Fact]
    public void INuiRuntime_fire_event_with_no_behavior_is_safe()
    {
        var rt = new TestRuntime();
        var comp = new NuiComponent
        {
            Id = "btn",
            Type = "Button",
            Events = { ["clicked"] = "ghost_behavior" },
        };

        rt.FireEvent(comp, "clicked"); // behavior doesn't exist — logged, not thrown

        Assert.Single(rt.FiredEvents);
        Assert.Contains("FireEvent(btn, clicked)", rt.Log[0]);
    }

    [Fact]
    public void INuiRuntime_fire_event_with_no_binding_is_safe()
    {
        var rt = new TestRuntime();
        var comp = new NuiComponent
        {
            Id = "btn",
            Type = "Button",
            Events = { ["clicked"] = null }, // no behavior bound
        };

        rt.FireEvent(comp, "clicked");

        Assert.Single(rt.FiredEvents); // TestRuntime records all calls
    }
}
