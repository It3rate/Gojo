using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using N = ConnectionAssembly.NativeMath;

namespace FailureSubscriptions;

internal static class ProspectiveOfferChecks
{
    public static int Run()
    {
        FrozenQuoteAndConditionalCounts();
        MissingAndMismatchedContext();
        MeanResolutionResidue();
        BasisAndFutureWindow();
        NativeTransport();
        SignedInputPressure();
        return 6;
    }

    private static void FrozenQuoteAndConditionalCounts()
    {
        var signal = new Carrier();
        var clock = new Carrier();
        var source = N.Group(new CarrierLocation(signal, 0));
        var state = ProspectiveOffer.Create(source, Relation.Segment(signal, -32, -1));
        var trigger = new CarrierLocation(signal, -4);
        var time = new CarrierLocation(clock, 10);
        var cold = ProspectiveOffer.Quote(state, trigger, time);
        Require(ProspectiveOffer.Matches(cold) && !ProspectiveOffer.TryExpected(cold, out _),
            "A cold matching offer remains eligible for exploration without inventing a value from 0/0.");
        var countsBefore = N.Parts(ProspectiveOffer.Counts(cold)).Cast<CarrierLocation>().Select(v => v.Ticks).ToArray();
        var provenance = N.Group(source, trigger, new CarrierLocation(clock, 11));
        var witness = ProspectiveOffer.Observe(state, cold, new CarrierLocation(signal, 3), provenance);
        Require(ReferenceEquals(N.Parts(witness)[2], provenance), "Count changes retain the supplied later outcome witness.");
        Require(N.Parts(ProspectiveOffer.Counts(cold)).Cast<CarrierLocation>().Select(v => v.Ticks).SequenceEqual(countsBefore) &&
            !ProspectiveOffer.TryExpected(cold, out _), "A later outcome cannot rewrite the offer already issued.");
        trigger.Tick.Set(4);
        time.Tick.Set(99);
        Require(ProspectiveOffer.Matches(cold) && ((CarrierLocation)N.Parts(cold)[1]).Ticks == -4 &&
            ((CarrierLocation)N.Parts(cold)[2]).Ticks == 10, "Retargeting live trigger/time sites cannot change a captured quote.");
        var warm = ProspectiveOffer.Quote(state, new CarrierLocation(signal, -4), new CarrierLocation(clock, 20));
        Require(ProspectiveOffer.TryExpected(warm, out var expected) && expected.Ticks == 15 &&
            ProspectiveOffer.Helpful(state).Value == 1 && ProspectiveOffer.Evaluated(state).Value == 1,
            "Future aggregate benefit derives from previous conditional resolved outcomes.");
    }

    private static void MissingAndMismatchedContext()
    {
        var signal = new Carrier();
        var state = ProspectiveOffer.Create(N.Group(signal), Relation.Segment(signal, 1, 32));
        var clock = new Carrier();
        var quote = ProspectiveOffer.Quote(state, new CarrierLocation(signal, 2), new CarrierLocation(clock, 0));
        ProspectiveOffer.Observe(state, quote, null, new Placeholder());
        Require(ProspectiveOffer.Unknown(state).Value == 1 && ProspectiveOffer.Evaluated(state).Value == 0 &&
            ProspectiveOffer.Helpful(state).Value == 0, "A missing outcome contributes only to explicitly unknown coverage.");
        var missing = ProspectiveOffer.Quote(state, new CarrierLocation(signal, 2), new CarrierLocation(clock, 5));
        Require(!ProspectiveOffer.TryExpected(missing, out _) &&
            ((CarrierLocation)N.Parts(ProspectiveOffer.Counts(missing))[2]).Ticks == 1,
            "Unknown-only history has no empirical mean, while its coverage remains visible.");
        foreach (var trigger in new[] { new CarrierLocation(signal, -2), new CarrierLocation(signal, 0),
                     new CarrierLocation(signal, 33), new CarrierLocation(new Carrier(), 2) })
        {
            var mismatch = ProspectiveOffer.Quote(state, trigger, new CarrierLocation(clock, 7));
            var before = Canonical(state);
            Require(!ProspectiveOffer.Matches(mismatch) && !ProspectiveOffer.TryExpected(mismatch, out _),
                "Out-of-range, zero and unaligned triggers do not borrow a conditional cohort.");
            ProspectiveOffer.Observe(state, mismatch, new CarrierLocation(signal, 8), new Placeholder());
            Require(Canonical(state) == before, "A mismatched conditional quote cannot accumulate an outcome.");
        }
        var foreign = ProspectiveOffer.Create(N.Group(signal), Relation.Segment(signal, 1, 32));
        ProspectiveOffer.Observe(foreign, quote, new CarrierLocation(signal, 1), new Placeholder());
        Require(ProspectiveOffer.Evaluated(foreign).Value == 0, "A copied-looking state cannot consume another state's quote.");
    }

