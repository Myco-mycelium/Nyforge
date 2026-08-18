using Nyforge.Core.Canvas;
using Xunit;

namespace Nyforge.Core.Tests;

public class AlignmentGuideTests
{
    private readonly AlignmentGuideService _service = new();

    [Fact]
    public void SnapX_Left_Edge()
    {
        // Sibling at x=100, width=50. Candidate at x=98, width=50.
        // Candidate left (98) is 2px from sibling left (100) → snap.
        var siblings = new[] { new AlignmentBounds(100, 0, 50, 50) };
        var result = _service.SnapX(98, 50, 98, siblings, 1440);
        Assert.NotNull(result);
        Assert.Equal(100, result!.Value.Position);
        Assert.Equal(100, result.Value.GuideLine);
    }

    [Fact]
    public void SnapX_Right_Edge()
    {
        // Sibling at x=200, width=50 → right = 250.
        // Candidate at x=248, width=50 → left = 248. dist to sibling right (250) = 2.
        // Also candidate center = 273, sibling center = 225. dist = 48. Too far.
        var siblings = new[] { new AlignmentBounds(200, 0, 50, 50) };
        var result = _service.SnapX(248, 50, 248, siblings, 1440);
        Assert.NotNull(result);
        Assert.Equal(250, result!.Value.Position);
        Assert.Equal(250, result.Value.GuideLine);
    }

    [Fact]
    public void SnapX_Center()
    {
        // Sibling at x=200, width=100 → center = 250.
        // Candidate at x=248, width=4 → center = 250. dist = 0. Perfect snap.
        var siblings = new[] { new AlignmentBounds(200, 0, 100, 50) };
        var result = _service.SnapX(248, 4, 248, siblings, 1440);
        Assert.NotNull(result);
        Assert.Equal(248, result!.Value.Position); // center = 250 → x = 250 - 2 = 248
        Assert.Equal(250, result.Value.GuideLine);
    }

    [Fact]
    public void SnapX_Canvas_Center()
    {
        // Canvas width 1440, center = 720.
        // Candidate at x=716, width=8 → center = 720. dist = 0.
        var result = _service.SnapX(716, 8, 716, Array.Empty<AlignmentBounds>(), 1440);
        Assert.NotNull(result);
        Assert.Equal(716, result!.Value.Position);
        Assert.Equal(720, result.Value.GuideLine);
    }

    [Fact]
    public void SnapX_No_Snap_Beyond_Threshold()
    {
        // Sibling at x=0, width=50 → right = 50, center = 25.
        // Candidate at x=60, width=50 → left = 60. dist to sibling right (50) = 10 > threshold (8).
        // dist to sibling center (25) = 35. Too far.
        // dist to canvas left (0) = 60. Too far.
        // dist to canvas center (720) = 660. Too far.
        var siblings = new[] { new AlignmentBounds(0, 0, 50, 50) };
        var result = _service.SnapX(60, 50, 60, siblings, 1440);
        Assert.Null(result);
    }

    [Fact]
    public void SnapY_Top_Edge()
    {
        var siblings = new[] { new AlignmentBounds(0, 100, 50, 50) };
        var result = _service.SnapY(98, 50, 98, siblings, 900);
        Assert.NotNull(result);
        Assert.Equal(100, result!.Value.Position);
        Assert.Equal(100, result.Value.GuideLine);
    }

    [Fact]
    public void SnapY_Bottom_Edge()
    {
        // Sibling at y=200, height=50 → bottom = 250, center = 225.
        // Candidate at y=246, height=4 → bottom = 250, center = 248.
        // Candidate bottom (250) matches sibling bottom (250) → dist = 0.
        // Candidate center (248) vs sibling bottom (250) → dist = 2.
        // Bottom edge wins with dist = 0 → y = 250 - 4 = 246.
        var siblings = new[] { new AlignmentBounds(0, 200, 50, 50) };
        var result = _service.SnapY(246, 4, 246, siblings, 900);
        Assert.NotNull(result);
        Assert.Equal(246, result!.Value.Position); // bottom = 250 → y = 250 - 4 = 246
        Assert.Equal(250, result.Value.GuideLine);
    }

    [Fact]
    public void SnapY_Canvas_Edge()
    {
        // Canvas height 900. Candidate at y=2, height=8 → top = 2. dist to 0 = 2. Snap.
        var result = _service.SnapY(2, 8, 2, Array.Empty<AlignmentBounds>(), 900);
        Assert.NotNull(result);
        Assert.Equal(0, result!.Value.Position);
        Assert.Equal(0, result.Value.GuideLine);
    }

    [Fact]
    public void SnapY_No_Snap_Beyond_Threshold()
    {
        // Sibling at y=0, height=50 → bottom = 50, center = 25.
        // Candidate at y=60, height=50 → top = 60. dist to sibling bottom (50) = 10 > threshold.
        var siblings = new[] { new AlignmentBounds(0, 0, 50, 50) };
        var result = _service.SnapY(60, 50, 60, siblings, 900);
        Assert.Null(result);
    }

    [Fact]
    public void Custom_Threshold()
    {
        var service = new AlignmentGuideService { Threshold = 2.0 };
        var siblings = new[] { new AlignmentBounds(100, 0, 50, 50) };
        // Candidate at x=95, width=50 → left = 95. dist to sibling left (100) = 5 > custom threshold (2).
        var result = service.SnapX(95, 50, 95, siblings, 1440);
        Assert.Null(result);
    }
}
