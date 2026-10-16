using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An optional measured interpretation of existing discrete stock states and
/// ordered landmark identities. Only finite advertised publications are read.
/// Numeric coordinates attach through explicit calibration, never replace the
/// original participants, and do not introduce a fractional stock transition.
/// </summary>
public sealed class MeasuredStructuralSupplier
{
    // calibration = state-calibration -> landmark-calibration
    // state-calibration = (stock-surface -> quantum-frame) -> Closed(state -> point)
    // landmark-calibration = (landmark-frame -> site-domain) -> Closed(site -> point)
    // envelope = ((source -> event) -> supplier-surface) -> finite-publication
    // qualification = Closed((ordered-points -> (order-claim -> separation-claim)), ...)
    // Output is QualitativePublication; its support contains the ORIGINAL
    // state/site identities and its evidence retains calibration and decisions.
    private readonly Placeholder stateRole = new();
    private readonly Placeholder siteRole = new();
    private readonly QualitativeStockFixture stock;
    private readonly StructuralLandmarkFixture landmarks;

    public Carrier Axis { get; }
    public Relation Capacity { get; }
    public Relation SiteDomain { get; }
    public Relation StateCalibration { get; }
    public Relation LandmarkCalibration { get; }
    public Relation Calibration { get; }
    public Relation StatePolicy { get; }
    public Relation SitePolicy { get; }
    public Relation Surface { get; }

    /// <summary>
    /// Stock's authored equal quantum corresponds to 0, q, 2q. Landmark
    /// coordinates are independent: the interior need not be halfway. Invalid
    /// order remains a failed native obligation and yields open observations.
    /// </summary>
    public MeasuredStructuralSupplier(QualitativeStockFixture stock,
        StructuralLandmarkFixture landmarks, Carrier axis, long quantum = 10,
        long beginning = 0, long interior = 7, long end = 20)
    {
        this.stock = stock;
        this.landmarks = landmarks;
        Axis = axis;
        var top = checked(quantum * 2);
        var minimum = new[] { 0L, quantum, top, beginning, interior, end }.Min();
        var maximum = new[] { 0L, quantum, top, beginning, interior, end }.Max();
        Capacity = FiniteReadSurface.Capacity(new Carrier(), axis, minimum, maximum);
        var sites = new List<IElement>();
        foreach (var site in new[] { landmarks.Beginning, landmarks.Interior, landmarks.End })
            if (!sites.Any(existing => FiniteLaw.SameParticipant(existing, site))) sites.Add(site);
        SiteDomain = StructuralConstraint.Closed(sites.ToArray());
        var stateBindings = StructuralConstraint.Closed(
            Binding(stock.Empty, 0), Binding(stock.Available, quantum), Binding(stock.Full, top));
        var siteBindings = StructuralConstraint.Closed(
            Binding(landmarks.Beginning, beginning), Binding(landmarks.Interior, interior), Binding(landmarks.End, end));
        StateCalibration = new Relation(new Relation(stock.Surface,
            new Relation(stock.Quantum, Relation.Segment(axis, 0, quantum))), stateBindings);
        LandmarkCalibration = new Relation(new Relation(landmarks.Frame, SiteDomain), siteBindings);
        Calibration = new Relation(StateCalibration, LandmarkCalibration);
        StatePolicy = Policy(stateRole, stateBindings, stock.StateDomain);
        SitePolicy = Policy(siteRole, siteBindings, SiteDomain);
        Surface = new Relation(Calibration,
            new Relation(Capacity, new Relation(StatePolicy, SitePolicy)));

        Relation Binding(IElement participant, long ticks) =>
            new(participant, Relation.Point(axis, ticks));
    }

    /// <summary>The supplier explicitly associates an exposed observation with
    /// its source, event and calibration. This is non-draining observation.</summary>
    public Relation Envelope(IElement source, IElement eventIdentity, Relation publication) =>
        new(new Relation(new Relation(source, eventIdentity), Surface), publication);

    public Relation ObserveState(Relation envelope) =>
        Observe(envelope, stock.Receiver, stock.Surface, stock.StateDomain,
            StateCalibration, StatePolicy, stateRole);

    public Relation ObserveLandmark(Relation envelope) =>
        Observe(envelope, landmarks.Frame, landmarks.Frame, SiteDomain,
            LandmarkCalibration, SitePolicy, siteRole);

    /// <summary>Reads a coordinate only through the published role calibration.
    /// Numeric equality never substitutes for identity of the selected site.</summary>
    public bool TryReadSitePosition(IElement site, out CarrierLocation position)
    {
        position = default;
        Qualify(LandmarkCalibration, out var qualified);
        if (!qualified || LandmarkCalibration.Right is not Relation bindings ||
            !FiniteLaw.ClosedEntries(bindings, out var entries)) return false;
        var matches = entries.OfType<Relation>()
            .Where(binding => FiniteLaw.SameParticipant(binding.Left, site)).ToArray();
        return matches.Length == 1 && Point(matches[0].Right, out position);
    }

