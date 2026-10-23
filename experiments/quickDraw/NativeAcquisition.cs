using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Experimental measured acquisition surface. A site identifies a
/// recorded sample event, not a semantic corner or contact. Coordinate equality
/// never aliases sample events. No carrier value is assigned or material moved.</summary>
public sealed class NativeAcquisition
{
    // basis = (S(X,0,1) -> S(Y,0,1)) -> (S(time,0,1) -> S(index,0,1))
    // scalar(C,n/d) = L(C,n) -> P(C,d), d > 0, reduced exact rational.
    // This contextual coordinate read is not a reciprocal unit or carrier state.
    // sample = event -> (L(index,i) -> ((scalarX -> scalarY) -> timeScope))
    // stroke = event -> (L(index,s) -> (Closed(samples) -> Closed(adjacent spans)))
    // span = strokeEvent -> ((fromEvent -> toEvent) -> Closed(actual sample rows))
    // acquisition = (sourceEvent -> basis) -> Closed(strokes)
    // A closed inventory scope is exhaustive enumeration, never geometric closure.
    public QuickDrawRecord Source { get; }
    public Placeholder Event { get; } = new();
    public Carrier X { get; } = new();
    public Carrier Y { get; }
    public Carrier Time { get; } = new();
    public Carrier Index { get; } = new();
    public Relation Basis { get; }
    public Relation Strokes { get; }
    public Relation Surface { get; }
    public IReadOnlyList<Stroke> StrokeReads { get; }

    public sealed record Sample(Placeholder Event, int Index, Relation Coordinates,
        Relation Time, Relation Surface);
    public sealed record Stroke(Placeholder Event, int Index, IReadOnlyList<Sample> Samples,
        Relation Spans, Relation Surface);

    public NativeAcquisition(QuickDrawRecord source)
    {
        Source = source;
        // Two independent input axes share a source coordinate unit. This is a
        // chosen Cartesian input basis, not discovered perpendicularity.
        Y = new(X.UnitShape);
        Basis = new(new Relation(Relation.Segment(X, 0, 1), Relation.Segment(Y, 0, 1)),
            new Relation(Relation.Segment(Time, 0, 1), Relation.Segment(Index, 0, 1)));
        var strokes = new List<Stroke>();
        for (var s = 0; s < source.Strokes.Count; s++)
        {
            var strokeEvent = new Placeholder(); var samples = new List<Sample>();
            for (var i = 0; i < source.Strokes[s].Samples.Count; i++)
            {
                var value = source.Strokes[s].Samples[i]; var identity = new Placeholder();
                var coordinates = new Relation(Scalar(X, value.X), Scalar(Y, value.Y));
                var time = value.Time is null ? StructuralConstraint.Open() : S(Scalar(Time, value.Time));
                var row = new Relation(identity, new Relation(new CarrierLocation(Index, i), new Relation(coordinates, time)));
                samples.Add(new(identity, i, coordinates, time, row));
            }
            var spans = S(samples.Skip(1).Select((sample, i) => (IElement)new Relation(strokeEvent,
                new Relation(new Relation(samples[i].Event, sample.Event), S(samples[i].Surface, sample.Surface)))).ToArray());
            var surface = new Relation(strokeEvent, new Relation(new CarrierLocation(Index, s),
                new Relation(S(samples.Select(sample => (IElement)sample.Surface).ToArray()), spans)));
            strokes.Add(new(strokeEvent, s, samples.AsReadOnly(), spans, surface));
        }
        StrokeReads = strokes.AsReadOnly();
        Strokes = S(strokes.Select(stroke => (IElement)stroke.Surface).ToArray());
        Surface = new(new Relation(Event, Basis), Strokes);
    }

    public static Relation Scalar(Carrier basis, ExactNumber number)
    {
        if (number.Denominator <= 0 || BigInteger.GreatestCommonDivisor(BigInteger.Abs(number.Numerator), number.Denominator) != BigInteger.One)
            throw new ArgumentException("A scalar must be an exact reduced rational with positive denominator.", nameof(number));
        if (number.Numerator < long.MinValue || number.Numerator > long.MaxValue || number.Denominator > long.MaxValue)
            throw new InvalidDataException($"Exact coordinate '{number.Lexeme}' exceeds the native Int64 rational surface; no rounding was applied.");
        return new(new CarrierLocation(basis, (long)number.Numerator), Relation.Point(basis, (long)number.Denominator));
    }

