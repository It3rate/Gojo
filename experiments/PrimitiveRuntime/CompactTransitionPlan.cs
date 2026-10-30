using System.Numerics;
using System.Runtime.InteropServices;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>Known alternatives plus an actual unaccounted placeholder address.
/// Bits select participants in one plan's domain; they are not magnitudes.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public readonly record struct CompactSupport(ulong Known, uint OpenTail, uint Extension = 0)
{
    public bool IsOpen => OpenTail != 0;
    public bool IsSingle => !IsOpen && BitOperations.IsPow2(Known);
    public static CompactSupport Closed(ulong known) => new(known, 0);
    public static CompactSupport Open(ulong known, uint tail) => tail != 0
        ? new(known, tail) : throw new ArgumentOutOfRangeException(nameof(tail));
}

/// <summary>A derived execution plan over an owned immutable arena snapshot.
/// Atomic outputs are obtained solely through the existing native finite-law
/// reader. Source edits require explicit validation/recapture; no native object
/// traversal is hidden in the update path.</summary>
public sealed class CompactTransitionPlan
{
    private readonly ulong[] atomicOutputs;
    private readonly byte[] atomicApplications;
    private readonly uint[] stateAddresses;
    private readonly uint[] commandAddresses;
    private readonly int[] proofOffsets;
    private readonly uint[] proofRows;
    private readonly ulong stateDomainMask;
    private readonly ulong commandDomainMask;
    private bool stale;

    private CompactTransitionPlan(CompactArena arena, uint lawAddress, uint guardAddress, uint[] states,
        uint[] commands, ulong[] outputs, byte[] applications, int[] offsets, uint[] rows)
    {
        Arena = arena; LawAddress = lawAddress; GuardAddress = guardAddress;
        stateAddresses = states; commandAddresses = commands;
        atomicOutputs = outputs; atomicApplications = applications; proofOffsets = offsets; proofRows = rows;
        stateDomainMask = Mask(states.Length); commandDomainMask = Mask(commands.Length);
    }

    public CompactArena Arena { get; }
    public uint LawAddress { get; }
    public uint GuardAddress { get; }
    public int StateCount => stateAddresses.Length;
    public int CommandCount => commandAddresses.Length;
    public bool IsStale => stale;
    public int TablePayloadBytes => checked(atomicOutputs.Length * 8 + atomicApplications.Length +
        (stateAddresses.Length + commandAddresses.Length + proofOffsets.Length + proofRows.Length) * 4);
    public int DefinitionPayloadBytes => checked(Arena.PayloadBytes + TablePayloadBytes);
    public int AtomicPairCount => atomicOutputs.Length;
    public int ProofAddressCount => proofRows.Length;
    public int PayloadArrayCount => 7; // Six plan arrays plus arena records.
    public ReadOnlySpan<uint> StateAddresses => stateAddresses;
    public ReadOnlySpan<uint> CommandAddresses => commandAddresses;

    public static CompactTransitionPlan Capture(Relation law, params IElement[] additionalRoots)
    {
        if (!FiniteLaw.TryRead(law, out var states, out var commands, out var outputs, out _) ||
            !ReferenceEquals(states, outputs))
            throw new ArgumentException("A transition law must expose the same actual state and output domain.", nameof(law));
        var native = FiniteLawBatchPlan.Capture(law) ??
            throw new ArgumentException("Expected a closed finite law with at most 64 participants per domain.", nameof(law));
        var guard = new TransitionApplicationGuard(law);
        var arena = CompactArena.Capture([law, guard.Surface, .. additionalRoots]);
        var stateEntries = FiniteLaw.Entries(states); var commandEntries = FiniteLaw.Entries(commands);
        var pairs = new FiniteLawMaskPair[stateEntries.Length * commandEntries.Length];
        var atomic = new ulong[pairs.Length]; var applications = new byte[pairs.Length];
        var guardedOutputs = new ulong[pairs.Length];
        var offsets = new int[pairs.Length + 1]; var proofs = new List<uint>();
        for (var i = 0; i < stateEntries.Length; i++)
            for (var j = 0; j < commandEntries.Length; j++)
            {
                var at = i * commandEntries.Length + j;
                pairs[at] = new(1UL << i, 1UL << j); offsets[at] = proofs.Count;
                if (!native.TryCreateWitness(pairs[at].Left, pairs[at].Right, out var witness) ||
                    !FiniteLaw.TryReadResult(witness, out _, out _, out var provenance))
                    throw new InvalidOperationException("Native law changed during capture.");
                foreach (var row in FiniteLaw.Entries(provenance)) proofs.Add(arena.AddressOf(row));
                if (guard.TryApply(StructuralConstraint.Closed(stateEntries[i]), StructuralConstraint.Closed(commandEntries[j]),
                        out var applied, out _))
                {
                    var outputIndex = Array.FindIndex(stateEntries, item => ReferenceEquals(item, applied) || item is Placeholder a && applied is Placeholder b && a.Equals(b));
                    if (outputIndex < 0) throw new InvalidOperationException("Native guard applied a participant outside its state domain.");
                    applications[at] = 1; guardedOutputs[at] = 1UL << outputIndex;
                }
            }
        offsets[^1] = proofs.Count;
        if (!native.TryEvaluateBatch(pairs, atomic) || !arena.IsSourceCurrent())
            throw new InvalidOperationException("Native law changed during capture.");
        for (var index = 0; index < applications.Length; index++)
            if (applications[index] != 0 && atomic[index] != guardedOutputs[index])
                throw new InvalidOperationException("Native guard and finite-law result disagree.");
        return new(arena, arena.AddressOf(law), arena.AddressOf(guard.Surface), stateEntries.Select(arena.AddressOf).ToArray(),
            commandEntries.Select(arena.AddressOf).ToArray(), atomic, applications, offsets, proofs.ToArray());
    }

