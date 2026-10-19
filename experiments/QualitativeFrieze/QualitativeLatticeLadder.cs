using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Experimental construction history over native unmeasured path
/// programs. Earlier programs stay complete participants, rather than labels
/// attached to the final drawing. This is the relation/placeholder adaptation
/// of the structural ladder spine; it does not create a numeric carrier.</summary>
public sealed class QualitativeLatticeLadder
{
    public QualitativeLatticeRung Current { get; }
    public IReadOnlyList<QualitativeLatticeRung> Rungs { get; }
    public Relation Surface => Current.Surface;

    private QualitativeLatticeLadder(QualitativeLatticeRung current, IEnumerable<QualitativeLatticeRung> earlier)
    { Current = current; Rungs = Array.AsReadOnly(earlier.Append(current).ToArray()); }

    public static QualitativeLatticeLadder Begin(UnitArrangementProgram program)
    {
        RequireNative(program);
        var paths = S(E(program.Paths).Select(path => (IElement)new Relation(path, S(path))).ToArray());
        var ports = S(E(program.Ports).Select(port => (IElement)new Relation(port, S(port))).ToArray());
        return new(new(null, program, program.Paths, S(), S(), new(paths, ports), S(), S(), program.Evidence), []);
    }

    /// <summary>Reads the exact native ladder contract after transport. It
    /// follows only the explicit previous-rung relation, replays each connected
    /// native construction, and verifies aliases against its declared inputs.
    /// Every retained program must use the supplied shared motion frame.</summary>
    public static bool TryRead(QualitativeMotionFrame actualFrame, Relation surface, out QualitativeLatticeLadder ladder)
    {
        ladder = null!;
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        try { RequireNative(surface); return Read(surface, out ladder); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidCastException or OverflowException)
        { ladder = null!; return false; }

        bool Read(Relation candidate, out QualitativeLatticeLadder result)
        {
            result = null!;
            if (!active.Add(candidate) || !Parts(candidate, out var source, out var focus, out var carried, out var replacement,
                    out var output, out var correspondence, out var blocked, out var open, out var provenance) ||
                !Program(output, out var program)) return false;
            QualitativeLatticeLadder expected;
            QualitativeLatticeLadder? previous = null;
            var anchors = new List<IElement>();
            if (UnitArrangementRead.ClosedEntries(source, out var noSource) && noSource.Length == 0)
            {
                expected = Begin(program); anchors.Add(program.Surface);
            }
            else
            {
                if (provenance is not Relation { Left: Relation prior, Right: Relation native } ||
                    !Read(prior, out previous) || !ReferenceEquals(source, previous.Current.Program.Surface) ||
                    !ReferenceEquals(program.Evidence, native) && !ReferenceEquals(program.Surface, source)) return false;
                anchors.Add(prior);
                var unit = previous.Current.Program;
                if (native.Left is not Relation actualSource || !ReferenceEquals(actualSource, source) || native.Right is not Relation instruction)
                    return false;
                if (instruction.Left is Relation { Left: Relation frames, Right: Relation declaration } transformSurface &&
                    ReferenceEquals(frames.Left, unit.Frame.Surface) && ReferenceEquals(frames.Right, actualFrame.Surface) &&
                    declaration.Left is Relation substitutions)
                {
                    anchors.Add(substitutions); anchors.Add(declaration.Right);
                    expected = previous.Transform(program.Name,
                        new QualitativePatternTransform(unit.Frame, actualFrame, substitutions, declaration.Right));
                }
                else if (instruction.Left is Relation patch && patch.Left is Relation selection &&
                    ReferenceEquals(selection.Left, focus) && ReferenceEquals(selection.Right, carried) &&
                    ReferenceEquals(patch.Right, replacement) && instruction.Right is Relation edit &&
                    ReferenceEquals(edit.Left, correspondence) && replacement.Left is Relation introduced &&
                    replacement.Right is Relation { Left: Relation ports, Right: Relation closed })
                {
                    anchors.Add(focus); anchors.Add(introduced); anchors.Add(ports); anchors.Add(closed); anchors.Add(edit.Right);
                    expected = previous.ReplacePaths(program.Name, focus, introduced, ports, closed, edit.Right);
                }
                else if (ReferenceEquals(instruction.Left, replacement))
                {
                    anchors.Add(replacement);
                    expected = previous.Refine(program.Name, replacement);
                }
                else return false;
            }
            if (!AlphaSame(expected.Surface, candidate, anchors)) return false;
            var rung = new QualitativeLatticeRung(previous?.Current.Program, program, focus, carried, replacement,
                correspondence, blocked, open, provenance, candidate);
            result = new(rung, previous?.Rungs ?? []); active.Remove(candidate); return true;
        }

        bool Program(Relation candidate, out UnitArrangementProgram program)
        {
            program = null!;
            if (!ReferenceEquals(candidate.Left, actualFrame.Surface) || candidate.Right is not Relation
                { Left: Relation { Left: Relation paths, Right: Relation ports },
                    Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
            program = new(actualFrame, "retained native rung", paths, ports, closed, evidence, candidate);
            return program.Complete();
        }
    }

    private static bool Parts(Relation surface, out Relation source, out Relation focus, out Relation carried,
        out Relation replacement, out Relation result, out Relation correspondence, out Relation blocked,
        out Relation open, out IElement provenance)
    {
        source = focus = carried = replacement = result = correspondence = blocked = open = null!; provenance = null!;
        if (surface is not { Left: Relation { Left: Relation ss, Right: Relation ff },
                Right: Relation { Left: Relation cc, Right: Relation { Left: Relation rr,
                    Right: Relation { Left: Relation unit, Right: Relation { Left: Relation pairs,
                        Right: Relation { Left: Relation denied, Right: Relation { Left: Relation pending, Right: var history } } } } } } }) return false;
        source = ss; focus = ff; carried = cc; replacement = rr; result = unit; correspondence = pairs;
        blocked = denied; open = pending; provenance = history; return true;
    }

    // Compare a replay with a retained surface while fixing every declared
    // input identity. Fresh scopes and receipt relations may be alpha-renamed;
    // source participants and imported instruction rows may not be substituted.
    private static bool AlphaSame(IElement expected, IElement actual, IEnumerable<IElement> anchors)
    {
        var forward = new Dictionary<IElement, IElement>(FieldNative.Participants.Instance);
        var reverse = new Dictionary<IElement, IElement>(FieldNative.Participants.Instance);
        var pending = new Stack<(IElement Expected, IElement Actual)>();
        foreach (var anchor in anchors)
        {
            var participants = new Stack<IElement>(); participants.Push(anchor);
            while (participants.TryPop(out var item))
            {
                if (forward.ContainsKey(item)) continue;
                forward.Add(item, item); reverse.Add(item, item);
                if (item is Placeholder slot)
                { forward.TryAdd(slot.Dual(), slot.Dual()); reverse.TryAdd(slot.Dual(), slot.Dual()); }
                if (item is Relation relation) { participants.Push(relation.Left); participants.Push(relation.Right); }
            }
        }
        pending.Push((expected, actual));
        while (pending.TryPop(out var pair))
        {
            if (forward.TryGetValue(pair.Expected, out var mapped)) { if (!Same(mapped, pair.Actual)) return false; continue; }
            if (reverse.ContainsKey(pair.Actual)) return false;
            if (pair.Expected is Placeholder a && pair.Actual is Placeholder b && a.IsInverse == b.IsInverse)
            {
                forward.Add(a, b); reverse.Add(b, a);
                if (forward.TryGetValue(a.Dual(), out var opposite) && !Same(opposite, b.Dual())) return false;
                forward.TryAdd(a.Dual(), b.Dual()); reverse.TryAdd(b.Dual(), a.Dual()); continue;
            }
            if (pair.Expected is not Relation left || pair.Actual is not Relation right) return false;
            forward.Add(left, right); reverse.Add(right, left);
            pending.Push((left.Right, right.Right)); pending.Push((left.Left, right.Left));
        }
        return true;
    }

    /// <summary>Executes the native word substitution. The retained receipts
    /// identify actual input moves, selected substitution rows and mapped words;
    /// this establishes correspondence, without asserting preserved angles.</summary>
    public QualitativeLatticeLadder Transform(string name, QualitativePatternTransform transform)
    {
        RequireNative(transform.Surface);
        var source = Current.Program;
        var result = source.Transform(name, transform);
        var evidence = (Relation)result.Evidence;
        var transition = (Relation)evidence.Right;
        var proof = (Relation)transition.Right;
        return Append(result, source.Paths, S(), result.Paths, (Relation)proof.Left, S(), S(), evidence);
    }

    /// <summary>Native refinement preserves each selected chord's formal net
    /// displacement. It does not claim that introduced bends are straight, or
    /// that the current path has every property of the earlier path.</summary>
    public QualitativeLatticeLadder Refine(string name, Relation replacements)
    {
        RequireNative(replacements);
        var source = Current.Program;
        var result = source.Refine(name, replacements);
        var rows = E(replacements).Cast<Relation>().ToArray();
        var selected = new List<IElement>(); var carried = new List<IElement>();
        foreach (var path in E(source.Paths).Cast<Relation>())
        {
            var words = E((Relation)path.Right);
            selected.Add(new Relation(path, S(words.Where(word => rows.Any(row => ReferenceEquals(row.Left, word))).ToArray())));
            carried.Add(new Relation(path, S(words.Where(word => !rows.Any(row => ReferenceEquals(row.Left, word))).ToArray())));
        }
        var pathPairs = S(E(source.Paths).Zip(E(result.Paths), (a, b) => (IElement)new Relation(a, b)).ToArray());
        var nativeProof = (Relation)((Relation)result.Evidence).Right;
        return Append(result, S(selected.ToArray()), S(carried.ToArray()), replacements,
            new(pathPairs, nativeProof.Right), S(), S(), result.Evidence);
    }

    /// <summary>Replaces an actual source focus and carries the remaining path
    /// identities. Ports and closedPaths are the complete result contracts.
    /// Unlike Refine, an edit makes no endpoint-preservation promise. Removed
    /// publication roles appear as old-port -> Closed() correspondence rows.</summary>
    public QualitativeLatticeLadder ReplacePaths(string name, Relation selectedActualPaths,
        Relation introducedPaths, Relation ports, Relation closedPaths, IElement? evidence = null)
    {
        RequireNative(new Relation(selectedActualPaths, new Relation(introducedPaths, new Relation(ports, new Relation(closedPaths, evidence ?? S())))));
        var source = Current.Program;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var selected = Read(selectedActualPaths); var introduced = Read(introducedPaths);
        Read(ports); Read(closedPaths);
        var oldPaths = E(source.Paths);
        foreach (var path in selected)
            if (path is not Relation || !oldPaths.Any(actual => ReferenceEquals(actual, path))) blocked.Add(new Relation(path, source.Paths));
        if (!Distinct(selected)) blocked.Add(selectedActualPaths);
        if (!Distinct(introduced) || introduced.Any(path => path is not Relation || oldPaths.Any(old => ReferenceEquals(old, path))))
            blocked.Add(introducedPaths);
        var carried = S(oldPaths.Where(path => !selected.Any(focus => ReferenceEquals(focus, path))).ToArray());
        var replacement = new Relation(introducedPaths, new Relation(ports, closedPaths));
        var pathPairs = S(E(carried).Select(path => (IElement)new Relation(path, S(path)))
            .Append(new Relation(selectedActualPaths, introducedPaths)).ToArray());
        // A role match is a publication correspondence, not geometric equality.
        var portPairs = S(E(source.Ports).Cast<Relation>().Select(old => (IElement)new Relation(old,
            S(E(ports).OfType<Relation>().Where(next => Same(old.Left, next.Left)).Cast<IElement>().ToArray()))).ToArray());
        var correspondence = new Relation(pathPairs, portPairs);
        var patch = new Relation(new Relation(selectedActualPaths, carried), replacement);
        var proof = new Relation(source.Surface, new Relation(patch, new Relation(correspondence, evidence ?? S())));
        var result = source;
        if (blocked.Count == 0 && pending.Count == 0)
        {
            var candidate = new UnitArrangementProgram(source.Frame, name,
                S([.. E(carried), .. introduced]), ports, closedPaths, proof);
            if (candidate.Complete()) result = candidate;
            else blocked.Add(new Relation(candidate.Surface, patch));
        }
        return Append(result, selectedActualPaths, carried, replacement, correspondence,
            S(blocked.ToArray()), S(pending.ToArray()), proof);

        IElement[] Read(Relation scope)
        {
            if (!StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail))
            { blocked.Add(scope); return []; }
            if (!seam.Equals(tail)) pending.Add(scope);
            return entries;
        }
    }

    /// <summary>Reads only actual retained rung identities. Equal-looking
    /// copied or foreign surfaces do not acquire membership in this history.</summary>
    public bool TryReadRung(Relation actualSurface, out QualitativeLatticeRung rung)
    {
        rung = Rungs.SingleOrDefault(candidate => ReferenceEquals(candidate.Surface, actualSurface))!;
        return rung is not null;
    }

    /// <summary>History-only read: the result is the actual earlier program.
    /// Its presence never establishes that Current has its shape properties.</summary>
    public bool TryReadAncestor(Relation actualProgramSurface, out UnitArrangementProgram ancestor, out Relation witness)
    {
        var retained = Rungs.Take(Rungs.Count - 1).LastOrDefault(rung => ReferenceEquals(rung.Program.Surface, actualProgramSurface));
        ancestor = retained?.Program!;
        witness = new(new Relation(Surface, actualProgramSurface), new Relation(
            retained is null ? StructuralConstraint.Open() : S(retained.Program.Surface),
            retained is null ? StructuralConstraint.Open(actualProgramSurface) : S(retained.Surface)));
        return retained is not null;
    }

    /// <summary>Captures only the current native frame, paths and contracts.
    /// Every placeholder and relation is independently identified; construction
    /// evidence is deliberately absent from the standalone result. The loss
    /// receipt stays outside that world and can be retained by its caller.</summary>
    public QualitativeLatticeCrystallization Crystallize(string name)
    {
        var source = Current.Program;
        RequireNative(source);
        if (!QualitativeMotionFrameRead.TryReadStandard(source.Frame.Surface, out var frame))
            throw new ArgumentException("Crystallization requires the complete declared motion frame.");
        var copies = new Dictionary<IElement, IElement>(FieldNative.Participants.Instance);
        foreach (var pair in E(frame.Correspondence).Cast<Relation>()) copies.Add(pair.Left, pair.Right);
        var paths = (Relation)Copy(source.Paths); var ports = (Relation)Copy(source.Ports); var closed = (Relation)Copy(source.ClosedPaths);
        var result = new UnitArrangementProgram(frame.Target, name, paths, ports, closed);
        var standalone = Begin(result);
        var correspondence = S(copies.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray());
        var forgotten = S(Rungs.Select(rung => (IElement)rung.Surface).Append(source.Evidence).ToArray());
        var loss = new Relation(new Relation(Surface, standalone.Surface), new Relation(correspondence, forgotten));
        return new(standalone, correspondence, forgotten, loss);

        IElement Copy(IElement item)
        {
            if (copies.TryGetValue(item, out var existing)) return existing;
            IElement fresh;
            if (item is Placeholder slot)
            {
                if (copies.TryGetValue(slot.Dual(), out var opposite)) fresh = ((Placeholder)opposite).Dual();
                else { var basis = new Placeholder(); fresh = slot.IsInverse ? basis.Dual() : basis; }
            }
            else if (item is Relation relation) fresh = new Relation(Copy(relation.Left), Copy(relation.Right));
            else throw new ArgumentException("Only relation/placeholder programs can crystallize here.");
            copies.Add(item, fresh); return fresh;
        }
    }

    private QualitativeLatticeLadder Append(UnitArrangementProgram result, Relation focus, Relation carried,
        Relation replacement, Relation correspondence, Relation blocked, Relation open, IElement evidence) =>
        new(new(Current.Program, result, focus, carried, replacement, correspondence, blocked, open,
            new Relation(Current.Surface, evidence)), Rungs);

    private static void RequireNative(UnitArrangementProgram program)
    {
        RequireNative(program.Surface);
        if (!program.Complete()) throw new ArgumentException("A lattice rung requires a complete native path program.", nameof(program));
    }
    private static void RequireNative(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
            else if (item is not Placeholder) throw new ArgumentException("Lattice construction requires only relations and placeholders.", nameof(source));
        }
    }
    private static bool Distinct(IElement[] entries) => entries.Distinct(ReferenceEqualityComparer.Instance).Count() == entries.Length;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>Compiled view of the exact connected rung below. Captions, names
/// and the helper type do not supply an ancestral grid or a current-shape claim.</summary>
public sealed class QualitativeLatticeRung
{
    // ((source -> focus) -> (carried -> (replacement -> (result ->
    //   (correspondence -> (blocked -> (open -> provenance)))))))
    public UnitArrangementProgram? Source { get; }
    public UnitArrangementProgram Program { get; }
    public Relation Focus { get; }
    public Relation Carried { get; }
    public Relation Replacement { get; }
    public Relation Correspondence { get; }
    public Relation Blocked { get; }
    public Relation Open { get; }
    public IElement Provenance { get; }
    public Relation Surface { get; }
    public bool IsComplete => E(Blocked).Length == 0 && E(Open).Length == 0;

    internal QualitativeLatticeRung(UnitArrangementProgram? source, UnitArrangementProgram program,
        Relation focus, Relation carried, Relation replacement, Relation correspondence,
        Relation blocked, Relation open, IElement provenance, Relation? actualSurface = null)
    {
        Source = source; Program = program; Focus = focus; Carried = carried; Replacement = replacement;
        Correspondence = correspondence; Blocked = blocked; Open = open; Provenance = provenance;
        Surface = actualSurface ?? new(new Relation(source?.Surface ?? StructuralConstraint.Closed(), focus), new Relation(carried,
            new Relation(replacement, new Relation(program.Surface, new Relation(correspondence,
                new Relation(blocked, new Relation(open, provenance)))))));
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

public sealed record QualitativeLatticeCrystallization(QualitativeLatticeLadder Standalone,
    Relation Correspondence, Relation Forgotten, Relation LossWitness);
