using System;
using System.Collections.Generic;

namespace Nyforge.Core.Canvas;

/// <summary>
/// A draggable element's bounds for alignment-guide computation.
/// </summary>
public readonly record struct AlignmentBounds(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Right => X + Width;
    public double Top => Y;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2.0;
    public double CenterY => Y + Height / 2.0;
}

/// <summary>
/// A snap result: the snapped coordinate and the guide line position
/// (the edge/center of the candidate that was snapped to).
/// </summary>
public readonly record struct SnapResult(double Position, double GuideLine);

/// <summary>
/// Provides alignment-guide snapping during drag/resize operations.
/// When a component is dragged near the edge or center of another
/// component (or the canvas bounds), the position snaps to that
/// alignment and a guide line is returned for visual rendering.
///
/// The threshold is 8 px (matching the design system's spacing scale).
/// </summary>
public sealed class AlignmentGuideService
{
    /// <summary>
    /// Snap threshold in pixels. A position within this distance of an
    /// alignment candidate will snap to it.
    /// </summary>
    public double Threshold { get; init; } = 8.0;

    /// <summary>
    /// Find snap candidates for a horizontal (X) position.
    /// Returns the snapped X and the guide line X, or null if no snap.
    /// </summary>
    public SnapResult? SnapX(
        double candidateX,
        double candidateWidth,
        double cursorX,
        IReadOnlyList<AlignmentBounds> siblings,
        double canvasWidth)
    {
        var left = candidateX;
        var right = candidateX + candidateWidth;
        var centerX = candidateX + candidateWidth / 2.0;

        // Build candidate X positions: canvas edges + siblings' edges/centers
        var candidates = new List<(double pos, double guide)>();

        // Canvas edges
        candidates.Add((0.0, 0.0));                        // canvas left
        candidates.Add((canvasWidth, canvasWidth));          // canvas right
        candidates.Add((canvasWidth / 2.0, canvasWidth / 2.0)); // canvas center

        foreach (var s in siblings)
        {
            candidates.Add((s.Left, s.Left));
            candidates.Add((s.Right, s.Right));
            candidates.Add((s.CenterX, s.CenterX));
        }

        // Check left edge
        SnapResult? best = null;
        double bestDist = Threshold + 1;

        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(left - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos, guide);
            }
        }

        // Check right edge
        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(right - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos - candidateWidth, guide);
            }
        }

        // Check center
        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(centerX - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos - candidateWidth / 2.0, guide);
            }
        }

        return bestDist <= Threshold ? best : null;
    }

    /// <summary>
    /// Find snap candidates for a vertical (Y) position.
    /// Returns the snapped Y and the guide line Y, or null if no snap.
    /// </summary>
    public SnapResult? SnapY(
        double candidateY,
        double candidateHeight,
        double cursorY,
        IReadOnlyList<AlignmentBounds> siblings,
        double canvasHeight)
    {
        var top = candidateY;
        var bottom = candidateY + candidateHeight;
        var centerY = candidateY + candidateHeight / 2.0;

        var candidates = new List<(double pos, double guide)>();

        // Canvas edges
        candidates.Add((0.0, 0.0));
        candidates.Add((canvasHeight, canvasHeight));
        candidates.Add((canvasHeight / 2.0, canvasHeight / 2.0));

        foreach (var s in siblings)
        {
            candidates.Add((s.Top, s.Top));
            candidates.Add((s.Bottom, s.Bottom));
            candidates.Add((s.CenterY, s.CenterY));
        }

        SnapResult? best = null;
        double bestDist = Threshold + 1;

        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(top - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos, guide);
            }
        }

        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(bottom - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos - candidateHeight, guide);
            }
        }

        foreach (var (pos, guide) in candidates)
        {
            var dist = Math.Abs(centerY - pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = new SnapResult(pos - candidateHeight / 2.0, guide);
            }
        }

        return bestDist <= Threshold ? best : null;
    }
}
