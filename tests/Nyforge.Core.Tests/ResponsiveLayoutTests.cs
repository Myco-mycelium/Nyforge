using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Responsive layout constraints (NUI-SCHEMA §4): anchors, min/max
/// bounds, and aspect ratio — the design-time mirror of the Nyrqis
/// floor's resolve_layout (same rules, differential-tested there).
/// </summary>
public class ResponsiveLayoutTests
{
    private static NuiLayout Abs(double x, double y, double w, double h) => new()
    {
        X = x, Y = y, Width = w, Height = h,
    };

    [Fact]
    public void No_constraints_is_absolute()
    {
        var r = ResponsiveLayout.Compute(Abs(24, 36, 200, 50), 1000, 500);
        Assert.Equal(24, r.X);
        Assert.Equal(36, r.Y);
        Assert.Equal(200, r.Width);
        Assert.Equal(50, r.Height);
    }

    [Fact]
    public void Both_horizontal_anchors_stretch_and_clamp()
    {
        var layout = Abs(0, 0, 100, 20);
        layout.AnchorLeft = true;
        layout.AnchorRight = true;
        layout.MinWidth = 500;
        layout.MaxWidth = 800;

        var r = ResponsiveLayout.Compute(layout, 1000, 500);

        Assert.Equal(800, r.Width); // 1000 - 2*0, clamped to max
        Assert.Equal(20, r.Height);
    }

    [Fact]
    public void Bottom_anchor_docks_from_bottom()
    {
        var layout = Abs(0, 0, 1000, 80);
        layout.AnchorBottom = true;

        var r = ResponsiveLayout.Compute(layout, 1000, 500);

        Assert.Equal(420, r.Y); // 500 - 0 - 80
        Assert.Equal(80, r.Height);
    }

    [Fact]
    public void Right_anchor_measures_from_right_edge()
    {
        var layout = Abs(24, 10, 200, 50);
        layout.AnchorRight = true;

        var r = ResponsiveLayout.Compute(layout, 1000, 500);

        Assert.Equal(776, r.X); // 1000 - 24 - 200
        Assert.Equal(200, r.Width);
    }

    [Fact]
    public void Aspect_ratio_keeps_authored_size_when_not_stretched()
    {
        var layout = Abs(0, 0, 96, 96);
        layout.AspectRatio = 1.0;

        var r = ResponsiveLayout.Compute(layout, 1000, 500);

        Assert.Equal(96, r.Width);
        Assert.Equal(96, r.Height);
    }

    [Fact]
    public void Aspect_ratio_derives_stretched_axis()
    {
        var layout = Abs(0, 0, 96, 10);
        layout.AnchorLeft = true;
        layout.AnchorRight = true;
        layout.AspectRatio = 2.0;

        var r = ResponsiveLayout.Compute(layout, 1000, 500);

        Assert.Equal(1000, r.Width);      // stretched
        Assert.Equal(500, r.Height);      // 1000 / 2
    }

    [Fact]
    public void Min_width_floor_survives_narrower_container()
    {
        var layout = Abs(0, 0, 1440, 80);
        layout.AnchorLeft = true;
        layout.AnchorRight = true;
        layout.AnchorBottom = true;
        layout.MinWidth = 1200;
        layout.MaxWidth = 1600;

        var r = ResponsiveLayout.Compute(layout, 800, 600);

        Assert.Equal(1200, r.Width); // minWidth floor
        Assert.Equal(520, r.Y);      // still docked: 600 - 0 - 80
    }

    [Fact]
    public void Constraint_fields_survive_round_trip()
    {
        var doc = NyforgeProject.CreateBlank();
        var taskbar = new NuiComponent
        {
            Id = "taskbar",
            Type = "Taskbar",
            Layout = new NuiLayout
            {
                X = 0, Y = 0, Width = 1440, Height = 80,
                AnchorLeft = true, AnchorRight = true, AnchorBottom = true,
                MinWidth = 1200, MaxWidth = 1600, MaxHeight = 96,
            },
        };
        doc.Screens[0].Root.Children.Add(taskbar);

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);
        var layout = reloaded.Screens[0].Root.Children.Single().Layout;

        Assert.True(layout.AnchorLeft);
        Assert.True(layout.AnchorRight);
        Assert.True(layout.AnchorBottom);
        Assert.Equal(1200, layout.MinWidth);
        Assert.Equal(1600, layout.MaxWidth);
        Assert.Equal(96, layout.MaxHeight);
    }
}
