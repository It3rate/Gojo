using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A selected finite boundary observation, not a topology rewrite. Cardinal
/// and equal-step diagonal line sections are read as shared-reach atoms. The
/// ordinary undirected-ink contract ignores traversal and subdivision, while
/// retaining multiplicity, original sections and zero-at-resolution phases.
/// Curves, other slopes and open scopes remain visible as unresolved evidence.
/// </summary>
public static class BoundaryExchangeRead
{
    // contract = (frame -> (source scope -> target scope)) ->
    //            (allowed endpoint orders -> (shared reach -> allowed step words))
    // allowed orders = (a -> b) -> Closed(a -> b [, b -> a])
    // atom = actual original section -> (native start word -> native end word)
    // result = contract -> Closed(matches, source-only, target-only, unresolved,
    //                            source-zero, target-zero, source-atoms, target-atoms)
    // match = actual source atom -> actual target atom
    // unresolved = owning scope -> unresolved scope/item
    // Atom order retains occurrence multiplicity even when the same section
    // appears twice. Zero-at-resolution sections retain their original sites;
    // lack of visible length is not deletion or proof of physical absence.

    public sealed class Read
    {
        public Relation Surface { get; }
        public Relation Matches { get; }
        public Relation SourceOnly { get; }
        public Relation TargetOnly { get; }
        public Relation Unresolved { get; }
        public Relation SourceZero { get; }
        public Relation TargetZero { get; }
        public Relation SourceAtoms { get; }
        public Relation TargetAtoms { get; }
        public bool Equivalent => E(SourceOnly).Length == 0 && E(TargetOnly).Length == 0 && E(Unresolved).Length == 0;

        internal Read(Relation surface, Relation[] fields)
        {
            Surface = surface; Matches = fields[0]; SourceOnly = fields[1]; TargetOnly = fields[2];
            Unresolved = fields[3]; SourceZero = fields[4]; TargetZero = fields[5];
            SourceAtoms = fields[6]; TargetAtoms = fields[7];
        }
    }

    private readonly record struct Key(Curve.Vector Start, Curve.Vector End);
    private sealed record Atoms(Relation[] Items, Relation[] Zero, Relation[] Pending);
    private static readonly (int X, int Y)[] Steps =
        [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    public static Read Evaluate(Curve.Frame frame, Relation source, Relation target, bool directed = false)
    {
        var a = new Placeholder(); var b = new Placeholder(); var forward = new Relation(a, b);
        var orders = new Relation(forward, directed ? S(forward) : S(forward, new Relation(b, a)));
        var resolution = new Relation(frame.Reach, S(Steps.Select(step => (IElement)frame.Word(step.X, step.Y)).ToArray()));
        var contract = new Relation(new Relation(frame.Surface, new Relation(source, target)), new Relation(orders, resolution));
        var left = Split(frame, source); var right = Split(frame, target);
        var candidates = new Dictionary<Key, Queue<Relation>>();
        foreach (var atom in right.Items)
        {
            Atom(frame, atom, directed, out var key);
            if (!candidates.TryGetValue(key, out var queue)) candidates.Add(key, queue = new());
            queue.Enqueue(atom);
        }
        var matches = new List<IElement>(); var sourceOnly = new List<IElement>();
        var used = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var atom in left.Items)
        {
            Atom(frame, atom, directed, out var key);
            if (!candidates.TryGetValue(key, out var queue) || queue.Count == 0) sourceOnly.Add(atom);
            else
            {
                var partner = queue.Dequeue(); used.Add(partner);
                matches.Add(new Relation(atom, partner));
            }
        }
        Relation[] fields = [S(matches.ToArray()), S(sourceOnly.ToArray()),
            S(right.Items.Where(atom => !used.Contains(atom)).Cast<IElement>().ToArray()),
            S(left.Pending.Concat(right.Pending).Cast<IElement>().ToArray()), S(left.Zero), S(right.Zero),
            S(left.Items), S(right.Items)];
        return new(new Relation(contract, S(fields)), fields);
    }

    /// <summary>
    /// Rechecks the selected grammar, every source subdivision and complete
    /// one-to-one accounting of atoms. Favourable summary fields cannot hide a
    /// duplicate, dropped section, changed contract or unresolved source.
    /// </summary>
    public static bool TryRead(Curve.Frame frame, Relation surface, out Read read)
    {
        read = null!;
        if (!Contract(frame, surface.Left, out var source, out var target, out var directed) ||
            surface.Right is not Relation body || !Closed(body, out var raw) || raw.Length != 8 ||
            raw.Any(item => item is not Relation)) return false;
        var fields = raw.Cast<Relation>().ToArray();
        var entries = new IElement[8][];
        for (var i = 0; i < fields.Length; i++) if (!Closed(fields[i], out entries[i])) return false;
        var left = Split(frame, source); var right = Split(frame, target);
        if (!SameAtoms(left.Items, entries[6]) || !SameAtoms(right.Items, entries[7]) ||
            !SameReferences(left.Zero, entries[4]) || !SameReferences(right.Zero, entries[5]) ||
            !SamePending(left.Pending.Concat(right.Pending).ToArray(), entries[3])) return false;
        var leftSet = entries[6].ToHashSet(ReferenceEqualityComparer.Instance);
        var rightSet = entries[7].ToHashSet(ReferenceEqualityComparer.Instance);
        if (leftSet.Count != entries[6].Length || rightSet.Count != entries[7].Length) return false;
        var seenLeft = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var seenRight = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var row in entries[0])
        {
            if (row is not Relation { Left: Relation first, Right: Relation second } ||
                !leftSet.Contains(first) || !rightSet.Contains(second) || !seenLeft.Add(first) || !seenRight.Add(second) ||
                !Atom(frame, first, directed, out var x) || !Atom(frame, second, directed, out var y) || x != y) return false;
        }
        foreach (var item in entries[1]) if (!leftSet.Contains(item) || !seenLeft.Add(item)) return false;
        foreach (var item in entries[2]) if (!rightSet.Contains(item) || !seenRight.Add(item)) return false;
        if (seenLeft.Count != leftSet.Count || seenRight.Count != rightSet.Count) return false;
        // Unmatched compatible atoms would contradict this complete selected
        // boundary comparison even though their inventory remained accounted.
        var unmatched = entries[1].Cast<Relation>().Select(atom => { Atom(frame, atom, directed, out var key); return key; }).ToHashSet();
        if (entries[2].Cast<Relation>().Any(atom => { Atom(frame, atom, directed, out var key); return unmatched.Contains(key); })) return false;
        read = new(surface, fields); return true;

