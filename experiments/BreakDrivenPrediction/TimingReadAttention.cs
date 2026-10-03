using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using N = BreakDrivenPrediction.NativeCircuit;
using L = BreakDrivenPrediction.TimingLadder;

namespace BreakDrivenPrediction;

/// <summary>Local native policy reads, not an additional learner object.</summary>
internal static class TimingReadAttention
{
    // Attention: phase carrier, positive probe-period span, dormant carrier,
    // dormant 0->1 span, outstanding probe-demand carrier, its 0->1 span,
    // connected demand/support/admission/ladder interfaces, latest transition.
    public static Relation Create(Relation root, long probeEvery)
    {
        var p = C.Parts(root);
        var phase = new Carrier();
        var dormant = new Carrier();
        var probe = new Carrier();
        return C.G(phase, Relation.Segment(phase, 0, probeEvery),
            dormant, Relation.Segment(dormant, 0, 1),
            probe, Relation.Segment(probe, 0, 1),
            C.G(p[6], p[5], p[10], p[11], p[1]), new Placeholder());
    }

    public static Carrier Site(Relation attention, int index) => (Carrier)C.Parts(attention)[index];
    public static CarrierLocation Sample(Relation attention, int index) => C.Copy(Site(attention, index).ValueSlot);
    public static Relation On(CarrierLocation sample) => OperationClaim.AtLeast(new Carrier(), sample,
        new CarrierLocation(sample.Carrier, 1), new Carrier());
    public static Relation Zero(CarrierLocation sample) => OperationClaim.Equality(new Carrier(), sample,
        new CarrierLocation(sample.Carrier, 0), new Carrier());

    public static bool TrySelect(Relation attention, Relation ladder, CarrierLocation demand,
        CarrierLocation requestedProbe, out Relation selectedLadder, out CarrierLocation phaseAfter,
        out CarrierLocation probeAfter, out Relation evidence)
    {
        var p = C.Parts(attention);
        phaseAfter = Sample(attention, 0);
        probeAfter = requestedProbe;
        selectedLadder = C.G(demand.Carrier, C.G());
        evidence = Relation.UnresolvedRead(demand, ladder, demand.Carrier);
        if (C.Parts(ladder) is not { Length: 2 } ladderFields || ladderFields[0] is not Carrier clock)
            return false;
        var demandRead = N.Read(N.Reframe(demand, clock));
        if (!Equation.TryReadPrediction(demandRead, out var wanted, out _)) return false;

        var matches = new List<IElement>();
        Relation? selected = null;
        CarrierLocation selectedWidth = default;
        foreach (var map in L.Maps(ladder))
        {
            TimeStepSequence.TryReadSpan((Relation)map.Left, out _, out var width);
            var match = OperationClaim.Equality(new Carrier(), width, wanted, new Carrier());
            matches.Add(C.G(map, match));
            if (selected is null && C.Pass(match)) { selected = map; selectedWidth = C.Copy(width); }
        }
        if (selected is null)
        {
            evidence = C.G(demand, demandRead, C.G(matches.ToArray()));
            return false;
        }

        var phaseBefore = Sample(attention, 0);
        var phaseCircuit = N.Reframe(N.Sum((phaseBefore, 1),
            (new CarrierLocation(phaseBefore.Carrier, 1), 1)), phaseBefore.Carrier);
        var phaseRead = N.Read(phaseCircuit);
        if (!Equation.TryReadPrediction(phaseRead, out var incremented, out _))
        { evidence = C.G(demandRead, phaseCircuit, phaseRead); return false; }
        var period = C.Length((Relation)p[1]);
        var periodic = OperationClaim.AtLeast(new Carrier(), incremented, period, new Carrier());
        var requested = On(requestedProbe);
        phaseAfter = C.Pass(periodic) ? new CarrierLocation(phaseBefore.Carrier, 0) : incremented;

        var ordering = new List<IElement>();
        Relation? finer = null;
        CarrierLocation finerWidth = default;
        if (C.Pass(periodic) || C.Pass(requested))
        {
            foreach (var map in L.Maps(ladder))
            {
                TimeStepSequence.TryReadSpan((Relation)map.Left, out _, out var width);
                var notLarger = OperationClaim.AtMost(new Carrier(), width, selectedWidth, new Carrier());
                var unequal = OperationClaim.Equality(new Carrier(), width, selectedWidth, new Carrier());
                var strict = OperationClaim.TryReadResolvedViolation(unequal, out var difference)
                    ? On(difference) : Relation.UnresolvedRead(width, unequal, width.Carrier);
                IElement nearer = new Placeholder();
                if (finer is not null)
                    nearer = OperationClaim.AtLeast(new Carrier(), width, finerWidth, new Carrier());
                ordering.Add(C.G(map, notLarger, unequal, strict, nearer));
                // Keep the first authored map for ties. Order magnitude is a
                // native claim, while traversal chooses among its witnesses.
                if (!C.Pass(notLarger) || !C.Pass(strict)) continue;
                if (finer is null)
                { finer = map; finerWidth = C.Copy(width); continue; }
                var same = OperationClaim.Equality(new Carrier(), width, finerWidth, new Carrier());
                ordering.Add(same);
                if (nearer is Relation claim && C.Pass(claim) && !C.Pass(same))
                { finer = map; finerWidth = C.Copy(width); }
            }
        }

        probeAfter = new CarrierLocation(requestedProbe.Carrier, 0);
        var maps = finer is null ? C.G(selected) : C.G(selected, finer);
        selectedLadder = C.G(wanted.Carrier, maps);
        evidence = C.G(demand, demandRead, C.G(matches.ToArray()), selected,
            new Relation(phaseCircuit, phaseRead), C.G(phaseBefore, period, periodic, phaseAfter),
            C.G(requestedProbe, requested, probeAfter), C.G(ordering.ToArray()), maps);
        return true;
    }

    public static Relation Retain(Relation attention, IElement transition)
    {
        var p = C.Parts(attention);
        // No reference to the previous attention relation or its latest witness.
        return C.G(p[0], p[1], p[2], p[3], p[4], p[5], p[6], transition);
    }

    public static bool TryDelta(CarrierLocation after, CarrierLocation before,
        out CarrierLocation delta, out Relation evidence)
    {
        var circuit = N.Reframe(N.Sum((after, 1), (before, -1)), before.Carrier);
        var read = N.Read(circuit);
        evidence = C.G(before, after, circuit, read);
        return Equation.TryReadPrediction(read, out delta, out _);
    }
}
