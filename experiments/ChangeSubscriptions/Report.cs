using Gojo.Core;
using C = ChangeSubscriptions.ChangeSurface;
using R = ChangeSubscriptions.ComparativeRetry;
using S = ChangeSubscriptions.NativeExperiment;

namespace ChangeSubscriptions;

// All records below are diagnostic projections. They are never fed to actors.
internal sealed record ConsumerState(long Paid, long Error, long ReferenceError,
    long Evaluated, long Benefit, long Helpful, long Harmful, long Neutral, long Unknown,
    long CostlyTies, long Rung, long RetryAfter, long BlockCount, long BlockBenefit,
    long Notifications, long Joins, long Leaves, long Skips, bool Attached, long? Current);
internal sealed record ViewState(long Revisions, long Validations, bool Valid, long Held,
    long IntervalStart, long IntervalEnd);
internal sealed record WorldState(long Arrival, long Opportunity, long? Source,
    ConsumerState[] Consumers, ViewState[] Views, long CoarseNotifications);
internal sealed record PhaseRow(string Phase, long Arrivals, long Opportunities,
    ConsumerState[] Changes, long[] PublicationChanges, long CoarseNotifications,
    long[] StartRungs, long[] EndRungs);

internal static class Report
{
    public static long Number(Carrier carrier) => C.Signed(carrier).Ticks;
    public static WorldState State(Relation root) => new(S.Arrival(root).Ticks, S.Opportunity(root).Ticks,
        S.SourcePresence(root).Ticks == 1 ? S.Source(root).Ticks : null,
        S.Consumers(root).Select(consumer =>
        {
            var r = S.Retry(consumer); var sub = S.Subscriber(consumer);
            return new ConsumerState(Number(S.Counter(consumer, 0)), Number(S.Counter(consumer, 1)), Number(S.Counter(consumer, 2)),
                Number(R.Evaluated(r)), Number(R.Benefit(r)), Number(R.Helpful(r)), Number(R.Harmful(r)), Number(R.Neutral(r)),
                Number(R.Unknown(r)), Number(R.CostlyTies(r)), Number(R.Rung(r)), R.RetryAfter(r).Ticks,
                Number(R.BlockCount(r)), Number(R.BlockBenefit(r)), Number(C.NotificationCount(sub)),
                Number(S.Counter(consumer, 3)), Number(S.Counter(consumer, 4)), Number(S.Counter(consumer, 5)),
                C.Attached(sub), C.TryReceived(sub, out var value, out _) ? value.Ticks : null);
        }).ToArray(),
        S.Views(root).Select(view => new ViewState(Number(C.Revision(view)), Number(C.ValidationCount(view)),
            C.Validity(view).Ticks == 1, C.Current(view).Ticks,
            ((CarrierLocation)C.Interval(view).Left).Ticks, ((CarrierLocation)C.Interval(view).Right).Ticks)).ToArray(),
        Number(C.NotificationCount(S.CoarseSubscriber(root))));

    public static PhaseRow Phase(string name, WorldState before, WorldState after) => new(name,
        after.Arrival - before.Arrival, after.Opportunity - before.Opportunity,
        before.Consumers.Zip(after.Consumers, Difference).ToArray(),
        before.Views.Zip(after.Views, (a, b) => b.Revisions - a.Revisions).ToArray(),
        after.CoarseNotifications - before.CoarseNotifications,
        before.Consumers.Select(c => c.Rung).ToArray(), after.Consumers.Select(c => c.Rung).ToArray());

    private static ConsumerState Difference(ConsumerState a, ConsumerState b) => new(
        b.Paid-a.Paid,b.Error-a.Error,b.ReferenceError-a.ReferenceError,b.Evaluated-a.Evaluated,b.Benefit-a.Benefit,
        b.Helpful-a.Helpful,b.Harmful-a.Harmful,b.Neutral-a.Neutral,b.Unknown-a.Unknown,b.CostlyTies-a.CostlyTies,
        b.Rung,b.RetryAfter,b.BlockCount,b.BlockBenefit,b.Notifications-a.Notifications,b.Joins-a.Joins,
        b.Leaves-a.Leaves,b.Skips-a.Skips,b.Attached,b.Current);

    public static object Arrival(InputEvent sample, Relation root, Relation frame)
    {
        var p = S.Parts(frame);
        return new
        {
            Step = S.Arrival(root).Ticks, sample.Phase, sample.Level,
            Opportunity = S.Opportunity(root).Ticks,
            ActualDelta = C.Scalar((Relation)p[2], out var actual) ? (long?)actual.Ticks : null,
            NextPrediction = S.SourcePresence(root).Ticks == 1 ? (long?)S.Source(root).Ticks : null,
            Outcomes = S.Parts((Relation)p[3]).Cast<Relation>().Select(outcome =>
            {
                var o = S.Parts(outcome);
                var promise = o[0] as Relation;
                return new
                {
                    Prior = promise is not null && S.Parts(promise)[2] is CarrierLocation forecast ? (long?)forecast.Ticks : null,
                    Error = o[4] is CarrierLocation e ? (long?)e.Ticks : null,
                    Reference = o[5] is CarrierLocation b ? (long?)b.Ticks : null,
                    Used = ((CarrierLocation)o[6]).Ticks, Evaluated = ((CarrierLocation)o[7]).Ticks
                };
            }).ToArray(),
            Uses = S.Consumers(root).Select(consumer => new
            {
                Attached = C.Attached(S.Subscriber(consumer)),
                Rung = Number(R.Rung(S.Retry(consumer))),
                RetryAt = R.RetryAfter(S.Retry(consumer)).Ticks,
                Notifications = Number(C.NotificationCount(S.Subscriber(consumer)))
            }).ToArray(),
            Views = S.Views(root).Select(v => new { Held = C.Current(v).Ticks, Valid = C.Validity(v).Ticks,
                Revision = Number(C.Revision(v)) }).ToArray()
        };
    }
}