    public static bool TryReadScalar(IElement surface, Carrier basis, out BigInteger numerator, out BigInteger denominator)
    {
        numerator = denominator = default;
        if (surface is not Relation { Left: CarrierLocation n, Right: Relation { Left: CarrierLocation d, Right: Carrier c } } ||
            !ReferenceEquals(n.Carrier, basis) || !ReferenceEquals(d.Carrier, basis) || !ReferenceEquals(c, basis) || d.Ticks <= 0 ||
            BigInteger.GreatestCommonDivisor(BigInteger.Abs(n.Ticks), d.Ticks) != BigInteger.One) return false;
        numerator = n.Ticks; denominator = d.Ticks; return true;
    }

    /// <summary>Read precisely this acquisition layout after transport. The reader
    /// checks sample aliases and adjacent source spans, not merely field counts.
    /// Acquisition events are fresh forward slots; an opposed face is not an
    /// independently acquired source, stroke or sample event.</summary>
    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: Placeholder sourceEvent, Right: Relation basis }, Right: Relation strokes } || sourceEvent.IsInverse ||
            basis is not { Left: Relation xy, Right: Relation ti } ||
            !Axis(xy.Left, out var x) || !Axis(xy.Right, out var y) || !Axis(ti.Left, out var time) || !Axis(ti.Right, out var index) ||
            new[] { x, y, time, index }.Distinct(ReferenceEqualityComparer.Instance).Count() != 4 ||
            !x.UnitShape.CompatibleWith(y.UnitShape) ||
            !Closed(strokes, out var strokeRows)) return false;
        var samples = new List<ReadSample>(); var events = new HashSet<Placeholder> { sourceEvent };
        for (var s = 0; s < strokeRows.Length; s++)
        {
            if (strokeRows[s] is not Relation { Left: Placeholder strokeEvent, Right: Relation { Left: CarrierLocation si,
                    Right: Relation { Left: Relation sampleScope, Right: Relation spans } } } || strokeEvent.IsInverse || !events.Add(strokeEvent) ||
                !ReferenceEquals(si.Carrier, index) || si.Ticks != s || !Closed(sampleScope, out var sampleRows) ||
                !Closed(spans, out var spanRows) || spanRows.Length != Math.Max(0, sampleRows.Length - 1)) return false;
            var strokeEvents = new List<Placeholder>();
            for (var i = 0; i < sampleRows.Length; i++)
            {
                if (sampleRows[i] is not Relation { Left: Placeholder sampleEvent, Right: Relation { Left: CarrierLocation ordinal,
                        Right: Relation { Left: Relation coords, Right: Relation timeScope } } } || sampleEvent.IsInverse || !events.Add(sampleEvent) ||
                    !ReferenceEquals(ordinal.Carrier, index) || ordinal.Ticks != i ||
                    !TryReadScalar(coords.Left, x, out var nx, out var dx) || !TryReadScalar(coords.Right, y, out var ny, out var dy) ||
                    !StructuralConstraint.TryReadScope(timeScope, out var times, out var seam, out var tail)) return false;
                BigInteger? nt = null, dt = null;
                if (seam.Equals(tail))
                {
                    if (times.Length != 1 || !TryReadScalar(times[0], time, out var n, out var d)) return false;
                    nt = n; dt = d;
                }
                else if (times.Length != 0) return false;
                samples.Add(new(sampleEvent, s, i, nx, dx, ny, dy, nt, dt)); strokeEvents.Add(sampleEvent);
            }
            for (var i = 0; i < spanRows.Length; i++)
                if (spanRows[i] is not Relation { Left: var owner, Right: Relation { Left: Relation endpoints, Right: Relation covered } } ||
                    !ReferenceEquals(owner, strokeEvent) || !ReferenceEquals(endpoints.Left, strokeEvents[i]) ||
                    !ReferenceEquals(endpoints.Right, strokeEvents[i + 1]) || !Closed(covered, out var coveredRows) ||
                    coveredRows.Length != 2 || !ReferenceEquals(coveredRows[0], sampleRows[i]) || !ReferenceEquals(coveredRows[1], sampleRows[i + 1])) return false;
        }
        read = new(sourceEvent, x, y, time, index, strokeRows.Length, samples.AsReadOnly()); return true;
    }

    public sealed record Read(Placeholder Event, Carrier X, Carrier Y, Carrier Time, Carrier Index,
        int StrokeCount, IReadOnlyList<ReadSample> Samples);
    public sealed record ReadSample(Placeholder Event, int StrokeIndex, int SampleIndex,
        BigInteger XNumerator, BigInteger XDenominator, BigInteger YNumerator, BigInteger YDenominator,
        BigInteger? TimeNumerator, BigInteger? TimeDenominator);

    private static bool Axis(IElement element, out Carrier carrier)
    {
        carrier = null!;
        if (element is not Relation { Left: Relation { Left: CarrierLocation a, Right: Carrier c },
                Right: Relation { Left: CarrierLocation b, Right: Carrier d } } || !ReferenceEquals(c, d) ||
            !ReferenceEquals(a.Carrier, c) || !ReferenceEquals(b.Carrier, c) || a.Ticks != 0 || b.Ticks != 1) return false;
        carrier = c; return true;
    }

    internal static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    internal static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}

