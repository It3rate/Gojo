using CompositeEmergence;
using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace UtilityEcology;

/// <summary>Adjacent completed blocks of pre-update error for one exposed
/// source contract. Open error is retained as missing coverage, never successful
/// zero error. This observer has no permission or currency authority: callers
/// admit its work through the experiment's existing paid inspection contract.
/// Partial blocks cannot alter a completed comparison.</summary>
public sealed class LearningProgress
{
    public const int ReceiptCapacity = 64;
    public Carrier ResourceAxis { get; }
    public Carrier ErrorAxis { get; }
    public Carrier Source { get; }
    public Carrier ProgressAxis { get; }
    public Carrier CoverageAxis { get; }
    public Carrier MeanErrorAxis { get; }
    public Carrier VersionSite => version;
    public ulong Version => version.Value;
    public ulong ObservationCount => observations.Value;
    public int BlockSize { get; }
    public int MinimumKnown { get; }
    public Relation Root => NativeGraph.G(NativeGraph.G(ResourceAxis, ErrorAxis, Source, ProgressAxis, CoverageAxis, MeanErrorAxis, countAxis, timeAxis),
        law, older, newer, current, version, observations, forgotten,
        NativeGraph.G(receipts.Cast<IElement>().ToArray()), lastObservation, lastProgress, lastCoverage, lastRecent, lastCompletion);

    private readonly Carrier countAxis, timeAxis, version, observations, forgotten;
    private readonly Relation law;
    private IElement older, newer;
    private Relation current, lastObservation, lastProgress, lastCoverage, lastRecent, lastCompletion;
    private readonly List<Relation> receipts = [];

    // block = G(time frame, G(events), total count, known count, raw-error sum,
    //           latest sum/settlement witness).
    // event = witness(independent physical identity, error frame,
    //                 exact raw error OR open, retained supplied error,
    //                 G(public error,public consequence,coverage point)).
    public LearningProgress(Carrier resourceAxis, Carrier errorAxis, Carrier source, int blockSize = 16, int minimumKnown = 8)
    {
        if (resourceAxis.IsUnknown || errorAxis.IsUnknown || source.IsUnknown || blockSize is < 2 or > 128 || minimumKnown < 1 || minimumKnown > blockSize)
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Use concrete source/bases and a bounded block with sufficient known observations.");
        ResourceAxis = resourceAxis; ErrorAxis = errorAxis; Source = source;
        ProgressAxis = new(); CoverageAxis = new(); MeanErrorAxis = new(); countAxis = new(); timeAxis = new();
        version = new(countAxis.UnitShape); observations = new(countAxis.UnitShape); forgotten = new(countAxis.UnitShape);
        BlockSize = blockSize; MinimumKnown = minimumKnown;
        law = NativeGraph.G(Relation.Point(countAxis, blockSize), Relation.Point(countAxis, minimumKnown),
            Relation.Segment(ErrorAxis, 0, 2), Relation.Segment(ResourceAxis, -1, 1), Relation.Point(countAxis, ReceiptCapacity),
            Relation.Segment(ProgressAxis, -512, 512), Relation.Segment(CoverageAxis, -256, 256), Relation.Segment(MeanErrorAxis, 0, 512));
        older = newer = new Placeholder(); current = NewBlock(0);
        lastObservation = lastProgress = lastCoverage = lastRecent = lastCompletion = NativeGraph.Open(Source, law);
    }

