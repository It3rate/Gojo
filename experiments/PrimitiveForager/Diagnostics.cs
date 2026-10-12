using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveForager;

/// <summary>Small authored encounters. Private fixture values enter only the
/// external comparison service; every controller response uses the same native
/// observer and rule structure as the longer world runs.</summary>
public static class Diagnostics
{
    public static Diagnostic[] Run()
    {
        var results = new List<Diagnostic>();
        var warmup = new SignalFixture([0]);
        var zero = new SignalFixture([0, 0]);
        var warmDecision = Decide(warmup);
        var zeroDecision = Decide(zero);
        results.Add(new("Warmup is not a zero score", "unknown / relative reference",
            warmup.FastScore is null && warmup.SlowScore is null && zero.FastScore == 0 && zero.SlowScore == 0 &&
            warmup.Presence == 1 && zero.Presence == 1 && zero.AllSame,
            "The first present acquisition has no reference score. A second equal acquisition produces a native zero score with all comparison signs equal. The private fixture value is zero in both cases; that value is never published as a brightness coordinate.",
            SignalText(warmup) + "; " + DecisionText(warmDecision),
            SignalText(zero) + "; " + DecisionText(zeroDecision)));

        var missing = new SignalFixture([40, 40]); missing.Unavailable();
        var absent = new SignalFixture([40, 40]); absent.Absent();
        var missingFull = Decide(missing); var absentFull = Decide(absent);
        var missingRemoved = Decide(missing, "no_presence"); var absentRemoved = Decide(absent, "no_presence");
        results.Add(new("Missing, absent, and present zero stay distinct", "presence / unknown",
            missing.Presence is null && absent.Presence == 0 && zero.Presence == 1 &&
            missing.FastScore is null && absent.FastScore is null && zero.FastScore == 0 &&
            missingFull.Action == 0 && absentFull.Action == 0 && zeroDecision.Action == 1 &&
            missingFull.Reason.Contains("Unresolved acquisition", StringComparison.Ordinal) &&
            absentFull.Reason.Contains("Measured absence", StringComparison.Ordinal) &&
            missingRemoved.Action == 1 && absentRemoved.Action == 1,
            "An unavailable acquisition exposes open presence; a dark source exposes presence 0; a present zero-valued private fixture exposes presence 1 and comparable equal events. The full controller holds for the first two with different native reasons. Removing the presence routes makes both continue.",
            "Missing: " + DecisionText(missingFull) + "; absent: " + DecisionText(absentFull) +
                "; present private zero: " + DecisionText(zeroDecision),
            "Missing: " + DecisionText(missingRemoved) + "; absent: " + DecisionText(absentRemoved)));

        var plateau = new SignalFixture([100, 100]);
        var mirrored = Decide(plateau, mirrored: true);
        var unoriented = Decide(plateau, "no_orientation", mirrored: true);
        results.Add(new("Reversed eye wiring needs its installed orientation", "order / orientation",
            mirrored.Action == 1 && unoriented.Action == -1 && mirrored.SpatialQueries == 1 && unoriented.SpatialQueries == 1,
            "The physical right eye is brighter. Mirrored wiring returns sensor order −1 over its actual swapped acquisition pair. The exposed inverse frame calibration maps that ordinal sign to body-right; removing it makes the same native controller choose body-left.",
            DecisionText(mirrored), DecisionText(unoriented)));

        var coarseSufficient = Decide(plateau, energy: 90, heat: 10);
        var alwaysFine = Decide(plateau, "always_fine", energy: 90, heat: 10);
        results.Add(new("A sufficient coarse support avoids extra precision", "coarse support / selected precision",
            coarseSufficient.Mode == 1 && alwaysFine.Mode == 1 && coarseSufficient.Action == alwaysFine.Action &&
            coarseSufficient.FineQueries == 0 && alwaysFine.FineQueries == 2,
            "The exposed energy region 85..100 lies wholly above the feeding-to-cooling threshold, and heat 0..19 already establishes cooling. Finer energy and heat readings do not change this response, so the selective native query rules avoid both fees.",
            DecisionText(coarseSufficient), DecisionText(alwaysFine)));

        var below = Decide(plateau, energy: 79, heat: 30);
        var above = Decide(plateau, energy: 80, heat: 30);
        var belowRemoved = Decide(plateau, "no_refinement", energy: 79, heat: 30);
        var aboveRemoved = Decide(plateau, "no_refinement", energy: 80, heat: 30);
        results.Add(new("Precision is useful when the same support crosses a decision", "decision-driven precision",
            below.Action == 1 && above.Action == -1 && below.Mode == 0 && above.Mode == 1 &&
            below.FineQueries == 1 && above.FineQueries == 1 &&
            belowRemoved.Action == aboveRemoved.Action && belowRemoved.FineQueries == 0 && aboveRemoved.FineQueries == 0,
            "Energy 79 and 80 initially publish the same 70..84 region. A paid finite body reading resolves the native threshold: seek the brighter right side at 79, leave it to cool at 80. Without that service both remain under the same conservative coarse response.",
            "79: " + DecisionText(below) + "; 80: " + DecisionText(above),
            "79: " + DecisionText(belowRemoved) + "; 80: " + DecisionText(aboveRemoved)));

        // These are actual ordinal observer reads, not authored score points.
        // Recent heavier references see a fall to 154; older lighter references
        // still see a value above their earlier acquisitions.
        var dip = new SignalFixture(Enumerable.Range(100, 60).Append(154).ToArray());
        var hasOppositeContext = dip.FastScore is <= -4 && dip.SlowScore is >= 1;
        var boundedWait = Decide(dip, energy: 60, heat: 0);
        var slowRemoved = Decide(dip, "no_slow", energy: 60, heat: 0);
        results.Add(new("Longer context can change a brief decline response", "fast / slow relative reference",
            hasOppositeContext && boundedWait.Action == 0 && boundedWait.Wait == 4 &&
            slowRemoved.Action == 1 && boundedWait.SpatialQueries == 0 && slowRemoved.SpatialQueries == 1 &&
            boundedWait.Used.Contains("slow", StringComparer.Ordinal),
            "A private rise from 100 to 159 followed by 154 yields " + SignalText(dip) +
                ". With sufficient declared reserve and heat margin, the native rule arms a four-tick wait. Removing the slow read makes direction action-relevant and requests the eyes instead. This is an authored encounter, not a claim that waiting always helps in the habitat.",
            DecisionText(boundedWait), DecisionText(slowRemoved)));

        var lowReserve = Decide(dip, energy: 30, heat: 0);
        var reserveRemoved = Decide(dip, "no_budget", energy: 30, heat: 0);
        results.Add(new("An unsafe reserve prevents the same wait", "explicit amount / unit math",
            hasOppositeContext && lowReserve.Action == 1 && lowReserve.Wait == 0 &&
            reserveRemoved.Action == 0 && reserveRemoved.Wait == 4,
            "The same ordinal light history now has energy 30. Its exposed support minus the declared four-tick energy drain does not establish the required reserve. Native amount evidence rejects the wait; removing the budget conditions admits it. Light scores are never treated as energy amounts.",
            DecisionText(lowReserve), DecisionText(reserveRemoved)));

        var hotForecast = Decide(dip, energy: 60, heat: 60);
        var heatBudgetRemoved = Decide(dip, "no_budget", energy: 60, heat: 60);
        results.Add(new("Heat forecast can independently reject a wait", "explicit amount / unit math",
            hasOppositeContext && hotForecast.Action == 1 && hotForecast.Wait == 0 &&
            heatBudgetRemoved.Action == 0 && heatBudgetRemoved.Wait == 4,
            "At heat 60, the receiver's coarse heat support plus its declared four-tick heat rise does not lie wholly inside the waiting safety region. The full controller refuses the same dip wait even with energy 60. This arithmetic uses the explicit body heat domain, not an absolute light origin.",
            DecisionText(hotForecast), DecisionText(heatBudgetRemoved)));

        var rising = new SignalFixture(Enumerable.Range(100, 60).ToArray());
        var recentRise = Decide(rising); var fastRemoved = Decide(rising, "no_fast");
        results.Add(new("A recent rise saves an action-irrelevant eye request", "fast reference / query relevance",
            rising.FastScore is >= 4 && recentRise.Action == fastRemoved.Action && recentRise.Action == 1 &&
            recentRise.SpatialQueries == 0 && fastRemoved.SpatialQueries == 1 &&
            recentRise.Used.Contains("fast", StringComparer.Ordinal),
            "A sustained ordinal rise establishes the native recent-direction condition. Continuing the remembered heading needs no spatial request. Removing that read produces the same action in this encounter but requires one paid eye comparison; the comparison is not claimed to improve the already-settled action.",
            SignalText(rising) + "; " + DecisionText(recentRise), DecisionText(fastRemoved)));
        return results.ToArray();
    }

