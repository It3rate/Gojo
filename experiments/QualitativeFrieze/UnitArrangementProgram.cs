using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An authored finite path program. Names never select execution.
/// paths = Closed(start-word -> Closed(chord words)); ports = Closed(role ->
/// (actual path -> actual chord)). A path itself in the chord position selects
/// its terminal cursor. ClosedPaths explicitly selects contour closure claims.</summary>
public sealed class UnitArrangementProgram
{
    public string Name { get; }
    public QualitativeMotionFrame Frame { get; }
    public Relation Paths { get; }
    public Relation Ports { get; }
    public Relation ClosedPaths { get; }
    public IElement Evidence { get; }
    public Relation Surface { get; }

    public UnitArrangementProgram(QualitativeMotionFrame frame, string name, Relation paths,
        Relation ports, Relation? closedPaths = null, IElement? evidence = null)
    {
        Frame = frame; Name = name; Paths = paths; Ports = ports;
        ClosedPaths = closedPaths ?? S(); Evidence = evidence ?? S();
        Surface = new(frame.Surface, new Relation(new Relation(paths, ports), new Relation(ClosedPaths, Evidence)));
    }

    // An exact native reader preserves the declaration identity shared by
    // ladder sources, results and imported program contracts.
    internal UnitArrangementProgram(QualitativeMotionFrame frame, string name, Relation paths,
        Relation ports, Relation closedPaths, IElement evidence, Relation actualSurface)
        : this(frame, name, paths, ports, closedPaths, evidence) => Surface = actualSurface;

    /// <summary>Applies the actual substitution to every start/chord and keeps
    /// path, word and port correspondences. Incomplete transforms are refused;
    /// no unchanged-word fallback supplies missing geometric participation.</summary>
    public UnitArrangementProgram Transform(string name, QualitativePatternTransform transform)
    {
        if (!Complete()) throw new ArgumentException("A transform requires a complete program with unambiguous landmark selectors.");
        if (!ReferenceEquals(Frame, transform.Source)) throw new ArgumentException("Transform source must be the program's frame.", nameof(transform));
        var reads = new List<IElement>(); var pathMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var wordMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        foreach (var path in E(Paths).Cast<Relation>())
        {
            var start = Map((Relation)path.Left);
            var words = E((Relation)path.Right).Cast<Relation>().Select(word =>
            {
                if (wordMap.TryGetValue(word, out var prior)) return (IElement)prior;
                var mapped = Map(word); wordMap.Add(word, mapped); return (IElement)mapped;
            }).ToArray();
            pathMap.Add(path, new Relation(start, S(words)));
        }
        var ports = E(Ports).Cast<Relation>().Select(port =>
        {
            var pick = (Relation)port.Right; var oldPath = (Relation)pick.Left;
            return (IElement)new Relation(port.Left, new Relation(pathMap[oldPath],
                ReferenceEquals(pick.Right, oldPath) ? pathMap[oldPath] : wordMap[(Relation)pick.Right]));
        }).ToArray();
        var correspondence = new Relation(S(pathMap.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
            S(wordMap.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()));
        return new(transform.Target, name, S(pathMap.Values.Cast<IElement>().ToArray()), S(ports),
            S(E(ClosedPaths).Cast<Relation>().Select(path => (IElement)pathMap[path]).ToArray()),
            new Relation(Surface, new Relation(transform.Surface, new Relation(correspondence, S(reads.ToArray())))));

        Relation Map(Relation source)
        {
            var read = transform.Apply(source); reads.Add(read);
            if (!transform.TryReadComplete(read, out var mapped)) throw new ArgumentException("A unit transform must supply every published path word.", nameof(transform));
            return mapped;
        }
    }

    /// <summary>Each visible replacement row maps one actual coarse chord to
    /// an ordered fine chain with the same formal net displacement. Intermediate
    /// detail may bend outward; preservation of straightness is not claimed.</summary>
    public UnitArrangementProgram Refine(string name, Relation replacements)
    {
        if (!Complete()) throw new ArgumentException("A refinement requires a complete program with unambiguous landmark selectors.");
        var proof = new List<IElement>(); var pathMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var firstMap = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        if (!UnitArrangementRead.ClosedEntries(replacements, out var rows)) throw new ArgumentException("Refinement rows must be closed.", nameof(replacements));
        var used = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var path in E(Paths).Cast<Relation>())
        {
            var fineWords = new List<IElement>();
            foreach (var word in E((Relation)path.Right).Cast<Relation>())
            {
                var selected = rows.OfType<Relation>().Where(row => ReferenceEquals(row.Left, word)).ToArray();
                if (selected.Length > 1) throw new ArgumentException("A coarse chord needs one selected refinement.", nameof(replacements));
                var chain = selected.Length == 0 ? S(word) : selected[0].Right as Relation;
                if (chain is null) throw new ArgumentException("A refinement needs a closed chord chain.", nameof(replacements));
                if (!UnitArrangementRead.ClosedEntries(chain, out var fine) || fine.Length == 0 || fine.Any(item => item is not Relation))
                    throw new ArgumentException("A refinement needs a nonempty closed chord chain.", nameof(replacements));
                var flattened = S(fine.Cast<Relation>().SelectMany(E).ToArray());
                if (!Word(flattened)) throw new ArgumentException("Refined chords must belong to the same exposed frame.", nameof(replacements));
                var read = Frame.Reduce(flattened, UnitArrangementRead.Inverse(Frame, word));
                if (!UnitArrangementRead.IsZero(Frame, read)) throw new ArgumentException("Refinement must preserve the selected chord's endpoint displacement.", nameof(replacements));
                proof.Add(new Relation(word, new Relation(chain, read))); fineWords.AddRange(fine); firstMap.TryAdd(word, (Relation)fine[0]);
                if (selected.Length != 0) used.Add(selected[0]);
            }
            pathMap.Add(path, new Relation(path.Left, S(fineWords.ToArray())));
        }
        if (used.Count != rows.Length) throw new ArgumentException("A refinement row must select an actual coarse chord.", nameof(replacements));
        var ports = E(Ports).Cast<Relation>().Select(port =>
        {
            var pick = (Relation)port.Right; var oldPath = (Relation)pick.Left;
            return (IElement)new Relation(port.Left, new Relation(pathMap[oldPath],
                ReferenceEquals(pick.Right, oldPath) ? pathMap[oldPath] : firstMap[(Relation)pick.Right]));
        }).ToArray();
        return new(Frame, name, S(pathMap.Values.Cast<IElement>().ToArray()), S(ports),
            S(E(ClosedPaths).Cast<Relation>().Select(path => (IElement)pathMap[path]).ToArray()),
            new Relation(Surface, new Relation(replacements, S(proof.ToArray()))));
    }

