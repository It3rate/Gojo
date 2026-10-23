using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Finite unmeasured substrate occurrences, observed strand events
/// and exact polynomial-boundary correspondence. Events select actual native
/// participants; coordinate coincidence never supplies a weld. The declared
/// independent U/V basis is not a metric, infinite embedding or fill proof.</summary>
public static class OrnamentSubstrate
{
    // lattice = ((frame -> (origin -> (U -> V))) ->
    //            ((Closed(U phases) -> Closed(V phases)) -> (U links -> V links))) -> Closed(nodes)
    // node = (site -> position word) -> ((U phase -> V phase) -> (H read -> (V read -> agreement)))
    // visit = ((lattice -> actual node) -> envelope) -> (start difference -> end difference)
    // cross = (lattice -> actual node) -> ((first envelope -> second envelope) -> crossing read)
    // pair request = ((frame -> (source curve -> target curve)) -> (transform -> translation)) ->
    //                (target curve -> selected target curve)
    // boundary pair = request -> Closed((source control -> target control) ->
    //                                  (substitution -> (displacement -> difference)))
    // difference = (from -> to) -> (Closed((move -> opposed move) -> actual opposite row) -> sum read)

    public sealed record Landmark(IElement Site, Relation Position, IElement UPhase,
        IElement VPhase, Relation Surface);
    public sealed record Lattice(Curve.Frame Frame, Relation U, Relation V, Relation Origin,
        IReadOnlyList<IElement> ColumnPhases, IReadOnlyList<IElement> RowPhases,
        IReadOnlyList<Landmark> Landmarks, bool Complete, Relation Surface);
    public sealed record VisitRead(bool? AtStart, bool? AtEnd, bool JoinedToLandmark, Relation Surface);
    public sealed record CrossRead(bool? Crosses, bool Joined, Relation Surface);
    public sealed record BoundaryPairRead(Relation Source, Relation Target, Relation SelectedTarget,
        Relation Translation, bool? Compatible, bool Welded, Relation Surface);

    /// <summary>columns/rows are finite landmark counts. Create(...,5,4)
    /// exposes At(column 0..4,row 0..3). Both routes to an interior occurrence
    /// retain their additive receipts and a checked zero difference.</summary>
    public static Lattice Create(Curve.Frame frame, Relation u, Relation v, Relation origin, int columns, int rows)
    {
        if (columns is < 1 or > 128 || rows is < 1 or > 128 || columns * rows > 4096)
            throw new ArgumentOutOfRangeException(nameof(columns));
        var up = Enumerable.Range(0, columns).Select(_ => (IElement)new Placeholder()).ToArray();
        var vp = Enumerable.Range(0, rows).Select(_ => (IElement)new Placeholder()).ToArray();
        var nodes = new List<IElement>(); var words = new Relation[rows, columns];
        for (var r = 0; r < rows; r++) for (var c = 0; c < columns; c++)
        {
            Relation h = S(), vertical = S(), agreement = S(); Relation position;
            if (c > 0) h = frame.Motion.Reduce(words[r, c - 1], u);
            if (r > 0) vertical = frame.Motion.Reduce(words[r - 1, c], v);
            position = c > 0 ? Result(h) : r > 0 ? Result(vertical) : origin;
            if (r > 0 && c > 0) agreement = Difference(frame, Result(vertical), position);
            words[r, c] = position;
            nodes.Add(new Relation(new Relation(new Placeholder(), position), new Relation(new Relation(up[c], vp[r]),
                new Relation(h, new Relation(vertical, agreement)))));
        }
        var basis = new Relation(frame.Surface, new Relation(origin, new Relation(u, v)));
        var phases = new Relation(new Relation(S(up), S(vp)), new Relation(Links(up), Links(vp)));
        return Read(frame, new Relation(new Relation(basis, phases), S(nodes)));
        Relation Result(Relation receipt) => frame.Motion.TryReadResult(receipt, out var result, out _, out _) ? result : StructuralConstraint.Open(receipt);
    }

