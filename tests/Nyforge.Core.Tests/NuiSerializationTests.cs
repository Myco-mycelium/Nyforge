using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;
using Xunit;

namespace Nyforge.Core.Tests;

public class NuiSerializationTests
{
    [Fact]
    public void Blank_project_round_trips_through_json()
    {
        var doc = NyforgeProject.CreateBlank();

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        Assert.Equal(doc.Version, reloaded.Version);
        Assert.Single(reloaded.Screens);
        Assert.Equal("window_main", reloaded.Screens[0].Root.Id);
        Assert.Equal("Window", reloaded.Screens[0].Root.Type);
    }

    [Fact]
    public void Nested_children_survive_round_trip()
    {
        var doc = NyforgeProject.CreateBlank();
        var window = doc.Screens[0].Root;

        var sidebar = new NuiComponent { Id = "sidebar", Type = "Sidebar" };
        var button = new NuiComponent
        {
            Id = "btn_save",
            Type = "Button",
            Properties = { ["text"] = "Save" },
            Layout = new NuiLayout { X = 24, Y = 320, Width = 120, Height = 36 },
            Events = { ["clicked"] = null }
        };
        sidebar.Children.Add(button);
        window.Children.Add(sidebar);

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        var reloadedSidebar = reloaded.Screens[0].Root.Children.Single();
        var reloadedButton = reloadedSidebar.Children.Single();

        Assert.Equal("sidebar", reloadedSidebar.Id);
        Assert.Equal("btn_save", reloadedButton.Id);
        Assert.Equal(120, reloadedButton.Layout.Width);
        Assert.True(reloadedButton.Events.ContainsKey("clicked"));
    }

    [Fact]
    public void Incompatible_major_minor_version_throws()
    {
        var future = """
        {
          "version": "9.9.0",
          "project": { "name": "x", "id": "x" },
          "themes": { "active": "Eclipse" },
          "screens": []
        }
        """;

        Assert.Throws<NuiVersionMismatchException>(() => ProjectSerializer.Deserialize(future));
    }

    [Fact]
    public void Every_palette_component_type_has_a_contract()
    {
        // Guards NFC-001 §4.3: the palette must never offer a type absent
        // from the contract table.
        Assert.NotEmpty(ComponentContracts.All);
        Assert.True(ComponentContracts.TryGet("Button", out var contract));
        Assert.Contains("clicked", contract!.Events);
    }

