using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Declarative animations (NUI-SCHEMA §8.3): the document's animations
/// section — unique ids, targets that name components, non-empty
/// properties, validated timing parameters — and the
/// Nyrqis.Animation.Play behavior reference, mirrored from the Nyrqis
/// import gate (floor + Rust crate) at design time (ER-NUI-022).
/// </summary>
public class AnimationTests
{
    private static NuiDocument DocWithAnimation(
        NuiAnimation? animation = null,
        NuiBehavior? behavior = null)
    {
        var doc = NyforgeProject.CreateBlank();
        var screen = doc.Screens[0];
        screen.Root = new NuiComponent
        {
            Id = "menu",
            Type = "StartMenu",
            Properties = new Dictionary<string, object?>
            {
                ["open"] = false,
                ["pinnedApps"] = new List<string>(),
                ["recommendedApps"] = new List<string>(),
            },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 200, Height = 300 },
        };
        if (animation is not null) doc.Animations.Add(animation);
        if (behavior is not null) doc.Behaviors.Add(behavior);
        return doc;
    }

    private static NuiAnimation Fade(string id = "fade") => new()
    {
        Id = id,
        Target = "menu",
        Property = "opacity",
        Duration = 200,
        Easing = "ease-out",
    };

    private static NuiBehavior PlayAnimation(string animationId) => new()
    {
        Id = "b1",
        Action = new NuiAction
        {
            Target = "System",
            Name = "Nyrqis.Animation.Play",
            Arguments = new Dictionary<string, object?> { ["animation"] = animationId },
        },
    };

    // ---- model / serialization -----------------------------------------------

    [Fact]
    public void Animation_round_trips_through_json()
    {
        var doc = DocWithAnimation(Fade());
        var json = ProjectSerializer.Serialize(doc);
        var back = ProjectSerializer.Deserialize(json);
        var anim = Assert.Single(back.Animations);
        Assert.Equal("fade", anim.Id);
        Assert.Equal("menu", anim.Target);
        Assert.Equal("opacity", anim.Property);
        Assert.Equal(200, anim.Duration);
        Assert.Equal("ease-out", anim.Easing);
    }

    [Fact]
    public void Defaults_applied_when_omitted()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
        });
        var anim = Assert.Single(doc.Animations);
        Assert.Equal(300, anim.Duration);
        Assert.Equal(0, anim.Delay);
        Assert.Equal("ease-in-out", anim.Easing);
        Assert.Equal(0, anim.Repeat);
        Assert.Equal("forward", anim.Direction);
    }

    // ---- validator (ER-NUI-022) ----------------------------------------------

    [Fact]
    public void Validator_accepts_declared_animation()
    {
        var doc = DocWithAnimation(Fade(), PlayAnimation("fade"));
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Errors, e => e.Code == "ER-NUI-022");
    }

    [Fact]
    public void Validator_rejects_undeclared_animation_reference()
    {
        var doc = DocWithAnimation(Fade(), PlayAnimation("ghost"));
        var result = NuiValidator.Validate(doc);
        var issue = Assert.Single(result.Errors);
        Assert.Equal("ER-NUI-022", issue.Code);
        Assert.Contains("plays animation 'ghost'", issue.Message);
    }

    [Fact]
    public void Validator_rejects_unknown_target()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "ghost",
            Property = "opacity",
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("targets component 'ghost'"));
    }

    [Fact]
    public void Validator_rejects_missing_property()
    {
        var doc = DocWithAnimation(new NuiAnimation { Id = "f", Target = "menu" });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("must declare a property"));
    }

    [Fact]
    public void Validator_rejects_bad_easing_and_direction()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
            Easing = "bounce",
            Direction = "sideways",
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("easing 'bounce'"));
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("direction 'sideways'"));
    }

    [Fact]
    public void Validator_rejects_negative_duration()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
            Duration = -5,
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("'duration' must be non-negative"));
    }

    [Fact]
    public void Validator_rejects_duplicate_animation_ids()
    {
        var doc = DocWithAnimation(Fade());
        doc.Animations.Add(Fade("fade"));
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" && e.Message.Contains("Duplicate animation id 'fade'"));
    }

    // ---- keyframes (NUI-SCHEMA §8.3) ----------------------------------------

    private static NuiAnimation KeyframedFade() => new()
    {
        Id = "fade",
        Target = "menu",
        Property = "opacity",
        Duration = 200,
        Easing = "ease-out",
        Keyframes = new List<NuiKeyframe>
        {
            new() { Offset = 0.0, Value = 0.0 },
            new() { Offset = 0.6, Value = 0.75 },
            new() { Offset = 1.0, Value = 1.0 },
        },
    };

    [Fact]
    public void Keyframes_round_trip_through_json()
    {
        var doc = DocWithAnimation(KeyframedFade());
        var json = ProjectSerializer.Serialize(doc);
        var back = ProjectSerializer.Deserialize(json);
        var anim = Assert.Single(back.Animations);
        Assert.Equal(3, anim.Keyframes.Count);
        Assert.Equal(0.6, anim.Keyframes[1].Offset);
        Assert.Equal(0.75, anim.Keyframes[1].Value);
    }

    [Fact]
    public void Validator_accepts_keyframed_animation()
    {
        var doc = DocWithAnimation(KeyframedFade(), PlayAnimation("fade"));
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Errors, e => e.Code == "ER-NUI-022");
    }

    [Fact]
    public void Validator_rejects_out_of_range_keyframe_offset()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
            Keyframes = new List<NuiKeyframe>
            {
                new() { Offset = 1.5, Value = 1.0 },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" &&
            e.Message.Contains("keyframe 0 'offset' must be a number in [0, 1]"));
    }

    [Fact]
    public void Validator_rejects_non_increasing_keyframe_offsets()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
            Keyframes = new List<NuiKeyframe>
            {
                new() { Offset = 0.5, Value = 1.0 },
                new() { Offset = 0.5, Value = 2.0 },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" &&
            e.Message.Contains("keyframe 1 'offset' must be greater than " +
                               "the previous offset"));
    }

    [Fact]
    public void Validator_rejects_keyframe_without_value()
    {
        var doc = DocWithAnimation(new NuiAnimation
        {
            Id = "f",
            Target = "menu",
            Property = "opacity",
            Keyframes = new List<NuiKeyframe>
            {
                new() { Offset = 0.0, Value = 0.0 },
                new() { Offset = 1.0, Value = null },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-022" &&
            e.Message.Contains("keyframe 1 'value' must be a number, " +
                               "string, or boolean"));
    }

    // ---- example fixture -----------------------------------------------------

    [Fact]
    public void Desktop_fixture_animation_validates()
    {
        var document = ProjectSerializer.LoadFromFile(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                         "examples", "nyrqis-shell", "desktop.nstudio"));
        var result = NuiValidator.Validate(document);
        Assert.DoesNotContain(result.Errors, e => e.Code == "ER-NUI-022");

        var anim = Assert.Single(document.Animations);
        Assert.Equal("start_menu_fade", anim.Id);
        Assert.Equal("start_menu", anim.Target);
        Assert.Equal("opacity", anim.Property);
        Assert.Equal(200, anim.Duration);
        Assert.Equal("ease-out", anim.Easing);
        Assert.Equal(3, anim.Keyframes.Count);
        Assert.Equal(0.6, anim.Keyframes[1].Offset);

        var behavior = document.Behaviors
            .First(b => b.Id == "behavior_start_toggle");
        Assert.Equal("Nyrqis.Animation.Play", behavior.Action.Name);
        Assert.Equal("start_menu_fade", behavior.Action.Arguments["animation"]);
    }
}
