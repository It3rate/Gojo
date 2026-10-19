using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Native selection of actual guide paths for an optional curved
/// presentation. Fixed public ports retain landmarks that the presentation
/// must visit. This is a linked observation hint, not native curve geometry.</summary>
public sealed class OrnamentCurveSelection
{
    // Surface = (actual source program -> (requested paths -> requested fixed ports))
    //        -> (selected paths -> (carried paths -> (fixed ports -> (blocked -> unresolved))))
    public QualitativeMotionFrame Frame { get; }
    public Relation SourceSurface { get; }
    public Relation Requested { get; }
    public Relation RequestedFixedPorts { get; }
    public Relation Selected { get; }
    public Relation Complement { get; }
    public Relation FixedPorts { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentCurveSelection(UnitArrangementProgram source, Relation selectedPaths, Relation? fixedPorts = null)
        : this(source.Frame, source.Surface, selectedPaths, fixedPorts) { }

    public OrnamentCurveSelection(QualitativeMotionFrame frame, Relation actualSourceSurface,
        Relation selectedPaths, Relation? fixedPorts = null)
    {
        Frame = frame; SourceSurface = actualSourceSurface; Requested = selectedPaths; RequestedFixedPorts = fixedPorts ?? N.S();
        Selected = Complement = FixedPorts = N.S();
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        if (!TryProgram(frame, actualSourceSurface, out var source)) blocked.Add(actualSourceSurface);
        else if (!source.Complete()) pending.Add(actualSourceSurface);
        else
        {
            var closedPaths = N.Read(selectedPaths, out var paths);
            var closedPorts = N.Read(RequestedFixedPorts, out var ports);
            if (!closedPaths) pending.Add(selectedPaths);
            if (!closedPorts) pending.Add(RequestedFixedPorts);
            if (!N.Distinct(paths) || paths.Any(path => !N.E(source.Paths).Any(actual => ReferenceEquals(actual, path))))
                blocked.Add(new Relation(selectedPaths, source.Paths));
            if (!N.Distinct(ports) || ports.Any(port => port is not Relation { Right: Relation pick } ||
                !N.E(source.Ports).Any(actual => ReferenceEquals(actual, port)) ||
                !paths.Any(path => ReferenceEquals(path, pick.Left))))
                blocked.Add(new Relation(RequestedFixedPorts, source.Ports));
            if (pending.Count == 0 && blocked.Count == 0)
            {
                Selected = selectedPaths; FixedPorts = RequestedFixedPorts;
                Complement = N.S(N.E(source.Paths).Where(path => !paths.Any(actual => ReferenceEquals(actual, path))).ToArray());
            }
        }
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(actualSourceSurface, new Relation(Requested, RequestedFixedPorts)),
            new Relation(Selected, new Relation(Complement, new Relation(FixedPorts, new Relation(Blocked, Unresolved)))));
    }

    public bool Contains(Relation actualPath) => N.E(Selected).Any(path => ReferenceEquals(path, actualPath));

    /// <summary>Reads vertex ordinals from actual public selectors. A chord
    /// selects its start; the path itself selects its terminal vertex.</summary>
    public IReadOnlySet<int> FixedLandmarks(Relation actualPath)
    {
        var result = new HashSet<int>(); if (!Contains(actualPath)) return result;
        var words = N.E((Relation)actualPath.Right);
        foreach (var port in N.E(FixedPorts).Cast<Relation>())
            if (port.Right is Relation pick && ReferenceEquals(pick.Left, actualPath))
            {
                var index = ReferenceEquals(pick.Right, actualPath) ? words.Length :
                    Array.FindIndex(words, word => ReferenceEquals(word, pick.Right));
                if (index >= 0) result.Add(index);
            }
        return result;
    }

    /// <summary>Requires the actual imported motif declaration, not a copied
    /// wrapper or a matching path list borrowed from another source.</summary>
    public static bool TryRead(QualitativeMotionFrame frame, Relation actualSourceSurface, Relation candidate,
        out OrnamentCurveSelection selection)
    {
        selection = null!;
        if (candidate is not { Left: Relation { Left: var actual, Right: Relation { Left: Relation requested, Right: Relation requestedPorts } },
            Right: Relation { Left: Relation selected, Right: Relation { Left: Relation complement,
                Right: Relation { Left: Relation ports, Right: Relation { Left: Relation blocked, Right: Relation unresolved } } } } } ||
            !ReferenceEquals(actual, actualSourceSurface) || !N.Read(blocked, out var denied) || denied.Length != 0 ||
            !N.Read(unresolved, out var pending) || pending.Length != 0) return false;
        var expected = new OrnamentCurveSelection(frame, actualSourceSurface, requested, requestedPorts);
        if (!expected.IsComplete || !SameEntries(selected, expected.Selected) || !SameEntries(complement, expected.Complement) ||
            !SameEntries(ports, expected.FixedPorts)) return false;
        selection = new(frame, actualSourceSurface, requested, requestedPorts, selected, complement, ports, blocked, unresolved, candidate);
        return true;
    }
    private OrnamentCurveSelection(QualitativeMotionFrame frame, Relation source, Relation requested, Relation requestedPorts,
        Relation selected, Relation complement, Relation ports, Relation blocked, Relation unresolved, Relation surface)
    {
        Frame = frame; SourceSurface = source; Requested = requested; RequestedFixedPorts = requestedPorts;
        Selected = selected; Complement = complement; FixedPorts = ports; Blocked = blocked; Unresolved = unresolved; Surface = surface;
    }
    private static bool TryProgram(QualitativeMotionFrame frame, Relation surface, out UnitArrangementProgram source)
    {
        source = null!;
        if (!ReferenceEquals(surface.Left, frame.Surface) ||
            surface.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
        source = new(frame, "native curve guide selection", paths, ports, closed, evidence); return true;
    }
    private static bool SameEntries(Relation first, Relation second) => N.Read(first, out var a) && N.Read(second, out var b) &&
        a.Length == b.Length && a.Zip(b).All(pair => ReferenceEquals(pair.First, pair.Second));
}