    [Fact]
    public void Behavior_with_condition_round_trips_through_json()
    {
        var doc = NyforgeProject.CreateBlank();
        var window = doc.Screens[0].Root;

        var toggle = new NuiComponent
        {
            Id = "toggle_eclipse",
            Type = "Toggle",
            Events = { ["changed"] = "behavior_toggle_theme" }
        };
        window.Children.Add(toggle);

        doc.States["theme"] = "Eclipse";
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "behavior_toggle_theme",
            Condition = new NuiCondition { State = "theme", Operator = "equals", Value = "Eclipse" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Theme.Set",
                Arguments = { ["theme"] = "Solar" }
            }
        });

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        var behavior = Assert.Single(reloaded.Behaviors);
        Assert.Equal("behavior_toggle_theme", behavior.Id);
        Assert.NotNull(behavior.Condition);
        Assert.Equal("theme", behavior.Condition!.State);
        Assert.Equal("Nyrqis.Theme.Set", behavior.Action.Name);
        Assert.Equal("System", behavior.Action.Target);

        var reloadedToggle = reloaded.Screens[0].Root.Children.Single();
        Assert.Equal("behavior_toggle_theme", reloadedToggle.Events["changed"]);
    }

    [Fact]
    public void Behavior_without_condition_serializes_null_condition()
    {
        var behavior = new NuiBehavior
        {
            Id = "behavior_commit_settings",
            Condition = null,
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" }
        };

        var doc = NyforgeProject.CreateBlank();
        doc.Behaviors.Add(behavior);

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        Assert.Null(reloaded.Behaviors.Single().Condition);
    }

    [Fact]
    public void System_action_names_are_a_closed_set()
    {
        // Guards the anti-drift rule extended to system actions in
        // NFS-002 — a behavior's DO clause targeting "System" must name
        // something in this table, not an arbitrary string.
        Assert.True(NuiSystemActions.TryGet("Nyrqis.Theme.Set", out var contract));
        Assert.Contains("theme", contract!.ArgumentNames);
        Assert.False(NuiSystemActions.TryGet("Nyrqis.Nonexistent.Action", out _));
    }

    [Fact]
    public void Binding_round_trips_through_json()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.Bindings.Add(new NuiBinding
        {
            ComponentId = "toggle_eclipse",
            Property = "value",
            State = "useDarkTheme"
        });

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        var binding = Assert.Single(reloaded.Bindings);
        Assert.Equal("toggle_eclipse", binding.ComponentId);
        Assert.Equal("value", binding.Property);
        Assert.Equal("useDarkTheme", binding.State);
    }

    [Fact]
    public void BehaviorEvaluator_with_no_condition_always_runs()
    {
        Assert.True(BehaviorEvaluator.Evaluate(null, new Dictionary<string, object?>()));
    }

    [Fact]
    public void BehaviorEvaluator_equals_matches_when_state_matches()
    {
        var condition = new NuiCondition { State = "theme", Operator = "equals", Value = "Eclipse" };
        var states = new Dictionary<string, object?> { ["theme"] = "Eclipse" };

        Assert.True(BehaviorEvaluator.Evaluate(condition, states));
    }

    [Fact]
    public void BehaviorEvaluator_equals_fails_when_state_differs()
    {
        var condition = new NuiCondition { State = "theme", Operator = "equals", Value = "Eclipse" };
        var states = new Dictionary<string, object?> { ["theme"] = "Solar" };

        Assert.False(BehaviorEvaluator.Evaluate(condition, states));
    }

    [Fact]
    public void BehaviorEvaluator_notEquals_inverts_the_check()
    {
        var condition = new NuiCondition { State = "theme", Operator = "notEquals", Value = "Eclipse" };
        var states = new Dictionary<string, object?> { ["theme"] = "Solar" };

        Assert.True(BehaviorEvaluator.Evaluate(condition, states));
    }

    [Fact]
    public void BehaviorEvaluator_missing_state_does_not_match_a_non_null_expectation()
    {
        var condition = new NuiCondition { State = "missing", Operator = "equals", Value = "Eclipse" };
        Assert.False(BehaviorEvaluator.Evaluate(condition, new Dictionary<string, object?>()));
    }

    [Fact]
    public void Object_typed_values_deserialize_to_native_clr_types_not_JsonElement()
    {
        // Regression test: without ObjectToInferredTypesConverter,
        // System.Text.Json boxes object?-typed values (Properties,
        // Arguments, States, Condition.Value) as JsonElement on
        // deserialization, which silently breaks any `is bool`/`is string`
        // pattern match downstream (BehaviorDispatcher, PreviewViewModel)
        // even though everything still "compiles" and no exception is
        // thrown — the match just quietly fails. This test exercises a
        // real Serialize -> Deserialize round trip, not in-memory
        // construction, because that's the only path the bug shows up on.
        var doc = NyforgeProject.CreateBlank();
        var window = doc.Screens[0].Root;

        var toggle = new NuiComponent
        {
            Id = "toggle_x",
            Type = "Toggle",
            Properties = { ["value"] = true, ["label"] = "Some label" }
        };
        window.Children.Add(toggle);

        doc.States["useDarkTheme"] = true;
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "behavior_x",
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Theme.Set",
                Arguments = { ["theme"] = "Eclipse" }
            }
        });

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        var reloadedToggle = reloaded.Screens[0].Root.Children.Single();
        Assert.True(reloadedToggle.Properties["value"] is bool);
        Assert.True((bool)reloadedToggle.Properties["value"]!);

        Assert.True(reloaded.States["useDarkTheme"] is bool);

        var action = reloaded.Behaviors.Single().Action;
        Assert.NotNull(action);
        var arg = action!.Arguments["theme"];
        Assert.True(arg is string);
        Assert.Equal("Eclipse", arg);
    }

    [Fact]
    public void ActionArgumentResolver_substitutes_state_reference()
    {
        var arguments = new Dictionary<string, object?> { ["theme"] = "$state:choice" };
        var states = new Dictionary<string, object?> { ["choice"] = "Solar" };

        var resolved = ActionArgumentResolver.Resolve(arguments, states);

        Assert.Equal("Solar", resolved["theme"]);
    }

    [Fact]
    public void ActionArgumentResolver_leaves_literal_values_unchanged()
    {
        var arguments = new Dictionary<string, object?> { ["theme"] = "Eclipse", ["count"] = 3L };
        var resolved = ActionArgumentResolver.Resolve(arguments, new Dictionary<string, object?>());

        Assert.Equal("Eclipse", resolved["theme"]);
        Assert.Equal(3L, resolved["count"]);
    }

    [Fact]
    public void ActionArgumentResolver_leaves_placeholder_text_when_state_missing()
    {
        // A reference to a state that doesn't exist should surface as an
        // obvious authoring mistake, not silently become null.
        var arguments = new Dictionary<string, object?> { ["theme"] = "$state:doesNotExist" };
        var resolved = ActionArgumentResolver.Resolve(arguments, new Dictionary<string, object?>());

        Assert.Equal("$state:doesNotExist", resolved["theme"]);
    }

    [Fact]
    public void ActionArgumentResolver_does_not_mutate_input_dictionary()
    {
        var arguments = new Dictionary<string, object?> { ["theme"] = "$state:choice" };
        var states = new Dictionary<string, object?> { ["choice"] = "Solar" };

        ActionArgumentResolver.Resolve(arguments, states);

        Assert.Equal("$state:choice", arguments["theme"]);
    }

    [Fact]
    public void Desktop_shell_example_opens_in_Forge()
    {
        // The flagship reference application: the real desktop shell
        // design (examples/nyrqis-shell/desktop.nstudio, authored with
        // the Shell vocabulary — Taskbar, StartMenu, DesktopSurface,
        // CommandPalette, LockScreen, …) must open in Nyforge itself.
        // This is the editor-side half of the ADR-0025 conformance story:
        // the Nyrqis import gate (floor + Rust crate) accepts the same
        // file byte-for-byte.
        var path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "nyrqis-shell", "desktop.nstudio");
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path), $"fixture missing: {path}");

        var doc = ProjectSerializer.LoadFromFile(path);

        Assert.Equal("1.0.0", doc.Version);
        Assert.Equal(2, doc.Screens.Count);
        Assert.Equal("desktop", doc.Screens[0].Id);
        Assert.Equal("lock", doc.Screens[1].Id);
        // The shell vocabulary is real: the taskbar resolves through the
        // registry-backed contract tables.
        Assert.True(ComponentContracts.TryGet("Taskbar", out var taskbar));
        Assert.Equal("Shell", taskbar!.Category);
        Assert.Contains("pinnedApps", taskbar.Properties);
    }

    [Fact]
    public void Windows_shell_example_opens_in_Forge()
    {
        // The window-system + power-UI shell screens
        // (examples/nyrqis-shell/windows.nstudio): WindowFrame +
        // WindowControls driving component-targeted actions, and a
        // PowerMenu with a bound open state — the second reference shell
        // design must open in Nyforge itself.
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "nyrqis-shell", "windows.nstudio"));
        Assert.True(File.Exists(path), $"fixture missing: {path}");

        var doc = ProjectSerializer.LoadFromFile(path);

        Assert.Equal("1.0.0", doc.Version);
        Assert.Equal(2, doc.Screens.Count);
        Assert.Equal("windows", doc.Screens[0].Id);
        Assert.Equal("power", doc.Screens[1].Id);
        // The window-system vocabulary resolves through the contract
        // tables.
        Assert.True(ComponentContracts.TryGet("WindowFrame", out var frame));
        Assert.Equal("Shell", frame!.Category);
        Assert.Contains("Minimize", frame.Actions);
        Assert.Contains("Maximize", frame.Actions);
        Assert.Contains("Close", frame.Actions);
    }

    [Fact]
    public void Widgets_osd_login_example_opens_in_Forge()
    {
        // The widgets + OSD + login shell screens
        // (examples/nyrqis-shell/widgets.nstudio) — the third reference
        // shell design, using the WidgetHost/OSD/Login vocabulary.
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "nyrqis-shell", "widgets.nstudio"));
        Assert.True(File.Exists(path), $"fixture missing: {path}");

        var doc = ProjectSerializer.LoadFromFile(path);

        Assert.Equal("1.0.0", doc.Version);
        Assert.Equal(3, doc.Screens.Count);
        Assert.Equal("widgets", doc.Screens[0].Id);
        Assert.Equal("osd", doc.Screens[1].Id);
        Assert.Equal("login", doc.Screens[2].Id);
        Assert.True(ComponentContracts.TryGet("WidgetHost", out var host));
        Assert.Equal("Shell", host!.Category);
        Assert.Contains("AddWidget", host.Actions);
    }
}
