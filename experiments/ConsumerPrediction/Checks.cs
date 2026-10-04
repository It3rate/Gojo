using Gojo.Core;
using Gojo.Structures;
using P = ConsumerPrediction.PredictionProbe;

namespace ConsumerPrediction;

/// <summary>Checks the experimental contracts on small explicit native cases.
/// Trial-level superiority, adaptation quality and hypothesis success are
/// observations for the report, not conditions for these checks to pass.</summary>
public static class Checks
{
    public static string[] Run()
    {
        var passed = new List<string>();
        RunOne("Known zero, absent input and bounded warmup remain distinct native reads.", MissingAndZero);
        RunOne("A prediction is issued before its outcome; recruitment affects a later issue only.", ProspectiveRecruitment);
        RunOne("Signed negative contributions include receiver tension and close without accumulating stale output.", SignedContribution);
        RunOne("Demand and tolerance belong to the issued obligation, not the later observation.", FrozenObligation);
        RunOne("A missing immediate outcome remains unresolved and is never scored against a later known sample.", MissingImmediateOutcome);
        RunOne("Harm reduces support even when the baseline already satisfies the consumer.", HarmAndWithdrawal);
        RunOne("Identical candidate and baseline have zero native marginal response.", RedundantResponse);
        RunOne("Direction disagreement remains distinct from equal absolute scale.", DirectionAndScale);
        RunOne("Repeated inspection cannot refresh support, change attachments or retarget input sites.", InspectionIsPure);
        RunOne("Current forecasts recalculate from existing retargetable native sites.", NativeRetargeting);
        RunOne("Captured issue numbers survive subsequent changes to the live predictor.", FrozenIssueNumbers);
        RunOne("Persistent equations and one pending issue do not accumulate an observation archive.", BoundedLiveWorld);
        RunOne("Longer flat gaps add neither eligible reinforcement nor missed turn opportunities.", QuietGapControl);
        RunOne("Stale callers and invalid host tolerance cannot partially change the live world.", RejectedAdmission);
        return passed.ToArray();

        void RunOne(string description, Action check)
        {
            check();
            passed.Add(description);
        }
    }

    private static void MissingAndZero()
    {
        var root = P.CreateScaleRestoration();
        Require(!P.ForecastReady(P.Forecast(root)), "An unobserved source has no forecast.");
        Observe(ref root, 0);
        Observe(ref root, 0);
        Require(!P.ForecastReady(P.Forecast(root)), "Two levels do not supply two changes.");
        Observe(ref root, 0);
        Require(P.ForecastReady(P.Forecast(root)) && !P.ForecastEligible(P.Forecast(root)),
            "Three known zeros supply a flat read without inventing a turn.");
        Require(ForecastNumber(P.Pending(root)!, 1) == 0, "A known zero delta is readable.");
        Observe(ref root, null);
        Require(!P.ForecastReady(P.Forecast(root)), "Missing input withdraws native presence.");
        Observe(ref root, 0);
        Observe(ref root, 0);
        Require(!P.ForecastReady(P.Forecast(root)), "Old retained coordinates cannot bridge a missing sample.");
        Observe(ref root, 0);
        Require(P.ForecastReady(P.Forecast(root)), "Three new known levels restore readiness.");
    }

