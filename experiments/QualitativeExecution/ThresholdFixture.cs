using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An authored detector partition and two opposed reads of it. Nothing here
/// stores a temperature, numerical target, unit, exact zero or scalar trend.
/// Category, direction and local outcome laws are ordinary exposed relations.
/// </summary>
public sealed class ThresholdFixture
{
    public ThresholdView Forward { get; }
    public ThresholdView Reverse { get; }
    public Relation Correspondence { get; }
    public Relation Surface { get; }

    public ThresholdFixture()
    {
        var outerLow = new Placeholder();
        var bandLow = new Placeholder();
        var bandHigh = new Placeholder();
        var outerHigh = new Placeholder();
        var below = new Relation(outerLow, bandLow);
        var band = new Relation(bandLow, bandHigh);
        var above = new Relation(bandHigh, outerHigh);
        var resolution = new Relation(band, StructuralConstraint.Closed(below, band, above));
        Forward = new ThresholdView(band, resolution, new Relation(outerLow, outerHigh),
            below, band, above);
        Reverse = new ThresholdView(band, resolution, Forward.Order.Reverse(),
            above.Reverse(), band.Reverse(), below.Reverse(), Forward);
        Correspondence = new Relation(new Relation(Forward.Surface, Reverse.Surface),
            StructuralConstraint.Closed(
                new Relation(Forward.Below, Reverse.Above),
                new Relation(Forward.WithinBand, Reverse.WithinBand),
                new Relation(Forward.Above, Reverse.Below),
                new Relation(Forward.Falling, Reverse.Rising),
                new Relation(Forward.Steady, Reverse.Steady),
                new Relation(Forward.Rising, Reverse.Falling)));
        Surface = new Relation(new Relation(Forward.Surface, Reverse.Surface), Correspondence);
    }

    /// <summary>
    /// Change the observer frame through the published correspondence, retaining
    /// the same source/event and detector resolution. This does not change the
    /// observed world or ask the detector for additional information.
    /// </summary>
    public Relation ReframeToReverse(Relation observation)
    {
        if (!Forward.TryReadObservation(observation, out var source, out var eventIdentity,
                out var sides, out var trends) ||
            Correspondence.Right is not Relation mappings ||
            !Map(sides, mappings, out var reversedSides) ||
            !Map(trends, mappings, out var reversedTrends))
            return new Relation(observation, Correspondence);
        return Reverse.Observe(source, eventIdentity, reversedSides, reversedTrends,
            new Relation(observation, Correspondence));
    }

    private static bool Map(Relation support, Relation mappings, out Relation mapped)
    {
        mapped = StructuralConstraint.Open();
        if (!StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail) ||
            !StructuralConstraint.TryReadScope(mappings, out var pairs, out var mapSeam, out var mapTail) ||
            !mapSeam.Equals(mapTail)) return false;
        var targets = new List<IElement>();
        var incomplete = !seam.Equals(tail);
        foreach (var entry in entries)
        {
            var matches = pairs.OfType<Relation>().Where(pair => ReferenceEquals(pair.Left, entry)).ToArray();
            if (matches.Length != 1) { incomplete = true; continue; }
            targets.Add(matches[0].Right);
        }
        mapped = incomplete ? StructuralConstraint.Open(targets.ToArray())
            : StructuralConstraint.Closed(targets.ToArray());
        return true;
    }
}

/// <summary>A view's wrappers only expose its native partition and laws.</summary>
public sealed class ThresholdView
{
    private readonly FiniteLawPlan plan;

    public Relation Surface { get; }
    public Relation Boundary { get; }
    public Relation Resolution { get; }
    public Relation Order { get; }
    public Relation SideDomain { get; }
    public Relation TrendDomain { get; }
    public Relation Outcomes { get; }
    public Relation Law { get; }
    public Relation CategoryChangeLaw { get; }
    public Relation Below { get; }
    public Relation WithinBand { get; }
    public Relation Above { get; }
    public Relation Falling { get; }
    public Relation Steady { get; }
    public Relation Rising { get; }
    public Relation Approaching { get; }
    public Relation Departing { get; }
    public Relation OutsideSteady { get; }
    public Relation WithinSteady { get; }
    public Relation WithinChanging { get; }

