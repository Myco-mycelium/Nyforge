namespace Nyforge.Core.Nui;

/// <summary>
/// The responsive layout constraint engine (NUI-SCHEMA §4): computes the
/// effective <see cref="NuiLayout"/> for a given container size from the
/// authored layout plus its anchors, min/max bounds, and aspect ratio.
/// This is the design-time mirror of the Nyrqis floor's
/// <c>resolve_layout</c> — one design adapts to desktop/laptop/tablet/
/// handheld/console containers instead of needing per-size canvases.
///
/// Rules (identical to the runtime floor, differential-tested):
/// - All anchors default false — a layout without constraints keeps its
///   absolute authored coordinates exactly.
/// - <see cref="NuiLayout.AnchorLeft"/> fixes the left edge at X;
///   <see cref="NuiLayout.AnchorRight"/> fixes the right edge at
///   <c>containerWidth - X</c> (X doubles as the right inset). Both
///   together make the width stretch: <c>width = containerWidth - 2*X</c>,
///   clamped to min/max width. Vertical is the mirror.
/// - Min/max bounds clamp the computed (or authored) size.
/// - <see cref="NuiLayout.AspectRatio"/> derives the non-stretched axis
///   when exactly one axis stretches; otherwise the authored size stands.
/// </summary>
public static class ResponsiveLayout
{
    public static NuiLayout Compute(NuiLayout layout, double containerWidth, double containerHeight)
    {
        var x = layout.X;
        var y = layout.Y;
        var w = layout.Width;
        var h = layout.Height;

        var stretchW = layout.AnchorLeft && layout.AnchorRight;
        var stretchH = layout.AnchorTop && layout.AnchorBottom;

        if (stretchW)
        {
            w = containerWidth - 2 * x;
        }
        else if (layout.AnchorRight)
        {
            x = containerWidth - x - w;
        }

        if (stretchH)
        {
            h = containerHeight - 2 * y;
        }
        else if (layout.AnchorBottom)
        {
            y = containerHeight - y - h;
        }

        // Aspect ratio derives the non-stretched axis (width-driven when
        // both stretch).
        if (layout.AspectRatio is > 0)
        {
            if (stretchW && !stretchH)
            {
                h = w / layout.AspectRatio.Value;
            }
            else if (stretchH && !stretchW)
            {
                w = h * layout.AspectRatio.Value;
            }
        }

        if (layout.MinWidth is { } minW) w = Math.Max(w, minW);
        if (layout.MaxWidth is { } maxW) w = Math.Min(w, maxW);
        if (layout.MinHeight is { } minH) h = Math.Max(h, minH);
        if (layout.MaxHeight is { } maxH) h = Math.Min(h, maxH);

        return new NuiLayout
        {
            X = x,
            Y = y,
            Width = w,
            Height = h,
            AnchorLeft = layout.AnchorLeft,
            AnchorRight = layout.AnchorRight,
            AnchorTop = layout.AnchorTop,
            AnchorBottom = layout.AnchorBottom,
            MinWidth = layout.MinWidth,
            MaxWidth = layout.MaxWidth,
            MinHeight = layout.MinHeight,
            MaxHeight = layout.MaxHeight,
            AspectRatio = layout.AspectRatio,
        };
    }
}
