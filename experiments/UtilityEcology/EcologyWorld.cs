using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;

namespace UtilityEcology;

// External tape/evaluator data. A trial is never passed through a learner port.
// RawLevel, Phase, DesiredAction and NoiseDirection are private-world facts;
// the observation boundary below publishes only an opaque acquisition.
public sealed record EcologyTrial(int Index, int Phase, int Source, int RawLevel,
    int DesiredAction, int ExploratoryAction, int NoiseDirection = 1);

/// <summary>Four equal phases on an action-independent deterministic tape.
/// Separate generators supply requests, sample classes, unpredictable physical
/// consequences and fallback exploratory commands. Reusing the seed gives all
/// experimental arms the same external events, independent of their decisions.
/// A PRNG construction is reproducibility evidence, not an independence proof.</summary>
public sealed class WorldTape
{
    private readonly EcologyTrial[] trials;
    public int Count => trials.Length;
    public int PhaseLength { get; }
    public EcologyTrial this[int index] => trials[index];
    public IReadOnlyList<EcologyTrial> Trials { get; }

    private WorldTape(EcologyTrial[] trials, int phaseLength)
    {
        this.trials = trials; PhaseLength = phaseLength;
        Trials = Array.AsReadOnly(trials);
    }

    public static WorldTape Create(int seed, int phaseLength = 256)
    {
        if (phaseLength is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(phaseLength));
        var requestRandom = new Random(unchecked(seed * 104729 + 101));
        var sampleRandom = new Random(unchecked(seed * 13007 + 307));
        var noiseRandom = new Random(unchecked(seed * 8191 + 503));
        var actionRandom = new Random(unchecked(seed * 10009 + 701));
        var trials = new EcologyTrial[checked(4 * phaseLength)];
        for (var index = 0; index < trials.Length; index++)
        {
            var phase = index / phaseLength;
            // Consume one draw from every stream at every step, including when
            // the physical law does not need the noise draw.
            var request = requestRandom.NextDouble();
            var raw = sampleRandom.Next(6);
            var noise = noiseRandom.Next(2) == 0 ? -1 : 1;
            var fallback = actionRandom.Next(2) == 0 ? -1 : 1;
            var source = SelectSource(phase, request);
            trials[index] = new(index, phase, source, raw,
                PhysicalDirection(source, phase, raw, noise), fallback, noise);
        }
        return new(trials, phaseLength);
    }

    /// <summary>The apparatus law, available only to host physics and evaluator
    /// tests. No learner publication calls this method or exposes its inputs.
    /// Under the fair-input law, service 2's consequence is independent of each
    /// sample class; finite deterministic tapes need not be exactly balanced.</summary>
    public static int PhysicalDirection(int source, int phase, int rawLevel, int noiseDirection)
    {
        if (source is < 0 or > 3 || phase is < 0 or > 3 || rawLevel is < 0 or > 5 || noiseDirection is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(source), "The external apparatus requires its declared source, phase, sample and direction domains.");
        if (source == 0 && phase == 0) return 1;
        if (source == 2 || source == 3 && phase < 3) return noiseDirection;
        return rawLevel < 3 ? -1 : 1;
    }

    public string Digest() => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(trials)))).ToLowerInvariant();

    private static int SelectSource(int phase, double request) => phase switch
    {
        0 => request < .55 ? 0 : request < .75 ? 1 : request < .90 ? 2 : 3,
        1 or 2 => request < .65 ? 0 : request < .90 ? 2 : 3,
        _ => request < .35 ? 0 : request < .60 ? 1 : request < .75 ? 2 : 3
    };
}

