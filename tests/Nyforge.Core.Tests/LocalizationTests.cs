using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Localization (NUI-SCHEMA §8.1): $localize:key references resolve
/// through the active locale's table; the validator rejects missing
/// keys up front, mirroring the Nyrqis import gate.
/// </summary>
public class LocalizationTests
{
    private static NuiDocument DocWithLocales()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.Locales.Active = "en";
        doc.Locales.Tables["en"] = new Dictionary<string, string>
        {
            ["search.label"] = "Search",
            ["notif.dnd"] = "Notifications paused until disabled",
        };
        doc.Locales.Tables["af"] = new Dictionary<string, string>
        {
            ["search.label"] = "Soek",
            ["notif.dnd"] = "Kennisgewings onderbreek",
        };
        return doc;
    }

    [Fact]
    public void Resolve_uses_active_locale_table()
    {
        var doc = DocWithLocales();
        Assert.Equal("Search", Localize.Resolve("$localize:search.label", doc));
    }

    [Fact]
    public void Resolve_switches_with_active_locale()
    {
        var doc = DocWithLocales();
        doc.Locales.Active = "af";
        Assert.Equal("Soek", Localize.Resolve("$localize:search.label", doc));
    }

    [Fact]
    public void Resolve_plain_and_missing_keys_pass_through()
    {
        var doc = DocWithLocales();
        Assert.Equal("Hello", Localize.Resolve("Hello", doc));
        // Missing key stays literal (fail-soft at resolution).
        Assert.Equal("$localize:ghost", Localize.Resolve("$localize:ghost", doc));
    }

    [Fact]
    public void References_extracts_keys_in_order()
    {
        Assert.Equal(
            new[] { "a.b", "c_d" },
            Localize.References("$localize:a.b and $localize:c_d"));
    }

    [Fact]
    public void Locales_section_survives_round_trip()
    {
        var doc = DocWithLocales();
        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);

        Assert.Equal("en", reloaded.Locales.Active);
        Assert.Equal("Search", reloaded.Locales.Tables["en"]["search.label"]);
        Assert.Equal("Kennisgewings onderbreek", reloaded.Locales.Tables["af"]["notif.dnd"]);
    }

    [Fact]
    public void Validator_accepts_localized_document()
    {
        var doc = DocWithLocales();
        var btn = new NuiComponent
        {
            Id = "btn_search",
            Type = "Button",
            Properties = new Dictionary<string, object?> { ["text"] = "$localize:search.label" },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        };
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Validator_rejects_missing_localize_key()
    {
        var doc = DocWithLocales();
        var btn = new NuiComponent
        {
            Id = "btn_search",
            Type = "Button",
            Properties = new Dictionary<string, object?> { ["text"] = "$localize:ghost.key" },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        };
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-019");
    }

    [Fact]
    public void Validator_rejects_localize_without_locales_section()
    {
        var doc = NyforgeProject.CreateBlank(); // no locales
        var btn = new NuiComponent
        {
            Id = "btn_search",
            Type = "Button",
            Properties = new Dictionary<string, object?> { ["text"] = "$localize:search.label" },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        };
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-019");
    }

    [Fact]
    public void Validator_rejects_localize_in_behavior_argument()
    {
        var doc = DocWithLocales();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?> { ["message"] = "$localize:ghost.key" },
            },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-019" &&
            i.Message.Contains("behavior 'b1' argument"));
    }
}