    private static BrainDecision Decide(SignalFixture signal, string variant = "full", int energy = 60,
        int heat = 0, bool mirrored = false)
    {
        var sensorOrder = new Carrier(); var bodyOrder = new Carrier();
        var orientation = new ForagingWorld(0, mirrored).Orientation(sensorOrder, bodyOrder);
        var brain = new NativeForager(signal.ScoreAxis, signal.PresenceAxis, bodyOrder, orientation, variant);
        var energyRead = CoarseReadSurface.Measure(new Carrier(), brain.EnergyPartition, energy, new Placeholder());
        var heatRead = CoarseReadSurface.Measure(new Carrier(), brain.HeatPartition, heat, new Placeholder());
        return brain.Decide(signal.Sample, signal.FastRead, signal.SlowRead, energyRead, heatRead,
            () => FiniteReadSurface.Publish(new Carrier(), brain.EnergyCapacity,
                new CarrierLocation(brain.EnergyAxis, energy), new Placeholder()),
            () => FiniteReadSurface.Publish(new Carrier(), brain.HeatCapacity,
                new CarrierLocation(brain.HeatAxis, heat), new Placeholder()),
            () => signal.Spatial(sensorOrder, mirrored));
    }

    private static string DecisionText(BrainDecision decision) =>
        $"action {decision.Action} ({(decision.Action == 0 ? "hold" : decision.Action < 0 ? "left" : "right")}), " +
        $"mode {decision.Mode}, wait {decision.Wait}; {decision.Reason}; " +
        $"fine queries {decision.FineQueries}, spatial queries {decision.SpatialQueries}; " +
        $"reserve/heat forecast {decision.BudgetSignature}";
    private static string SignalText(SignalFixture fixture) =>
        $"presence {fixture.Presence?.ToString() ?? "open"}, fast {fixture.FastScore?.ToString() ?? "open"}, " +
        $"slow {fixture.SlowScore?.ToString() ?? "open"}";

