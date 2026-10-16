using System.Numerics;
using System.Runtime.InteropServices;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>Addresses two closed supports in one captured law's ordered bases.
/// The bits are derived addresses, not values or portable category codes.</summary>
public readonly record struct FiniteLawMaskPair(ulong Left, ulong Right);

/// <summary>
/// A derived bulk reader for a native finite law with up to 64 participants in
/// each domain. It stores only atomic pair results, not an exponential table of
/// all possible supports. The native law and its proof rows remain authoritative.
/// </summary>
/// <remarks>
/// A batch validates the captured graph once, validates every input, then reads
/// synchronously without callbacks or allocation. The caller must prevent edits
/// to that graph and the supplied buffers until the call returns. No per-read
/// witness or history is retained in this compute lane; selected reads can be
/// materialized separately while the source remains current.
/// </remarks>
public sealed class FiniteLawBatchPlan
{
    private readonly IElement[] leftStates;
    private readonly IElement[] rightStates;
    private readonly IElement[] outputStates;
    private readonly ulong[] atomicOutputs;
    private readonly Relation[][] consumedRows;
    private readonly (Relation Site, IElement Left, IElement Right)[] relationSites;
    private readonly (TickRef Site, long Value)[] tickSites;
    private readonly ulong leftMask;
    private readonly ulong rightMask;
    private readonly ulong outputMask;

    public Relation SourceLaw { get; }
    public Relation LeftDomain { get; }
    public Relation RightDomain { get; }
    public Relation OutputDomain { get; }
    public int LeftCount => leftStates.Length;
    public int RightCount => rightStates.Length;
    public int OutputCount => outputStates.Length;
    public int AtomicPairCount => atomicOutputs.Length;
    public bool IsCurrent => Intact();

    private FiniteLawBatchPlan(Relation law, Relation leftDomain, Relation rightDomain,
        Relation outputDomain, IElement[] left, IElement[] right, IElement[] output,
        ulong[] atomic, Relation[][] consumed)
    {
        SourceLaw = law; LeftDomain = leftDomain; RightDomain = rightDomain;
        OutputDomain = outputDomain; leftStates = left; rightStates = right;
        outputStates = output; atomicOutputs = atomic; consumedRows = consumed;
        leftMask = DomainMask(left.Length); rightMask = DomainMask(right.Length);
        outputMask = DomainMask(output.Length);

        var relations = new List<(Relation, IElement, IElement)>();
        var ticks = new List<(TickRef, long)>();
        var seenRelations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var seenTicks = new HashSet<TickRef>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(law);
        while (pending.TryPop(out var element))
            if (element is Relation relation && seenRelations.Add(relation))
            {
                relations.Add((relation, relation.Left, relation.Right));
                pending.Push(relation.Left); pending.Push(relation.Right);
            }
            else if (element is CarrierLocation location && seenTicks.Add(location.Tick))
                ticks.Add((location.Tick, location.Ticks));
            else if (element is TickRef tick && seenTicks.Add(tick))
                ticks.Add((tick, tick.Ticks));
        relationSites = relations.ToArray(); tickSites = ticks.ToArray();
    }