    internal ThresholdView(Relation boundary, Relation resolution, Relation order,
        Relation below, Relation withinBand, Relation above, ThresholdView? sharedOutcomes = null)
    {
        Boundary = boundary; Resolution = resolution; Order = order;
        Below = below; WithinBand = withinBand; Above = above;
        SideDomain = StructuralConstraint.Closed(Below, WithinBand, Above);
        Falling = order.Reverse(); Steady = new Relation(order.Left, order.Left); Rising = order;
        TrendDomain = StructuralConstraint.Closed(Falling, Steady, Rising);
        if (sharedOutcomes is null)
        {
            Approaching = StructuralConstraint.Closed(new Relation(Below, Rising), new Relation(Above, Falling));
            Departing = StructuralConstraint.Closed(new Relation(Below, Falling), new Relation(Above, Rising));
            OutsideSteady = StructuralConstraint.Closed(new Relation(Below, Steady), new Relation(Above, Steady));
            WithinSteady = new Relation(WithinBand, Steady);
            WithinChanging = StructuralConstraint.Closed(new Relation(WithinBand, Falling), new Relation(WithinBand, Rising));
            Outcomes = StructuralConstraint.Closed(Approaching, Departing, OutsideSteady, WithinSteady, WithinChanging);
        }
        else
        {
            Approaching = sharedOutcomes.Approaching; Departing = sharedOutcomes.Departing;
            OutsideSteady = sharedOutcomes.OutsideSteady; WithinSteady = sharedOutcomes.WithinSteady;
            WithinChanging = sharedOutcomes.WithinChanging; Outcomes = sharedOutcomes.Outcomes;
        }
        Law = FiniteLaw.Create(SideDomain, TrendDomain, Outcomes,
            FiniteLaw.Row(Below, Falling, StructuralConstraint.Closed(Departing)),
            FiniteLaw.Row(Below, Steady, StructuralConstraint.Closed(OutsideSteady)),
            FiniteLaw.Row(Below, Rising, StructuralConstraint.Closed(Approaching)),
            FiniteLaw.Row(WithinBand, Falling, StructuralConstraint.Closed(WithinChanging)),
            FiniteLaw.Row(WithinBand, Steady, StructuralConstraint.Closed(WithinSteady)),
            FiniteLaw.Row(WithinBand, Rising, StructuralConstraint.Closed(WithinChanging)),
            FiniteLaw.Row(Above, Falling, StructuralConstraint.Closed(Approaching)),
            FiniteLaw.Row(Above, Steady, StructuralConstraint.Closed(OutsideSteady)),
            FiniteLaw.Row(Above, Rising, StructuralConstraint.Closed(Departing)));
        CategoryChangeLaw = FiniteLaw.Create(SideDomain, SideDomain, TrendDomain,
            FiniteLaw.Row(Below, Below, TrendDomain),
            FiniteLaw.Row(Below, WithinBand, StructuralConstraint.Closed(Rising)),
            FiniteLaw.Row(Below, Above, StructuralConstraint.Closed(Rising)),
            FiniteLaw.Row(WithinBand, Below, StructuralConstraint.Closed(Falling)),
            FiniteLaw.Row(WithinBand, WithinBand, TrendDomain),
            FiniteLaw.Row(WithinBand, Above, StructuralConstraint.Closed(Rising)),
            FiniteLaw.Row(Above, Below, StructuralConstraint.Closed(Falling)),
            FiniteLaw.Row(Above, WithinBand, StructuralConstraint.Closed(Falling)),
            FiniteLaw.Row(Above, Above, TrendDomain));
        Surface = new Relation(new Relation(Boundary, Resolution),
            new Relation(Order, new Relation(new Relation(SideDomain, TrendDomain),
                new Relation(Outcomes, new Relation(Law, CategoryChangeLaw)))));
        plan = FiniteLawPlan.Capture(Law) ?? throw new InvalidOperationException("The authored finite law must be readable.");
    }

    /// <summary>
    /// Publish detector evidence directly. A singleton category is not a hidden
    /// scalar, and an unmeasured trend should be the full TrendDomain support.
    /// </summary>
    public Relation Observe(IElement source, IElement eventIdentity, Relation sideSupport,
        Relation trendSupport, IElement? evidence = null) =>
        DetectorObservation.Create(source, eventIdentity, Surface, Boundary, Resolution,
            sideSupport, trendSupport, evidence ?? StructuralConstraint.Closed());

    public Relation ReadNative(Relation observation) => Read(observation, compiled: false);

    public Relation ReadCompiled(Relation observation) => Read(observation, compiled: true);

    private Relation Read(Relation observation, bool compiled)
    {
        if (!TryReadObservation(observation, out _, out _, out var sides, out var trends))
            return FiniteLaw.WithProvenance(FiniteLaw.Evaluate(Law,
                StructuralConstraint.Open(), StructuralConstraint.Open()), observation);
        return FiniteLaw.WithProvenance(compiled ? plan.Evaluate(sides, trends)
            : FiniteLaw.Evaluate(Law, sides, trends), observation);
    }

    internal bool TryReadObservation(Relation observation, out IElement source,
        out IElement eventIdentity, out Relation sides, out Relation trends)
    {
        if (DetectorObservation.TryReadContract(observation, out source, out eventIdentity,
            out var view, out var boundary, out var resolution, out sides, out trends, out _) &&
            ReferenceEquals(view, Surface) && ReferenceEquals(boundary, Boundary) &&
            ReferenceEquals(resolution, Resolution)) return true;
        source = eventIdentity = null!; sides = trends = null!;
        return false;
    }

    /// <summary>
    /// An ordered comparison of two category publications. Crossing ordered
    /// bins supplies a coarse direction; staying in one bin supplies no trend.
    /// This is not evidence of monotonic motion throughout the interval.
    /// </summary>
    public Relation CompareCategories(Relation previous, Relation current)
    {
        var comparison = new Relation(previous, current);
        if (!TryReadObservation(previous, out var source, out var beforeEvent, out var before, out _) ||
            !TryReadObservation(current, out var otherSource, out var afterEvent, out var after, out _) ||
            !ReferenceEquals(source, otherSource) || ReferenceEquals(beforeEvent, afterEvent))
            return FiniteLaw.WithProvenance(FiniteLaw.Evaluate(CategoryChangeLaw,
                StructuralConstraint.Open(), StructuralConstraint.Open()), comparison);
        return FiniteLaw.WithProvenance(FiniteLaw.Evaluate(CategoryChangeLaw, before, after), comparison);
    }
}
