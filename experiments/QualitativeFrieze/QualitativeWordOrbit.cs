using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A finite substitution orbit through an actual phase cycle. Return
/// means zero formal displacement difference for the supplied seed words;
/// it is not literal word identity, a calibrated angle, path-shape equality or
/// a theorem about every possible input word.</summary>
public sealed class QualitativeWordOrbit
{
    public QualitativePatternTransform Transform { get; }
    public Relation Seeds { get; }
    public Relation Cycle { get; }
    /// <summary>(actual seed -> actual successor row) -> (before -> substitution read).</summary>
    public Relation Steps { get; }
    /// <summary>(seed -> terminal word) -> (actual cycle -> zero difference read).</summary>
    public Relation Returns { get; }
    public Relation Mismatches { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0 && N.E(Mismatches).Length == 0;

    public QualitativeWordOrbit(QualitativePatternTransform transform, Relation seeds, Relation cycle)
    {
        Transform = transform; Seeds = seeds; Cycle = cycle;
        var steps = new List<IElement>(); var returns = new List<IElement>(); var mismatches = new List<IElement>();
        var blocked = new List<IElement>(); var unresolved = new List<IElement>();
        var validCycle = ReadCycle(cycle, out var successors);
        var validSeeds = ReadClosed(seeds, out var words);
        if (validSeeds && (words.Length == 0 || !N.Distinct(words))) { blocked.Add(seeds); validSeeds = false; }
        if (!ReferenceEquals(transform.Source, transform.Target)) blocked.Add(transform.Surface);
        if (validCycle && validSeeds && blocked.Count == 0)
            foreach (var item in words)
            {
                if (item is not Relation seed || !ReadClosed(seed, out var moves))
                { if (item is not Relation) blocked.Add(item); continue; }
                if (moves.Any(move => move is not Relation actual || !transform.Source.Direction(actual, out _, out _, out _)))
                { blocked.Add(seed); continue; }
                var current = seed; var complete = true;
                foreach (var successor in successors)
                {
                    var read = transform.Apply(current);
                    var step = new Relation(new Relation(seed, successor), new Relation(current, read)); steps.Add(step);
                    if (!transform.TryReadResult(read, out var output, out var denied, out var pending, out _))
                    { unresolved.Add(step); complete = false; break; }
                    if (N.E(denied).Length != 0) blocked.Add(new Relation(step, denied));
                    if (N.E(pending).Length != 0) unresolved.Add(new Relation(step, pending));
                    if (N.E(denied).Length != 0 || N.E(pending).Length != 0 || !N.Read(output, out _))
                    { complete = false; break; }
                    current = output;
                }
                if (!complete) continue;
                var difference = transform.Source.Reduce(current, UnitArrangementRead.Inverse(transform.Source, seed));
                var result = new Relation(new Relation(seed, current), new Relation(cycle, difference));
                if (!transform.Source.TryReadResult(difference, out var remainder, out _, out var pendingReturn)) unresolved.Add(result);
                else if (N.E(pendingReturn).Length != 0 || !N.Read(remainder, out var terms)) unresolved.Add(result);
                else if (terms.Length == 0) returns.Add(result);
                else mismatches.Add(result);
            }
        Steps = N.S(steps.ToArray()); Returns = N.S(returns.ToArray()); Mismatches = N.S(mismatches.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(unresolved.ToArray());
        Surface = new(new Relation(transform.Surface, new Relation(seeds, cycle)),
            new Relation(Steps, new Relation(Returns, new Relation(Mismatches, new Relation(Blocked, Unresolved)))));

        bool ReadClosed(Relation scope, out IElement[] entries)
        {
            if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail))
            { entries = []; blocked.Add(scope); return false; }
            if (!seam.Equals(tail)) { unresolved.Add(scope); return false; }
            return true;
        }
        bool ReadCycle(Relation candidate, out Relation[] actualSuccessors)
        {
            actualSuccessors = [];
            if (candidate is not { Left: Relation phaseScope, Right: Relation linkScope }) { blocked.Add(candidate); return false; }
            var havePhases = ReadClosed(phaseScope, out var phases); var haveLinks = ReadClosed(linkScope, out var links);
            if (!havePhases || !haveLinks) return false;
            if (phases.Length == 0 || !N.Distinct(phases) || phases.Any(phase => phase is not Placeholder) || links.Length != phases.Length)
            { blocked.Add(candidate); return false; }
            var rows = new List<Relation>();
            for (var i = 0; i < phases.Length; i++)
            {
                var matches = links.OfType<Relation>().Where(row => N.Same(row.Left, phases[i]) &&
                    N.Same(row.Right, phases[(i + 1) % phases.Length])).ToArray();
                if (matches.Length != 1) { blocked.Add(candidate); return false; }
                rows.Add(matches[0]);
            }
            actualSuccessors = rows.ToArray(); return true;
        }
    }

    public bool TryReadStep(Relation row, out Relation seed, out Relation successor,
        out Relation before, out Relation after, out Relation substitution)
    {
        seed = successor = before = after = substitution = null!;
        if (!N.E(Steps).Any(actual => ReferenceEquals(actual, row)) ||
            row is not { Left: Relation { Left: Relation original, Right: Relation transition },
                Right: Relation { Left: Relation input, Right: Relation read } } ||
            read.Left is not Relation request || !ReferenceEquals(request.Right, input) ||
            !Transform.TryReadComplete(read, out var output)) return false;
        seed = original; successor = transition; before = input; after = output; substitution = read; return true;
    }

    public bool TryReadReturn(Relation row, out Relation seed, out Relation terminal, out Relation difference)
    {
        seed = terminal = difference = null!;
        if (!N.E(Returns).Any(actual => ReferenceEquals(actual, row)) ||
            row is not { Left: Relation { Left: Relation original, Right: Relation last },
                Right: Relation { Left: var premise, Right: Relation read } } || !ReferenceEquals(premise, Cycle) ||
            !UnitArrangementRead.IsZero(Transform.Source, read)) return false;
        seed = original; terminal = last; difference = read; return true;
    }
}