    private LearningProgress(Relation root)
    {
        var fields = NativeGraph.Parts(root, 14); var axes = NativeGraph.Parts((Relation)fields[0], 8).Cast<Carrier>().ToArray();
        ResourceAxis = axes[0]; ErrorAxis = axes[1]; Source = axes[2]; ProgressAxis = axes[3]; CoverageAxis = axes[4]; MeanErrorAxis = axes[5];
        countAxis = axes[6]; timeAxis = axes[7]; law = (Relation)fields[1];
        var contract = NativeGraph.Parts(law, 8); NativeGraph.TryPoint(contract[0], out var block); NativeGraph.TryPoint(contract[1], out var minimum);
        BlockSize = checked((int)block.Ticks); MinimumKnown = checked((int)minimum.Ticks);
        older = fields[2]; newer = fields[3]; current = (Relation)fields[4]; version = (Carrier)fields[5]; observations = (Carrier)fields[6]; forgotten = (Carrier)fields[7];
        receipts.AddRange(NativeGraph.Parts((Relation)fields[8]).Cast<Relation>());
        lastObservation = (Relation)fields[9]; lastProgress = (Relation)fields[10]; lastCoverage = (Relation)fields[11];
        lastRecent = (Relation)fields[12]; lastCompletion = (Relation)fields[13];
    }
    public static LearningProgress FromRoot(Relation root) => new(root);

