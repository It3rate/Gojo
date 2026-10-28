using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// An exposed linear observation, permitted interval and calibrated response
/// passages. Terms consume public samples only. Names never select a law.
/// </summary>
public sealed class EvidenceObligation
{
    // G(support correspondence, G(G(input port, input-to-feature map,
    // response map)), permitted interval, feature axis, influence, rules).
    // rule = G(two-input error/trend equation, clipping interval, share).
    public Relation Surface { get; }
    public IElement Support => F(Surface)[0];
    public Relation[] Terms => F(F(Surface)[1]).Cast<Relation>().ToArray();
    public Relation Allowed => (Relation)F(Surface)[2];
    public Carrier Axis => (Carrier)F(Surface)[3];
    public Relation[] Rules => F(F(Surface)[5]).Cast<Relation>().ToArray();
    public static EvidenceObligation Restore(Relation surface) => new(surface);
    private EvidenceObligation(Relation surface) => Surface = surface;

    public EvidenceObligation((Relation Port, long Coefficient)[] inputs, long lower, long upper,
        long[] shares, long influenceN = 1, long influenceD = 1, IElement? support = null)
    {
        if (inputs.Length == 0 || inputs.Any(t => t.Coefficient == 0) || shares.Length != 3 || shares.Any(s => s < 0) || shares.Sum() != 6)
            throw new ArgumentException("An obligation needs actual inputs and six response shares.");
        var axis = new Carrier(); var norm = inputs.Sum(t => checked(t.Coefficient * t.Coefficient));
        var terms = inputs.Select(t => (IElement)G(t.Port,
            new Relation(Relation.Segment(NegotiatingMachine.PortAxis(t.Port), 0, 1), Relation.Segment(axis, 0, t.Coefficient)),
            new Relation(Relation.Segment(axis, 0, norm), Relation.Segment(NegotiatingMachine.PortAxis(t.Port), 0, t.Coefficient)))).ToArray();
        Relation Rule(long trendN, long trendD, long cap, long share) => G(
            Equation.Sum(axis, Equation.UnitPort(axis), Equation.WeightedPort(axis, 0, trendD, 0, trendN)),
            Relation.Segment(axis, -cap, cap), ProjectionResidue.Create(new Carrier(), share, 6));
        Surface = G(support ?? new Placeholder(), G(terms), Relation.Segment(axis, lower, upper), axis,
            ProjectionResidue.Create(new Carrier(), influenceN, influenceD),
            G(Rule(0, 1, 4096, shares[0]), Rule(0, 1, 12, shares[1]), Rule(-1, 2, 4096, shares[2])));
    }

    /// <summary>Pure service query. A hypothetical sample remains qualified by
    /// its supplied event/provenance; querying does not train or change a source.</summary>
    public Relation Assess(IReadOnlyDictionary<Relation, Relation> samples)
    {
        var trace = new List<IElement>(); IElement sum = new CarrierLocation(Axis, 0);
        foreach (var term in Terms)
        {
            var t = F(term); var port = (Relation)t[0];
            if (!samples.TryGetValue(port, out var publication) || !ReferenceEquals(F(publication)[0], port)) return Pending(G(term));
            var map = (Relation)t[1];
            var projected = Equation.PredictWitness(new Carrier(), map, F(publication)[2], Axis);
            if (!ProjectionResidue.TryRead(F(projected)[2], out _, out _, out _)) return Pending(projected);
            var added = Equation.PredictWitness(new Carrier(), Equation.Sum(Axis, Equation.UnitPort(Axis), Equation.UnitPort(Axis)),
                new Relation(sum, F(projected)[2]), Axis);
            sum = F(added)[2]; trace.Add(G(publication, projected, added));
        }
        if (!TimeStepSequence.TryReadSpan(Allowed, out var lo, out var hi)) return Pending(Allowed);
        var claim = OperationClaim.Between(new Carrier(), sum, lo, hi, new Carrier(Axis.UnitShape));
        if (!OperationClaim.TryReadResolvedBetweenViolation(claim, out var demand) || !ProjectionResidue.TryRead(sum, out _, out var n, out var d) || d != 1)
            return Pending(G(claim, sum));
        var actual = checked((long)n); var nearest = Math.Clamp(actual, lo.Ticks, hi.Ticks);
        var correction = Equation.PredictWitness(new Carrier(), Equation.Difference(Axis, Axis, Axis),
            new Relation(sum, new CarrierLocation(Axis, nearest)), Axis);
        // This ordered interval IS the excess/missing participation, not a
        // renderer-only red decoration or a free adjustable error variable.
        var excess = Relation.Segment(Axis, actual, nearest);
        return ReadWitness.Create(new Carrier(), G(Terms.Select(t => (IElement)samples[(Relation)F(t)[0]]).ToArray()), Surface,
            G(sum, claim, F(correction)[2], new CarrierLocation(Axis, demand.Ticks), excess), G(), G(G(trace.ToArray()), correction));

        Relation Pending(IElement why) => ReadWitness.Create(new Carrier(), G(samples.Values.Cast<IElement>().ToArray()), Surface,
            new Placeholder(), G(why), G(trace.ToArray()));
    }

    public static bool Resolved(Relation read) => F(read)[2] is Relation result && result.TryReadOrderedGroup(out var f) && f.Length == 5;
    public static IElement Value(Relation read) => F(F(read)[2])[0];
    public static IElement Demand(Relation read) => F(F(read)[2])[3];