/// <summary>Fresh, history-qualified acquisition edge publication. Its topology is
/// sampled pen traversal, not inferred contour/junction topology. Degenerate
/// displacements have open headings. Fitting and contact discovery are unperformed.</summary>
public sealed class AcquisitionPublication
{
    public QualitativeRecognitionObservation Observation { get; }
    public Relation SampleMap { get; }
    public Relation SpanMap { get; }
    public Relation BasisMap { get; }
    public Relation Audit { get; }

    public AcquisitionPublication(NativeAcquisition acquisition)
    {
        var frame = new QualitativeMotionFrame(); var sites = new List<IElement>(); var segments = new List<IElement>();
        var sampleMap = new List<IElement>(); var spanMap = new List<IElement>();
        foreach (var stroke in acquisition.StrokeReads)
        {
            var fresh = stroke.Samples.Select(_ => (IElement)new Placeholder()).ToArray(); sites.AddRange(fresh);
            for (var i = 0; i < fresh.Length; i++) sampleMap.Add(new Relation(stroke.Samples[i].Event, fresh[i]));
            NativeAcquisition.Closed(stroke.Spans, out var spans);
            for (var i = 1; i < fresh.Length; i++)
            {
                var a = stroke.Samples[i - 1].Coordinates; var b = stroke.Samples[i].Coordinates;
                int Compare(IElement first, IElement second, Carrier axis)
                {
                    if (!NativeAcquisition.TryReadScalar(first, axis, out var an, out var ad) ||
                        !NativeAcquisition.TryReadScalar(second, axis, out var bn, out var bd))
                        throw new InvalidDataException("The selected acquisition coordinate is not readable in its declared basis.");
                    return (bn * ad).CompareTo(an * bd);
                }
                var dx = Compare(a.Left, b.Left, acquisition.X); var dy = Compare(a.Right, b.Right, acquisition.Y);
                var directions = new List<IElement>();
                if (dx != 0) directions.Add(dx > 0 ? frame.Right : frame.Left);
                // Quick Draw's supplied screen Y increases downwards. This is
                // the explicit presentation basis correspondence below.
                if (dy != 0) directions.Add(dy > 0 ? frame.Down : frame.Up);
                var heading = directions.Count == 0 ? StructuralConstraint.Open() : NativeAcquisition.S(directions.ToArray());
                var segment = new Relation(new Placeholder(), new Relation(new Relation(fresh[i - 1], fresh[i]), heading));
                segments.Add(segment); spanMap.Add(new Relation(spans[i - 1], segment));
            }
        }
        Observation = new(frame, NativeAcquisition.S(sites.ToArray()), NativeAcquisition.S(segments.ToArray()),
            StructuralConstraint.Open(), StructuralConstraint.Open());
        SampleMap = NativeAcquisition.S(sampleMap.ToArray()); SpanMap = NativeAcquisition.S(spanMap.ToArray());
        BasisMap = NativeAcquisition.S(new Relation(acquisition.X, frame.Right), new Relation(acquisition.Y, frame.Down));
        Audit = new(acquisition.Surface, new Relation(Observation.Surface,
            new Relation(BasisMap, new Relation(SampleMap, SpanMap))));
    }
}
