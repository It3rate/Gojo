using Gojo.Core;

namespace Gojo.Patterns;

/// <summary>
/// Construction and read helpers for local repeat controller surfaces. The
/// controller state is ordinary carrier/relation structure; these helpers do
/// not create a durable executor object.
/// </summary>
public static class RepeatController
{
    /// <summary>Creates the phase wrap surface `P(phase,span) -> P(phase,0)`.</summary>
    public static Relation WrapSurface(Carrier phase, long span) =>
        new(Relation.Point(phase, NonNegative(span)), Relation.Point(phase, 0));

    /// <summary>Attaches the phase wrap surface `P(phase,span) -> P(phase,0)`.</summary>
    public static Relation AttachWrap(Carrier phase, long span) =>
        phase.Attach(WrapSurface(phase, span));

    /// <summary>Creates the phase clamp surface `P(phase,limit) -> P(phase,limit)`.</summary>
    public static Relation ClampSurface(Carrier phase, long limit)
    {
        var ticks = NonNegative(limit);
        return new Relation(Relation.Point(phase, ticks), Relation.Point(phase, ticks));
    }

    /// <summary>Attaches the phase clamp surface `P(phase,limit) -> P(phase,limit)`.</summary>
    public static Relation AttachClamp(Carrier phase, long limit) =>
        phase.Attach(ClampSurface(phase, limit));

    /// <summary>Creates the crossing counter surface `P(phase,span) -> P(cycles,0)`.</summary>
    public static Relation CycleCounterSurface(Carrier phase, long span, Carrier cycles) =>
        new(Relation.Point(phase, NonNegative(span)), Relation.Point(cycles, 0));

    /// <summary>Attaches the crossing counter surface `P(phase,span) -> P(cycles,0)`.</summary>
    public static Relation AttachCycleCounter(Carrier phase, long span, Carrier cycles) =>
        phase.Attach(CycleCounterSurface(phase, span, cycles));

    /// <summary>Attaches a doubled-period wrap used by a ping-pong phase read.</summary>
    public static Relation AttachPingPongPhase(Carrier phase, long span) =>
        AttachWrap(phase, SaturatingDouble(NonNegative(span)));

    /// <summary>Reads a reflected position from a phase carrier over `[0, span, 0]`.</summary>
    public static ulong PingPongValue(Carrier phase, long span)
    {
        var ticks = NonNegative(span);
        if (ticks == 0)
        {
            return 0;
        }

        var spanTicks = checked((ulong)ticks);
        var period = checked(spanTicks * 2UL);
        var local = phase.Value % period;
        return local <= spanTicks
            ? local
            : period - local;
    }

    /// <summary>Reads the currently selected repeated frame from a phase carrier.</summary>
    public static bool TryReadFrame(IReadOnlyList<IElement> frames, Carrier phase, out IElement frame)
    {
        if (frames.Count == 0)
        {
            frame = default!;
            return false;
        }

        var index = checked((int)(phase.Value % checked((ulong)frames.Count)));
        frame = frames[index];
        return true;
    }

    private static long NonNegative(long ticks) => Math.Max(0, ticks);

    private static long SaturatingDouble(long ticks) =>
        ticks > long.MaxValue / 2
            ? long.MaxValue
            : ticks * 2;
}
