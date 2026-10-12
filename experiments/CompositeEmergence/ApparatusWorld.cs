using Gojo.Core;
using Gojo.Structures;

namespace CompositeEmergence;

// These records are the external apparatus/evaluator, never learner inputs.
public sealed record ApparatusTrial(int Index, int Rule, int Input, int Goal,
    int Delay, int ExploratoryAction, bool MissingA, bool MissingB);

public sealed class ApparatusTape
{
    private readonly ApparatusTrial[] trials;
    public int Count => trials.Length;
    public ApparatusTrial this[int index] => trials[index];

    private ApparatusTape(ApparatusTrial[] trials) => this.trials = trials;

    public static ApparatusTape Create(int seed, int count, bool persistent = false,
        double switchProbability = .04, int maximumDelay = 3, double missingProbability = 0)
    {
        if (count < 1 || maximumDelay is < 1 or > 59 || switchProbability is < 0 or > 1 || missingProbability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Positive trial count, delays 1..59, and probabilities 0..1 are required.");
        var ruleRandom = new Random(unchecked(seed * 104729 + 13));
        var inputRandom = new Random(unchecked(seed * 13007 + 71));
        var goalRandom = new Random(unchecked(seed * 8191 + 173));
        var actionRandom = new Random(unchecked(seed * 10009 + 29));
        var delayRandom = new Random(unchecked(seed * 4001 + 311));
        var missingRandom = new Random(unchecked(seed * 1223 + 907));
        var rule = Sign(ruleRandom);
        var trials = new ApparatusTrial[count];
        for (var i = 0; i < count; i++)
        {
            if (!persistent) rule = Sign(ruleRandom);
            else if (i > 0 && ruleRandom.NextDouble() < switchProbability) rule = -rule;
            trials[i] = new(i, rule, Sign(inputRandom), Sign(goalRandom),
                delayRandom.Next(1, maximumDelay + 1), Sign(actionRandom),
                missingRandom.NextDouble() < missingProbability,
                missingRandom.NextDouble() < missingProbability);
        }
        return new(trials);
    }

    private static int Sign(Random random) => random.Next(2) == 0 ? -1 : 1;
}

/// <summary>The external machine publishes observations and resource changes.
/// It holds no reference to a learner and cannot write learner memory.</summary>
public sealed class ApparatusWorld
{
    public Carrier TimeAxis { get; } = new();
    public int Wiring { get; }
    public int ProbePrice { get; }
    public const int CorrectResource = 8;
    public const int WrongResource = -12;

    public ApparatusWorld(int wiring = 1, int probePrice = 1)
    {
        if (wiring is not (-1 or 1) || probePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(wiring));
        Wiring = wiring; ProbePrice = probePrice;
    }

    public Relation Input(ApparatusTrial trial, Carrier axis, int orientation = 1) =>
        trial.MissingA ? WorldPublication.Open(axis, Time(trial.Index, 0)) :
            WorldPublication.Value(axis, trial.Input * orientation, Time(trial.Index, 0));

    public Relation Response(ApparatusTrial trial, Carrier axis, int orientation = 1,
        int? replacement = null) => trial.MissingB ? WorldPublication.Open(axis, Time(trial.Index, trial.Delay)) :
            WorldPublication.Value(axis, (replacement ?? trial.Input * trial.Rule) * orientation,
                Time(trial.Index, trial.Delay));

    public Relation Goal(ApparatusTrial trial, Carrier axis) =>
        WorldPublication.Value(axis, trial.Goal, Time(trial.Index, trial.Delay + 1));

    public Relation ExploratoryAction(ApparatusTrial trial, Carrier axis) =>
        WorldPublication.Value(axis, trial.ExploratoryAction, Time(trial.Index, trial.Delay + 2));

    public Relation DirectionConsequence(ApparatusTrial trial, Relation action, Carrier actionAxis, Carrier outcomeAxis)
    {
        if (!WorldPublication.TryValue(action, actionAxis, out var chosen) || chosen is not (-1 or 1))
            return WorldPublication.Open(outcomeAxis, action);
        return WorldPublication.Value(outcomeAxis, chosen * trial.Rule,
            Relation.OrderedGroup(new Carrier(), action, Time(trial.Index, trial.Delay + 3)));
    }

    public Relation Resource(ApparatusTrial trial, int action, Carrier resourceAxis, bool probed = false,
        IElement? command = null)
    {
        if (action is not (-1 or 0 or 1)) return WorldPublication.Open(resourceAxis, command ?? new Placeholder());
        var gross = action == 0 ? 0 : action * trial.Rule * trial.Goal * Wiring > 0 ? CorrectResource : WrongResource;
        return WorldPublication.Value(resourceAxis, gross - (probed ? ProbePrice : 0),
            Relation.OrderedGroup(new Carrier(), command ?? new Placeholder(), Time(trial.Index, trial.Delay + 4)), -1024, 1024);
    }

    public bool IsCorrect(ApparatusTrial trial, int action) => action is -1 or 1 && action * trial.Rule * trial.Goal * Wiring > 0;

    private IElement Time(int index, int offset) => new CarrierLocation(TimeAxis, checked(index * 64L + offset));
}

/// <summary>An observation adapter. The connected witness is its entire public
/// meaning; the C# helper retains no private value or world identity map.</summary>
public static class WorldPublication
{
    public static Relation Value(Carrier axis, long value, IElement source, long low = -16, long high = 16)
    {
        var identity = new Carrier();
        return ReadWitness.Create(new Carrier(), identity, Relation.Segment(axis, low, high),
            new CarrierLocation(axis, value), Relation.OrderedGroup(new Carrier()),
            Relation.OrderedGroup(new Carrier(), identity, EventTime(source), CommandPoint(source)));
    }
    public static Relation Open(Carrier axis, IElement source)
    {
        var identity = new Carrier();
        return ReadWitness.Create(new Carrier(), identity, Relation.Segment(axis, -16, 16),
            new Placeholder(), Relation.OrderedGroup(new Carrier()),
            Relation.OrderedGroup(new Carrier(), identity, EventTime(source)));
    }
    private static IElement CommandPoint(IElement source)
    {
        if (source is Relation group && group.TryReadOrderedGroup(out var fields) && fields.Length > 0 &&
            fields[0] is Relation command && ReadWitness.TryRead(command, out _, out _, out var result, out _, out _) &&
            result is CarrierLocation point) return new CarrierLocation(point.Carrier, point.Ticks);
        return new Placeholder();
    }
    private static IElement EventTime(IElement source)
    {
        if (source is CarrierLocation) return source;
        if (source is Relation group && group.TryReadOrderedGroup(out var fields) && fields.Length > 0 &&
            fields[^1] is CarrierLocation time) return time;
        return new Placeholder();
    }
    public static bool TryValue(Relation witness, Carrier axis, out long value)
    {
        value = 0;
        if (!ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _) ||
            result is not CarrierLocation point || !point.Carrier.SameIdentityAs(axis)) return false;
        value = point.Ticks; return true;
    }
}