    public Relation Propose(IReadOnlyDictionary<Relation, Relation> samples, Relation assessment, IElement? previous, IElement occasion)
    {
        if (!Resolved(assessment)) return ReadWitness.Create(new Carrier(), assessment, Surface, new Placeholder(), G(assessment), occasion);
        var a = F(F(assessment)[2]);
        var trend = previous is null ? Equation.PredictWitness(new Carrier(), Equation.Constant(Axis, 0), new Placeholder(), Axis)
            : Equation.PredictWitness(new Carrier(), Equation.Difference(Axis, Axis, Axis), new Relation(previous, a[0]), Axis);
        var parts = new List<IElement>(); IElement combined = new CarrierLocation(Axis, 0);
        foreach (var rule in Rules)
        {
            var f = F(rule);
            var prediction = Equation.PredictWitness(new Carrier(), (Relation)f[0], new Relation(a[2], F(trend)[2]), Axis);
            var raw = Fraction.Read(F(prediction)[2]);
            TimeStepSequence.TryReadSpan((Relation)f[1], out var low, out var high);
            // An inactive permitted-region claim makes no motion request. Its
            // structure remains attached and can wake on later evidence.
            var gated = Fraction.Read(a[3]) == Fraction.Zero ? Fraction.Zero : raw;
            var clipped = Fraction.Clamp(gated, new Fraction(low.Ticks), new Fraction(high.Ticks));
            var share = Fraction.Read(f[2]); var influence = Fraction.Read(F(Surface)[4]);
            var weightingMap = Equation.Affine(Axis, 0, checked((long)(share.D * influence.D)), Axis, 0, checked((long)(share.N * influence.N)));
            var weighting = Equation.PredictWitness(new Carrier(), weightingMap, clipped.Encode(Axis), Axis);
            var weighted = Fraction.Read(F(weighting)[2]);
            var contribution = ReadWitness.Create(new Carrier(), G(assessment, trend, prediction), rule, weighted.Encode(Axis),
                G((raw - gated).Encode(Axis), (gated - clipped).Encode(Axis)), G(weighting, f[2], F(Surface)[4], assessment));
            parts.Add(contribution);
            var addition = Equation.PredictWitness(new Carrier(), Equation.Sum(Axis, Equation.UnitPort(Axis), Equation.UnitPort(Axis)),
                new Relation(combined, weighted.Encode(Axis)), Axis);
            combined = F(addition)[2]; parts.Add(addition);
        }
        var requests = new List<IElement>(); var outputs = new List<IElement>();
        foreach (var term in Terms)
        {
            var t = F(term); var port = (Relation)t[0]; var own = samples[port]; var axis = NegotiatingMachine.PortAxis(port);
            var displacement = Equation.PredictWitness(new Carrier(), (Relation)t[2], combined, axis);
            var target = Equation.PredictWitness(new Carrier(), Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis)),
                new Relation(F(own)[2], F(displacement)[2]), axis);
            // A response is its own synthetic publication, explicitly linked to
            // the assessment and occasion. It is not another acquisition.
            var signal = Signal(port, own, target, G(assessment, occasion, displacement, G(parts.ToArray())));
            requests.Add(signal.Request); outputs.Add(signal.Witness);
        }
        return ReadWitness.Create(new Carrier(), assessment, Surface, G(combined, G(requests.ToArray()), G(parts.ToArray())),
            G(), G(trend, occasion, G(outputs.ToArray())));
    }

    public static (Relation Request, Relation Witness) Signal(Relation targetPort, Relation own, Relation targetRead, IElement provenance)
    {
        var axis = NegotiatingMachine.PortAxis(targetPort); var clock = new Carrier();
        // The exposed origin establishes this synthetic signal's scalar basis;
        // targetRead is its producer. It does not claim a shared drawing site.
        var service = G(new CarrierLocation(axis, 0), Relation.Segment(axis, 0, 1), clock, targetRead);
        var publication = G(service, new CarrierLocation(clock, 0), F(targetRead)[2], provenance);
        var link = new NegotiationLink(targetPort, service);
        if (F(own)[1] is not CarrierLocation)
        {
            // A hypothetical evaluation has no event on the live clock. This
            // is a conditional request, rejected by a live receiver unless a
            // preview explicitly rebinds it to its own current publication.
            var difference = Equation.PredictWitness(new Carrier(), Equation.Difference(axis, axis, axis),
                new Relation(F(own)[2], F(targetRead)[2]), axis);
            var conditional = G(targetPort, own, publication, F(difference)[2], link.Surface);
            return (conditional, G(publication, targetRead, difference, own));
        }
        var plan = link.Plan(own, publication);
        return (NegotiationLink.Requests(plan)[0], G(publication, targetRead, plan));
    }

    public static Relation[] Requests(Relation plan) => F(plan)[2] is Relation result && result.TryReadOrderedGroup(out var f) && f.Length == 3
        ? F(f[1]).Cast<Relation>().ToArray() : [];

    public Relation Response(Relation before, Relation after, IElement transition) => Resolved(before) && Resolved(after)
        ? OperationClaim.IntervalResponse(new Carrier(), Allowed, Value(before), Value(after), new Carrier(Axis.UnitShape), transition)
        : ReadWitness.Create(new Carrier(), G(before, after), Allowed, new Placeholder(), G(before, after), transition);
}
