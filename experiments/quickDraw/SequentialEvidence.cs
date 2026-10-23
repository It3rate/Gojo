using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QuickDraw;

/// <summary>A measured, lossy drawing message. These are selected acquisition
/// sections, not semantic objects. Its only dependencies point to an already
/// introduced origin or previous endpoint. Numeric returns never create joins.</summary>
public static class SequentialEvidence
{
    // basis = Closed(S(X,0,Q), S(Y,0,Q), S(order,0,1), Closed(feature frames))
    // packet = (basis -> (origin site -> (L(X,0) -> L(Y,0)))) -> Closed(strokes)
    // stroke = (event -> L(order,s)) -> Closed(sections)
    // section = (event -> L(order,k)) -> ((from -> to) ->
    //           ((earlier anchor -> start offset) -> (end offset -> features)))
    // Feature roles are ordered: constant Q, end dx/dy, three coarse profile
    // dx/dy pairs. Profiles are observations, never hidden fitted controls.
    public const int FeatureCount = 9;
    public sealed record SectionInput(long[] Start, long[] End, long[] Features);
    /// <summary>The importer contract declares consecutive zero-offset section
    /// boundaries within this stroke to be the same acquired event. Callers must
    /// establish that correspondence from source spans before constructing this
    /// input; equal coordinates alone do not justify it. Independent touching
    /// traces belong in separate stroke inputs. A nonzero offset declares a gap.</summary>
    public sealed record StrokeInput(IReadOnlyList<SectionInput> Sections);
    public readonly record struct Point(long X, long Y);
    public sealed record Frame(Carrier X, Carrier Y, Carrier Order, Carrier[] Features,
        long Quantum, Relation Surface);
    public sealed record Section(Placeholder Event, Placeholder From, Placeholder To,
        Placeholder Anchor, int Stroke, int Sequence, Point Start, Point End,
        CarrierLocation[] Features, Relation Surface);
    public sealed record Read(Frame Frame, Placeholder Origin, IReadOnlyList<Section[]> Strokes, Relation Surface)
    { public IEnumerable<Section> Sections => Strokes.SelectMany(stroke => stroke); }