    private static void ProspectiveRecruitment()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        var issued = P.Pending(root)!;
        Require(P.ForecastEligible((Relation)P.Parts(issued)[2]), "The observed turn qualifies the next issue.");
        Require(ForecastNumber(issued, 1) == -6 && ForecastNumber(issued, 3) == -2,
            "Both next-change predictions are already concrete before the next level arrives.");
        Require(!P.Attached(root) && Number(issued, 6) == 0 && Number(issued, 5) == -6,
            "An untested shadow cannot be retroactively delivered.");
        var outcome = Observe(ref root, -4);
        Require(ReferenceEquals(P.Parts(outcome)[0], issued), "The next outcome names the issued obligation.");
        Require(Response(outcome, 2).Reduction == 4 && Response(outcome, 3).Reduction == 0,
            "Shadow improvement and actual baseline-only delivery remain separate.");
        Require(P.Support(root).Value == 4 && P.Attached(root), "The native signed response admits later participation.");
        Turn(ref root);
        Require(Number(P.Pending(root)!, 6) == 1 && Number(P.Pending(root)!, 5) == -2,
            "The retained publication can contribute to a subsequent issue.");
    }

    private static void SignedContribution()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        Observe(ref root, -4);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            Turn(ref root, mirror: true);
            var issued = P.Pending(root)!;
            Require(Number(issued, 9) == -4 && Number(issued, 5) == 2,
                "A negative correction lowers the baseline forecast through the connected sum.");
            Require(P.Receipt(root).Value == 0 && P.Receipt(root).Tension == -4,
                "Negative published participation is present as signed receiver tension.");
            var outcome = Observe(ref root, 4);
            Require(Response(outcome, 3).Reduction == 4, "The signed delivered result is evaluated on the consumer basis.");
            Require(P.Receipt(root).Value == 0 && P.Receipt(root).Tension == 0 && P.Gate(root).Value == 0,
                "Closing the previous pulse removes both its value and signed remainder.");
        }
    }

    private static void FrozenObligation()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root, demand: true, tolerance: 0);
        var issued = P.Pending(root)!;
        var outcome = Observe(ref root, -4, demand: false, tolerance: 4);
        Require(Number(issued, 3) == 1 && Response(outcome, 2).Reduction == 4 && P.Support(root).Value == 4,
            "Later withdrawal of demand or widening of tolerance cannot rewrite a committed obligation.");

        root = P.CreateScaleRestoration();
        Turn(ref root, demand: false);
        outcome = Observe(ref root, -4, demand: true);
        Require(Response(outcome, 2).Reduction == 4 && P.Support(root).Value == 0 && !P.Attached(root),
            "A newly demanded outcome cannot reward an issue that had no demand.");

        root = P.CreateScaleRestoration();
        Turn(ref root, tolerance: 4);
        outcome = Observe(ref root, -4, tolerance: 0);
        Require(Response(outcome, 2).Before == 0 && Response(outcome, 2).After == 0 && P.Support(root).Value == 0,
            "Both errors satisfy the previously issued inclusive tolerance despite a later tighter read.");
    }

    private static void MissingImmediateOutcome()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        var issued = P.Pending(root)!;
        var missing = Observe(ref root, null);
        Require(ReferenceEquals(P.Parts(missing)[0], issued) &&
            !OperationClaim.TryReadIntervalResponse((Relation)P.Parts(missing)[2], out _, out _, out _, out _, out _, out _),
            "The immediate missing outcome is explicitly unresolved.");
        Require(P.Support(root).Value == 0 && !P.Attached(root), "Missing outcome supplies no reinforcement.");
        var later = Observe(ref root, -4);
        Require(!ReferenceEquals(P.Parts(later)[0], issued) && P.Support(root).Value == 0,
            "A later known level cannot be substituted as that old forecast's outcome.");
    }

    private static void HarmAndWithdrawal()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        Observe(ref root, -4);
        var publication = P.Publication(root);
        Require(P.Receipt(root).AttachedRelations.Contains(publication), "Recruitment physically attaches the retained relation.");
        Turn(ref root);
        var outcome = Observe(ref root, -8);
        var delivered = Response(outcome, 3);
        Require(delivered.Before == 0 && delivered.After == 4 && delivered.Reduction == -4,
            "A contribution can damage a satisfied baseline; zero baseline demand does not hide the harm.");
        Require(P.Support(root).Value == 0 && P.Support(root).Tension == 0 && !P.Attached(root) &&
            !P.Receipt(root).AttachedRelations.Contains(publication), "Exhausted support withdraws actual attachment without negative debt.");
        Require(P.Receipt(root).Value == 0 && P.Receipt(root).Tension == 0,
            "Publication closes before withdrawal, leaving no stale result.");
        Turn(ref root);
        Observe(ref root, -4);
        Require(P.Attached(root) && ReferenceEquals(P.Publication(root), publication),
            "A later qualified probe resumes the same relation identity.");
    }

    private static void RedundantResponse()
    {
        var root = P.CreateRedundant();
        Turn(ref root);
        var outcome = Observe(ref root, -4);
        var response = Response(outcome, 2);
        Require(response.Before == 4 && response.After == 4 && response.Reduction == 0,
            "Equal nonzero errors do not fabricate marginal usefulness.");
        Require(P.Support(root).Value == 0 && !P.Attached(root), "Zero response neither strengthens nor invents participation.");
    }

    private static void DirectionAndScale()
    {
        var root = P.CreateScaleRestoration();
        Observe(ref root, 0);
        Observe(ref root, 2);
        Observe(ref root, 4);
        var outcome = Observe(ref root, 6);
        var evidence = P.Parts((Relation)P.Parts(outcome)[4]);
        var diagnostics = P.Parts((Relation)evidence[2]).Cast<Relation>().ToArray();
        Require(P.DirectionAgrees(diagnostics[0]) && !P.DirectionAgrees(diagnostics[1]) && P.ScaleError(diagnostics[1]) == 0,
            "An opposite prediction can have exact magnitude while failing direction.");
        Require(P.Support(root).Value == 0, "Diagnostic partial agreement cannot override turn eligibility.");
    }

    private static void InspectionIsPure()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        Observe(ref root, -4);
        Turn(ref root, mirror: true);
        var before = GojoNumericFormat.Encode(root);
        for (var i = 0; i < 4; i++)
        {
            var forecast = P.Forecast(root);
            Require(P.ForecastReady(forecast) && P.ForecastEligible(forecast), "Current forecast remains readable.");
            _ = P.Operational(root);
            _ = P.Attached(root);
            _ = P.Pending(root);
            _ = P.LatestOutcome(root);
        }
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)),
            "Pure inspection cannot retarget structure, refresh support, or settle output.");
    }

    private static void NativeRetargeting()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        var equation = P.Equations(root)[1];
        var last = (Relation)P.Parts(P.InputSites(root))[2];
        var coordinate = (CarrierLocation)last.Left;
        var previous = coordinate.Ticks;
        coordinate.Tick.Set(-8);
        var reads = P.Parts((Relation)P.Parts(P.Forecast(root))[0]).Cast<Relation>().ToArray();
        Require(P.Value(reads[1]).Ticks == -12 && P.Value(reads[3]).Ticks == -2 && P.Value(reads[4]).Ticks == 10,
            "Retargeting a current input recalculates the persistent difference and correction equations.");
        Require(ReferenceEquals(P.Equations(root)[1], equation), "A different forecast does not require a replacement equation.");
        coordinate.Tick.Set(previous);
        Require(ForecastNumber(P.Pending(root)!, 1) == -6,
            "A changed live input does not alter the previously issued forecast.");
    }

    private static void FrozenIssueNumbers()
    {
        var root = P.CreateScaleRestoration();
        Turn(ref root);
        var issued = P.Pending(root)!;
        var premise = (Relation)P.Parts(issued)[8];
        var coordinates = P.Parts(premise).Cast<Relation>().Select(item => ((CarrierLocation)item.Left).Ticks).ToArray();
        Observe(ref root, 100);
        Turn(ref root, mirror: true, demand: false, tolerance: 7);
        Require(ForecastNumber(issued, 1) == -6 && ForecastNumber(issued, 3) == -2 &&
            Number(issued, 3) == 1 && Number(issued, 5) == -6 && Number(issued, 6) == 0,
            "Selected historical predictions and obligation participation stay fixed.");
        Require(coordinates.SequenceEqual(P.Parts(premise).Cast<Relation>().Select(item => ((CarrierLocation)item.Left).Ticks)),
            "Frozen three-level premises do not follow retargeted live sites.");
    }

    private static void BoundedLiveWorld()
    {
        var root = P.CreateScaleRestoration();
        var operational = P.Operational(root);
        var equations = P.Equations(root);
        var publication = P.Publication(root);
        var oldRootIds = new List<ulong>();
        (int Carriers, int Relations)? stableSize = null;
        for (var episode = 0; episode < 16; episode++)
        {
            oldRootIds.Add(root.Id);
            Turn(ref root);
            Observe(ref root, -4);
            Observe(ref root, -6);
            Require(ReferenceEquals(P.Operational(root), operational) &&
                equations.Zip(P.Equations(root)).All(pair => ReferenceEquals(pair.First, pair.Second)) &&
                ReferenceEquals(P.Publication(root), publication), "Observations reuse the operational equations and publication.");
            Require(P.Parts(root).Length == 4 && P.Parts(P.Pending(root)!).Length == 10,
                "One current issue and the latest outcome remain the bounded learner surface.");
            if (episode < 3) continue;
            var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
            var size = (snapshot.Carriers.Length, snapshot.Relations.Length);
            stableSize ??= size;
            Require(size == stableSize.Value, "Repeated equal-shape episodes do not grow retained graph size.");
            Require(!snapshot.Relations.Any(relation => oldRootIds.Contains(relation.Id)),
                "Prior root envelopes are not retained as an observation archive.");
        }
    }

    private static void QuietGapControl()
    {
        var compact = Run(0);
        var padded = Run(24);
        Require(compact.SequenceEqual(padded), "Extra terminal flat observations preserve the ordered eligible response/support trace.");

        static List<(long Relief, ulong Support)> Run(int flats)
        {
            var root = P.CreateScaleRestoration();
            var trace = new List<(long, ulong)>();
            for (var episode = 0; episode < 4; episode++)
            {
                Turn(ref root);
                var outcome = Observe(ref root, -4);
                trace.Add((Response(outcome, 2).Reduction, P.Support(root).Value));
                Observe(ref root, -6);
                var support = P.Support(root).Value;
                for (var i = 0; i < flats; i++)
                {
                    Observe(ref root, -6);
                    Require(!P.ForecastEligible(P.Forecast(root)) && P.Support(root).Value == support,
                        "Flat padding neither issues a turn promise nor decays opportunity-qualified support.");
                }
            }
            return trace;
        }
    }

    private static void RejectedAdmission()
    {
        var stale = P.CreateScaleRestoration();
        var root = stale;
        Turn(ref root);
        var before = GojoNumericFormat.Encode(root);
        Require(!P.TryObserve(stale, -4, true, 0, out _, out _), "A stale admission token is rejected.");
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "Stale admission cannot close or retarget current output.");
        Require(!P.TryObserve(root, -4, true, -1, out _, out _), "Negative host tolerance is rejected before mutation.");
        Require(before.AsSpan().SequenceEqual(GojoNumericFormat.Encode(root)), "Invalid host tolerance leaves the current native world untouched.");
    }

    private static void Turn(ref Relation root, bool mirror = false, bool demand = true, long tolerance = 0)
    {
        Observe(ref root, null, demand, tolerance);
        foreach (var level in mirror ? new long[] { 0, -2, -4, 2 } : new long[] { 0, 2, 4, -2 })
            Observe(ref root, level, demand, tolerance);
    }

    private static Relation Observe(ref Relation root, long? value, bool demand = true, long tolerance = 0)
    {
        Require(P.TryObserve(root, value, demand, tolerance, out var next, out var outcome), "The supplied observation is admitted.");
        root = next;
        return outcome;
    }

    private static (long Before, long After, long Reduction) Response(Relation outcome, int field)
    {
        Require(P.Parts(outcome)[field] is Relation, "The selected outcome exposes a response witness.");
        Require(OperationClaim.TryReadIntervalResponse((Relation)P.Parts(outcome)[field], out _, out _, out _,
            out var before, out var after, out var reduction), "The paired response resolves on one consumer interval.");
        return (before.Ticks, after.Ticks, reduction.Ticks);
    }

    private static long ForecastNumber(Relation issue, int field) =>
        P.Value((Relation)P.Parts((Relation)P.Parts((Relation)P.Parts(issue)[2])[0])[field]).Ticks;
    private static long Number(Relation group, int field) => ((CarrierLocation)P.Parts(group)[field]).Ticks;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Consumer prediction check failed: " + message);
    }
}
