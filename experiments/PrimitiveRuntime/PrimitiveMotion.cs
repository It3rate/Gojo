using Gojo.Core;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>One persistent live occurrence. UI calls schedule structural inputs;
/// the captured native transition law alone supplies its response.</summary>
public sealed class PrimitiveMotion
{
    private readonly CompactSupport pulse;
    private readonly CompactSupport reverse;
    private readonly CompactSupport blocked;
    private readonly CompactSupport initial;
    private MotionCalibration? calibration;
    public MotionDefinition Definition { get; }
    public CompactTransitionPlan Plan { get; }
    public CompactOccurrence Occurrence { get; }

    public PrimitiveMotion() : this(new MotionDefinition()) { }

    public PrimitiveMotion(MotionDefinition definition)
    {
        Definition = definition;
        Plan = CompactTransitionPlan.Capture(definition.Law, definition.Surface);
        initial = CompactSupport.Closed(Plan.StateMask(definition.State(definition.Landmarks[1], definition.Forward)));
        pulse = CompactSupport.Closed(Plan.CommandMask(definition.Pulse));
        reverse = CompactSupport.Closed(Plan.CommandMask(definition.ReverseInput));
        blocked = CompactSupport.Closed(Plan.CommandMask(definition.UnadmittedInput));
        Occurrence = Plan.CreateOccurrence(initial, Plan.Arena.AddressOf(definition.Input));
    }

    public void Step() => Occurrence.Step(pulse);
    public void ReverseTravel() => Occurrence.Step(reverse);
    public void Block() => Occurrence.Step(blocked);
    public void Reset() => Occurrence.Reset(initial);
    public void SetUnknown() => Occurrence.Import(CompactSupport.Open(0, Plan.Arena.AddressOf(Definition.OpenRemainder)));
    public void SetAlternatives() => Occurrence.Import(CompactSupport.Closed(
        Plan.StateMask(Definition.State(Definition.Landmarks[0], Definition.Forward)) |
        Plan.StateMask(Definition.State(Definition.Landmarks[^1], Definition.Backward))));

    /// <summary>Captures the selected occurrence's connected current evidence.
    /// Fresh native identities preserve aliases within this inspection, including
    /// moving site -> input slot -> current support. Retaining it is optional.
    /// </summary>
    public Relation Inspect()
    {
        var receipt = Occurrence.CurrentReceipt;
        if (receipt.Step == 0)
            receipt = new(0, Occurrence.State, CompactSupport.Closed(0), CompactSupport.Closed(0),
                Occurrence.State, 0, 0, CompactStepStatus.Unobserved,
                StateSlotAddress: Occurrence.StateSlotAddress, OccurrenceIdentity: Occurrence.OccurrenceIdentity);
        var inspection = Plan.InspectReceipt(receipt);
        var nativeSurface = inspection.Snapshot[Plan.Arena.AddressOf(Definition.Surface)];
        var outputBinding = new Relation(inspection.Snapshot[Plan.Arena.AddressOf(Definition.Output)], inspection.Result);
        return new(new Relation(nativeSurface, outputBinding), inspection.Surface);
    }

    /// <summary>Allocating, optional observation. No call is needed for stepping.
    /// Metric coordinates originate only in an explicitly selected calibration.</summary>
    public MotionView Observe(bool reverseObserver = false, int? quantum = null)
    {
        if (quantum is <= 0) throw new ArgumentOutOfRangeException(nameof(quantum));
        if (quantum.HasValue) calibration ??= new(Definition);
        var state = Occurrence.State;
        var candidates = new List<MotionCandidate>();
        for (var index = 0; index < Definition.States.Count; index++)
        {
            if ((state.Known & (1UL << index)) == 0) continue;
            var atom = Definition.States[index];
            var landmark = Definition.LandmarkOf(atom);
            var metric = quantum.HasValue ? calibration!.Observe(landmark, reverseObserver, quantum.Value) : null;
            candidates.Add(new(landmark, ReferenceEquals(atom.Right, Definition.Forward), metric?.Ticks,
                metric?.CoarseTicks, metric?.ResidueNumerator, metric?.ResidueDenominator));
        }
        var receipt = Occurrence.CurrentReceipt;
        var status = receipt.Step == 0 ? "Ready" : receipt.Status.ToString();
        var receiptText = receipt.Step == 0 ? "Explicit input binding; no transition receipt yet." :
            $"Input {receipt.Command.Known:X}; offered {receipt.Result.Known:X}; " +
            $"{receipt.MatchedRows} native row(s), {receipt.MissingPairs} unsupported pair(s). " +
            (receipt.Applied ? "Unique closed response applied." : "Current binding retained.");
        return new(checked((long)Occurrence.CurrentStep), candidates.ToArray(), state.IsOpen, status,
            state.Known, receipt.Result.Known, reverseObserver ? [3, 2, 1, 0] : [0, 1, 2, 3],
            quantum.HasValue ? calibration!.Positions.ToArray() : null, quantum,
            checked((int)Plan.Arena.AddressOf(Definition.Anchor)),
            checked((int)Plan.Arena.AddressOf(Definition.Moving)),
            checked((int)Plan.Arena.AddressOf(Definition.Independent)),
            Plan.DefinitionPayloadBytes, CompactOccurrence.EstimatedObjectBytes, receiptText,
            receipt.Step != 0 && receipt.Applied);
    }
}

public sealed record MotionCandidate(int Landmark, bool Forward, long? Ticks, long? CoarseTicks,
    long? ResidueNumerator, long? ResidueDenominator);

public sealed record MotionView(long Step, MotionCandidate[] Candidates, bool Open, string Status,
    ulong StateMask, ulong OfferedMask, int[] LandmarkOrder, long[]? CalibratedTicks, long? Quantum,
    int AnchorAddress, int MovingAddress, int IndependentAddress, long DefinitionBytes,
    long OccurrenceBytes, string Receipt, bool Applied);