    public ulong StateMask(IElement participant) => BitFor(stateAddresses, Arena.AddressOf(participant));
    public ulong CommandMask(IElement participant) => BitFor(commandAddresses, Arena.AddressOf(participant));
    public CompactOccurrence CreateOccurrence(CompactSupport initial, uint stateSlotAddress = 0) => new(this, initial, stateSlotAddress);

    /// <summary>Once explicitly found stale, a plan refuses future occurrence
    /// changes. Restore by capturing a fresh plan, not by quietly retagging it.</summary>
    public bool ValidateSource() { stale |= !Arena.IsSourceCurrent(); return !stale; }

    public ReadOnlySpan<uint> ProofRows(int stateIndex, int commandIndex)
    {
        if ((uint)stateIndex >= StateCount || (uint)commandIndex >= CommandCount)
            throw new ArgumentOutOfRangeException();
        var at = stateIndex * CommandCount + commandIndex;
        return proofRows.AsSpan(proofOffsets[at], proofOffsets[at + 1] - proofOffsets[at]);
    }

    public CompactSupport Read(CompactSupport state, CompactSupport command, out uint missingPairs, out uint matchedRows)
    {
        ValidateState(state); ValidateCommand(command);
        ulong result = 0; missingPairs = matchedRows = 0;
        var remainingStates = state.Known;
        while (remainingStates != 0)
        {
            var i = BitOperations.TrailingZeroCount(remainingStates); remainingStates &= remainingStates - 1;
            var remainingCommands = command.Known;
            while (remainingCommands != 0)
            {
                var j = BitOperations.TrailingZeroCount(remainingCommands); remainingCommands &= remainingCommands - 1;
                var at = i * CommandCount + j; result |= atomicOutputs[at];
                var count = proofOffsets[at + 1] - proofOffsets[at];
                if (count == 0) missingPairs++;
                matchedRows = checked(matchedRows + (uint)count);
            }
        }
        // A closed contradiction has no possible pairs even beside an open input.
        if ((!state.IsOpen && state.Known == 0) || (!command.IsOpen && command.Known == 0)) return CompactSupport.Closed(0);
        // The result's tail is a representative unresolved cause. The complete
        // cause pair remains in the receipt's Before and Command supports.
        return new(result, state.IsOpen ? state.OpenTail : command.OpenTail);
    }

    internal void ValidateState(CompactSupport support) => ValidateSupport(support, stateDomainMask);
    internal void ValidateCommand(CompactSupport support) => ValidateSupport(support, commandDomainMask);
    internal CompactStepStatus Classify(CompactSupport before, CompactSupport command,
        CompactSupport result, uint missing, bool staleDefinition)
    {
        if (staleDefinition) return CompactStepStatus.StaleDefinition;
        if (before.IsOpen || command.IsOpen) return CompactStepStatus.Open;
        if (before.Known == 0 || command.Known == 0) return CompactStepStatus.Contradiction;
        if (missing != 0 || result.Known == 0) return CompactStepStatus.NoTransition;
        if (!before.IsSingle || !command.IsSingle) return CompactStepStatus.Ambiguous;
        var index = BitOperations.TrailingZeroCount(before.Known) * CommandCount + BitOperations.TrailingZeroCount(command.Known);
        return atomicApplications[index] != 0 ? CompactStepStatus.Applied : CompactStepStatus.Ambiguous;
    }
    private void ValidateSupport(CompactSupport support, ulong domain)
    {
        if (support.Extension != 0 || (support.Known & ~domain) != 0 || support.IsOpen && !Arena.IsOpenTail(support.OpenTail))
            throw new ArgumentException("Support has foreign bits, an invalid open tail, or an unsupported extension.");
    }
    private static ulong Mask(int count) => count == 64 ? ulong.MaxValue : (1UL << count) - 1;
    private static ulong BitFor(uint[] addresses, uint address)
    {
        var index = Array.IndexOf(addresses, address);
        return index >= 0 ? 1UL << index : throw new ArgumentException("Participant is outside the selected domain.");
    }

