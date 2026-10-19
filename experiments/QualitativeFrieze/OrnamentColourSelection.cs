using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Four ordered, disjoint selections of actual native paths and a
/// carried complement. A presentation may bind these groups to a palette;
/// neither RGB values nor screen-coordinate shape guesses define membership.</summary>
public sealed class OrnamentColourSelection
{
    // Surface = (actual source -> requested groups)
    //        -> (selected groups -> (carried paths -> (blocked -> unresolved)))
    public QualitativeMotionFrame Frame { get; }
    public Relation SourceSurface { get; }
    public Relation Requested { get; }
    public Relation Groups { get; }
    public Relation Complement { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentColourSelection(UnitArrangementProgram source, Relation requestedGroups)
        : this(source.Frame, source.Surface, requestedGroups) { }

    public OrnamentColourSelection(QualitativeMotionFrame frame, Relation actualSourceSurface, Relation requestedGroups)
    {
        Frame = frame; SourceSurface = actualSourceSurface; Requested = requestedGroups;
        Groups = N.S(); Complement = N.S();
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        if (!TryProgram(frame, actualSourceSurface, out var source)) blocked.Add(actualSourceSurface);
        else if (!source.Complete()) pending.Add(actualSourceSurface);
        else
        {
            var closed = N.Read(requestedGroups, out var groups);
            if (!closed) pending.Add(requestedGroups);
            if (closed && (groups.Length != 4 || groups.Any(group => group is not Relation))) blocked.Add(requestedGroups);
            var selected = new List<IElement>();
            foreach (var group in groups.OfType<Relation>())
            {
                if (!N.Read(group, out var paths)) pending.Add(group);
                selected.AddRange(paths);
            }
            if (!N.Distinct(selected.ToArray()) || selected.Any(path => !N.E(source.Paths).Any(actual => ReferenceEquals(actual, path))))
                blocked.Add(new Relation(requestedGroups, source.Paths));
            if (pending.Count == 0 && blocked.Count == 0)
            {
                Groups = requestedGroups;
                Complement = N.S(N.E(source.Paths).Where(path => !selected.Any(actual => ReferenceEquals(actual, path))).ToArray());
            }
        }
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(actualSourceSurface, requestedGroups),
            new Relation(Groups, new Relation(Complement, new Relation(Blocked, Unresolved))));
    }

    /// <summary>Returns the selected palette role, or -1 for carried/foreign
    /// paths. Selection is by actual source identity, never path similarity.</summary>
    public int GroupIndex(Relation actualPath)
    {
        var groups = N.E(Groups);
        for (var index = 0; index < groups.Length; index++)
            if (groups[index] is Relation group && N.E(group).Any(path => ReferenceEquals(path, actualPath))) return index;
        return -1;
    }

    /// <summary>Reads only this exact complete selection shape against the
    /// actual imported source declaration. Forged complements are refused.</summary>
    public static bool TryRead(QualitativeMotionFrame frame, Relation actualSourceSurface, Relation candidate,
        out OrnamentColourSelection selection)
    {
        selection = null!;
        if (candidate is not { Left: Relation { Left: var actual, Right: Relation requested },
            Right: Relation { Left: Relation groups, Right: Relation { Left: Relation complement,
                Right: Relation { Left: Relation blocked, Right: Relation unresolved } } } } ||
            !ReferenceEquals(actual, actualSourceSurface) || !N.Read(blocked, out var denied) || denied.Length != 0 ||
            !N.Read(unresolved, out var pending) || pending.Length != 0) return false;
        var expected = new OrnamentColourSelection(frame, actualSourceSurface, requested);
        if (!expected.IsComplete || !SameGroups(groups, expected.Groups) || !SameEntries(complement, expected.Complement)) return false;
        selection = new(frame, actualSourceSurface, requested, groups, complement, blocked, unresolved, candidate);
        return true;
    }

    private OrnamentColourSelection(QualitativeMotionFrame frame, Relation source, Relation requested, Relation groups,
        Relation complement, Relation blocked, Relation unresolved, Relation surface)
    {
        Frame = frame; SourceSurface = source; Requested = requested; Groups = groups; Complement = complement;
        Blocked = blocked; Unresolved = unresolved; Surface = surface;
    }

    private static bool TryProgram(QualitativeMotionFrame frame, Relation surface, out UnitArrangementProgram source)
    {
        source = null!;
        if (!ReferenceEquals(surface.Left, frame.Surface) ||
            surface.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
        source = new(frame, "native palette-group selection", paths, ports, closed, evidence); return true;
    }

    private static bool SameGroups(Relation first, Relation second) => N.Read(first, out var a) && N.Read(second, out var b) &&
        a.Length == b.Length && a.Zip(b).All(pair => pair.First is Relation x && pair.Second is Relation y && SameEntries(x, y));
    private static bool SameEntries(Relation first, Relation second) => N.Read(first, out var a) && N.Read(second, out var b) &&
        a.Length == b.Length && a.Zip(b).All(pair => ReferenceEquals(pair.First, pair.Second));
}