    /// <summary>Interprets each atomic pair with the native reader. Open,
    /// foreign, duplicate-domain or oversized law surfaces are not compiled.</summary>
    public static FiniteLawBatchPlan? Capture(Relation law)
    {
        if (!FiniteLaw.TryRead(law, out var leftDomain, out var rightDomain,
                out var outputDomain, out var rows)) return null;
        FiniteLaw.DomainEntries(leftDomain, out var left);
        FiniteLaw.DomainEntries(rightDomain, out var right);
        FiniteLaw.DomainEntries(outputDomain, out var output);
        if (left.Length > 64 || right.Length > 64 || output.Length > 64) return null;
        foreach (var row in rows)
            if (!FiniteLaw.TryReadRow(row, out var a, out var b, out var support) ||
                FiniteLaw.IndexOf(left, a) < 0 || FiniteLaw.IndexOf(right, b) < 0 ||
                !TryEncode(support, output, out _)) return null;

        var atomic = new ulong[left.Length * right.Length];
        var consumed = new Relation[atomic.Length][];
        for (var i = 0; i < left.Length; i++)
            for (var j = 0; j < right.Length; j++)
            {
                var read = FiniteLaw.Evaluate(law, StructuralConstraint.Closed(left[i]),
                    StructuralConstraint.Closed(right[j]));
                if (!FiniteLaw.TryReadResult(read, out var support, out var residue, out var provenance) ||
                    FiniteLaw.Entries(residue).Length != 0 || !TryEncode(support, output, out var mask)) return null;
                var index = i * right.Length + j;
                atomic[index] = mask;
                consumed[index] = FiniteLaw.Entries(provenance).Cast<Relation>().ToArray();
            }
        return new FiniteLawBatchPlan(law, leftDomain, rightDomain, outputDomain,
            left, right, output, atomic, consumed);
    }

    public bool TryEncodeLeft(Relation support, out ulong mask) => EncodeCurrent(support, leftStates, out mask);
    public bool TryEncodeRight(Relation support, out ulong mask) => EncodeCurrent(support, rightStates, out mask);
    public bool TryEncodeOutput(Relation support, out ulong mask) => EncodeCurrent(support, outputStates, out mask);

    /// <summary>Reads a batch without witness allocation. Refusal leaves the
    /// complete output buffer unchanged. Masks must already use this plan's bases.
    /// Pair storage may not overlap output storage.</summary>
    public bool TryEvaluateBatch(ReadOnlySpan<FiniteLawMaskPair> inputs, Span<ulong> outputs)
    {
        if (outputs.Length < inputs.Length || !Intact() ||
            MemoryMarshal.AsBytes(inputs).Overlaps(MemoryMarshal.AsBytes(outputs))) return false;
        foreach (var pair in inputs)
            if (!Valid(pair.Left, leftMask) || !Valid(pair.Right, rightMask)) return false;
        for (var i = 0; i < inputs.Length; i++)
            outputs[i] = Compute(inputs[i].Left, inputs[i].Right);
        return true;
    }

    /// <summary>Separate support arrays avoid packing intermediate stage results.
    /// Exact in-place output is supported. Shifted overlap is refused before any
    /// writes so an earlier result cannot replace a later operand.</summary>
    public bool TryEvaluateBatch(ReadOnlySpan<ulong> left, ReadOnlySpan<ulong> right, Span<ulong> outputs)
    {
        if (left.Length != right.Length || outputs.Length < left.Length || !Intact() ||
            ShiftedOverlap(left, outputs) || ShiftedOverlap(right, outputs)) return false;
        for (var i = 0; i < left.Length; i++)
            if (!Valid(left[i], leftMask) || !Valid(right[i], rightMask)) return false;
        for (var i = 0; i < left.Length; i++) outputs[i] = Compute(left[i], right[i]);
        return true;
    }

    /// <summary>Materializes a closed output support in the original native
    /// participant identities. Empty support remains distinct from unknown.</summary>
    public bool TryDecodeOutput(ulong mask, out Relation support) =>
        TryDecodeOutput(OutputDomain, mask, out support);

    /// <summary>A foreign or merely equal-looking basis cannot reinterpret the
    /// addresses of this plan, even if its bit count happens to match.</summary>
    public bool TryDecodeOutput(Relation outputBasis, ulong mask, out Relation support)
    {
        support = null!;
        if (!ReferenceEquals(outputBasis, OutputDomain) || !Intact() || !Valid(mask, outputMask)) return false;
        support = Decode(mask, outputStates);
        return true;
    }

    /// <summary>Materializes one selected read with its source law and all actual
    /// consumed native rows. It does not reconstruct events, schedules or history
    /// that the compute-only caller did not retain.</summary>
    public bool TryCreateWitness(ulong left, ulong right, out Relation witness)
    {
        witness = null!;
        if (!Intact() || !Valid(left, leftMask) || !Valid(right, rightMask)) return false;
        witness = CreateWitness(left, right, Decode(left, leftStates), Decode(right, rightStates));
        return true;
    }

