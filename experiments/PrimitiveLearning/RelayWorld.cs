using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning;

// External world and artifact projections. None of these fields are learner state.
public sealed record RelayEpisode(int Tick, int Load, bool Fits, int Continuity,
    int PreviousDirection, int ArrivingDirection, bool Unavailable, bool Absent, int ReferenceAge = 1, int FineOffset = 0);

public sealed record RelayCondition(string Name, int Threshold = 8,
    double Persistence = .95, int QueryPrice = 1, int Encoding = 0,
    int MissingEvery = 0, int AbsentEvery = 0, bool NovelLoads = false);

public sealed class RelayWorld
{
    private readonly Dictionary<Relation, int> acquisitions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Relation, RelayEpisode> acquisitionEvents = new(ReferenceEqualityComparer.Instance);
    public Carrier LoadSource { get; } = new();
    public Carrier PresenceAxis { get; } = new();
    public Carrier OrderAxis { get; } = new();
    public Carrier FitAxis { get; } = new();
    public Carrier ContextAxis { get; } = new();
    public Carrier DirectionAxis { get; } = new();
    public Carrier ActionAxis { get; } = new();
    public Carrier UtilityAxis { get; } = new();
    public Carrier CostAxis { get; } = new();
    public Carrier QueryBenefitAxis { get; } = new();
    public RelayCondition Condition { get; }
    public int ComparisonQueries { get; private set; }
    public int FitProbes { get; private set; }
    public int DirectionQueries { get; private set; }
    public int ShadowActions { get; private set; }
    public RelayWorld(RelayCondition condition) => Condition = condition;

    public Relation Acquire(RelayEpisode episode)
    {
        if (episode.Unavailable)
            return ReadWitness.Create(new Carrier(), LoadSource,
                Relation.Segment(PresenceAxis, 0, 1), new Placeholder(),
                new Relation(LoadSource, new Placeholder()), new Placeholder());
        var sample = SamplingSurface.Sample(new Carrier(), LoadSource, PresenceAxis, !episode.Absent);
        if (!episode.Absent)
        {
            acquisitions.Add(sample, Encode(episode.Load * 16 + episode.FineOffset));
            acquisitionEvents.Add(sample, episode);
        }
        return sample;
    }

    public Relation Compare(Relation candidate, Relation anchor)
    {
        ComparisonQueries++;
        if (!acquisitions.TryGetValue(candidate, out var a) || !acquisitions.TryGetValue(anchor, out var b))
            return NativePublication.Open(OrderAxis, new Relation(candidate, anchor));
        return SamplingSurface.Comparison(new Carrier(), candidate, anchor, OrderAxis, a.CompareTo(b));
    }

    public Relation ProbeFit(RelayEpisode episode, Relation sample)
    {
        FitProbes++;
        if (episode.Unavailable || episode.Absent || !acquisitionEvents.TryGetValue(sample, out var selected)
            || !ReferenceEquals(episode, selected)) return NativePublication.Open(FitAxis, sample);
        return NativePublication.Point(FitAxis, episode.Fits ? 1 : 0, sample, 0, 1);
    }

    public Relation Context(RelayEpisode episode) => NativePublication.Point(ContextAxis,
        episode.Continuity, Relation.Point(new Carrier(), episode.Tick), 0, 1);
    public Relation Previous(RelayEpisode episode) => NativePublication.Point(DirectionAxis,
        episode.PreviousDirection, Relation.OrderedGroup(new Carrier(),
            Relation.Point(new Carrier(), episode.Tick - episode.ReferenceAge),
            Relation.Point(new Carrier(), episode.ReferenceAge)), -1, 1);
    public Relation QueryDirection(RelayEpisode episode)
    {
        DirectionQueries++;
        return Arrival(episode);
    }
    // This observation is available only AFTER acting, or within a billed training audit.
    public Relation Arrival(RelayEpisode episode) => episode.Unavailable || episode.Absent
        ? NativePublication.Open(DirectionAxis, Relation.Point(new Carrier(), episode.Tick))
        : NativePublication.Point(DirectionAxis, episode.ArrivingDirection, Relation.Point(new Carrier(), episode.Tick), -1, 1);

    public Relation Execute(RelayEpisode episode, Relation command, int paidFee = 0, bool shadow = false)
    {
        if (shadow) ShadowActions++;
        var action = NativePublication.Read(command, ActionAxis);
        if (action is null || action is < -1 or > 1 || paidFee < 0) return NativePublication.Open(UtilityAxis, command);
        var consequence = action == 0 || episode.Absent ? 0
            : action == (episode.Fits ? episode.ArrivingDirection : -episode.ArrivingDirection) ? 4 : -4;
        // The world publishes the actual resource change; it cannot touch a learner carrier.
        return NativePublication.Point(UtilityAxis, consequence - paidFee, command);
    }

    public static RelayEpisode[] Tape(int seed, int count, RelayCondition condition)
    {
        var random = new Random(seed);
        var events = new RelayEpisode[count];
        var previous = random.Next(2) == 0 ? -1 : 1;
        var lastObserved = previous;
        var lastObservedTick = -1;
        for (var tick = 0; tick < count; tick++)
        {
            var context = random.Next(2);
            var arrival = context == 0
                ? random.NextDouble() < condition.Persistence ? previous : -previous
                : random.Next(2) == 0 ? -1 : 1;
            var fits = random.Next(2) == 0;
            var load = fits ? random.Next(condition.Threshold) : random.Next(condition.Threshold, 16);
            var missing = condition.MissingEvery > 0 && (tick + 1) % condition.MissingEvery == 0;
            var absent = condition.AbsentEvery > 0 && (tick + 1) % condition.AbsentEvery == 0;
            var offset = random.Next(1, 16); // Consume equally in baseline and novel-load tapes.
            events[tick] = new(tick, load, fits, context, lastObserved, arrival,
                missing, absent, tick - lastObservedTick, condition.NovelLoads ? offset : 0);
            previous = arrival;
            if (!missing && !absent) { lastObserved = arrival; lastObservedTick = tick; }
        }
        return events;
    }

    public static string Digest(RelayEpisode[] tape) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tape)))).ToLowerInvariant();
    private int Encode(int value) => Condition.Encoding switch
    {
        1 => value * 19 + 700,
        2 => value * value * value + 31,
        _ => value
    };
}

/// <summary>Thin external publication/inspection adapter. Result basis and source
/// are retained in native witnesses; no hidden learner state is held here.</summary>
public static class NativePublication
{
    public static Relation Point(Carrier axis, long value, IElement? source = null, long low = -16, long high = 16) =>
        ReadWitness.Create(new Carrier(), source ?? axis, Relation.Segment(axis, low, high),
            new CarrierLocation(axis, value), Relation.OrderedGroup(new Carrier()), source ?? axis);
    public static Relation Open(Carrier axis, IElement? source = null) =>
        ReadWitness.Create(new Carrier(), source ?? axis, Relation.Segment(axis, -16, 16),
            new Placeholder(), new Relation(source ?? axis, new Placeholder()), source ?? axis);
    public static long? Read(Relation read) => ReadWitness.TryRead(read, out _, out _, out var result, out _, out _)
        && result is CarrierLocation location ? location.Ticks : null;
    public static long? Read(Relation read, Carrier axis) => ReadWitness.TryRead(read, out _, out _, out var result, out _, out _)
        && result is CarrierLocation location && location.Carrier.SameIdentityAs(axis) ? location.Ticks : null;
}
