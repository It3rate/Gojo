using SkiaSharp;

namespace GojoDraw;

/// <summary>Small timeline contract for GojoDraw presentation surfaces.</summary>
public interface IGojoDrawTimeline
{
    TimeSpan Duration { get; }

    bool Loops { get; }
}

/// <summary>
/// Render-boundary animation clock helpers. Animation describes presentation
/// over inspectable structure; it does not own Gojo meaning.
/// </summary>
public static class GojoDrawAnimationClock
{
    public static GojoDrawAnimationFrame FrameFor(TimeSpan elapsed, IGojoDrawTimeline? timeline) =>
        timeline is null
            ? new GojoDrawAnimationFrame(elapsed, 0, 0)
            : FrameFor(elapsed, timeline.Duration, timeline.Loops);

    public static GojoDrawAnimationFrame FrameFor(TimeSpan elapsed, TimeSpan duration, bool loops = true)
    {
        if (duration <= TimeSpan.Zero)
        {
            return new GojoDrawAnimationFrame(elapsed, 0, 0);
        }

        var durationSeconds = duration.TotalSeconds;
        var elapsedSeconds = elapsed.TotalSeconds;
        var loopSeconds = loops
            ? elapsedSeconds % durationSeconds
            : Math.Min(elapsedSeconds, durationSeconds);
        var progress = durationSeconds <= 0 ? 0 : loopSeconds / durationSeconds;
        return new GojoDrawAnimationFrame(elapsed, loopSeconds, progress);
    }

    public static double ProgressFor(GojoDrawVisualTrack track, TimeSpan elapsed)
    {
        if (track.Duration <= TimeSpan.Zero || elapsed <= track.StartTime)
        {
            return 0;
        }

        var raw = (elapsed - track.StartTime).TotalSeconds / track.Duration.TotalSeconds;
        return Ease(Math.Clamp(raw, 0, 1), track.Easing);
    }

    public static double Ease(double t, GojoDrawEasing easing) =>
        easing switch
        {
            GojoDrawEasing.SmoothStep => t * t * (3 - 2 * t),
            _ => t
        };
}

public readonly record struct GojoDrawAnimationFrame(
    TimeSpan Elapsed,
    double LoopSeconds,
    double LoopProgress);

public sealed record GojoDrawVisualTrack(
    GojoDrawScenePoint Start,
    GojoDrawScenePoint End,
    TimeSpan StartTime,
    TimeSpan Duration,
    GojoDrawEasing Easing = GojoDrawEasing.SmoothStep);

public readonly record struct GojoDrawScenePoint(float X, float Y)
{
    public SKPoint In(SKRect bounds) =>
        new(bounds.Left + bounds.Width * X, bounds.Top + bounds.Height * Y);
}

public enum GojoDrawEasing
{
    Linear,
    SmoothStep
}