    /// <summary>Witness-producing read for diagnostics. Unencodable supports use
    /// the native reader and stale sources retain explicit unresolved evidence.</summary>
    public Relation Evaluate(Relation left, Relation right)
    {
        if (!Intact()) return FiniteLaw.Unresolved(SourceLaw, left, right, StaleEvidence());
        if (!TryEncode(left, leftStates, out var a) || !TryEncode(right, rightStates, out var b))
            return FiniteLaw.Evaluate(SourceLaw, left, right);
        return CreateWitness(a, b, left, right);
    }

    private Relation CreateWitness(ulong left, ulong right, Relation leftSupport, Relation rightSupport)
    {
        var used = new List<IElement>();
        var remainingLeft = left;
        while (remainingLeft != 0)
        {
            var i = BitOperations.TrailingZeroCount(remainingLeft);
            remainingLeft &= remainingLeft - 1;
            var remainingRight = right;
            while (remainingRight != 0)
            {
                var j = BitOperations.TrailingZeroCount(remainingRight);
                remainingRight &= remainingRight - 1;
                foreach (var row in consumedRows[i * rightStates.Length + j])
                    if (!used.Any(prior => ReferenceEquals(prior, row))) used.Add(row);
            }
        }
        return FiniteLaw.Witness(SourceLaw, leftSupport, rightSupport, OutputDomain,
            Decode(Compute(left, right), outputStates), StructuralConstraint.Closed(),
            StructuralConstraint.Closed(used.ToArray()));
    }

    private ulong Compute(ulong left, ulong right)
    {
        ulong output = 0;
        while (left != 0)
        {
            var row = BitOperations.TrailingZeroCount(left) * rightStates.Length;
            left &= left - 1;
            var remainingRight = right;
            while (remainingRight != 0)
            {
                var column = BitOperations.TrailingZeroCount(remainingRight);
                remainingRight &= remainingRight - 1;
                output |= atomicOutputs[row + column];
            }
        }
        return output;
    }

    private bool EncodeCurrent(Relation support, IElement[] states, out ulong mask)
    {
        mask = 0;
        return Intact() && TryEncode(support, states, out mask);
    }

    private static bool TryEncode(Relation support, IElement[] states, out ulong mask)
    {
        mask = 0;
        if (!FiniteLaw.ClosedEntries(support, out var entries)) return false;
        foreach (var entry in entries)
        {
            var index = FiniteLaw.IndexOf(states, entry);
            if (index < 0) { mask = 0; return false; }
            mask |= 1UL << index;
        }
        return true;
    }

    private static Relation Decode(ulong mask, IElement[] states)
    {
        var participants = new List<IElement>();
        while (mask != 0)
        {
            var index = BitOperations.TrailingZeroCount(mask);
            mask &= mask - 1;
            participants.Add(states[index]);
        }
        return StructuralConstraint.Closed(participants.ToArray());
    }

    private bool Intact()
    {
        foreach (var site in relationSites)
            if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right)) return false;
        foreach (var site in tickSites)
            if (site.Site.Ticks != site.Value) return false;
        return true;
    }

    private Relation StaleEvidence()
    {
        var changed = new List<IElement>();
        foreach (var site in relationSites)
            if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right))
                changed.Add(new Relation(site.Site, new Relation(site.Left, site.Right)));
        foreach (var site in tickSites)
            if (site.Site.Ticks != site.Value) changed.Add(new Relation(site.Site, new TickRef(site.Value)));
        return new Relation(SourceLaw, StructuralConstraint.Closed(changed.ToArray()));
    }

    private static ulong DomainMask(int count) => count == 64 ? ulong.MaxValue : (1UL << count) - 1;
    private static bool Valid(ulong mask, ulong domainMask) => (mask & ~domainMask) == 0;
    private static bool ShiftedOverlap(ReadOnlySpan<ulong> source, Span<ulong> target) =>
        source.Overlaps(target, out var offset) && offset != 0;
}
