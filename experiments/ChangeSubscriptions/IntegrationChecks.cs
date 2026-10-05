using System.Text.Json;
using Gojo.Core;
using C = ChangeSubscriptions.ChangeSurface;
using E = ChangeSubscriptions.NativeExperiment;
using R = ChangeSubscriptions.ComparativeRetry;

namespace ChangeSubscriptions;

internal static class IntegrationChecks
{
    public static int Run()
    {
        FrozenNextPrediction();
        IndependentUsefulness();
        OccasionalReversalInUsefulBlock();
        NotificationsAreNotEvaluations();
        GapsAndTwoObservationRecovery();
        ControlShapesAndThrottling();
        StaleEnvelopeRejectsDuplicateOutcome();
        TransportAndBoundedContinuation();
        return 8;
    }

    private static void FrozenNextPrediction()
    {
        var root = E.Create(); E.Observe(ref root, 0);
        var issued = E.Observe(ref root, 1);
        var promises = E.Parts((Relation)E.Parts(issued)[7]).Cast<Relation>().ToArray();
        var direction = E.Parts(promises[0]); var magnitude = E.Parts(promises[1]);
        Require(Number(direction[2]) == 1 && Number(magnitude[2]) == 3 && E.Source(root).Ticks == 3,
            "The supplied source publishes current-delta plus two direction ticks for the next input opportunity.");
        var evaluatedBefore = R.Evaluated(E.Retry(E.Consumers(root)[0])).Value;
        var reversal = E.Observe(ref root, -1);
        var outcomes = E.Parts((Relation)E.Parts(reversal)[3]).Cast<Relation>().Select(E.Parts).ToArray();
        Require(Number(outcomes[0][4]) == 2 && Number(outcomes[0][5]) == 1 &&
            Number(outcomes[1][4]) == 5 && Number(outcomes[1][5]) == 2,
            "The next actual delta minus two judges the frozen positive promises, rather than the newly computed negative prediction.");
        Require(Number(direction[2]) == 1 && Number(magnitude[2]) == 3 && E.Source(root).Ticks == -4 &&
            R.Evaluated(E.Retry(E.Consumers(root)[0])).Value == evaluatedBefore + 1,
            "Updating the live source cannot rewrite the prior receipt or create more than one paid outcome.");
        Require(ReferenceEquals(outcomes[0][0], promises[0]) && ReferenceEquals(outcomes[1][0], promises[1]),
            "Each result connects back to the exact promise whose later usefulness was measured.");
    }

    private static void IndependentUsefulness()
    {
        var root = E.Create();
        for (var level = 0; level <= 5; level++) E.Observe(ref root, level);
        var consumers = E.Consumers(root); var direction = E.Retry(consumers[0]); var magnitude = E.Retry(consumers[1]);
        Require(R.Evaluated(direction).Value == 4 && R.Helpful(direction).Value == 4 && Signed(R.Benefit(direction)) == 4 &&
            R.Rung(direction).Value == 0 && C.Attached(E.Subscriber(consumers[0])),
            "The direction use is useful against its zero-change reference during steady increase.");
        Require(R.Evaluated(magnitude).Value == 4 && R.Harmful(magnitude).Value == 4 && Signed(R.Benefit(magnitude)) == -4 &&
            R.Rung(magnitude).Value == 1 && !C.Attached(E.Subscriber(consumers[1])),
            "The very same source overstates amplitude, so its independent magnitude use closes participation after a harmful block.");
        Require(ReferenceEquals(C.Source(E.Surface(consumers[0])).Tick, C.Source(E.Surface(consumers[1])).Tick) &&
            ReferenceEquals(R.Source(direction), R.Source(magnitude)),
            "Independent utility judgments are attached to one actual producer, not separately authored good and bad suppliers.");
        Require(E.Counter(consumers[0], 0).Value == 5 && E.Counter(consumers[1], 0).Value == 4,
            "The delayed direction promise is billed while a blocked magnitude use receives no new paid promise.");
    }

    private static void OccasionalReversalInUsefulBlock()
    {
        var root = E.Create();
        foreach (var level in new long[] { 0, 1, 2, 3, 2, 1 }) E.Observe(ref root, level);
        var direction = E.Retry(E.Consumers(root)[0]);
        Require(R.Evaluated(direction).Value == 4 && R.Helpful(direction).Value == 3 && R.Harmful(direction).Value == 1 &&
            Signed(R.Benefit(direction)) == 2 && R.Rung(direction).Value == 0 && R.CompletedBlocks(direction).Value == 1,
            "One reversal preserves a net-positive direction block instead of treating every mistake as a new resistance penalty.");
    }