    /// <summary>The finite private oracle is an external diagnostic instrument.
    /// Its dictionary never becomes a native metric publication or policy.</summary>
    private sealed class SignalFixture
    {
        private readonly Carrier source = new(), orderAxis = new();
        private readonly Dictionary<Relation, int> values = new(ReferenceEqualityComparer.Instance);
        private Relation state;
        public Carrier PresenceAxis { get; } = new();
        public Carrier ScoreAxis { get; }
        public Relation Sample { get; private set; } = null!;
        public Relation FastRead { get; private set; } = null!;
        public Relation SlowRead { get; private set; } = null!;
        public int? Presence => SamplingSurface.TryReadSample(Sample, out var present, out _) ? (int)present.Ticks : null;
        public int? FastScore => Score(FastRead);
        public int? SlowScore => Score(SlowRead);
        public bool AllSame => AllSameRead(FastRead) && AllSameRead(SlowRead);

        public SignalFixture(int[] trace)
        {
            state = RelativeSignalSurface.Create(new Carrier(), source, PresenceAxis, orderAxis);
            if (!RelativeSignalSurface.TryReadState(state, out _, out var policy, out _, out _, out _) ||
                !RelativeSignalSurface.TryReadPolicy(policy, out _, out _, out _, out var axis, out _, out _))
                throw new InvalidDataException("The diagnostic relative-reference policy is malformed.");
            ScoreAxis = axis;
            foreach (var value in trace) Observe(Acquire(value, true));
        }

        public void Unavailable()
        {
            var frame = Relation.Segment(PresenceAxis, 0, 1);
            Observe(ReadWitness.Create(new Carrier(), source, frame,
                NativeForager.G(new Placeholder(), new Placeholder()), new Relation(source, frame), source));
        }
        public void Absent() => Observe(Acquire(0, false));
        public Relation Spatial(Carrier sensorAxis, bool mirrored)
        {
            var left = Acquire(100, true); var right = Acquire(101, true);
            return Compare(mirrored ? left : right, mirrored ? right : left, sensorAxis);
        }
        private Relation Acquire(int value, bool present)
        {
            var eventSample = SamplingSurface.Sample(new Carrier(), source, PresenceAxis, present);
            values.Add(eventSample, value); return eventSample;
        }
        private Relation Compare(Relation candidate, Relation anchor, Carrier axis) =>
            SamplingSurface.Comparison(new Carrier(), candidate, anchor, axis, values[candidate].CompareTo(values[anchor]));
        private void Observe(Relation sample)
        {
            state = RelativeSignalSurface.Observe(new Carrier(), state, sample, (a, b) => Compare(a, b, orderAxis));
            if (!RelativeSignalSurface.TryReadState(state, out _, out _, out _, out _, out var latest) ||
                !RelativeSignalSurface.TryReadLatest(latest, out var observed, out var fast, out var slow, out _, out _))
                throw new InvalidDataException("The diagnostic observer returned malformed native state.");
            Sample = observed; FastRead = fast; SlowRead = slow;
        }
        private bool AllSameRead(Relation read) =>
            RelativeSignalSurface.TryReadBankRead(read, out _, out _, out _, out _) &&
            RelativeSignalSurface.TryReadBankComparisons(read, out var comparisons) && comparisons.Length > 0 &&
            comparisons.All(comparison => ReadWitness.TryRead(comparison, out _, out _, out var result, out _, out _) &&
                result is CarrierLocation sign && sign.Carrier.SameIdentityAs(orderAxis) && sign.Ticks == 0);
        private static int? Score(Relation read) =>
            RelativeSignalSurface.TryReadBankRead(read, out var score, out _, out _, out _) ? (int)score.Ticks : null;
    }
}