    /// <summary>Admits one genuine pre-update error event. Invalid/foreign
    /// actuals and malformed error publications leave every window unchanged.
    /// Duplicate exclusion promises only the retained receipt window.</summary>
    public Relation Observe(Relation preUpdateError, Relation independentPhysicalResource)
    {
        var identity = NativeGraph.Identity(independentPhysicalResource);
        if (receipts.Any(receipt => ReadWitness.TryRead(receipt, out var prior, out _, out _, out _, out _) && ReferenceEquals(prior, identity)) ||
            !ReadExact(independentPhysicalResource, ResourceAxis, out var physical) || physical.Ticks is not (-1 or 1) ||
            !ReadError(preUpdateError, out var known, out var error))
            return lastObservation = NativeGraph.Open(NativeGraph.G(identity, preUpdateError), law);
        var capturedPhysical = NativeGraph.CapturePublication(independentPhysicalResource, ResourceAxis);
        var capturedError = known ? NativeGraph.CapturePublication(preUpdateError, ErrorAxis) : preUpdateError;
        var eventRead = ReadWitness.Create(new Carrier(), identity, Relation.Segment(ErrorAxis, 0, 2),
            known ? new CarrierLocation(ErrorAxis, error) : new Placeholder(), capturedError,
            NativeGraph.G(capturedError, capturedPhysical, Relation.Point(countAxis, known ? 1 : 0)));
        var fields = NativeGraph.Parts(current, 6); var events = NativeGraph.Parts((Relation)fields[1]).Cast<Relation>().Append(eventRead).ToArray();
        var total = (Carrier)fields[2]; var counted = (Carrier)fields[3]; var sum = (Carrier)fields[4];
        var before = CaptureCounts(current); var sumLaw = Equation.Sum(sum, Equation.UnitPort(sum), Equation.UnitPort(ErrorAxis));
        Relation sumRead = NativeGraph.Open(preUpdateError, sumLaw);
        if (known)
            sumRead = Equation.Predict(sumLaw, new Relation(new CarrierLocation(sum, checked((long)sum.Value)), new CarrierLocation(ErrorAxis, error)), sum);
        var settlement = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(total, 1), new CarrierLocation(counted, known ? 1 : 0),
            new CarrierLocation(sum, known ? error : 0), new CarrierLocation(observations, 1)));
        var update = ReadWitness.Create(new Carrier(), eventRead, sumLaw, NativeGraph.G(total.ValueSlot, counted.ValueSlot, sum.ValueSlot),
            known ? NativeGraph.G() : new Relation(preUpdateError, Relation.Segment(ErrorAxis, 0, 2)), NativeGraph.G(before, sumRead, settlement));
        current = NativeGraph.G(fields[0], NativeGraph.G(events), total, counted, sum, update);
        lastObservation = ReadWitness.Create(new Carrier(), identity, law, eventRead, update,
            NativeGraph.G(Source, before, CaptureCounts(current), Relation.Point(countAxis, checked((long)observations.Value))));
        receipts.Add(eventRead);
        if (receipts.Count > ReceiptCapacity) { receipts.RemoveAt(0); forgotten.Add(1); }
        var complete = OperationClaim.Equality(new Carrier(), new CarrierLocation(countAxis, checked((long)total.Value)), Relation.Point(countAxis, BlockSize), new Carrier());
        if (NativeGraph.Pass(complete))
        {
            var previousPair = NativeGraph.G(older, newer); var dropped = older;
            older = newer; newer = current;
            if (older is Relation) version.Add(1);
            lastCompletion = ReadWitness.Create(new Carrier(), Source, complete, NativeGraph.G(older, newer), dropped,
                NativeGraph.G(previousPair, Relation.Point(countAxis, checked((long)version.Value))));
            current = NewBlock(checked((long)observations.Value));
        }
        return lastObservation;
    }

    public Relation ReadProgress()
    {
        if (older is not Relation oldBlock || newer is not Relation newBlock)
            return lastProgress = NativeGraph.Open(NativeGraph.G(older, newer, CaptureCounts(current)), law);
        var oldMean = Mean(oldBlock); var newMean = Mean(newBlock);
        if (!NativeGraph.TryValue(oldMean, MeanErrorAxis, out var a) || !NativeGraph.TryValue(newMean, MeanErrorAxis, out var b))
            return lastProgress = NativeGraph.Open(NativeGraph.G(oldMean, newMean), law);
        var difference = Equation.Sum(ProgressAxis, Equation.WeightedPort(MeanErrorAxis, 0, 1, 0, 1), Equation.WeightedPort(MeanErrorAxis, 0, 1, 0, -1));
        var result = Equation.Predict(difference, new Relation(a, b), ProgressAxis);
        return lastProgress = Publish(result, ProgressAxis, NativeGraph.G(oldMean, newMean), difference);
    }

    public Relation ReadCoverageChange()
    {
        if (older is not Relation oldBlock || newer is not Relation newBlock)
            return lastCoverage = NativeGraph.Open(NativeGraph.G(older, newer), law);
        var a = (Carrier)NativeGraph.Parts(oldBlock, 6)[3]; var b = (Carrier)NativeGraph.Parts(newBlock, 6)[3];
        var difference = Equation.Sum(countAxis, Equation.WeightedPort(countAxis, 0, 1, 0, -1), Equation.WeightedPort(countAxis, 0, 1, 0, 1));
        var change = Equation.Predict(difference, new Relation(new CarrierLocation(countAxis, checked((long)a.Value)), new CarrierLocation(countAxis, checked((long)b.Value))), countAxis);
        Equation.TryReadPrediction(change, out var delta, out _);
        var scaling = Equation.Affine(countAxis, 0, BlockSize, CoverageAxis, 0, 256);
        var result = Equation.Predict(scaling, delta, CoverageAxis);
        return lastCoverage = Publish(result, CoverageAxis, NativeGraph.G(CaptureCounts(oldBlock), CaptureCounts(newBlock)), NativeGraph.G(difference, change, scaling));
    }

    public Relation ReadRecentError() => lastRecent = newer is Relation block ? Mean(block) : NativeGraph.Open(newer, law);

    private Relation Mean(Relation block)
    {
        var fields = NativeGraph.Parts(block, 6); var known = (Carrier)fields[3]; var sum = (Carrier)fields[4]; var captured = CaptureCounts(block);
        var sufficient = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(countAxis, checked((long)known.Value)), Relation.Point(countAxis, MinimumKnown), new Carrier());
        if (!NativeGraph.Pass(sufficient)) return NativeGraph.Open(NativeGraph.G(captured, sufficient), law);
        // Raw discrepancy 2 means a wrong opposed direction; 0 is correct.
        // Preserve the raw discrepancy unit: one mean-error unit is 256 ticks,
        // so a wholly wrong 0/2 direction stream has mean error 512.
        var meanLaw = Equation.Affine(sum, 0, checked((long)known.Value), MeanErrorAxis, 0, 256);
        var prediction = Equation.Predict(meanLaw, new CarrierLocation(sum, checked((long)sum.Value)), MeanErrorAxis);
        return Publish(prediction, MeanErrorAxis, captured, NativeGraph.G(meanLaw, sufficient));
    }
    private Relation Publish(Relation prediction, Carrier axis, IElement selected, IElement surface)
    {
        if (!Equation.TryReadPrediction(prediction, out var value, out var residue)) return NativeGraph.Open(selected, surface);
        return ReadWitness.Create(new Carrier(), selected, surface, new CarrierLocation(axis, value.Ticks), residue,
            NativeGraph.G(prediction, Relation.Point(countAxis, checked((long)version.Value))));
    }
    private Relation NewBlock(long start)
    {
        var total = new Carrier(countAxis.UnitShape); var known = new Carrier(countAxis.UnitShape); var sum = new Carrier(ErrorAxis.UnitShape);
        foreach (var pair in new[] { (total, (long)BlockSize), (known, (long)BlockSize), (sum, 2L * BlockSize) })
            pair.Item1.Attach(new Relation(Relation.Point(pair.Item1, pair.Item2), Relation.Point(pair.Item1, pair.Item2)));
        return NativeGraph.G(Relation.Segment(timeAxis, start, checked(start + BlockSize)), NativeGraph.G(), total, known, sum, NativeGraph.Open(Source, law));
    }
    private static Relation CaptureCounts(Relation block)
    {
        var fields = NativeGraph.Parts(block, 6);
        return NativeGraph.G(new[] { fields[0] }.Concat(fields[2..5].Cast<Carrier>()
            .Select(carrier => (IElement)new CarrierLocation(carrier, checked((long)carrier.Value)))).ToArray());
    }
    private bool ReadError(Relation publication, out bool known, out long error)
    {
        known = false; error = 0;
        if (!ReadWitness.TryRead(publication, out _, out var surface, out var result, out _, out _)) return false;
        if (FrameAxis(surface, out var frameAxis) && !frameAxis.SameIdentityAs(ErrorAxis)) return false;
        if (QualifiedReadSurface.TryReadPublished(publication, out _, out var basis, out _, out _, out _, out _, out _, out _) &&
            (!FrameAxis(basis, out var basisAxis) || !basisAxis.SameIdentityAs(ErrorAxis))) return false;
        if (NativeGraph.TryPoint(result, out var nominal) && (!nominal.Carrier.SameIdentityAs(ErrorAxis) || nominal.Ticks is < 0 or > 2)) return false;
        if (ReadExact(publication, ErrorAxis, out var point)) { known = true; error = point.Ticks; return error is >= 0 and <= 2; }
        // Qualified ambiguous support is retained as unavailable error, as is
        // an explicitly open publication. No nominal fallback becomes success.
        return result is Placeholder || QualifiedReadSurface.TryReadPublished(publication, out _, out _, out _, out _, out _, out _, out _, out _);
    }
    private static bool ReadExact(Relation publication, Carrier axis, out CarrierLocation value)
    {
        value = default;
        if (ReadWitness.TryRead(publication, out _, out var surface, out _, out _, out _) && FrameAxis(surface, out var frameAxis) && !frameAxis.SameIdentityAs(axis)) return false;
        if (QualifiedReadSurface.TryReadPublished(publication, out _, out var basis, out _, out _, out _, out _, out _, out _) &&
            (!FrameAxis(basis, out var basisAxis) || !basisAxis.SameIdentityAs(axis))) return false;
        return NativeGraph.TryValue(publication, axis, out value);
    }
    private static bool FrameAxis(IElement element, out Carrier axis)
    {
        if (element is Relation frame && NativeGraph.TryPoint(frame.Left, out var left) && NativeGraph.TryPoint(frame.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier)) { axis = left.Carrier; return true; }
        if (element is Relation { Left: Relation source, Right: Relation target } && FrameAxis(source, out _) && FrameAxis(target, out var output))
        { axis = output; return true; }
        axis = null!; return false;
    }
}
