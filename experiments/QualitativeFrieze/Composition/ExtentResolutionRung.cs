using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A supplied finer response rung. Activation copies an observed
/// extent through an explicit calibration into an independent actuator. This
/// is an observation import, never a draining material transfer. The original
/// unit, value and history remain available; held debt prevents activation.</summary>
public sealed class ExtentResolutionRung
{
    // Surface = Closed(coarse machine, fine machine, calibration, fine clamp,
    //   response-reversal reader, activation port, admission route,
    //   Closed(attempt witnesses)).
    // admission route = try-finer result -> (calibration -> fine extent port).
    // calibration = S(coarse,0,1) -> S(fine,0,parts).
    // Fine.UnitShape = SubdivisionOf(Coarse.UnitShape,parts).
    // An attempt retains source sample, projected demand, target before/after,
    // admission evidence and any blocked/open premise. Activation is one-shot;
    // this does not create an automatic material allocator or change a unit.
    private Relation surface;
    private readonly Carrier activation;
    private readonly Carrier coarseExtent;
    private readonly Carrier fineExtent;
    private readonly Relation capacity;
    private readonly List<IElement> attempts;
    public CoupledExtentMachine Coarse { get; }
    public CoupledExtentMachine Fine { get; }
    public ResponseReversalRead Reversal { get; }
    public Relation Calibration { get; }
    public Relation Surface => surface;
    public bool Active => activation.Value == 1;

    public sealed record ActivationRead(bool Activated, bool AlreadyActive, bool Blocked,
        long? ProjectedFine, Relation Witness);

    private ExtentResolutionRung(Relation surface, CoupledExtentMachine coarse, CoupledExtentMachine fine)
    {
        var fields = E(surface);
        if (fields is not [Relation coarseSurface, Relation fineSurface, Relation calibration,
            Relation clamp, Relation reversal, Relation { Left: CarrierLocation selected, Right: Carrier active },
            Relation route, Relation history] ||
            !ReferenceEquals(coarseSurface, coarse.Surface) || !ReferenceEquals(fineSurface, fine.Surface) ||
            !ReferenceEquals(selected.Carrier, active) || !selected.SameTickRefAs(active.ValueSlot) || active.Value > 1 || active.Tension != 0 ||
            coarse.ExtentPort.Right is not Carrier source || fine.ExtentPort.Right is not Carrier target ||
            !Span(calibration.Left, source, out var ca, out var cb) || ca != 0 || cb != 1 ||
            !Span(calibration.Right, target, out var fa, out var fb) || fa != 0 || fb <= 1 ||
            !target.UnitShape.Equals(UnitShape.SubdivisionOf(source.UnitShape, fb)) ||
            !ReferenceEquals(E(fine.Surface)[3], clamp) || !Clamp(clamp, target, out _))
            throw new ArgumentException("Expected an exact independent finer extent rung.", nameof(surface));
        this.surface = surface; Coarse = coarse; Fine = fine; Calibration = calibration;
        coarseExtent = source; fineExtent = target; capacity = clamp; activation = active;
        attempts = E(history).ToList(); Reversal = ResponseReversalRead.FromSurface(coarse, reversal);
        if (route is not { Left: var permit, Right: Relation { Left: var mapping, Right: var destination } } ||
            !ReferenceEquals(mapping, calibration) || !ReferenceEquals(destination, fine.ExtentPort) ||
            permit is not CarrierLocation p || Reversal.Permit is not CarrierLocation expected ||
            !ReferenceEquals(p.Carrier, expected.Carrier) || !p.SameTickRefAs(expected))
            throw new ArgumentException("The admitted response must lead through this calibration to this fine extent.", nameof(surface));
    }