        bool SameAtoms(Relation[] expected, IElement[] actual)
        {
            if (expected.Length != actual.Length) return false;
            for (var i = 0; i < expected.Length; i++)
                if (actual[i] is not Relation atom || !ReferenceEquals(atom.Left, expected[i].Left) ||
                    !Atom(frame, atom, true, out var x) || !Atom(frame, expected[i], true, out var y) || x != y) return false;
            return true;
        }
    }

    private static Atoms Split(Curve.Frame frame, Relation scope)
    {
        var atoms = new List<Relation>(); var zero = new List<Relation>(); var pending = new List<Relation>();
        if (!StructuralConstraint.TryReadScope(scope, out var items, out var start, out var end))
            return new([], [], [new Relation(scope, scope)]);
        if (!start.Equals(end)) pending.Add(new Relation(scope, scope));
        foreach (var item in items)
        {
            if (item is not Relation section) { pending.Add(new Relation(scope, item)); continue; }
            var read = frame.Read(section);
            if (!read.Complete || read.Controls.Length != 2) { pending.Add(new Relation(scope, section)); continue; }
            var from = read.Controls[0]; var to = read.Controls[1]; var delta = to - from;
            if (delta.Zero) { zero.Add(section); continue; }
            var dx = BigInteger.Abs(delta.X); var dy = BigInteger.Abs(delta.Y);
            // Only the explicitly published cardinal/equal-step diagonal
            // grammar is admitted. A finer or differently calibrated read
            // requires another construction, not an invented interpolation.
            if (!dx.IsZero && !dy.IsZero && dx != dy || !WordFits(from) || !WordFits(to) || BigInteger.Max(dx, dy) > int.MaxValue)
            { pending.Add(new Relation(scope, section)); continue; }
            var step = new Curve.Vector(delta.X.Sign, delta.Y.Sign);
            var count = (int)BigInteger.Max(dx, dy); var point = from;
            for (var i = 0; i < count; i++)
            {
                var next = point + step;
                atoms.Add(new Relation(section, new Relation(Word(frame, point), Word(frame, next))));
                point = next;
            }
        }
        return new(atoms.ToArray(), zero.ToArray(), pending.ToArray());
    }

    private static bool Contract(Curve.Frame frame, IElement surface, out Relation source, out Relation target, out bool directed)
    {
        source = target = null!; directed = false;
        if (surface is not Relation
            { Left: Relation { Left: var basis, Right: Relation { Left: Relation left, Right: Relation right } },
              Right: Relation { Left: Relation { Left: Relation { Left: Placeholder a, Right: Placeholder b }, Right: Relation orders },
                Right: Relation { Left: var reach, Right: Relation steps } } } ||
            !ReferenceEquals(basis, frame.Surface) || !ReferenceEquals(reach, frame.Reach) || ReferenceEquals(a, b) ||
            !Closed(orders, out var orderRows) || orderRows.Length is < 1 or > 2 ||
            orderRows[0] is not Relation first || !ReferenceEquals(first.Left, a) || !ReferenceEquals(first.Right, b) ||
            !Closed(steps, out var stepWords) || stepWords.Length != Steps.Length) return false;
        if (orderRows.Length == 2 && (orderRows[1] is not Relation second || !ReferenceEquals(second.Left, b) || !ReferenceEquals(second.Right, a))) return false;
        for (var i = 0; i < Steps.Length; i++)
            if (stepWords[i] is not Relation word || !frame.Vector(word, out var v) || v != new Curve.Vector(Steps[i].X, Steps[i].Y)) return false;
        source = left; target = right; directed = orderRows.Length == 1; return true;
    }

    private static bool Atom(Curve.Frame frame, Relation atom, bool directed, out Key key)
    {
        key = default;
        if (atom.Left is not Relation || atom.Right is not Relation { Left: Relation first, Right: Relation second } ||
            !frame.Vector(first, out var a) || !frame.Vector(second, out var b)) return false;
        if (!directed && (a.X > b.X || a.X == b.X && a.Y > b.Y)) (a, b) = (b, a);
        key = new(a, b); return true;
    }

    private static bool SameReferences(Relation[] expected, IElement[] actual) => expected.Length == actual.Length &&
        expected.Zip(actual).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static bool SamePending(Relation[] expected, IElement[] actual) => expected.Length == actual.Length &&
        expected.Zip(actual).All(pair => pair.Second is Relation row &&
            ReferenceEquals(pair.First.Left, row.Left) && ReferenceEquals(pair.First.Right, row.Right));
    private static bool WordFits(Curve.Vector v) => BigInteger.Abs(v.X) + BigInteger.Abs(v.Y) <= 65536;
    private static Relation Word(Curve.Frame frame, Curve.Vector v) => frame.Word((int)v.X, (int)v.Y);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] items) =>
        StructuralConstraint.TryReadScope(scope, out items, out var first, out var last) && first.Equals(last);
}