    private Relation Observe(Relation envelope, IElement subject, IElement basis,
        Relation domain, Relation calibration, Relation policy, IElement role)
    {
        // A malformed outer shape cannot invent source or event identities.
        if (envelope.Left is not Relation { Left: Relation owner, Right: var contract } ||
            envelope.Right is not Relation publication) return envelope;
        var qualification = Qualify(calibration, out var qualified);
        IElement decision = new Relation(envelope, Surface);
        Relation support = StructuralConstraint.Open();
        if (ReferenceEquals(contract, Surface) && qualified)
        {
            decision = SupportDecisionSurface.Select(new Carrier(), policy,
                SupportDecisionSurface.Binding(new Carrier(), role, publication));
            if (SupportDecisionSurface.TryReadSelection((Relation)decision,
                    out _, out var response, out _, out _) && response is Relation selected)
                support = selected;
        }
        return QualitativePublication.Create(owner.Left, subject, owner.Right, basis, domain,
            support, new Relation(Surface, new Relation(envelope, new Relation(qualification, decision))));
    }

    private Relation Policy(IElement role, Relation bindings, Relation domain)
    {
        var rules = FiniteLaw.Entries(bindings).Cast<Relation>().Select(binding =>
            SupportDecisionSurface.Rule(new Carrier(), StructuralConstraint.Closed(binding.Left),
                SupportDecisionSurface.Condition(new Carrier(), role,
                    new Relation(binding.Right, binding.Right)))).ToList();
        // A coarse, clipped, foreign or unmapped publication does not authorize
        // an invented intermediate state or a guessed landmark coordinate.
        rules.Add(SupportDecisionSurface.Rule(new Carrier(), domain));
        return SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
    }

    private Relation Qualify(Relation calibration, out bool qualified)
    {
        var obligations = new List<IElement>();
        qualified = ConnectedContract(calibration) && calibration.Right is Relation bindings &&
            FiniteLaw.ClosedEntries(bindings, out var entries) && entries.Length == 3;
        if (!qualified) return StructuralConstraint.Closed(new Relation(calibration, StructuralConstraint.Open()));
        var rows = FiniteLaw.Entries((Relation)calibration.Right).OfType<Relation>().ToArray();
        var policy = ReferenceEquals(calibration, StateCalibration) ? StatePolicy : SitePolicy;
        var role = ReferenceEquals(calibration, StateCalibration) ? stateRole : siteRole;
        var domain = ReferenceEquals(calibration, StateCalibration) ? stock.StateDomain : SiteDomain;
        if (!PolicyCorresponds(policy, role, domain, rows))
        {
            qualified = false;
            return StructuralConstraint.Closed(new Relation(calibration, policy));
        }
        if (rows.Length != 3 || !Point(rows[0].Right, out var first) ||
            !Point(rows[1].Right, out var middle) || !Point(rows[2].Right, out var last) ||
            !first.Carrier.SameIdentityAs(Axis) || !middle.Carrier.SameIdentityAs(Axis) ||
            !last.Carrier.SameIdentityAs(Axis))
        {
            qualified = false;
            return StructuralConstraint.Closed(new Relation(calibration, StructuralConstraint.Open()));
        }
        // Capture the currently advertised coordinates for stable claim reads.
        // Shared site identity remains in the original binding rows.
        first = new CarrierLocation(Axis, first.Ticks);
        middle = new CarrierLocation(Axis, middle.Ticks);
        last = new CarrierLocation(Axis, last.Ticks);
        qualified = Strict(first, middle) & Strict(middle, last);
        if (ReferenceEquals(calibration, StateCalibration))
        {
            if (calibration.Left is not Relation { Right: Relation quantumBinding } ||
                !ReferenceEquals(quantumBinding.Left, stock.Quantum) ||
                quantumBinding.Right is not Relation quantumFrame ||
                !Point(quantumFrame.Left, out var origin) || !Point(quantumFrame.Right, out var unit) ||
                !origin.Carrier.SameIdentityAs(Axis) || !unit.Carrier.SameIdentityAs(Axis)) qualified = false;
            else
            {
                var originClaim = OperationClaim.Equality(new Carrier(), first, origin, Axis);
                obligations.Add(originClaim);
                qualified &= OperationClaim.TryReadResolvedViolation(originClaim, out var offset) && offset.Ticks == 0;
                var difference = Equation.Difference(Axis, Axis, Axis);
                var quantumRead = Equation.Predict(difference, new Relation(origin, unit), Axis);
                obligations.Add(quantumRead);
                if (Equation.TryReadPrediction(quantumRead, out var quantumValue, out _))
                    qualified &= EqualIncrement(first, middle, quantumValue, difference) &
                        EqualIncrement(middle, last, quantumValue, difference);
                else qualified = false;
            }
        }
        var expected = ReferenceEquals(calibration, StateCalibration)
            ? FiniteLaw.Entries(stock.StateDomain) : FiniteLaw.Entries(SiteDomain);
        for (var i = 0; i < rows.Length; i++)
        {
            var roleRead = StructuralConstraint.Match(rows[i].Left,
                StructuralConstraint.Closed(), StructuralConstraint.Closed(expected[i]));
            obligations.Add(roleRead);
            qualified &= StructuralConstraint.TryRead(roleRead, out _, out var candidates, out _, out var pending) &&
                FiniteLaw.Entries(candidates).Length == 1 && FiniteLaw.Entries(pending).Length == 0;
        }
        for (var i = 0; i < rows.Length; i++)
            for (var j = 0; j < i; j++)
            {
                var identity = StructuralConstraint.Match(rows[i].Left,
                    StructuralConstraint.Closed(), StructuralConstraint.Closed(rows[j].Left));
                obligations.Add(identity);
                qualified &= StructuralConstraint.TryRead(identity, out _, out var candidates, out _, out var pending) &&
                    FiniteLaw.Entries(candidates).Length == 0 && FiniteLaw.Entries(pending).Length == 0;
            }
        return new Relation(calibration, StructuralConstraint.Closed(obligations.ToArray()));

        bool Strict(CarrierLocation before, CarrierLocation after)
        {
            var ordered = OperationClaim.AtMost(new Carrier(), before, after, Axis);
            var separated = OperationClaim.Equality(new Carrier(), before, after, Axis);
            obligations.Add(new Relation(new Relation(before, after), new Relation(ordered, separated)));
            return OperationClaim.TryReadResolvedViolation(ordered, out var violation) && violation.Ticks == 0 &&
                OperationClaim.TryReadResolvedViolation(separated, out var distance) && distance.Ticks > 0;
        }

        bool EqualIncrement(CarrierLocation before, CarrierLocation after,
            CarrierLocation quantumValue, Relation difference)
        {
            var prediction = Equation.Predict(difference, new Relation(before, after), Axis);
            if (!Equation.TryReadPrediction(prediction, out var increment, out _))
            { obligations.Add(prediction); return false; }
            var equal = OperationClaim.Equality(new Carrier(), increment, quantumValue, Axis);
            obligations.Add(new Relation(prediction, equal));
            return OperationClaim.TryReadResolvedViolation(equal, out var mismatch) && mismatch.Ticks == 0;
        }
    }