    public static ExtentResolutionRung Create(CoupledExtentMachine coarse, long parts = 3, long fineCapacity = 36)
    {
        if (parts <= 1) throw new ArgumentOutOfRangeException(nameof(parts), "A finer rung needs more than one part.");
        var source = (Carrier)coarse.ExtentPort.Right;
        var fine = CoupledExtentMachine.Create(0, fineCapacity, unit: UnitShape.SubdivisionOf(source.UnitShape, parts));
        var target = (Carrier)fine.ExtentPort.Right; var active = new Carrier();
        var calibration = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, parts));
        var reversal = ResponseReversalRead.Create(coarse);
        return new(S(coarse.Surface, fine.Surface, calibration, E(fine.Surface)[3],
            reversal.Surface, new Relation(active.ValueSlot, active),
            new Relation(reversal.Permit, new Relation(calibration, fine.ExtentPort)), S()), coarse, fine);
    }

    public static ExtentResolutionRung FromSurface(Relation surface, CoupledExtentMachine coarse, CoupledExtentMachine fine) => new(surface, coarse, fine);

    /// <summary>Requires a currently applicable reversal witness. A nonempty
    /// fine target, source debt, or insufficient local fine capacity refuses
    /// activation without changing either extent. The refusal retains demand.</summary>
    public ActivationRead Activate(Relation reversalWitness)
    {
        var already = Active;
        var coarseBefore = Coarse.Snapshot(); var fineBefore = Fine.Snapshot();
        var sample = new CarrierLocation(coarseExtent, coarseBefore.Value);
        var projected = Calibration.Project(sample, fineExtent);
        long? demand = null;
        var exact = projected.TryReadProjection(out var target, out var residue) &&
            ProjectionResidue.TryRead(residue, out _, out var numerator, out _) && numerator == 0;
        if (exact) demand = target.Ticks;
        var permitted = !already && Reversal.StillPermits(reversalWitness) && coarseBefore.Tension == 0 &&
            fineBefore.Value == 0 && fineBefore.Tension == 0 && demand.HasValue && demand.Value >= 0 &&
            Clamp(capacity, fineExtent, out var limit) && demand.Value <= limit;
        if (permitted)
        {
            // Import is an explicit delta-set through the retained calibration.
            // The source sample remains available and is never drained.
            if (!fineExtent.TrySetFrom(Calibration, sample))
                throw new InvalidOperationException("The preflighted exact observation import did not apply.");
            activation.Add(1);
        }
        var sourceRead = new Relation(Coarse.ExtentPort, sample);
        var import = new Relation(new Relation(sourceRead, Calibration), new Relation(projected, Fine.ExtentPort));
        var witness = new Relation(new Relation(surface, reversalWitness), S(import,
            coarseBefore.Surface, fineBefore.Surface, Coarse.Snapshot().Surface, Fine.Snapshot().Surface,
            permitted ? S(new Relation(Fine.ExtentPort, new CarrierLocation(fineExtent, (long)fineExtent.Value))) : StructuralConstraint.Open(import)));
        attempts.Add(witness);
        var fields = E(surface); fields[^1] = S(attempts.ToArray()); surface = S(fields);
        return new(permitted, already, !permitted && !already, demand, witness);
    }

    /// <summary>Non-draining coarse read of the fine extent. Fractional fine-
    /// basis residue remains explicit; it does not overwrite the coarse state.</summary>
    public Relation ReadCoarse() => Calibration.Project(new CarrierLocation(fineExtent, (long)fineExtent.Value), coarseExtent);

    public Relation ArchiveSurface() => S(Surface, Coarse.ArchiveSurface(), Fine.ArchiveSurface());
    public static ExtentResolutionRung Restore(Relation archive)
    {
        if (E(archive) is not [Relation surface, Relation coarse, Relation fine])
            throw new ArgumentException("Expected a rung and both local machine archives.", nameof(archive));
        return FromSurface(surface, CoupledExtentMachine.Restore(coarse), CoupledExtentMachine.Restore(fine));
    }

    private static bool Clamp(Relation clamp, Carrier owner, out long limit)
    {
        limit = 0;
        if (!owner.IsAttached(clamp) || !Span(clamp, owner, out var a, out var b) || a != b || a < 0) return false;
        limit = a; return true;
    }
    private static bool Span(IElement surface, Carrier owner, out long a, out long b)
    {
        a = b = 0;
        if (surface is not Relation { Left: Relation { Left: CarrierLocation x, Right: Carrier xc },
            Right: Relation { Left: CarrierLocation y, Right: Carrier yc } } ||
            !ReferenceEquals(x.Carrier, owner) || !ReferenceEquals(y.Carrier, owner) ||
            !ReferenceEquals(xc, owner) || !ReferenceEquals(yc, owner)) return false;
        a = x.Ticks; b = y.Ticks; return true;
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var items, out var seam, out var tail) && seam.Equals(tail)
        ? items : throw new ArgumentException("Expected a closed native scope.");
}