    private static void MeanResolutionResidue()
    {
        var signal = new Carrier();
        var state = ProspectiveOffer.Create(signal, Relation.Segment(signal, -32, -1));
        var trigger = new CarrierLocation(signal, -2);
        var clock = new Carrier();
        var quote = ProspectiveOffer.Quote(state, trigger, new CarrierLocation(clock, 0));
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, 1), new Placeholder());
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, 0), new Placeholder());
        quote = ProspectiveOffer.Quote(state, trigger, new CarrierLocation(clock, 10));
        var counts = N.Parts(ProspectiveOffer.Counts(quote)).Cast<CarrierLocation>().ToArray();
        Require(counts[0].Ticks == 1 && counts[1].Ticks == 2 && counts[3].Ticks == 1,
            "The unreduced empirical count pair and relief numerator remain separate.");
        Require(Equation.TryReadPrediction(ProspectiveOffer.Expected(quote), out var expected, out var residue) &&
            expected.Ticks == 2 && residue is Relation,
            "The 5/2 aggregate read is two whole ticks plus explicit fractional source residue.");
    }

    private static void BasisAndFutureWindow()
    {
        var signal = new Carrier();
        var source = N.Group(new CarrierLocation(signal, 1));
        var state = ProspectiveOffer.Create(source, Relation.Segment(signal, 1, 32));
        var clock = new Carrier();
        var quote = ProspectiveOffer.Quote(state, new CarrierLocation(signal, 1), new CarrierLocation(clock, 12), 5);
        var future = ProspectiveOffer.Future(quote);
        Require(future.Left is CarrierLocation start && future.Right is CarrierLocation end &&
            start.Carrier.SameIdentityAs(clock) && end.Carrier.SameIdentityAs(clock) && start.Ticks == 12 && end.Ticks == 17,
            "The requested arrival interval retains its original clock basis and ordered endpoints.");
        Require(ReferenceEquals(ProspectiveOffer.Source(state), source), "The source attach point remains physically connected.");
        foreach (var horizon in new[] { -1, 0, 6 })
        {
            var invalid = ProspectiveOffer.Quote(state, new CarrierLocation(signal, 1), new CarrierLocation(clock, 12), horizon);
            Require(!ProspectiveOffer.Matches(invalid) && !ProspectiveOffer.TryExpected(invalid, out _),
                "An unavailable requested horizon remains an unresolved offer rather than a guessed extension.");
        }
    }

    private static void NativeTransport()
    {
        var signal = new Carrier();
        var state = ProspectiveOffer.Create(N.Group(signal), Relation.Segment(signal, -32, -1));
        var quote = ProspectiveOffer.Quote(state, new CarrierLocation(signal, -2), new CarrierLocation(new Carrier(), 3));
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, -3), new Placeholder());
        var root = N.Group(state, quote, signal);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone), "Native transport preserves conditional basis, captured quote and current signed evidence.");
        var copied = N.Parts(clone);
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, 1), new Placeholder());
        ProspectiveOffer.Observe((Relation)copied[0], (Relation)copied[1], new CarrierLocation((Carrier)copied[2], 1), new Placeholder());
        Require(Canonical(root) == Canonical(clone), "Restored aliases continue to accumulate the same prospective evidence.");
    }

    private static void SignedInputPressure()
    {
        var signal = new Carrier();
        var state = ProspectiveOffer.Create(signal, Relation.Segment(signal, 1, 32));
        var quote = ProspectiveOffer.Quote(state, new CarrierLocation(signal, 2), new CarrierLocation(new Carrier(), 0));
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, -6), new Placeholder());
        Require(ProspectiveOffer.Benefit(state).Value == 0 && ProspectiveOffer.Benefit(state).Tension == -6,
            "Harm enters through negative native pressure and is not clipped to zero benefit.");
        ProspectiveOffer.Observe(state, quote, null, new Placeholder());
        ProspectiveOffer.Observe(state, quote, new CarrierLocation(signal, 2), new Placeholder());
        Require(ProspectiveOffer.Benefit(state).Tension == -4 && ProspectiveOffer.Evaluated(state).Value == 2 &&
            ProspectiveOffer.Helpful(state).Value == 1 && ProspectiveOffer.Unknown(state).Value == 1,
            "Later useful input only offsets its actual signed relief; unknowns do not erase prior harm.");
    }

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
        if (!condition) throw new InvalidOperationException("Prospective offer check failed: " + message);
    }
}