    public static Relation Create(long quantum, IReadOnlyList<StrokeInput> strokes)
    {
        if (quantum <= 0) throw new ArgumentOutOfRangeException(nameof(quantum));
        var x = new Carrier(); var y = new Carrier(x.UnitShape); var order = new Carrier();
        var features = Enumerable.Range(0, FeatureCount).Select(_ => new Carrier(x.UnitShape)).ToArray();
        var basis = S(Relation.Segment(x, 0, quantum), Relation.Segment(y, 0, quantum),
            Relation.Segment(order, 0, 1), S(features.Select(c => (IElement)Relation.Segment(c, 0, 1)).ToArray()));
        var origin = new Placeholder(); Placeholder previous = origin; var position = new Point();
        var rows = new List<IElement>(); var sequence = 0;
        for (var s = 0; s < strokes.Count; s++)
        {
            var sections = new List<IElement>(); var first = true;
            foreach (var source in strokes[s].Sections)
            {
                if (source.Start.Length != 2 || source.End.Length != 2 || source.Features.Length != FeatureCount)
                    throw new InvalidDataException("A section requires two endpoints and nine measured features.");
                var start = new Point(source.Start[0], source.Start[1]); var end = new Point(source.End[0], source.End[1]);
                var delta = Subtract(end, start); var offset = Subtract(start, position);
                if (source.Features[0] != quantum || source.Features[1] != delta.X || source.Features[2] != delta.Y)
                    throw new InvalidDataException("Feature constant and chord must agree with the declared coordinate basis.");
                // This realizes StrokeInput's declared acquisition continuity;
                // it is not a discovery rule for arbitrary coincident traces.
                var from = !first && offset == new Point() ? previous : new Placeholder();
                var to = new Placeholder(); var identity = new Placeholder();
                var featureRow = S(source.Features.Select((v, i) => (IElement)new CarrierLocation(features[i], v)).ToArray());
                sections.Add(new Relation(new Relation(identity, new CarrierLocation(order, sequence++)),
                    new Relation(new Relation(from, to), new Relation(new Relation(previous, Position(x, y, offset)),
                        new Relation(Position(x, y, delta), featureRow)))));
                previous = to; position = end; first = false;
            }
            rows.Add(new Relation(new Relation(new Placeholder(), new CarrierLocation(order, s)), S(sections.ToArray())));
        }
        return new(new Relation(basis, new Relation(origin, Position(x, y, new Point()))), S(rows.ToArray()));
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: Relation basis, Right: Relation { Left: Placeholder origin, Right: Relation originPosition } },
                Right: Relation strokeScope } || origin.IsInverse || !TryReadFrame(basis, out var frame) ||
            !TryPosition(originPosition, frame, out var zero) || zero != new Point() || !Entries(strokeScope, out var strokeRows)) return false;
        var identities = new HashSet<Placeholder> { origin }; var prior = origin; var previousPosition = zero;
        var strokes = new List<Section[]>(); var sequence = 0;
        for (var s = 0; s < strokeRows.Length; s++)
        {
            if (strokeRows[s] is not Relation { Left: Relation { Left: Placeholder strokeEvent, Right: CarrierLocation ordinal }, Right: Relation sectionScope } ||
                !Fresh(strokeEvent, identities) || !On(ordinal, frame.Order, s) || !Entries(sectionScope, out var sectionRows)) return false;
            var sections = new List<Section>();
            foreach (var item in sectionRows)
            {
                if (item is not Relation row || row is not { Left: Relation { Left: Placeholder ev, Right: CarrierLocation n },
                    Right: Relation { Left: Relation { Left: Placeholder from, Right: Placeholder to },
                        Right: Relation { Left: Relation { Left: Placeholder anchor, Right: Relation offsetSurface },
                            Right: Relation { Left: Relation deltaSurface, Right: Relation featureScope } } } } ||
                    !Fresh(ev, identities) || !On(n, frame.Order, sequence) || !ReferenceEquals(anchor, prior) ||
                    !TryPosition(offsetSurface, frame, out var offset) || !TryPosition(deltaSurface, frame, out var delta) ||
                    !Entries(featureScope, out var fs) || fs.Length != FeatureCount) return false;
                // First sections start a fresh acquisition site after a pen lift.
                // Consecutive zero-offset sections share the actual previous site.
                var share = sections.Count > 0 && offset == new Point();
                if (share ? !ReferenceEquals(from, prior) : !Fresh(from, identities)) return false;
                if (!Fresh(to, identities)) return false;
                var featureReads = new CarrierLocation[FeatureCount];
                for (var i = 0; i < fs.Length; i++)
                {
                    if (fs[i] is not CarrierLocation location || !ReferenceEquals(location.Carrier, frame.Features[i])) return false;
                    featureReads[i] = location;
                }
                if (featureReads[0].Ticks != frame.Quantum || featureReads[1].Ticks != delta.X || featureReads[2].Ticks != delta.Y) return false;
                Point start, end;
                try { start = Add(previousPosition, offset); end = Add(start, delta); }
                catch (OverflowException) { return false; }
                sections.Add(new(ev, from, to, anchor, s, sequence++, start, end, featureReads, row));
                prior = to; previousPosition = end;
            }
            strokes.Add(sections.ToArray());
        }
        read = new(frame, origin, strokes.AsReadOnly(), surface); return true;
    }

    public static bool TryReadFrame(Relation basis, out Frame frame)
    {
        frame = null!;
        if (!Entries(basis, out var parts) || parts is not [var xs, var ys, var os, Relation featureScope] ||
            !Axis(xs, out var x, out var quantum) || !Axis(ys, out var y, out var yq) || quantum <= 0 || yq != quantum ||
            !Axis(os, out var order, out var oq) || oq != 1 || !Entries(featureScope, out var featureRows) || featureRows.Length != FeatureCount) return false;
        var features = new Carrier[FeatureCount];
        for (var i = 0; i < FeatureCount; i++)
            if (!Axis(featureRows[i], out features[i], out var q) || q != 1) return false;
        if (features.Concat([x, y, order]).Distinct(ReferenceEqualityComparer.Instance).Count() != FeatureCount + 3 ||
            !x.UnitShape.CompatibleWith(y.UnitShape) || features.Any(feature => !x.UnitShape.CompatibleWith(feature.UnitShape))) return false;
        frame = new(x, y, order, features, quantum, basis); return true;
    }

    internal static bool Axis(IElement surface, out Carrier carrier, out long end)
    {
        carrier = null!; end = 0;
        if (surface is not Relation { Left: Relation { Left: CarrierLocation a, Right: Carrier c },
                Right: Relation { Left: CarrierLocation b, Right: Carrier d } } ||
            c.IsUnknown || c.IsInverse || !ReferenceEquals(c, d) || !ReferenceEquals(a.Carrier, c) || !ReferenceEquals(b.Carrier, c) || a.Ticks != 0) return false;
        carrier = c; end = b.Ticks; return true;
    }
    internal static Relation Position(Carrier x, Carrier y, Point p) => new(new CarrierLocation(x, p.X), new CarrierLocation(y, p.Y));
    internal static bool TryPosition(IElement surface, Frame frame, out Point p)
    {
        p = default;
        if (surface is not Relation { Left: CarrierLocation x, Right: CarrierLocation y } ||
            !ReferenceEquals(x.Carrier, frame.X) || !ReferenceEquals(y.Carrier, frame.Y)) return false;
        p = new(x.Ticks, y.Ticks); return true;
    }
    private static bool Fresh(Placeholder site, HashSet<Placeholder> seen) => !site.IsInverse && seen.Add(site);
    private static bool On(CarrierLocation value, Carrier carrier, long ticks) => ReferenceEquals(value.Carrier, carrier) && value.Ticks == ticks;
    private static Point Add(Point a, Point b) => new(checked(a.X + b.X), checked(a.Y + b.Y));
    private static Point Subtract(Point a, Point b) => new(checked(a.X - b.X), checked(a.Y - b.Y));
    internal static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    internal static bool Entries(Relation scope, out IElement[] values) =>
        StructuralConstraint.TryReadScope(scope, out values, out var seam, out var tail) && seam.Equals(tail);
}
