using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded experiments in construction-dependent continuation. The
/// captions are presentation only: actual path selectors, endpoint reductions,
/// replacement scopes and retained/captured relations carry the evidence.</summary>
public sealed class ConstructionContextStudy
{
    public sealed record Case(string Id, string Name, string Description, IReadOnlyList<Variant> Variants);
    public sealed record Variant(string Id, string Name, string Description, IReadOnlyList<Step> Steps);
    public sealed record Step(string Id, string Name, string Description, QualitativeLatticeLadder Ladder,
        IReadOnlyList<Relation> Receipts, OrnamentProgramRun? RepeatRun = null)
    {
        public UnitArrangementProgram Program => Ladder.Current.Program;
        public Relation Surface => new(Ladder.Surface, new Relation(S(Receipts.Cast<IElement>().ToArray()), RepeatRun?.Surface ?? S()));
    }

    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }

    private ConstructionContextStudy(IReadOnlyList<Case> cases)
    {
        Cases = cases;
        Surface = S(cases.Select(c => (IElement)S(c.Variants.Select(v => (IElement)S(v.Steps.Select(s => (IElement)s.Surface).ToArray())).ToArray())).ToArray());
    }

    public static ConstructionContextStudy Build()
    {
        var a = new LatticeLadderAuthor();
        var r = a.Word(3, 0); var u = a.Word(0, 3); var l = a.Word(-3, 0); var d = a.Word(0, -3);
        var words = new[] { r, u, l, d };
        var loop = LatticeLadderAuthor.Path(S(), words);
        var square = Unit(a, "one closed ordered path", [loop], [loop]);
        var original = QualitativeLatticeLadder.Begin(square);
        var splitPaths = Split(a, loop, out var prefixes);
        var split = Unit(a, "four independently reset side paths", splitPaths);
        var independent = QualitativeLatticeLadder.Begin(split);
        var support = new ConstructionSegmentSupport(square, split, PairSegments(loop, splitPaths));
        var sameSupport = support.Evaluate();
        if (!support.TryReadComplete(sameSupport)) throw new InvalidOperationException("The two supplied constructions must expose the same ordered segment support.");

        // This experiment uses the same path-replacement contract on both
        // constructions. It does not claim that a closed path lacks other
        // contracts: endpoint-preserving chord refinement is exercised below.
        var shiftedTop = LatticeLadderAuthor.Path(a.Advance((Relation)splitPaths[2].Left, a.Word(0, 1), out var shiftRead), l);
        var movedPorts = LatticeLadderAuthor.Ports(splitPaths[0], splitPaths[1], shiftedTop, splitPaths[3]);
        var wholeRefusal = original.ReplacePaths("side is not a whole-path focus", S(splitPaths[2]), S(shiftedTop), movedPorts, S());
        var sideMove = independent.ReplacePaths("move the exposed upper rail", S(splitPaths[2]), S(shiftedTop), movedPorts, S(), shiftRead);

        var pathCase = new Case("C01", "Equal support, different replacement scopes",
            "Both drawings have exactly corresponding formal segment support. One construction has a closed ordered path; the other resets before each side. A whole-path replacement can select only actual paths in its supplied source. This is a difference in exposed focus granularity, not an impossibility theorem about other encodings.",
            [new("C01-A", "One closed path", "The foreign side selector cannot become a member of the closed-path source merely by having the same drawing.",
                [T("C01-A1", "Closed path", "One path, four chords and one explicit closure claim.", original, sameSupport, prefixes),
                 T("C01-A2", "Side replacement refused", "The requested side belongs to the alternative construction. The native blocked scope retains the refused focus; the original path is carried unchanged.", wholeRefusal)]),
             new("C01-B", "Four exposed side paths", "The same replacement question can select an actual side here. No contour-closure promise is supplied for the four-path construction.",
                [T("C01-B1", "Same segment support", "Four reset paths retain distinct cursor/site identities despite their formal endpoint agreement.", independent, sameSupport, prefixes),
                 T("C01-B2", "Upper rail detached", "The actual upper path moves outward. The resulting opening is intentional and no closed-interior claim is invented.", sideMove, shiftRead)])]);

        var switched = original.ReplacePaths("withdraw continuous traversal; expose four paths", S(loop), S(splitPaths), split.Ports, S(), sameSupport);
        var switchedMove = switched.ReplacePaths("move upper rail in the new context", S(splitPaths[2]), S(shiftedTop), movedPorts, S(), shiftRead);
        var taller = LatticeLadderAuthor.Path(S(), r, a.Word(0, 4), l, a.Word(0, -4));
        var restored = switchedMove.ReplacePaths("renegotiate side extents and bind a closed path",
            switchedMove.Current.Program.Paths, S(taller), LatticeLadderAuthor.Ports(taller), S(taller),
            new Relation(switchedMove.Current.Program.Ports, new Relation(splitPaths[1], splitPaths[3])));
        var switchCase = new Case("C02", "Reformulation changes the next available focus",
            "A ladder may withdraw a traversal/closure contract while retaining exact support correspondence. Moving the newly exposed rail leaves a gap. A subsequent explicit replacement extends both side reaches and restores a declared closed walk; this is a proposed construction, not an automatic solver.",
            [new("C02-A", "Split, move, reconnect", "Construction history retains the withdrawn path and the scope of each replacement.",
                [T("C02-A1", "Before reformulation", "The closed square is the actual initial rung.", original),
                 T("C02-A2", "Same drawing, new scope", "The whole path is replaced by four independent sides. Its closure role is withdrawn; ordered support is checked separately.", switched, sameSupport, prefixes),
                 T("C02-A3", "Expose the gap", "One actual rail moves. The other three path identities are carried, and no closure is asserted.", switchedMove, shiftRead),
                 T("C02-A4", "New rectangular closure", "The side extents are explicitly revised and the four-side traversal is rebound as one closed path. Earlier square history is retained, not asserted as the current shape.", restored)])]);

        var chains = new[]
        {
            S(a.Word(1, 0), a.Word(0, -1), a.Word(1, 0), a.Word(0, 1), a.Word(1, 0)),
            S(a.Word(0, 1), a.Word(1, 0), a.Word(0, 1), a.Word(-1, 0), a.Word(0, 1)),
            S(a.Word(-1, 0), a.Word(0, 1), a.Word(-1, 0), a.Word(0, -1), a.Word(-1, 0)),
            S(a.Word(0, -1), a.Word(-1, 0), a.Word(0, -1), a.Word(1, 0), a.Word(0, -1))
        };
        var oneNotch = original.Refine("one endpoint-preserving outward notch", S(new Relation(r, chains[0])));
        var allNotches = oneNotch.Refine("repeat the relative bend on the other sides",
            S(new Relation(u, chains[1]), new Relation(l, chains[2]), new Relation(d, chains[3])));
        var notchedPath = (Relation)E(allNotches.Current.Program.Paths).Single();
        var repeatedPaths = new List<Relation>(); var translations = new List<IElement>();
        foreach (var displacement in new[] { a.Word(0, 0), a.Word(6, 0), a.Word(0, 6), a.Word(6, 6) })
        {
            repeatedPaths.Add(a.Translate(notchedPath, displacement, out var translation)); translations.Add(translation);
        }
        var repeated = allNotches.ReplacePaths("four translated occurrences of the same detailed path", S(notchedPath),
            S(repeatedPaths.Cast<IElement>().ToArray()), LatticeLadderAuthor.Ports(repeatedPaths.ToArray()),
            S(repeatedPaths.Cast<IElement>().ToArray()), S(translations.ToArray()));
        // The field definition separately retains actual phase cycles, shared
        // motif selection and recurrent step words. It is not inferred from
        // four visually similar path copies.
        var field = a.Compile("context-notch", "notched repeat", "One reusable notched contour", "construction-context study",
            allNotches.Current.Program, a.Word(6, 0), a.Word(0, 6), 2, 2);
        var run = field.Program.Generate(field.Bindings, field.Window);
        if (!run.IsComplete) throw new InvalidOperationException("The declared four-unit repeat must settle before presentation.");
        var notchCase = new Case("C03", "Chord detail becomes a reusable construction unit",
            "A closed-path context does support local chord refinement: every replacement chain must retain its selected chord's formal endpoint displacement. The coarse square survives as history while the current shape acquires new bends. A further layer repeats the complete detailed unit.",
            [new("C03-A", "Preserve ends, change contour", "Shared reach participation and cancellation receipts justify the endpoint promises without a measured length.",
                [T("C03-A1", "Coarse square", "Each coarse chord has three repeated participations in one unmeasured reach.", original),
                 T("C03-A2", "One notched side", "Five fine chords replace one actual coarse chord. Their net displacement is exactly the old chord; straightness is deliberately not preserved.", oneNotch),
                 T("C03-A3", "Four notched sides", "The same relative outward/forward/inward construction is supplied on each side; all closure reductions remain readable.", allNotches),
                 new("C03-A4", "Repeat the detailed unit", "Four translated occurrences share the actual detailed chord scope. The additional native ornament run retains its selected motif, two phase cycles, step words and four accepted unit executions.", repeated,
                     [S(translations.ToArray()), run.Surface], run)])]);

        var capture = allNotches.Crystallize("independent captured notched contour");
        allNotches.TryReadAncestor(square.Surface, out _, out var knownAncestor);
        capture.Standalone.TryReadAncestor(square.Surface, out _, out var unknownAncestor);
        var oldFocusRefused = capture.Standalone.ReplacePaths("old path is not a selector in the captured world",
            S(notchedPath), S(), capture.Standalone.Current.Program.Ports, capture.Standalone.Current.Program.ClosedPaths);
        var historyCase = new Case("C04", "Construction memory is a useful optional context",
            "The current notched contour can retain the actual square construction or be captured into an independently identified native world. The loss receipt stays outside the standalone result. Visible agreement does not transfer old selector identities, and a remembered square never proves that the current fine chain is straight.",
            [new("C04-A", "Keep construction memory", "The earlier coarse program remains directly addressable under a history question.",
                [T("C04-A1", "Current detailed contour", "The notched program retains every actual refinement rung.", allNotches),
                 T("C04-A2", "Read the earlier square", "This witness answers an ancestry question with the actual earlier program. It does not replace the current drawing or assert current straight sides.", allNotches, knownAncestor)]),
             new("C04-B", "Independent capture", "The same current relation shape survives, with fresh identities and no internal construction history.",
                [T("C04-B1", "Captured current structure", "The standalone program has one rung and an empty evidence scope. Exact source-to-copy correspondence is available only in the external loss receipt.", capture.Standalone, capture.Correspondence, capture.LossWitness),
                 T("C04-B2", "Old ancestry remains open", "The old square is not part of the standalone history. Its query stays unresolved rather than being recovered from visual resemblance.", capture.Standalone, unknownAncestor),
                 T("C04-B3", "Old focus refused", "A source path from before capture cannot select a path in the fresh world. All captured path and closure contracts remain carried.", oldFocusRefused)])]);

        return new([pathCase, switchCase, notchCase, historyCase]);
    }

    private static Step T(string id, string name, string description, QualitativeLatticeLadder ladder, params Relation[] receipts) =>
        new(id, name, description, ladder, Array.AsReadOnly(receipts));
    private static UnitArrangementProgram Unit(LatticeLadderAuthor author, string name, Relation[] paths, Relation[]? closed = null) =>
        new(author.Frame, name, S(paths), LatticeLadderAuthor.Ports(paths), S(closed ?? []));
    private static Relation[] Split(LatticeLadderAuthor author, Relation path, out Relation prefixes)
    {
        var reads = new List<IElement>(); var result = new List<Relation>(); var start = (Relation)path.Left;
        foreach (var chord in E((Relation)path.Right).Cast<Relation>())
        {
            result.Add(LatticeLadderAuthor.Path(start, chord));
            start = author.Advance(start, chord, out var read); reads.Add(read);
        }
        prefixes = S(reads.ToArray()); return result.ToArray();
    }
    private static Relation PairSegments(Relation path, Relation[] sides) => S(E((Relation)path.Right).Zip(sides,
        (word, side) => (IElement)new Relation(new Relation(path, word), new Relation(side, word))).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>Exact ordered chord support in one explicitly shared formal motion
/// frame. Selector pairs name actual path/chord occurrences. Zero displacement
/// compares their start/end words; it never merges site identities or asserts
/// an equal metric length for independently calibrated axes.</summary>
public sealed class ConstructionSegmentSupport
{
    // surface = (source-program -> target-program) -> actual-selector-pairs
    // receipt = surface -> (matched -> (blocked -> unresolved))
    // matched row = actual-pair -> (start-equality-reduction -> end-equality-reduction)
    public UnitArrangementProgram Source { get; }
    public UnitArrangementProgram Target { get; }
    public Relation Pairings { get; }
    public Relation Surface { get; }

    public ConstructionSegmentSupport(UnitArrangementProgram source, UnitArrangementProgram target, Relation pairings)
    {
        Source = source; Target = target; Pairings = pairings; Surface = new(new Relation(source.Surface, target.Surface), pairings);
        if (Participants(Surface).Any(item => item is not (Relation or Placeholder)))
            throw new ArgumentException("This construction-context support read requires only relation/placeholder participation.");
    }

    public Relation Evaluate()
    {
        var yes = new List<IElement>(); var no = new List<IElement>(); var pending = new List<IElement>();
        if (!ReferenceEquals(Source.Frame, Target.Frame)) no.Add(new Relation(Source.Frame.Surface, Target.Frame.Surface));
        if (!Source.Complete()) pending.Add(Source.Surface);
        if (!Target.Complete()) pending.Add(Target.Surface);
        if (!StructuralConstraint.TryReadScope(Pairings, out var pairs, out var seam, out var tail))
        { pairs = []; pending.Add(Pairings); }
        else if (!seam.Equals(tail)) pending.Add(Pairings);
        var selectedSource = new List<(Relation Path, Relation Word)>(); var selectedTarget = new List<(Relation Path, Relation Word)>();
        if (no.Count == 0 && pending.Count == 0)
        {
            foreach (var entry in pairs)
            {
                if (entry is not Relation { Left: Relation from, Right: Relation to } pair ||
                    !Select(Source, from, out var sourcePath, out var sourceWord, out var sourceStart, out var sourceEnd) ||
                    !Select(Target, to, out var targetPath, out var targetWord, out var targetStart, out var targetEnd))
                { no.Add(entry); continue; }
                var sourceSelector = (sourcePath, sourceWord); var targetSelector = (targetPath, targetWord);
                if (Has(selectedSource, sourceSelector) || Has(selectedTarget, targetSelector)) { no.Add(pair); continue; }
                selectedSource.Add(sourceSelector); selectedTarget.Add(targetSelector);
                var start = Source.Frame.Reduce(sourceStart, UnitArrangementRead.Inverse(Source.Frame, targetStart));
                var end = Source.Frame.Reduce(sourceEnd, UnitArrangementRead.Inverse(Source.Frame, targetEnd));
                var evidence = new Relation(pair, new Relation(start, end));
                if (UnitArrangementRead.IsZero(Source.Frame, start) && UnitArrangementRead.IsZero(Source.Frame, end)) yes.Add(evidence);
                else no.Add(evidence);
            }
            foreach (var missing in Occurrences(Source).Where(item => !Has(selectedSource, item))) pending.Add(new Relation(Source.Surface, new Relation(missing.Path, missing.Word)));
            foreach (var missing in Occurrences(Target).Where(item => !Has(selectedTarget, item))) pending.Add(new Relation(Target.Surface, new Relation(missing.Path, missing.Word)));
        }
        return new(Surface, new Relation(S(yes.ToArray()), new Relation(S(no.ToArray()), S(pending.ToArray()))));
    }

    /// <summary>Re-evaluates the supplied native selectors and authenticates
    /// both endpoint reductions. A favourable copied selector cannot establish
    /// membership or substitute an endpoint witness for another source.</summary>
    public bool TryReadResult(Relation receipt, out Relation matched, out Relation blocked, out Relation unresolved)
    {
        matched = blocked = unresolved = null!;
        if (!Parts(receipt, out var actualYes, out var actualNo, out var actualPending)) return false;
        var fresh = Evaluate(); Parts(fresh, out var yes, out var no, out var pending);
        var anchors = Participants(Surface);
        if (!Evidence(receipt.Right, fresh.Right, anchors)) return false;
        matched = actualYes; blocked = actualNo; unresolved = actualPending; return true;
    }
    public bool TryReadComplete(Relation receipt) => TryReadResult(receipt, out _, out var blocked, out var pending) && E(blocked).Length == 0 && E(pending).Length == 0;

    private bool Parts(Relation receipt, out Relation yes, out Relation no, out Relation pending)
    {
        yes = no = pending = null!;
        if (!ReferenceEquals(receipt.Left, Surface) || receipt.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b, Right: Relation c } } ||
            !UnitArrangementRead.ClosedEntries(a, out _) || !UnitArrangementRead.ClosedEntries(b, out _) || !UnitArrangementRead.ClosedEntries(c, out _)) return false;
        yes = a; no = b; pending = c; return true;
    }
    private static bool Select(UnitArrangementProgram program, Relation selector, out Relation path, out Relation word, out Relation start, out Relation end)
    {
        path = word = start = end = null!;
        if (selector.Left is not Relation p || selector.Right is not Relation w || !E(program.Paths).Any(item => ReferenceEquals(item, p))) return false;
        var chords = E((Relation)p.Right).Cast<Relation>().ToArray();
        if (chords.Count(item => ReferenceEquals(item, w)) != 1) return false;
        var before = chords.TakeWhile(item => !ReferenceEquals(item, w)).SelectMany(E).ToArray();
        var startRead = program.Frame.Reduce((Relation)p.Left, S(before));
        if (!program.Frame.TryReadResult(startRead, out start, out _, out var pending) || E(pending).Length != 0) return false;
        var endRead = program.Frame.Reduce(start, w);
        if (!program.Frame.TryReadResult(endRead, out end, out _, out pending) || E(pending).Length != 0) return false;
        path = p; word = w; return true;
    }
    private static IEnumerable<(Relation Path, Relation Word)> Occurrences(UnitArrangementProgram program) =>
        E(program.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right).Cast<Relation>().Select(word => (path, word)));
    private static bool Has(IEnumerable<(Relation Path, Relation Word)> entries, (Relation Path, Relation Word) actual) =>
        entries.Any(entry => ReferenceEquals(entry.Path, actual.Path) && ReferenceEquals(entry.Word, actual.Word));
    private static bool Evidence(IElement actual, IElement expected, HashSet<IElement> anchors)
    {
        var forward = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var reverse = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        return Match(actual, expected);
        bool Match(IElement a, IElement b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (anchors.Contains(a) || anchors.Contains(b)) return false;
            if (forward.TryGetValue(a, out var known)) return ReferenceEquals(known, b);
            if (reverse.ContainsKey(b)) return false;
            if (a is Placeholder x && b is Placeholder y)
            {
                if (x.IsInverse != y.IsInverse) return false;
                forward.Add(a, b); reverse.Add(b, a);
                if (forward.TryGetValue(x.Dual(), out var opposite) && !ReferenceEquals(opposite, y.Dual())) return false;
                if (reverse.TryGetValue(y.Dual(), out var original) && !ReferenceEquals(original, x.Dual())) return false;
                forward.TryAdd(x.Dual(), y.Dual()); reverse.TryAdd(y.Dual(), x.Dual()); return true;
            }
            if (a is not Relation left || b is not Relation right) return false;
            forward.Add(a, b); reverse.Add(b, a);
            return Match(left.Left, right.Left) && Match(left.Right, right.Right);
        }
    }
    private static HashSet<IElement> Participants(IElement source)
    {
        var result = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var item)) if (result.Add(item) && item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        return result;
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