/// <summary>A response reversal after an accepted step is a local reason to try
/// another response resolution. It is not proof of an unreachable goal: changing
/// goals or other participants may also reverse a correction.</summary>
public sealed class ResponseReversalRead
{
    // Surface = Closed(machine, comparison domain, decision basis/domain, law).
    // A nine-row law maps two singleton signs to keep(0) / try-finer(1).
    // Witness = (Surface -> Closed(earlier packet, current packet, prior step))
    //   -> Closed(law read, checked transition or open premise, decision support).
    private readonly CoupledExtentMachine receiver;
    private readonly Carrier decisions;
    private readonly Relation domain;
    private readonly Relation law;
    private readonly FiniteLawPlan plan;
    public Relation Surface { get; }
    public IElement Permit => E(domain)[1];
    public sealed record Read(bool Suggested, bool Resolved, Relation Witness, Relation LawRead);

    private ResponseReversalRead(CoupledExtentMachine receiver, Relation surface)
    {
        if (E(surface) is not [Relation machine, Relation comparisons, Carrier decisionBasis, Relation outputs, Relation selectedLaw] ||
            !ReferenceEquals(machine, receiver.Surface) || !ReferenceEquals(comparisons, receiver.ComparisonDomain) ||
            !FiniteLaw.TryRead(selectedLaw, out var left, out var right, out var result, out _) ||
            !ReferenceEquals(left, comparisons) || !ReferenceEquals(right, comparisons) || !ReferenceEquals(result, outputs) ||
            E(outputs) is not [CarrierLocation no, CarrierLocation yes] || no.Ticks != 0 || yes.Ticks != 1 ||
            !ReferenceEquals(no.Carrier, decisionBasis) || !ReferenceEquals(yes.Carrier, decisionBasis))
            throw new ArgumentException("Expected the exact local response reversal reader.", nameof(surface));
        this.receiver = receiver; Surface = surface; decisions = decisionBasis; domain = outputs; law = selectedLaw;
        plan = FiniteLawPlan.Capture(law)!;
    }

    public static ResponseReversalRead Create(CoupledExtentMachine receiver)
    {
        var decision = new Carrier(); var outputs = S(new CarrierLocation(decision, 0), new CarrierLocation(decision, 1));
        var signs = E(receiver.ComparisonDomain); var results = E(outputs); var rows = new List<Relation>();
        for (var a = 0; a < 3; a++) for (var b = 0; b < 3; b++)
            rows.Add(FiniteLaw.Row(signs[a], signs[b], S(results[a != 1 && b != 1 && a != b ? 1 : 0])));
        var law = FiniteLaw.Create(receiver.ComparisonDomain, receiver.ComparisonDomain, outputs, rows.ToArray());
        return new(receiver, S(receiver.Surface, receiver.ComparisonDomain, decision, outputs, law));
    }
    public static ResponseReversalRead FromSurface(CoupledExtentMachine receiver, Relation surface) => new(receiver, surface);