    private static void NotificationsAreNotEvaluations()
    {
        var root = E.Create();
        for (var level = 0; level <= 5; level++) E.Observe(ref root, level);
        root = E.Retain(root);
        var consumers = E.Consumers(root); var before = Canonical(root);
        var notifications = consumers.Select(consumer => C.NotificationCount(E.Subscriber(consumer)).Value).ToArray();
        var evaluations = consumers.Select(consumer => R.Evaluated(E.Retry(consumer)).Value).ToArray();
        var retries = consumers.Select(consumer => R.RetryAfter(E.Retry(consumer)).Ticks).ToArray();
        for (var i = 0; i < 12; i++)
        {
            foreach (var consumer in consumers)
            {
                C.Notify(E.Subscriber(consumer)); C.TryReceived(E.Subscriber(consumer), out _, out _);
                C.Advance(E.Surface(consumer), E.Copy(E.Arrival(root)));
                R.Ready(E.Retry(consumer), E.Copy(E.Opportunity(root))); R.Mean(E.Retry(consumer));
            }
        }
        Require(Canonical(root) == before && notifications.SequenceEqual(consumers.Select(c => C.NotificationCount(E.Subscriber(c)).Value)) &&
            evaluations.SequenceEqual(consumers.Select(c => R.Evaluated(E.Retry(c)).Value)) &&
            retries.SequenceEqual(consumers.Select(c => R.RetryAfter(E.Retry(c)).Ticks)),
            "Repeated notification, same-arrival validation and pure reads cannot tick retry clocks, duplicate scores or mutate retained structure.");
        var directionNotifications = C.NotificationCount(E.Subscriber(consumers[0])).Value;
        E.Observe(ref root, 6);
        Require(C.NotificationCount(E.Subscriber(consumers[0])).Value == directionNotifications &&
            R.Evaluated(E.Retry(consumers[0])).Value == evaluations[0] + 1,
            "An unchanged direction can support another later validation without emitting another change notification.");
    }

    private static void GapsAndTwoObservationRecovery()
    {
        var root = E.Create();
        foreach (var level in new long[] { 0, 1, 2 }) E.Observe(ref root, level);
        var consumers = E.Consumers(root); var count = E.Opportunity(root).Ticks;
        var evaluations = consumers.Select(c => R.Evaluated(E.Retry(c)).Value).ToArray();
        var beforeCounts = consumers.Select(c => R.BlockCount(E.Retry(c)).Value).ToArray();
        E.Observe(ref root, null);
        Require(E.Opportunity(root).Ticks == count && consumers.All(c => R.Unknown(E.Retry(c)).Value == 1) &&
            evaluations.SequenceEqual(consumers.Select(c => R.Evaluated(E.Retry(c)).Value)) &&
            beforeCounts.SequenceEqual(consumers.Select(c => R.BlockCount(E.Retry(c)).Value)) &&
            consumers.All(c => !C.TryReceived(E.Subscriber(c), out _, out _)),
            "A gap leaves the opportunity clock and resolved blocks fixed, records missing paid outcomes, and invalidates current receipts.");
        E.Observe(ref root, 3);
        Require(E.Opportunity(root).Ticks == count && E.SourcePresence(root).Ticks == 0 &&
            consumers.All(c => R.Unknown(E.Retry(c)).Value == 1),
            "The first observation after a gap cannot reuse an old sample as the missing adjacent input.");
        E.Observe(ref root, 4);
        Require(E.Opportunity(root).Ticks == count + 1 && E.SourcePresence(root).Ticks == 1 &&
            consumers.All(c => C.TryReceived(E.Subscriber(c), out _, out _)) &&
            evaluations.SequenceEqual(consumers.Select(c => R.Evaluated(E.Retry(c)).Value)),
            "Two consecutive observations restore a current forecast, without retroactively scoring unavailable promises.");
        E.Observe(ref root, 5);
        Require(consumers.Select(c => R.Evaluated(E.Retry(c)).Value).SequenceEqual(evaluations.Select(n => n + 1)),
            "The restored forecast receives its own score only when a subsequent actual observation arrives.");
    }

    private static void ControlShapesAndThrottling()
    {
        var unthrottled = E.Create(throttle: false); var fixedDelay = E.Create(maximumRung: 1); var escalating = E.Create();
        for (var level = 0; level <= 70; level++)
        {
            E.Observe(ref unthrottled, level); unthrottled = E.Retain(unthrottled);
            E.Observe(ref fixedDelay, level); fixedDelay = E.Retain(fixedDelay);
            E.Observe(ref escalating, level); escalating = E.Retain(escalating);
        }
        var freely = E.Consumers(unthrottled)[1]; var fixedUse = E.Consumers(fixedDelay)[1]; var resistant = E.Consumers(escalating)[1];
        Require(C.Attached(E.Subscriber(freely)) && R.Rung(E.Retry(freely)).Value == 5,
            "The unthrottled control still records comparative harm while its explicit policy bypasses retry admission.");
        Require(NativeLadderLength(E.Retry(fixedUse)) == 2 && R.Rung(E.Retry(fixedUse)).Value == 1 &&
            NativeLadderLength(E.Retry(resistant)) == 6 && R.Rung(E.Retry(resistant)).Value >= 2,
            "Fixed-delay and escalating controls differ through their connected finite policy ladders.");
        Require(E.Counter(fixedUse, 5).Value > 0 && E.Counter(resistant, 5).Value > 0 &&
            E.Counter(freely, 5).Value == 0,
            "Retry admission changes actual paid participation, while the bypassed control remains active.");
    }