    private bool ConnectedContract(Relation selected)
    {
        if (!ReferenceEquals(Surface.Left, Calibration) ||
            !ReferenceEquals(Calibration.Left, StateCalibration) ||
            !ReferenceEquals(Calibration.Right, LandmarkCalibration) ||
            Surface.Right is not Relation { Left: var capacity, Right: Relation policies } ||
            !ReferenceEquals(capacity, Capacity) || !ReferenceEquals(policies.Left, StatePolicy) ||
            !ReferenceEquals(policies.Right, SitePolicy)) return false;
        // Qualification is local to the selected interpretation. A bad stock
        // quantum is not evidence that a separate landmark order is invalid.
        if (ReferenceEquals(selected, StateCalibration))
            return StateCalibration.Left is Relation stateOwner && ReferenceEquals(stateOwner.Left, stock.Surface);
        return ReferenceEquals(selected, LandmarkCalibration) &&
            LandmarkCalibration.Left is Relation landmarkOwner &&
            ReferenceEquals(landmarkOwner.Left, landmarks.Frame) && ReferenceEquals(landmarkOwner.Right, SiteDomain) &&
            FiniteLaw.ClosedEntries(SiteDomain, out var sites) && sites.Length == 3 &&
            FiniteLaw.SameParticipant(sites[0], landmarks.Beginning) &&
            FiniteLaw.SameParticipant(sites[1], landmarks.Interior) &&
            FiniteLaw.SameParticipant(sites[2], landmarks.End);
    }

    private static bool PolicyCorresponds(Relation policy, IElement role, Relation domain, Relation[] rows)
    {
        if (rows.Length != 3 || !policy.TryReadOrderedGroup(out var rules) || rules.Length != 4) return false;
        for (var i = 0; i < rows.Length; i++)
        {
            if (rules[i] is not Relation rule || !rule.TryReadOrderedGroup(out var fields) || fields.Length != 2 ||
                fields[0] is not Relation conditions || !conditions.TryReadOrderedGroup(out var checks) || checks.Length != 1 ||
                checks[0] is not Relation check || !check.TryReadOrderedGroup(out var condition) || condition.Length != 2 ||
                !ReferenceEquals(condition[0], role) || condition[1] is not Relation frame ||
                !ReferenceEquals(frame.Left, rows[i].Right) || !ReferenceEquals(frame.Right, rows[i].Right) ||
                fields[1] is not Relation support || !FiniteLaw.ClosedEntries(support, out var responses) ||
                responses.Length != 1 || !FiniteLaw.SameParticipant(responses[0], rows[i].Left)) return false;
        }
        return rules[^1] is Relation fallback && fallback.TryReadOrderedGroup(out var last) && last.Length == 2 &&
            last[0] is Relation unconditional && unconditional.TryReadOrderedGroup(out var empty) && empty.Length == 0 &&
            ReferenceEquals(last[1], domain);
    }

    private static bool Point(IElement element, out CarrierLocation position)
    {
        if (element is Relation { Left: CarrierLocation point, Right: Carrier axis } &&
            point.Carrier is not null && point.Tick is not null && point.Carrier.SameIdentityAs(axis))
        { position = point; return true; }
        position = default; return false;
    }
}