/// <summary>The external sampling and actuator apparatus. Only the host-owned
/// instrument dictionary contains metric samples. The world holds no learner
/// references, publishes no phase/quality classifications, and cannot write a
/// model. Comparison fees are admitted by the caller's funded query contract.</summary>
public sealed class EcologyWorld
{
    private sealed record Acquisition(EcologyTrial Trial, long Encoded);
    private readonly Dictionary<Relation, Acquisition> acquisitions = new(ReferenceEqualityComparer.Instance);
    public Carrier[] Sources { get; } = Enumerable.Range(0, 4).Select(_ => new Carrier()).ToArray();
    public Carrier[] PresenceAxes { get; } = Enumerable.Range(0, 4).Select(_ => new Carrier()).ToArray();
    public Carrier[] OrderAxes { get; } = Enumerable.Range(0, 4).Select(_ => new Carrier()).ToArray();
    public Carrier ActionAxis { get; } = new();
    public Carrier ResourceAxis { get; } = new();
    public Carrier TimeAxis { get; } = new();
    public int Encoding { get; }
    public int ComparisonQueries { get; private set; }
    public int Executions { get; private set; }

    public EcologyWorld(int encoding = 0)
    {
        if (encoding is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(encoding));
        Encoding = encoding;
    }

    public Relation Request(EcologyTrial trial)
    {
        Validate(trial);
        var sample = SamplingSurface.Sample(new Carrier(), Sources[trial.Source], PresenceAxes[trial.Source], true);
        acquisitions.Add(sample, new(trial, Encode(trial.RawLevel)));
        return sample;
    }

    /// <summary>Discover the exposed source identity by the sample's native
    /// read boundary. This does not consult private measurement or trial facts.</summary>
    public bool TrySource(Relation sample, out int source)
    {
        source = -1;
        if (!SamplingSurface.TryReadSample(sample, out var presence, out _) || presence.Ticks != 1 ||
            !ReadWitness.TryRead(sample, out var selected, out _, out _, out _, out _) || selected is not Carrier carrier)
            return false;
        for (var index = 0; index < Sources.Length; index++)
            if (carrier.SameIdentityAs(Sources[index]) && presence.Carrier.SameIdentityAs(PresenceAxes[index]))
            { source = index; return true; }
        return false;
    }

    public Relation Compare(Relation candidate, Relation anchor)
    {
        ComparisonQueries++;
        var pair = NativeGraph.G(candidate, anchor);
        if (!TrySource(candidate, out var source) || !TrySource(anchor, out var other) || source != other ||
            !acquisitions.TryGetValue(candidate, out var a) || !acquisitions.TryGetValue(anchor, out var b))
            return NativeGraph.Open(pair, source >= 0 ? Relation.Segment(OrderAxes[source], -1, 1) : new Placeholder());
        return SamplingSurface.Comparison(new Carrier(), candidate, anchor, OrderAxes[source], a.Encoded.CompareTo(b.Encoded));
    }

    /// <summary>Actual signed physical resource caused by a command. A known
    /// hold gives zero; an unsupported or unavailable command remains open.
    /// The public event keeps an opaque identity, time and captured command
    /// coordinate, withholding the command's private graph and all trial truth.
    /// The caller can bind this publication to its exact retained decision via
    /// NativeGraph.BindFeedback before admitting it as learning evidence.</summary>
    public Relation Execute(EcologyTrial trial, Relation command)
    {
        Validate(trial); Executions++;
        var context = NativeGraph.G(command, new CarrierLocation(TimeAxis, trial.Index));
        if (!WorldPublication.TryValue(command, ActionAxis, out var chosen) || chosen is not (-1 or 0 or 1))
            return WorldPublication.Open(ResourceAxis, context);
        var scale = trial.Source == 1 ? 2 : 1;
        var gross = chosen == 0 ? 0 : chosen == trial.DesiredAction ? 8 * scale : -12 * scale;
        return WorldPublication.Value(ResourceAxis, gross, context, -24, 16);
    }

    private long Encode(int raw) => Encoding switch
    {
        1 => checked(17L * (raw + 11) * (raw + 11) * (raw + 11) + 5003),
        2 => checked(83L * (raw + 29) * (raw + 29) * (raw + 29) + 91007),
        _ => checked((long)raw * raw * raw + 71)
    };

    private static void Validate(EcologyTrial trial)
    {
        if (trial.Index < 0 || trial.Phase is < 0 or > 3 || trial.Source is < 0 or > 3 || trial.RawLevel is < 0 or > 5 ||
            trial.DesiredAction is not (-1 or 1) || trial.ExploratoryAction is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(trial), "The host trial is outside the declared external apparatus domains.");
    }
}