    private static void TransportAndBoundedContinuation()
    {
        var root = E.Create();
        for (var level = 0; level <= 8; level++) E.Observe(ref root, level);
        root = E.Retain(root);
        var clone = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root)).Single();
        Require(Canonical(root) == Canonical(clone), "Native network transport retains pending forecasts, detached routes, policy, accounting and notification state.");
        var oldCloneTime = E.Arrival(clone).Ticks;
        E.Observe(ref root, 9); root = E.Retain(root);
        Require(E.Arrival(clone).Ticks == oldCloneTime, "Restored operational structure has independent mutable clock identity.");
        E.Observe(ref clone, 9); clone = E.Retain(clone);
        Require(Canonical(root) == Canonical(clone), "The restored pending promises produce the same next accounting, notifications and retry transition.");

        var interrupted = E.Create();
        for (var level = 0; level <= 3; level++) E.Observe(ref interrupted, level);
        E.Observe(ref interrupted, null); interrupted = E.Retain(interrupted);
        var partial = E.Retry(E.Consumers(interrupted)[0]);
        Require(R.BlockCount(partial).Value == 2 && R.Unknown(partial).Value == 1 && E.SourcePresence(interrupted).Ticks == 0,
            "The transport fixture retains a partial resolved block and an explicitly unavailable latest source.");
        var restoredGap = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(interrupted)).Single();
        for (var level = 4; level <= 6; level++)
        {
            E.Observe(ref interrupted, level); interrupted = E.Retain(interrupted);
            E.Observe(ref restoredGap, level); restoredGap = E.Retain(restoredGap);
            Require(Canonical(interrupted) == Canonical(restoredGap),
                "Native transport preserves gap recovery, partial comparative blocks and notification transitions at each later observation.");
        }

        for (var level = 10; level <= 80; level++) { E.Observe(ref root, level); root = E.Retain(root); }
        var early = GojoEncodedSnapshot.CaptureGraph((Relation)E.Parts(root)[0]);
        var retainedEarly = GojoEncodedSnapshot.CaptureGraph(root);
        for (var level = 81; level <= 240; level++) { E.Observe(ref root, level); root = E.Retain(root); }
        var late = GojoEncodedSnapshot.CaptureGraph((Relation)E.Parts(root)[0]);
        var retainedLate = GojoEncodedSnapshot.CaptureGraph(root);
        Require(early.Carriers.Length == late.Carriers.Length && early.Relations.Length == late.Relations.Length &&
            early.TickRefs.Length == late.TickRefs.Length,
            "Repeated observations reuse a bounded operational graph instead of adding a record for every successful value.");
        Require(retainedLate.Carriers.Length <= retainedEarly.Carriers.Length + 8 &&
            retainedLate.Relations.Length <= retainedEarly.Relations.Length + 40,
            "Retain keeps only the fixed operational graph and bounded pending receipts, with no growing historical chain.");
    }

    private static void StaleEnvelopeRejectsDuplicateOutcome()
    {
        var root = E.Create(); var initial = root;
        E.Observe(ref root, 0);
        var afterFirst = Canonical(E.Retain(root)); var priorInitial = initial;
        var rejectedInitial = E.Observe(ref initial, 1);
        Require(!C.Scalar(rejectedInitial, out _) && ReferenceEquals(initial, priorInitial) &&
            Canonical(E.Retain(root)) == afterFirst,
            "Even the stale initial envelope cannot advance its shared live clock or mutate the current world.");
        E.Observe(ref root, 1);
        var pending = root;
        E.Observe(ref root, 2);
        var retry = E.Retry(E.Consumers(root)[0]);
        var evaluations = R.Evaluated(retry).Value;
        var paid = E.Counter(E.Consumers(root)[0], 0).Value;
        var current = Canonical(E.Retain(root)); var staleEnvelope = pending;
        var rejected = E.Observe(ref pending, 3);
        Require(!C.Scalar(rejected, out _) && ReferenceEquals(pending, staleEnvelope) &&
            R.Evaluated(retry).Value == evaluations && E.Counter(E.Consumers(root)[0], 0).Value == paid &&
            Canonical(E.Retain(root)) == current,
            "A stale envelope cannot assess a captured pending promise twice, duplicate its payment or modify shared operational state.");
        E.Observe(ref root, 3);
        Require(R.Evaluated(retry).Value == evaluations + 1,
            "Rejecting stale participation leaves the current envelope able to assess its next promise normally.");
    }

    private static int NativeLadderLength(Relation state) => E.Parts((Relation)E.Parts(R.Policy(state))[1]).Length;
    private static long Number(IElement value) => ((CarrierLocation)value).Ticks;
    private static long Signed(Carrier carrier) => C.Signed(carrier).Ticks;
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
        if (!condition) throw new InvalidOperationException("Change subscription integration check failed: " + message);
    }
}