    public Read Evaluate(Relation earlierPacket, Relation currentPacket, Relation previousStep, bool compiled = true)
    {
        var valid = Packet(earlierPacket, out var earlierEvent, out var earlier) &&
            Packet(currentPacket, out var currentEvent, out var current) && !Same(earlierEvent, currentEvent) &&
            FreshEvent(currentEvent) && ActualStep(previousStep, earlierEvent, earlier);
        Packet(earlierPacket, out _, out earlier); Packet(currentPacket, out _, out current);
        var left = valid ? earlier : StructuralConstraint.Open(earlierPacket);
        var right = valid ? current : StructuralConstraint.Open(currentPacket);
        var read = compiled ? plan.Evaluate(left, right) : FiniteLaw.Evaluate(law, left, right);
        FiniteLaw.TryReadResult(read, out var answer, out var residue, out _);
        var resolved = valid && Closed(answer, out var values) && values.Length == 1 &&
            Closed(residue, out var pending) && pending.Length == 0;
        var suggested = resolved && ((CarrierLocation)E(answer)[0]).Ticks == 1;
        var witness = new Relation(new Relation(Surface, S(earlierPacket, currentPacket, previousStep)),
            S(read, valid ? S(previousStep) : StructuralConstraint.Open(previousStep), answer));
        return new(suggested, resolved, witness, read);
    }

    public bool StillPermits(Relation witness)
    {
        if (witness is not { Left: Relation { Left: var source, Right: Relation inputs }, Right: Relation result } ||
            !ReferenceEquals(source, Surface) || E(inputs) is not [Relation a, Relation b, Relation step] ||
            E(result) is not [Relation _, Relation _, Relation answer] ||
            E(answer) is not [CarrierLocation selected] || !ReferenceEquals(selected.Carrier, decisions) || selected.Ticks != 1) return false;
        return Evaluate(a, b, step).Suggested;
    }

    private bool ActualStep(Relation step, IElement earlierEvent, Relation earlierSupport)
    {
        if (!ReferenceEquals(receiver.History.LastOrDefault(), step) ||
            !TimeStepSequence.TryReadStep(step, out _, out var start, out var end, out _, out var transition, out var after, out var residue) ||
            end.Ticks - start.Ticks != 1 ||
            transition is not Relation { Left: Relation { Left: Relation packet } } ||
            !Packet(packet, out var occurrence, out var consumedSupport) || !Same(occurrence, earlierEvent) ||
            !Same(E(consumedSupport)[0], E(earlierSupport)[0]) ||
            residue is not Relation rest || E(rest) is not [Relation accounting, Relation _, Relation _, Relation _] ||
            E(accounting) is not [CarrierLocation admitted, CarrierLocation accepted, CarrierLocation held, CarrierLocation unrouted] ||
            accepted.Ticks == 0 || admitted.Ticks != accepted.Ticks || held.Ticks != 0 || unrouted.Ticks != 0 ||
            Math.Sign(accepted.Ticks) != ((CarrierLocation)E(earlierSupport)[0]).Ticks ||
            !ReferenceEquals(accepted.Carrier, receiver.StepUnit) ||
            after is not Relation afterState || E(afterState) is not [CarrierLocation value, CarrierLocation tension, CarrierLocation time, CarrierLocation _]) return false;
        var now = receiver.Snapshot();
        return value.Ticks == now.Value && tension.Ticks == now.Tension && time.Ticks == now.Beat;
    }

    private bool FreshEvent(IElement occurrence) => !receiver.History.Any(step =>
        TimeStepSequence.TryReadStep(step, out _, out var start, out var end, out _, out var transition, out _, out _) &&
        end.Ticks > start.Ticks && transition is Relation { Left: Relation { Left: Relation packet } } &&
        QualitativePublication.TryRead(packet, out _, out _, out var consumed, out _, out _, out _, out _) && Same(consumed, occurrence));

    private bool Packet(Relation packet, out IElement occurrence, out Relation support)
    {
        support = StructuralConstraint.Open(); occurrence = null!;
        if (!QualitativePublication.TryRead(packet, out var source, out var owner, out occurrence, out var basis, out var domain, out var incoming, out _) ||
            !Same(source, receiver.Sensor) || !Same(owner, receiver.Owner) || !ReferenceEquals(basis, receiver.ComparisonBasis) ||
            !ReferenceEquals(domain, receiver.ComparisonDomain) || !Closed(incoming, out var values) || values.Length != 1 ||
            !E(domain).Any(state => Same(state, values[0]))) return false;
        support = incoming; return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static bool Closed(Relation scope, out IElement[] items) => StructuralConstraint.TryReadScope(scope, out items, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => Closed(scope, out var items) ? items : [];
}
