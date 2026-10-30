using System.Runtime.InteropServices;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>Diagnostic reads of the guarded application, not operation tags.</summary>
public enum CompactStepStatus : uint { Unobserved, Applied, Open, Contradiction, Ambiguous, NoTransition, StaleDefinition }

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public readonly record struct CompactStepReceipt(ulong Step, CompactSupport Before,
    CompactSupport Command, CompactSupport Result, CompactSupport After,
    uint MissingPairs, uint MatchedRows, CompactStepStatus Status, uint Extension = 0,
    uint StateSlotAddress = 0, ulong OccurrenceIdentity = 0)
{
    public bool Applied => Status == CompactStepStatus.Applied;
}

/// <summary>One persistent occurrence, one current receipt and no retained event
/// history. Step is an event ordinal, not measured time. Input application is
/// explicit structural binding, not carrier settlement or a material transfer.</summary>
public sealed class CompactOccurrence
{
    private static long lastIdentity;
    public CompactTransitionPlan Plan { get; }
    /// <summary>Physical occurrence address, never a magnitude or elapsed time.
    /// A template site is qualified by this identity in this execution world.</summary>
    public ulong OccurrenceIdentity { get; }
    public uint StateSlotAddress { get; }
    public CompactSupport State { get; private set; }
    public CompactStepReceipt CurrentReceipt { get; private set; }
    public ulong CurrentStep { get; private set; }
    public static int StatePayloadBytes => Marshal.SizeOf<CompactSupport>();
    public static int ReceiptPayloadBytes => Marshal.SizeOf<CompactStepReceipt>();
    public static int MutablePayloadBytes => StatePayloadBytes + ReceiptPayloadBytes + sizeof(ulong);
    public static int OccurrenceMetadataPayloadBytes => sizeof(ulong) + sizeof(uint);
    /// <summary>CLR x64 object estimate: payload, plan reference, object header;
    /// allocator/heap bookkeeping is not included. Measure allocation separately.</summary>
    public static int EstimatedObjectBytes => (MutablePayloadBytes + OccurrenceMetadataPayloadBytes + IntPtr.Size + 2 * IntPtr.Size + 7) / 8 * 8;

    internal CompactOccurrence(CompactTransitionPlan plan, CompactSupport initial, uint stateSlotAddress)
    {
        Plan = plan; plan.ValidateState(initial);
        if (stateSlotAddress != 0 && !plan.Arena.IsPlaceholder(stateSlotAddress))
            throw new ArgumentException("A bound state slot must address an actual captured placeholder.", nameof(stateSlotAddress));
        var identity = Interlocked.Increment(ref lastIdentity);
        if (identity <= 0) throw new OverflowException("Occurrence identity storage exhausted.");
        OccurrenceIdentity = (ulong)identity; StateSlotAddress = stateSlotAddress; State = initial;
    }

    public CompactStepReceipt Step(CompactSupport command)
    {
        Plan.ValidateCommand(command);
        var nextStep = checked(CurrentStep + 1);
        var before = State;
        var result = Plan.Read(before, command, out var missing, out var rows);
        var status = Plan.Classify(before, command, result, missing, Plan.IsStale);
        if (status == CompactStepStatus.Applied) State = result;
        CurrentStep = nextStep;
        return CurrentReceipt = new(nextStep, before, command, result, State, missing, rows, status,
            StateSlotAddress: StateSlotAddress, OccurrenceIdentity: OccurrenceIdentity);
    }

    /// <summary>An explicit external binding replacement. It does not replay an
    /// event or imply that an ambiguous support settled itself.</summary>
    public void Import(CompactSupport support)
    {
        Plan.ValidateState(support);
        if (Plan.IsStale) throw new InvalidOperationException("Recapture the edited definition before importing state.");
        State = support; CurrentReceipt = default;
    }

    /// <summary>Starts a new local event sequence with the same occurrence object.</summary>
    public void Reset(CompactSupport support) { Import(support); CurrentStep = 0; }
}
