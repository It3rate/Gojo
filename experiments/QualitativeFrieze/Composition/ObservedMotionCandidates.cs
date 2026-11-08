using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Finite motion hypotheses tested against exposed before/after
/// observations. The changed-input scope and landmark correspondence are supplied
/// premises, not discovered causal closure. Candidate elimination is conditional
/// on those premises; no confidence score or unobserved motion is invented.</summary>
public sealed class ObservedMotionCandidates
{
    // surface = frame -> Closed(actual extent port, source tick span, landmark role, candidate words)
    // trial = (surface -> occurrence) -> Closed(
    //   extent port -> (captured before -> captured after),
    //   landmark role -> (before landmark port -> after landmark port),
    //   changed-input-port scope, cause)
    // archive = surface -> Closed(trials)
    // prediction = candidate -> (Closed(repeated candidate uses) -> expanded word)
    // comparison = prediction -> native reduction(observed displacement - prediction)
    // A sole changed input plus nonzero actual extent difference permits filtering.
    // Open/confounded/zero-delta observations retain all previously possible words.
    // History is native structure; C# summaries are calculated from it.
    private readonly Curve.Frame frame;
    private Relation history;
    public Relation Surface { get; }
    public Relation ExtentPort { get; }
    public Relation SourceTick { get; }
    public IElement LandmarkRole { get; }
    public IReadOnlyList<Relation> Candidates { get; }

    public sealed record CandidateRead(Relation Candidate, bool Possible, bool Supported,
        bool Contradicted, int MatchingObservations, Relation Evidence);
    public sealed record ReadResult(Relation[] Possible, Relation[] Rejected, Relation? Unique,
        int InformativeObservations, CandidateRead[] Candidates, Relation Witness);
    public sealed record Observation(bool Recorded, long? ActualDelta, bool Complete,
        bool Isolated, bool Informative, ReadResult Result, Relation Witness);

    private sealed record TrialRead(Relation Trial, long? Delta, bool Complete, bool Isolated,
        bool Informative, bool?[] Matches, Relation[] Comparisons, Relation Witness);