    public static Lattice Read(Curve.Frame frame, Relation surface)
    {
        Lattice Open() => new(frame, StructuralConstraint.Open(), StructuralConstraint.Open(), StructuralConstraint.Open(), [], [], [], false, surface);
        if (surface is not { Left: Relation { Left: Relation { Left: var selected,
                Right: Relation { Left: Relation origin, Right: Relation { Left: Relation u, Right: Relation v } } },
                Right: Relation { Left: Relation { Left: Relation columnScope, Right: Relation rowScope },
                    Right: Relation { Left: Relation hLinks, Right: Relation vLinks } } }, Right: Relation nodeScope } ||
            !Same(selected, frame.Surface) || !Entries(columnScope, out var columns) || !Entries(rowScope, out var rows) ||
            columns.Length == 0 || rows.Length == 0 || columns.Length > 128 || rows.Length > 128 || columns.Length * rows.Length > 4096 ||
            !Distinct(columns.Concat(rows)) || !LinkRead(hLinks, columns) || !LinkRead(vLinks, rows) ||
            !Entries(nodeScope, out var items) || items.Length != columns.Length * rows.Length) return Open();
        var nodes = new List<Landmark>(); var valid = frame.Vector(origin, out _) && frame.Vector(u, out var uv) && frame.Vector(v, out var vv) &&
            !Curve.Vector.Cross(uv, vv).IsZero;
        var sites = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < items.Length; i++)
        {
            var r = i / columns.Length; var c = i % columns.Length;
            if (items[i] is not Relation { Left: Relation { Left: var site, Right: Relation position },
                    Right: Relation { Left: Relation { Left: var up, Right: var vp },
                        Right: Relation { Left: Relation h, Right: Relation { Left: Relation vertical, Right: Relation agreement } } } } node ||
                !Same(up, columns[c]) || !Same(vp, rows[r]) || !sites.Add(site)) return Open();
            valid &= frame.Vector(position, out _);
            if (r == 0 && c == 0) valid &= Same(position, origin) && Empty(h) && Empty(vertical) && Empty(agreement);
            else
            {
                if (c > 0) valid &= Sum(frame, h, nodes[i - 1].Position, u, out var resultH) && Same(resultH, position);
                else valid &= Empty(h);
                if (r > 0)
                {
                    valid &= Sum(frame, vertical, nodes[i - columns.Length].Position, v, out var resultV);
                    if (c == 0) valid &= Same(resultV, position);
                    else valid &= resultV is not null && ReadDifference(frame, agreement, resultV, position) == true;
                }
                else valid &= Empty(vertical);
                if (r == 0 || c == 0) valid &= Empty(agreement);
            }
            nodes.Add(new(site, position, up, vp, node));
        }
        return new(frame, u, v, origin, columns, rows, nodes, valid, surface);
    }

    public static Landmark At(Lattice lattice, int column, int row)
    {
        var read = Read(lattice.Frame, lattice.Surface);
        if (column < 0 || row < 0 || column >= read.ColumnPhases.Count || row >= read.RowPhases.Count)
            throw new ArgumentOutOfRangeException(nameof(column));
        return read.Landmarks[row * read.ColumnPhases.Count + column];
    }

    /// <summary>Endpoint incidence, useful for curved spans with known joins.
    /// This does not pretend to solve a curved interior intersection.</summary>
    public static VisitRead Visit(Lattice lattice, Landmark landmark, Relation envelope)
    {
        var read = Band.ReadEnvelope(lattice.Frame, envelope).Sweep;
        var body = read.Complete ? new Relation(Difference(lattice.Frame, Word(read.Spine, true), landmark.Position),
            Difference(lattice.Frame, Word(read.Spine, false), landmark.Position)) : StructuralConstraint.Open(envelope);
        return ReadVisit(lattice.Frame, new Relation(new Relation(new Relation(lattice.Surface, landmark.Surface), envelope), body));
    }

    public static VisitRead ReadVisit(Curve.Frame frame, Relation surface)
    {
        if (surface is not { Left: Relation { Left: Relation { Left: Relation latticeSurface, Right: Relation node }, Right: Relation envelope },
                Right: Relation { Left: Relation start, Right: Relation end } } ||
            !Select(frame, latticeSurface, node, out var landmark)) return new(null, null, false, surface);
        var band = Band.ReadEnvelope(frame, envelope).Sweep;
        if (!band.Complete) return new(null, null, false, surface);
        var a = ReadDifference(frame, start, Word(band.Spine, true), landmark.Position);
        var b = ReadDifference(frame, end, Word(band.Spine, false), landmark.Position);
        return new(a, b, a == true && Same(band.Spine.Start, landmark.Site) || b == true && Same(band.Spine.End, landmark.Site), surface);
    }

    public static CrossRead CrossEvent(Lattice lattice, Landmark landmark, Relation firstEnvelope, Relation secondEnvelope)
    {
        var a = Band.ReadEnvelope(lattice.Frame, firstEnvelope).Sweep; var b = Band.ReadEnvelope(lattice.Frame, secondEnvelope).Sweep;
        var crossing = Band.Crossing(lattice.Frame, a.Spine.Section, b.Spine.Section, landmark.Position);
        var surface = new Relation(new Relation(lattice.Surface, landmark.Surface),
            new Relation(new Relation(firstEnvelope, secondEnvelope), crossing.Surface));
        return ReadCrossEvent(lattice.Frame, surface);
    }

    public static CrossRead ReadCrossEvent(Curve.Frame frame, Relation surface)
    {
        if (surface is not { Left: Relation { Left: Relation latticeSurface, Right: Relation node },
                Right: Relation { Left: Relation { Left: Relation first, Right: Relation second }, Right: Relation crossing } } ||
            !Select(frame, latticeSurface, node, out var landmark)) return new(null, false, surface);
        var a = Band.ReadEnvelope(frame, first).Sweep; var b = Band.ReadEnvelope(frame, second).Sweep;
        if (!a.Complete || !b.Complete) return new(null, false, surface);
        var read = Band.ReadCrossing(frame, crossing);
        if (!Same(read.First, a.Spine.Section) || !Same(read.Second, b.Spine.Section) || !Same(read.Locus, landmark.Position)) return new(null, false, surface);
        var joined = new[] { (a.Spine.Start, a.Spine.Controls[0]), (a.Spine.End, a.Spine.Controls[^1]) }.Any(x =>
            new[] { (b.Spine.Start, b.Spine.Controls[0]), (b.Spine.End, b.Spine.Controls[^1]) }.Any(y =>
                Same(x.Item1, y.Item1) && frame.Vector(landmark.Position, out var at) && x.Item2 == at && y.Item2 == at));
        return new(read.Crosses, joined, surface);
    }

    /// <summary>Checks every ordered control under the actual substitution and
    /// translation. Equal endpoints alone are insufficient. A reversed target
    /// is an actual reverse section retained by the selection relation.</summary>
    public static BoundaryPairRead PairBoundary(Curve.Frame frame, Relation source, Relation target,
        QualitativePatternTransform transform, Relation translation, bool reverseTarget = false)
    {
        var selected = reverseTarget ? frame.Reverse(target) : target;
        var a = frame.Read(source); var b = frame.Read(selected); var rows = new List<IElement>();
        if (a.Complete && b.Complete && Entries(a.ControlWords, out var ac) && Entries(b.ControlWords, out var bc))
            for (var i = 0; i < Math.Min(ac.Length, bc.Length); i++)
            {
                var action = transform.Apply((Relation)ac[i]);
                var image = transform.TryReadComplete(action, out var output) ? output : StructuralConstraint.Open(action);
                var sum = frame.Motion.Reduce(image, translation);
                var displaced = frame.Motion.TryReadResult(sum, out var result, out _, out _) ? result : StructuralConstraint.Open(sum);
                rows.Add(new Relation(new Relation(ac[i], bc[i]), new Relation(action, new Relation(sum, Difference(frame, displaced, (Relation)bc[i])))));
            }
        var request = new Relation(new Relation(new Relation(frame.Surface, new Relation(source, target)), new Relation(transform.Surface, translation)),
            new Relation(target, selected));
        return ReadBoundaryPair(frame, transform, new Relation(request, S(rows)));
    }

    public static BoundaryPairRead ReadBoundaryPair(Curve.Frame frame, QualitativePatternTransform transform, Relation surface)
    {
        BoundaryPairRead Open(Relation? source = null, Relation? target = null, Relation? selected = null, Relation? shift = null) =>
            new(source ?? StructuralConstraint.Open(), target ?? StructuralConstraint.Open(), selected ?? StructuralConstraint.Open(),
                shift ?? StructuralConstraint.Open(), null, false, surface);
        if (surface is not { Left: Relation { Left: Relation { Left: Relation { Left: var selectedFrame,
                    Right: Relation { Left: Relation source, Right: Relation target } },
                Right: Relation { Left: var map, Right: Relation shift } }, Right: Relation { Left: var targetSource, Right: Relation selected } },
                Right: Relation rowScope } || !Same(selectedFrame, frame.Surface) || !Same(map, transform.Surface) || !Same(targetSource, target) ||
            !ReferenceEquals(transform.Source, frame.Motion) || !ReferenceEquals(transform.Target, frame.Motion) || !frame.Vector(shift, out _) ||
            !Entries(rowScope, out var rows)) return Open();
        var a = frame.Read(source); var original = frame.Read(target); var b = frame.Read(selected);
        if (!a.Complete || !original.Complete || !b.Complete || !Selection(original, b) ||
            !Entries(a.ControlWords, out var ac) || !Entries(b.ControlWords, out var bc) || ac.Length != bc.Length || rows.Length != ac.Length)
            return Open(source, target, selected, shift);
        bool? compatible = true;
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i] is not Relation { Left: Relation pair, Right: Relation { Left: Relation action,
                    Right: Relation { Left: Relation sum, Right: Relation difference } } } ||
                !Same(pair.Left, ac[i]) || !Same(pair.Right, bc[i]) || action.Left is not Relation request || !Same(request.Right, ac[i]) ||
                !transform.TryReadComplete(action, out var image) || !Sum(frame, sum, image, shift, out var displaced))
                return Open(source, target, selected, shift);
            var equal = ReadDifference(frame, difference, displaced, (Relation)bc[i]);
            if (equal is null) return Open(source, target, selected, shift);
            if (equal == false) compatible = false;
        }
        var welded = compatible == true && Same(a.Start, b.Start) && Same(a.End, b.End) &&
            a.Controls[0] == b.Controls[0] && a.Controls[^1] == b.Controls[^1];
        return new(source, target, selected, shift, compatible, welded, surface);
    }

    private static bool Selection(Curve.Read target, Curve.Read selected)
    {
        if (Same(target.Section, selected.Section)) return true;
        if (!Entries(target.ControlWords, out var tc) || !Entries(selected.ControlWords, out var sc)) return false;
        return Same(target.Start, selected.End) && Same(target.End, selected.Start) &&
            tc.Length == sc.Length && tc.Zip(sc.Reverse()).All(p => Same(p.First, p.Second));
    }
    private static bool Select(Curve.Frame frame, Relation surface, Relation node, out Landmark landmark)
    {
        landmark = null!; var lattice = Read(frame, surface);
        if (!lattice.Complete) return false;
        landmark = lattice.Landmarks.FirstOrDefault(n => Same(n.Surface, node))!; return landmark is not null;
    }
    private static Relation Word(Curve.Read read, bool start) => (Relation)Items(read.ControlWords)[start ? 0 : read.Controls.Length - 1];
    private static bool Sum(Curve.Frame frame, Relation receipt, Relation before, Relation incoming, out Relation result)
    {
        result = null!;
        return receipt.Left is Relation { Right: Relation inputs } && Same(inputs.Left, before) && Same(inputs.Right, incoming) &&
            frame.Motion.TryReadResult(receipt, out result, out _, out _) && frame.Vector(result, out _);
    }
    internal static Relation Difference(Curve.Frame frame, Relation from, Relation to)
    {
        var inverse = new List<IElement>(); var proofs = new List<IElement>();
        foreach (var item in Items(from))
        {
            if (item is not Relation move || !frame.Motion.Direction(move, out _, out _, out var reach))
            { inverse.Add(StructuralConstraint.Open(item)); continue; }
            var opposite = Items(frame.Motion.Opposites).OfType<Relation>().First(row => Same(row.Left, move.Left));
            var term = QualitativeMotionFrame.Move(opposite.Right, reach); inverse.Add(term);
            proofs.Add(new Relation(new Relation(move, term), opposite));
        }
        var receipt = frame.Motion.Reduce(Entries(from, out _) ? S(inverse) : StructuralConstraint.Open(inverse.ToArray()), to);
        return new(new Relation(from, to), new Relation(S(proofs), receipt));
    }
    internal static bool? ReadDifference(Curve.Frame frame, Relation difference, Relation from, Relation to)
    {
        if (difference is not { Left: Relation pair, Right: Relation { Left: Relation proofs, Right: Relation receipt } } ||
            !Same(pair.Left, from) || !Same(pair.Right, to) || !frame.Vector(from, out _) || !frame.Vector(to, out _) ||
            !Entries(from, out var words) || !Entries(proofs, out var rows) || rows.Length != words.Length ||
            receipt.Left is not Relation { Right: Relation { Left: Relation inverse, Right: var actualTo } } || !Same(actualTo, to) ||
            !Entries(inverse, out var inverted) || inverted.Length != words.Length) return null;
        for (var i = 0; i < words.Length; i++)
        {
            if (rows[i] is not Relation { Left: Relation { Left: var sourceMove, Right: Relation opposedMove }, Right: var oppositeRow } ||
                !Same(sourceMove, words[i]) || !Same(opposedMove, inverted[i]) ||
                sourceMove is not Relation actualMove || oppositeRow is not Relation opposite ||
                !Items(frame.Motion.Opposites).Any(row => Same(row, oppositeRow)) ||
                !Same(opposite.Left, actualMove.Left) || !Same(opposite.Right, opposedMove.Left) || !Same(actualMove.Right, opposedMove.Right)) return null;
        }
        if (!frame.Motion.TryReadResult(receipt, out var result, out _, out _) || !Entries(result, out var rest)) return null;
        return rest.Length == 0;
    }
    private static Relation Links(IElement[] phases) => S(phases.Zip(phases.Skip(1), (a, b) => (IElement)new Relation(a, b)));
    private static bool LinkRead(Relation links, IElement[] phases) => Entries(links, out var rows) && rows.Length == phases.Length - 1 &&
        rows.Select((row, i) => row is Relation pair && Same(pair.Left, phases[i]) && Same(pair.Right, phases[i + 1])).All(b => b);
    private static bool Distinct(IEnumerable<IElement> items) { var list = new List<IElement>(); foreach (var item in items) { if (list.Any(prior => Same(prior, item))) return false; list.Add(item); } return true; }
    private static bool Empty(Relation scope) => Entries(scope, out var items) && items.Length == 0;
    private static IElement[] Items(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(IEnumerable<IElement> items) => StructuralConstraint.Closed(items.ToArray());
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