    /// <summary>Allocating inspection of a retained receipt, using an independent
    /// native reconstruction of the frozen arena. It never reads edited sources
    /// and is deliberately excluded from the direct update path.</summary>
    public CompactReceiptInspection InspectReceipt(CompactStepReceipt receipt)
    {
        ValidateState(receipt.Before); ValidateState(receipt.Result); ValidateState(receipt.After);
        ValidateCommand(receipt.Command);
        if (receipt.Extension != 0 || receipt.StateSlotAddress != 0 && !Arena.IsPlaceholder(receipt.StateSlotAddress))
            throw new ArgumentException("Receipt has an invalid state slot or extension.", nameof(receipt));
        if (receipt.Status == CompactStepStatus.StaleDefinition && !IsStale)
            throw new ArgumentException("This plan has no captured source-invalidation event.", nameof(receipt));
        var result = Read(receipt.Before, receipt.Command, out var missingCount, out var rowCount);
        var status = Classify(receipt.Before, receipt.Command, result, missingCount,
            receipt.Status == CompactStepStatus.StaleDefinition);
        var afterSupport = status == CompactStepStatus.Applied ? result : receipt.Before;
        var unobserved = receipt.Status == CompactStepStatus.Unobserved && receipt.Step == 0 &&
            receipt.Command == CompactSupport.Closed(0) && receipt.Result == CompactSupport.Closed(0) &&
            receipt.After == receipt.Before && receipt.MissingPairs == 0 && receipt.MatchedRows == 0;
        if (receipt.OccurrenceIdentity == 0 || !unobserved && (receipt.Step == 0 || result != receipt.Result ||
            missingCount != receipt.MissingPairs || rowCount != receipt.MatchedRows ||
            receipt.Status != status || receipt.After != afterSupport))
            throw new ArgumentException("Receipt does not correspond to this captured plan.", nameof(receipt));
        var snapshot = Arena.Materialize();
        var before = Scope(receipt.Before, stateAddresses); var command = Scope(receipt.Command, commandAddresses);
        var after = Scope(receipt.After, stateAddresses); var resultScope = Scope(receipt.Result, stateAddresses);
        var law = (Relation)snapshot[LawAddress];
        var native = FiniteLaw.Evaluate(law, before, command);
        var guard = TransitionApplicationGuard.FromSurface((Relation)snapshot[GuardAddress]);
        guard.TryApply(before, command, out _, out var guardRead);
        var missing = new List<IElement>();
        for (var i = 0; i < StateCount; i++)
            if ((receipt.Before.Known & 1UL << i) != 0)
                for (var j = 0; j < CommandCount; j++)
                    if ((receipt.Command.Known & 1UL << j) != 0 && ProofRows(i, j).Length == 0)
                        missing.Add(new Relation(snapshot[stateAddresses[i]], snapshot[commandAddresses[j]]));
        var missingScope = StructuralConstraint.Closed(missing.ToArray());
        var occurrence = new Placeholder();
        var stateSlot = receipt.StateSlotAddress == 0 ? new Placeholder() : snapshot[receipt.StateSlotAddress];
        // The enclosing occurrence qualifies template sites. Independent
        // inspections reconstruct independent worlds with explicit correspondence.
        var beforeBinding = new Relation(stateSlot, before); var afterBinding = new Relation(stateSlot, after);
        var readSurface = new Relation(new Relation(new Relation(new Relation(law, snapshot[GuardAddress]), new Relation(native, guardRead)), new Relation(before, command)),
            new Relation(new Relation(resultScope, after), missingScope));
        var surface = new Relation(new Relation(occurrence, new Relation(beforeBinding, afterBinding)), readSurface);
        return new(surface, native, before, command, resultScope, after, missingScope, snapshot,
            occurrence, beforeBinding, afterBinding, guardRead);

        Relation Scope(CompactSupport support, uint[] participants)
        {
            var seam = new Placeholder();
            IElement spine = support.IsOpen ? snapshot[support.OpenTail] : seam;
            for (var index = participants.Length - 1; index >= 0; index--)
                if ((support.Known & 1UL << index) != 0) spine = new Relation(snapshot[participants[index]], spine);
            return new Relation(seam, spine);
        }
    }
}

public sealed record CompactReceiptInspection(Relation Surface, Relation NativeRead,
    Relation Before, Relation Command, Relation Result, Relation After,
    Relation MissingPairs, CompactNativeSnapshot Snapshot, IElement Occurrence,
    Relation BeforeBinding, Relation AfterBinding, Relation GuardRead);