    private ObservedMotionCandidates(Curve.Frame frame, Relation surface, Relation history)
    {
        if (!ReferenceEquals(surface.Left, frame.Surface) || surface.Right is not Relation body ||
            !Closed(body, out var fields) || fields is not [Relation port, Relation tick, Placeholder role, Relation words] ||
            !LivePort(port, out var owner) || !UnitSpan(tick, owner) || !Closed(words, out var candidates) || candidates.Length is < 1 or > 64 ||
            candidates.Any(c => c is not Relation) ||
            candidates.Distinct(ReferenceEqualityComparer.Instance).Count() != candidates.Length ||
            !Closed(history, out var trials))
            throw new ArgumentException("Expected a bounded motion candidate surface and native trial history.", nameof(surface));
        this.frame = frame; Surface = surface; ExtentPort = port; SourceTick = tick; LandmarkRole = role;
        Candidates = Array.AsReadOnly(candidates.Cast<Relation>().ToArray()); this.history = history;
        var events = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var item in trials)
            if (item is not Relation trial || !TryTrial(trial, out var occurrence, out _, out _, out _, out _, out _, out _) ||
                !events.Add(occurrence)) throw new ArgumentException("Malformed or repeated observation event.", nameof(history));
    }

    public static ObservedMotionCandidates Create(Curve.Frame frame, Relation actualExtentPort, params Relation[] candidates)
    {
        if (!LivePort(actualExtentPort, out var owner)) throw new ArgumentException("Expected an advertised live extent port.", nameof(actualExtentPort));
        return new(frame, new Relation(frame.Surface, S(actualExtentPort, Relation.Segment(owner, 0, 1),
            new Placeholder(), S(candidates.Cast<IElement>().ToArray()))), S());
    }

    /// <summary>Records frozen extent samples and a supplied correspondence
    /// between two observations of one landmark role. A closed singleton input
    /// scope asserts that only the selected extent changed; it cannot establish
    /// absence of unadvertised physical causes.</summary>
    public Observation Observe(CarrierLocation beforeExtentSample, CarrierLocation afterExtentSample,
        Relation beforeLandmarkPort, Relation afterLandmarkPort, Relation changedInputPorts,
        IElement? cause = null, IElement? eventIdentity = null)
    {
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder) throw new ArgumentException("Observation event identities must stay opaque.", nameof(eventIdentity));
        // Capture valid observations again at this boundary. Caller-owned
        // sample TickRefs may later be retargeted; history must not drift.
        var before = FrozenSample(beforeExtentSample) ? new CarrierLocation(beforeExtentSample.Carrier, beforeExtentSample.Ticks) : beforeExtentSample;
        var after = FrozenSample(afterExtentSample) ? new CarrierLocation(afterExtentSample.Carrier, afterExtentSample.Ticks) : afterExtentSample;
        var trial = new Relation(new Relation(Surface, occurrence), S(
            new Relation(ExtentPort, new Relation(before, after)),
            new Relation(LandmarkRole, new Relation(beforeLandmarkPort, afterLandmarkPort)),
            changedInputPorts, cause ?? new Placeholder()));
        var previous = E(history).Cast<Relation>().FirstOrDefault(t => Same(((Relation)t.Left).Right, occurrence));
        var read = Assess(trial);
        if (previous is not null)
            return new(false, read.Delta, read.Complete, read.Isolated, false, Read(),
                new Relation(trial, StructuralConstraint.Open(previous)));
        history = S([.. E(history), trial]);
        return new(true, read.Delta, read.Complete, read.Isolated, read.Informative, Read(), read.Witness);
    }

    public ReadResult Read()
    {
        var trials = E(history).Cast<Relation>().Select(Assess).ToArray();
        var readings = Candidates.Select((candidate, i) =>
        {
            var informative = trials.Where(t => t.Informative && t.Matches[i] is not null).ToArray();
            var contradicted = informative.Any(t => t.Matches[i] == false);
            var matches = informative.Count(t => t.Matches[i] == true);
            var evidence = S(informative.Select(t => (IElement)new Relation(t.Trial, t.Comparisons[i])).ToArray());
            return new CandidateRead(candidate, !contradicted, matches > 0, contradicted, matches, evidence);
        }).ToArray();
        var possible = readings.Where(r => r.Possible).Select(r => r.Candidate).ToArray();
        var rejected = readings.Where(r => r.Contradicted).Select(r => r.Candidate).ToArray();
        var unique = possible.Length == 1 && readings.Single(r => ReferenceEquals(r.Candidate, possible[0])).Supported ? possible[0] : null;
        var witness = new Relation(ArchiveSurface(), S(
            S(possible.Cast<IElement>().ToArray()), S(rejected.Cast<IElement>().ToArray()),
            S(readings.Select(r => (IElement)new Relation(r.Candidate, r.Evidence)).ToArray()),
            S(trials.Select(t => (IElement)t.Witness).ToArray())));
        return new(possible, rejected, unique, trials.Count(t => t.Informative), readings, witness);
    }

    public Relation ArchiveSurface() => new(Surface, history);
    public static ObservedMotionCandidates Restore(Curve.Frame frame, Relation archive) =>
        archive is { Left: Relation surface, Right: Relation history }
            ? new(frame, surface, history) : throw new ArgumentException("Expected a motion-candidate archive.", nameof(archive));
    public byte[] Encode() => GojoNumericFormat.Encode(frame.Surface, ArchiveSurface());
    public static ObservedMotionCandidates Decode(ReadOnlySpan<byte> bytes)
    {
        var roots = GojoNumericFormat.Decode(bytes);
        return roots is [Relation basis, Relation archive] ? Restore(Curve.Frame.FromSurface(basis), archive)
            : throw new ArgumentException("Expected a frame and motion-candidate archive.", nameof(bytes));
    }

    private TrialRead Assess(Relation trial)
    {
        if (!TryTrial(trial, out _, out var before, out var after, out var beforePort, out var afterPort,
                out var changes, out _)) throw new ArgumentException("Expected an exact motion observation.", nameof(trial));
        long? delta = null; var isolated = Closed(changes, out var inputs) && inputs.Length == 1 && ReferenceEquals(inputs[0], ExtentPort);
        var complete = FrozenSample(before) && FrozenSample(after) &&
            beforePort.Right is Relation a && afterPort.Right is Relation b && frame.Vector(a, out _) && frame.Vector(b, out _);
        Relation displacement = StructuralConstraint.Open(beforePort, afterPort);
        Relation observed = StructuralConstraint.Open();
        if (complete)
        {
            delta = checked(after.Ticks - before.Ticks);
            displacement = Difference((Relation)beforePort.Right, (Relation)afterPort.Right);
            if (!frame.Motion.TryReadResult(displacement, out observed, out _, out var remainder) || E(remainder).Length != 0)
                throw new InvalidOperationException("Shared-reach displacement failed to reduce.");
        }
        var informative = complete && isolated && delta != 0;
        var matches = new bool?[Candidates.Count]; var comparisons = new Relation[Candidates.Count];
        for (var i = 0; i < Candidates.Count; i++)
        {
            var candidate = Candidates[i]; comparisons[i] = StructuralConstraint.Open(candidate, displacement);
            if (!informative || !frame.Vector(candidate, out _) || !TryRepeat(candidate, delta!.Value, out var prediction)) continue;
            var expanded = (Relation)prediction.Right;
            var difference = Difference(expanded, observed);
            if (!frame.Motion.TryReadResult(difference, out var residual, out _, out var unresolved) ||
                !Closed(residual, out var terms) || !Closed(unresolved, out var pending)) continue;
            matches[i] = terms.Length == 0 && pending.Length == 0;
            comparisons[i] = new Relation(new Relation(candidate, prediction), difference);
        }
        var witness = new Relation(trial, S(delta is null ? StructuralConstraint.Open() : S(new CarrierLocation(((CarrierLocation)ExtentPort.Left).Carrier, delta.Value)),
            displacement, S(comparisons.Cast<IElement>().ToArray())));
        return new(trial, delta, complete, isolated, informative, matches, comparisons, witness);
    }

    private bool TryRepeat(Relation word, long delta, out Relation prediction)
    {
        prediction = null!; var moves = E(word); var count = BigInteger.Abs(delta);
        // A bounded execution budget leaves a hypothesis unresolved, not false.
        if (count > 65536 || count * moves.Length > 65536) return false;
        var selected = delta < 0 ? Oppose(word) : word;
        var repeats = S(Enumerable.Repeat<IElement>(word, (int)count).ToArray());
        var expanded = S(Enumerable.Range(0, (int)count).SelectMany(_ => E(selected)).ToArray());
        // The signed delta is also retained in the parent observation; repeated
        // uses are the source word even when the selected displacement is opposed.
        prediction = new Relation(repeats, expanded); return true;
    }

    private Relation Difference(Relation from, Relation to) => frame.Motion.Reduce(Oppose(from), to);
    private Relation Oppose(Relation word) => S(E(word).Cast<Relation>().Select(move =>
    {
        frame.Motion.Direction(move, out var axis, out var forward, out var reach);
        return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
            ? forward ? frame.Motion.Left : frame.Motion.Right : forward ? frame.Motion.Down : frame.Motion.Up, reach);
    }).ToArray());

    private bool TryTrial(Relation trial, out IElement occurrence, out CarrierLocation before, out CarrierLocation after,
        out Relation beforePort, out Relation afterPort, out Relation changes, out IElement cause)
    {
        occurrence = cause = null!; before = after = default; beforePort = afterPort = changes = null!;
        if (trial is not { Left: Relation { Left: var source, Right: Placeholder identity }, Right: Relation body } ||
            !ReferenceEquals(source, Surface) || !Closed(body, out var fields) || fields is not [
                Relation { Left: var port, Right: Relation { Left: CarrierLocation a, Right: CarrierLocation b } },
                Relation { Left: var role, Right: Relation { Left: Relation first, Right: Relation second } }, Relation changed, var why] ||
            !ReferenceEquals(port, ExtentPort) || !Same(role, LandmarkRole)) return false;
        occurrence = identity; before = a; after = b; beforePort = first; afterPort = second; changes = changed; cause = why; return true;
    }
    private bool FrozenSample(CarrierLocation sample) => sample.Carrier is not null && sample.Ticks >= 0 &&
        ReferenceEquals(sample.Carrier, ExtentPort.Right) && !sample.SameTickRefAs((CarrierLocation)ExtentPort.Left);
    private static bool LivePort(Relation port, out Carrier owner)
    {
        owner = null!;
        if (port is not { Left: CarrierLocation at, Right: Carrier carrier } || carrier.IsUnknown ||
            !ReferenceEquals(at.Carrier, carrier) || !at.SameTickRefAs(carrier.ValueSlot)) return false;
        owner = carrier; return true;
    }
    private static bool UnitSpan(Relation span, Carrier owner) => span is
        { Left: Relation { Left: CarrierLocation a, Right: Carrier ca }, Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        ReferenceEquals(ca, owner) && ReferenceEquals(cb, owner) && ReferenceEquals(a.Carrier, owner) &&
        ReferenceEquals(b.Carrier, owner) && a.Ticks == 0 && b.Ticks == 1;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
