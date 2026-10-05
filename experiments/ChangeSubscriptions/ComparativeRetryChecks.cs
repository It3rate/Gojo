using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using N = ConnectionAssembly.NativeMath;

namespace ChangeSubscriptions;

internal static class ComparativeRetryChecks
{
    public static int Run()
    {
        RepeatedBelowBaselineEscalates();
        AggregateImprovementAllowsMistakes();
        NeutralAndMissingAreDifferent();
        EventualRetryAndRecovery();
        IndependentUsesAndOpenBases();
        BoundedPolicyAndReadOnlyQueries();
        NativeTransportAndLatestBlock();
        return 7;
    }

    private static void RepeatedBelowBaselineEscalates()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = ComparativeRetry.Create(N.Group(loss), N.Group(clock), loss, clock);
        Require(ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, 0))), "A new relationship admits its initial paid trial.");
        long time = 0;
        foreach (var delay in new long[] { 2, 4, 8, 16, 32, 32 })
        {
            for (var j = 0; j < 3; j++)
            {
                Observe(state, loss, clock, money, ++time, 1, 3);
                Require(ComparativeRetry.BlockCount(state).Value == (ulong)(j + 1), "Only resolved paid trials advance the current block.");
            }
            var before = ComparativeRetry.Rung(state).Value;
            var witness = Observe(state, loss, clock, money, ++time, 1, 3);
            Require(ComparativeRetry.Rung(state).Value == Math.Min(before + 1, 5) &&
                ComparativeRetry.RetryAfter(state).Ticks == time + delay && ComparativeRetry.BlockCount(state).Value == 0,
                "A negative aggregate raises bounded resistance and starts a visible opportunity-clock pause.");
            Require(N.Parts(witness).Last() is Relation completed && N.Parts(completed).Length == 7,
                "Block completion retains count, signed benefit, time and before/after retry coordinates.");
            Require(!ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, time + delay - 1))) &&
                ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, time + delay))),
                "Retry remains possible exactly at the native deadline even after repeated underperformance.");
            time += delay;
        }
        Require(ComparativeRetry.Evaluated(state).Value == 24 && ComparativeRetry.Harmful(state).Value == 24 &&
            ComparativeRetry.Benefit(state).Tension == -48 && ComparativeRetry.CompletedBlocks(state).Value == 6,
            "Evidence is signed native pressure, with evaluated count and harmful outcomes still separate.");
    }

    private static void AggregateImprovementAllowsMistakes()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = ComparativeRetry.Create(loss, clock, loss, clock);
        // Signed improvements +3,-2,+2,-2 have net +1; individual mistakes
        // do not overturn the useful block or masquerade as perfect accuracy.
        Observe(state, loss, clock, money, 1, 3, 0);
        Observe(state, loss, clock, money, 2, 0, 2);
        Observe(state, loss, clock, money, 3, 2, 0);
        Observe(state, loss, clock, money, 4, 0, 2);
        Require(ComparativeRetry.Rung(state).Value == 0 && ComparativeRetry.RetryAfter(state).Ticks == 4 &&
            ComparativeRetry.Helpful(state).Value == 2 && ComparativeRetry.Harmful(state).Value == 2,
            "A positive aggregate permits occasional mistakes without increasing retry resistance.");
        var last = N.Parts(ComparativeRetry.LastBlock(state));
        Require(((Carrier)last[0]).Value == 4 && ((Carrier)last[1]).Value == 1,
            "The empirical numerator1 and denominator4 remain separate, unreduced evidence.");
        Require(Equation.TryReadPrediction(ComparativeRetry.LastMean(state), out var mean, out var residue) &&
            mean.Ticks == 0 && residue is Relation,
            "A positive quarter-tick mean retains residue; policy uses the signed aggregate rather than truncating it to neutral.");
        Require(!Equation.TryReadPrediction(ComparativeRetry.Mean(state), out _, out _),
            "The next empty block has an open mean rather than borrowing the completed answer.");
    }

    private static void NeutralAndMissingAreDifferent()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = ComparativeRetry.Create(loss, clock, loss, clock);
        for (var time = 1; time <= 4; time++) Observe(state, loss, clock, money, time, 0, 1);
        var deadline = ComparativeRetry.RetryAfter(state).Ticks;
        ComparativeRetry.Observe(state, L(clock, 6), L(loss, 0), null, L(money, 1), new Placeholder());
        Require(ComparativeRetry.Unknown(state).Value == 1 && ComparativeRetry.BlockCount(state).Value == 0 &&
            ComparativeRetry.Evaluated(state).Value == 4 && ComparativeRetry.Rung(state).Value == 1 &&
            ComparativeRetry.RetryAfter(state).Ticks == deadline,
            "An unavailable paid outcome neither enters the resolved block nor changes the retry window.");
        for (var time = 7; time <= 10; time++) Observe(state, loss, clock, money, time, 2, 2);
        Require(ComparativeRetry.Rung(state).Value == 1 && ComparativeRetry.RetryAfter(state).Ticks == deadline &&
            ComparativeRetry.Neutral(state).Value == 4 && ComparativeRetry.CostlyTies(state).Value == 4 &&
            ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, 10))),
            "A neutral block preserves resistance and its old deadline; costly ties do not create an inaccuracy penalty.");
        var before = Canonical(state);
        ComparativeRetry.Observe(state, L(clock, 11), L(loss, 0), L(loss, 1), L(money, 0), new Placeholder());
        ComparativeRetry.Observe(state, L(clock, 11), L(loss, 0), L(loss, 1), null, new Placeholder());
        Require(Canonical(state) == before, "A free notification or missing payment receipt cannot masquerade as a resolved paid trial.");
    }

    private static void EventualRetryAndRecovery()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = ComparativeRetry.Create(loss, clock, loss, clock); long time = 0;
        for (var block = 0; block < 3; block++)
        {
            for (var j = 0; j < 4; j++) Observe(state, loss, clock, money, ++time, 0, 1);
            time = ComparativeRetry.RetryAfter(state).Ticks;
        }
        Require(ComparativeRetry.Rung(state).Value == 3, "Repeated inferior blocks retain increased resistance.");
        for (var expectedRung = 2; expectedRung >= 0; expectedRung--)
        {
            Require(ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, time))), "The capped delay always leaves a future re-entry point.");
            for (var j = 0; j < 4; j++) Observe(state, loss, clock, money, ++time, 3, 1);
            Require(ComparativeRetry.Rung(state).Value == (ulong)expectedRung,
                "Fresh positive comparative evidence gradually lowers prior retry resistance after a source change.");
            time = ComparativeRetry.RetryAfter(state).Ticks;
        }
        Require(ComparativeRetry.Helpful(state).Value == 12 && ComparativeRetry.Harmful(state).Value == 12 &&
            ComparativeRetry.Benefit(state).Value == 12,
            "Recovery keeps prior harmful evidence in totals while the policy responds to recent completed blocks.");
    }

    private static void IndependentUsesAndOpenBases()
    {
        var direction = new Carrier(); var magnitude = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var source = N.Group(new Carrier());
        var first = ComparativeRetry.Create(N.Group(direction), source, direction, clock);
        var second = ComparativeRetry.Create(N.Group(magnitude), source, magnitude, clock);
        for (var time = 1; time <= 4; time++)
        {
            Observe(first, direction, clock, money, time, 2, 0);
            Observe(second, magnitude, clock, money, time, 1, 4);
        }
        Require(ComparativeRetry.Rung(first).Value == 0 && ComparativeRetry.Rung(second).Value == 1 &&
            ReferenceEquals(ComparativeRetry.Source(first), ComparativeRetry.Source(second)),
            "The same physical supplier can remain useful for direction while magnitude use independently resists it.");
        var before = Canonical(first);
        ComparativeRetry.Observe(first, L(Carrier.Unknown, 5), L(direction, 1), L(direction, 0), L(money, 1), new Placeholder());
        Require(Canonical(first) == before && !ComparativeRetry.Pass(ComparativeRetry.Ready(first, L(new Carrier(), 5))),
            "An unbound or unaligned opportunity clock cannot establish timing or accept an outcome.");
        ComparativeRetry.Observe(first, L(clock, 5), L(magnitude, 1), L(direction, 0), L(money, 1), new Placeholder());
        Require(ComparativeRetry.Unknown(first).Value == 1 && ComparativeRetry.Evaluated(first).Value == 4,
            "A foreign loss basis remains unknown rather than silently contributing to the direction block.");
    }

    private static void BoundedPolicyAndReadOnlyQueries()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier();
        var state = ComparativeRetry.Create(loss, clock, loss, clock, maximumRung: 1);
        for (var time = 1; time <= 8; time++) Observe(state, loss, clock, money, time, 0, 2);
        Require(ComparativeRetry.Rung(state).Value == 1 && ComparativeRetry.RetryAfter(state).Ticks == 10 &&
            N.Parts((Relation)N.Parts(ComparativeRetry.Policy(state))[1]).Length == 2,
            "The fixed-delay control is a native two-rung policy, not a hidden classifier override.");
        var before = Canonical(state);
        for (var i = 0; i < 20; i++)
        {
            Require(!ComparativeRetry.Pass(ComparativeRetry.Ready(state, L(clock, 9))), "Repeated notices cannot shorten the opportunity-clock delay.");
            _ = ComparativeRetry.LastMean(state); _ = ComparativeRetry.Mean(state);
        }
        Require(Canonical(state) == before, "Readiness and mean inspection do not mutate evidence or retry state.");
    }

    private static void NativeTransportAndLatestBlock()
    {
        var loss = new Carrier(); var clock = new Carrier(); var money = new Carrier(); var source = N.Group(L(loss, 0));
        var use = N.Group(L(loss, 0), L(clock, 0)); var state = ComparativeRetry.Create(use, source, loss, clock);
        for (var time = 1; time <= 4; time++) Observe(state, loss, clock, money, time, 0, 1);
        var firstMean = ComparativeRetry.LastMean(state);
        var root = N.Group(state, use, source, loss, clock, money, firstMean);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone), "Native transport preserves policy, reusable equations, signed sums and clock aliases.");
        var p = N.Parts(clone); var copied = (Relation)p[0];
        Require(ReferenceEquals(ComparativeRetry.Use(copied), p[1]) && ReferenceEquals(ComparativeRetry.Source(copied), p[2]),
            "The use and supplier remain actual shared structure after restore.");
        for (var time = 6; time <= 9; time++)
        {
            Observe(state, loss, clock, money, time, 2, 0);
            Observe(copied, (Carrier)p[3], (Carrier)p[4], (Carrier)p[5], time, 2, 0);
        }
        Require(Canonical(root) == Canonical(clone) && ComparativeRetry.CompletedBlocks(state).Value == 2 &&
            ((Carrier)N.Parts(ComparativeRetry.LastBlock(state))[1]).Value == 8,
            "Restored state continues identically, retaining only the latest completed block in its live slots.");
        Require(Equation.TryReadPrediction(firstMean, out var old, out _) && old.Ticks == -1,
            "A captured earlier mean is not rewritten when the live latest-block slots move.");
    }

    private static Relation Observe(Relation state, Carrier loss, Carrier clock, Carrier money, long time, long baseline, long supplied) =>
        ComparativeRetry.Observe(state, L(clock, time), L(loss, baseline), L(loss, supplied), L(money, 1), N.Group(L(clock, time)));
    private static CarrierLocation L(Carrier basis, long ticks) => new(basis, ticks);
    private static string Canonical(Relation root)
    {
        var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
        return JsonSerializer.Serialize(snapshot with
        {
            Carriers = snapshot.Carriers.Select(row => row with { Id = 0 }).ToArray(),
            TickRefs = snapshot.TickRefs.Select(row => row with { Id = 0 }).ToArray(),
            Relations = snapshot.Relations.Select(row => row with { Id = 0 }).ToArray(),
            UnitShapes = snapshot.UnitShapes.Select(row => row with { Id = 0 }).ToArray()
        });
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Comparative retry check failed: " + message);
    }
}
