using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An optional later interpretation of exposed numeric publications. Explicit
/// finite calibration connects numeric intervals to the existing detector's
/// category identities. Initial detector observations require none of this
/// structure, and this adapter cannot retrieve their discarded input signals.
/// </summary>
public sealed class MeasuredDetectorAdapter
{
    private readonly ThresholdView view;
    private readonly Placeholder sideRole = new();
    private readonly Placeholder trendRole = new();

    public Carrier Axis { get; }
    public Relation Capacity { get; }
    public Relation SidePolicy { get; }
    public Relation TrendPolicy { get; }
    public Relation Calibration { get; }
    public Relation Surface { get; }

    /// <summary>
    /// The selected native numeric axis increases in this view's ordered way.
    /// The threshold is a closed band, which need not contain numeric zero.
    /// Trend publications are signed changes on the same selected numeric axis.
    /// </summary>
    public MeasuredDetectorAdapter(ThresholdView view, Carrier axis,
        long minimum, long bandLow, long bandHigh, long maximum)
    {
        if (minimum >= 0 || maximum <= 0 || minimum >= bandLow || bandLow > bandHigh || bandHigh >= maximum)
            throw new ArgumentException("The finite calibration must contain zero and three nonempty side categories.");
        this.view = view; Axis = axis;
        Capacity = FiniteReadSurface.Capacity(new Carrier(), axis, minimum, maximum);
        SidePolicy = SupportDecisionSurface.Create(new Carrier(),
            Rule(sideRole, minimum, checked(bandLow - 1), view.Below),
            Rule(sideRole, bandLow, bandHigh, view.WithinBand),
            Rule(sideRole, checked(bandHigh + 1), maximum, view.Above),
            SupportDecisionSurface.Rule(new Carrier(), view.SideDomain));
        TrendPolicy = SupportDecisionSurface.Create(new Carrier(),
            Rule(trendRole, minimum, -1, view.Falling),
            Rule(trendRole, 0, 0, view.Steady),
            Rule(trendRole, 1, maximum, view.Rising),
            SupportDecisionSurface.Rule(new Carrier(), view.TrendDomain));
        Calibration = new Relation(new Relation(view.Order, Relation.Segment(axis, minimum, maximum)),
            new Relation(view.Boundary, Relation.Segment(axis, bandLow, bandHigh)));
        Surface = new Relation(new Relation(view.Surface, Calibration),
            new Relation(Capacity, new Relation(SidePolicy, TrendPolicy)));
    }

    private Relation Rule(IElement role, long low, long high, Relation category) =>
        SupportDecisionSurface.Rule(new Carrier(), StructuralConstraint.Closed(category),
            SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(Axis, low, high)));

    /// <summary>
    /// Read only supplied publication supports through the advertised policy.
    /// The caller explicitly associates these publications with the event.
    /// Ambiguous support retains the full corresponding qualitative domain;
    /// private source information is neither queried nor used as a fallback.
    /// </summary>
    public Relation Observe(IElement source, IElement eventIdentity,
        Relation sidePublication, Relation trendPublication)
    {
        var sideRead = SupportDecisionSurface.Select(new Carrier(), SidePolicy,
            SupportDecisionSurface.Binding(new Carrier(), sideRole, sidePublication));
        var trendRead = SupportDecisionSurface.Select(new Carrier(), TrendPolicy,
            SupportDecisionSurface.Binding(new Carrier(), trendRole, trendPublication));
        if (!SupportDecisionSurface.TryReadSelection(sideRead, out _, out var sides, out _, out _) ||
            !SupportDecisionSurface.TryReadSelection(trendRead, out _, out var trends, out _, out _) ||
            sides is not Relation sideSupport || trends is not Relation trendSupport)
            throw new InvalidOperationException("An authored policy's unconditional domain fallback must remain readable.");
        return view.Observe(source, eventIdentity, sideSupport, trendSupport,
            new Relation(Surface, new Relation(sideRead, trendRead)));
    }
}
