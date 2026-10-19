using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An authored interruption between the terminal port of one actual
/// path and the start port of another. Exact native prefix reads establish a
/// known nonzero displacement. This does not join the ends, infer continuity
/// behind another strand, prove empty clearance, or select over/under order.</summary>
public sealed class OrnamentStrandGap
{
    // Surface = (actual source program -> (actual terminal port -> actual start port))
    //        -> ((from prefix read -> to prefix read)
    //        -> (difference read -> (accepted -> (blocked -> unresolved))))
    public QualitativeMotionFrame Frame { get; }
    public Relation SourceSurface { get; }
    public Relation FromPort { get; }
    public Relation ToPort { get; }
    public Relation FromRead { get; }
    public Relation ToRead { get; }
    public Relation Difference { get; }
    public Relation Accepted { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Accepted).Length == 1 && N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentStrandGap(UnitArrangementProgram source, Relation fromPort, Relation toPort)
        : this(source.Frame, source.Surface, fromPort, toPort) { }

    public OrnamentStrandGap(QualitativeMotionFrame frame, Relation actualSourceSurface, Relation fromPort, Relation toPort)
    {
        Frame = frame; SourceSurface = actualSourceSurface; FromPort = fromPort; ToPort = toPort;
        var accepted = new List<IElement>(); var blocked = new List<IElement>(); var pending = new List<IElement>();
        FromRead = ToRead = Difference = N.S();
        var pair = new Relation(fromPort, toPort);
        if (!TryProgram(frame, actualSourceSurface, out var source)) blocked.Add(actualSourceSurface);
        else if (!source.Complete()) pending.Add(actualSourceSurface);
        else if (!N.E(source.Ports).Any(port => ReferenceEquals(port, fromPort)) ||
            !N.E(source.Ports).Any(port => ReferenceEquals(port, toPort)) ||
            fromPort.Right is not Relation { Left: Relation fromPath } from ||
            toPort.Right is not Relation { Left: Relation toPath } to ||
            ReferenceEquals(fromPath, toPath) || !ReferenceEquals(from.Right, fromPath) ||
            N.E(source.ClosedPaths).Any(path => ReferenceEquals(path, fromPath) || ReferenceEquals(path, toPath)) ||
            N.E((Relation)toPath.Right) is not [var first, ..] || !ReferenceEquals(to.Right, first))
            blocked.Add(pair);
        else
        {
            var fromKnown = OrnamentEnrichment.TryAnchorWord(source, fromPort, out var fromWord, out var fromRead);
            var toKnown = OrnamentEnrichment.TryAnchorWord(source, toPort, out var toWord, out var toRead);
            FromRead = fromRead ?? N.S(); ToRead = toRead ?? N.S();
            if (!fromKnown || !toKnown) pending.Add(new Relation(FromRead, ToRead));
            else
            {
                // The ordered gap runs from the first terminal to the second
                // start, so its displacement is target minus source.
                Difference = frame.Reduce(toWord, UnitArrangementRead.Inverse(frame, fromWord));
                if (!frame.TryReadResult(Difference, out var remaining, out _, out var unresolved) ||
                    !N.Read(remaining, out var terms) || N.E(unresolved).Length != 0) pending.Add(Difference);
                else if (terms.Length == 0) blocked.Add(Difference);
                else accepted.Add(pair);
            }
        }
        Accepted = N.S(accepted.ToArray()); Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(actualSourceSurface, pair), new Relation(new Relation(FromRead, ToRead),
            new Relation(Difference, new Relation(Accepted, new Relation(Blocked, Unresolved)))));
    }

    /// <summary>Reads only this complete contract shape, rechecking actual port
    /// membership and every retained native witness against the source words.
    /// Copied favourable result fields cannot manufacture an interruption.</summary>
    public static bool TryRead(QualitativeMotionFrame frame, Relation candidate, out OrnamentStrandGap gap)
    {
        gap = null!;
        if (candidate is not { Left: Relation { Left: Relation source, Right: Relation { Left: Relation from, Right: Relation to } },
            Right: Relation { Left: Relation { Left: Relation fromRead, Right: Relation toRead },
                Right: Relation { Left: Relation difference, Right: Relation { Left: Relation accepted,
                    Right: Relation { Left: Relation blocked, Right: Relation unresolved } } } } } ||
            !N.Read(accepted, out var acceptedRows) || acceptedRows is not [Relation selected] ||
            !ReferenceEquals(selected.Left, from) || !ReferenceEquals(selected.Right, to) ||
            !N.Read(blocked, out var blockedRows) || blockedRows.Length != 0 ||
            !N.Read(unresolved, out var pendingRows) || pendingRows.Length != 0) return false;
        var expected = new OrnamentStrandGap(frame, source, from, to);
        if (!expected.IsComplete || !SameRead(frame, fromRead, expected.FromRead) ||
            !SameRead(frame, toRead, expected.ToRead) || !SameRead(frame, difference, expected.Difference)) return false;
        gap = new(frame, source, from, to, fromRead, toRead, difference, accepted, blocked, unresolved, candidate);
        return true;
    }

    private OrnamentStrandGap(QualitativeMotionFrame frame, Relation source, Relation from, Relation to,
        Relation fromRead, Relation toRead, Relation difference, Relation accepted, Relation blocked, Relation pending, Relation surface)
    {
        Frame = frame; SourceSurface = source; FromPort = from; ToPort = to; FromRead = fromRead; ToRead = toRead;
        Difference = difference; Accepted = accepted; Blocked = blocked; Unresolved = pending; Surface = surface;
    }
    private static bool TryProgram(QualitativeMotionFrame frame, Relation actual, out UnitArrangementProgram program)
    {
        program = null!;
        if (!ReferenceEquals(actual.Left, frame.Surface) ||
            actual.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
        program = new(frame, "native interrupted-path read", paths, ports, closed, evidence); return true;
    }
    private static bool SameRead(QualitativeMotionFrame frame, Relation actual, Relation expected) =>
        actual.Left is Relation { Left: var actualFrame, Right: Relation { Left: Relation before, Right: Relation incoming } } &&
        expected.Left is Relation { Right: Relation { Left: Relation expectedBefore, Right: Relation expectedIncoming } } &&
        ReferenceEquals(actualFrame, frame.Surface) && SameWord(frame, before, expectedBefore) && SameWord(frame, incoming, expectedIncoming) &&
        frame.TryReadResult(actual, out var result, out _, out var pending) && N.E(pending).Length == 0 &&
        frame.TryReadResult(expected, out var wanted, out _, out _) && SameWord(frame, result, wanted);
    private static bool SameWord(QualitativeMotionFrame frame, Relation first, Relation second) =>
        N.Read(first, out var a) && N.Read(second, out var b) && a.Length == b.Length && a.Zip(b).All(pair =>
            pair.First is Relation x && pair.Second is Relation y && ReferenceEquals(x.Left, y.Left) &&
            frame.Direction(x, out _, out _, out var xr) && frame.Direction(y, out _, out _, out var yr) && N.Same(xr, yr));
}