    /// <summary>Preflight precedes actuator mutation. Reused chord identities
    /// are valid across paths; a selector within a path must identify exactly
    /// one occurrence. A repeated path needs its own path occurrence identity.</summary>
    internal bool Complete()
    {
        if (!UnitArrangementRead.ClosedEntries(Paths, out var pathEntries) || pathEntries.Length == 0 ||
            !UnitArrangementRead.ClosedEntries(Ports, out var portEntries) ||
            !UnitArrangementRead.ClosedEntries(ClosedPaths, out var closedEntries)) return false;
        var paths = pathEntries.OfType<Relation>().ToArray();
        if (paths.Length != pathEntries.Length || paths.Distinct(ReferenceEqualityComparer.Instance).Count() != paths.Length ||
            closedEntries.Any(path => !paths.Any(actual => ReferenceEquals(actual, path)))) return false;
        if (paths.Any(path => !Word(path.Left) || path.Right is not Relation words || !UnitArrangementRead.ClosedEntries(words, out var chords) ||
            chords.Length == 0 || chords.Any(chord => !Word(chord)))) return false;
        if (portEntries.Any(entry => entry is not Relation { Right: Relation pick } ||
            pick.Left is not Relation path || !paths.Any(actual => ReferenceEquals(actual, path)) ||
            !ReferenceEquals(pick.Right, path) && E((Relation)path.Right).Count(word => ReferenceEquals(word, pick.Right)) != 1) ||
            portEntries.Cast<Relation>().Select(port => port.Left).Distinct(ReferenceEqualityComparer.Instance).Count() != portEntries.Length) return false;
        return closedEntries.Cast<Relation>().All(path => UnitArrangementRead.IsZero(Frame,
            Frame.Reduce(S(), S(E((Relation)path.Right).Cast<Relation>().SelectMany(E).ToArray()))));
    }

    private bool Word(IElement candidate) => candidate is Relation word && UnitArrangementRead.ClosedEntries(word, out var moves) &&
        moves.All(item => item is Relation move && Frame.Direction(move, out _, out _, out _));

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

internal static class UnitArrangementRead
{
    internal static bool ClosedEntries(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    internal static Relation Inverse(QualitativeMotionFrame frame, Relation word) => StructuralConstraint.Closed(
        FiniteLaw.Entries(word).Reverse().Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(
            FiniteLaw.Entries(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    internal static bool IsZero(QualitativeMotionFrame frame, Relation read) =>
        frame.TryReadResult(read, out var remaining, out _, out var unresolved) &&
        UnitArrangementRead.ClosedEntries(remaining, out var terms) && terms.Length == 0 && FiniteLaw.Entries(unresolved).Length == 0;
}
