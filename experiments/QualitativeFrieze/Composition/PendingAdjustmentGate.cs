using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A native finite response filter for a comparison-driven extent
/// actuator. Repeated requests in the direction of existing debt become an
/// explicit hold; opposed requests remain able to account for that debt.
/// It neither clears tension nor changes the supplied goal.</summary>
public sealed class PendingAdjustmentGate
{
    // Closed(identity, advertised live tension port, receiver sensor, owner,
    //   comparison basis/domain, pending-sign basis/domain, nine-row law).
    // Private witness = (Surface -> original packet) ->
    //   Closed(captured pending sample, law read, output packet).
    // The transmitted packet retains the original event, with only an opaque
    // gate identity as evidence. Private pending magnitude stays at the gate.
    public Relation Surface { get; }
    public Relation Law { get; }
    private readonly IElement[] fields;
    private readonly IElement[] comparisonStates;
    private readonly IElement[] pendingStates;
    private readonly FiniteLawPlan plan;

    public sealed record Read(Relation Packet, int InputMask, int OutputMask,
        long? Pending, bool Resolved, bool Suppressed, Relation Witness, Relation LawRead);

    private PendingAdjustmentGate(Relation surface)
    {
        fields = Entries(surface);
        if (fields is not [Placeholder, Relation port, IElement, IElement,
            Carrier basis, Relation domain, Carrier pendingBasis, Relation pendingDomain, Relation law] ||
            port is not { Left: CarrierLocation at, Right: Carrier carrier } ||
            !ReferenceEquals(at.Carrier, carrier) || !at.SameTickRefAs(carrier.LiveTensionSlot) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, domain) || !ReferenceEquals(right, pendingDomain) || !ReferenceEquals(output, domain))
            throw new ArgumentException("Expected an advertised pending-adjustment port and its exact finite response law.", nameof(surface));
        comparisonStates = Entries(domain); pendingStates = Entries(pendingDomain);
        if (!Coordinates(comparisonStates, basis) || !Coordinates(pendingStates, pendingBasis))
            throw new ArgumentException("Expected separate decrease/equal/increase and negative/zero/positive domains.", nameof(surface));
        Surface = surface; Law = law;
        plan = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("The gate law cannot be compiled.", nameof(surface));
    }

    public static PendingAdjustmentGate Create(CoupledExtentMachine receiver)
    {
        var comparisons = Entries(receiver.ComparisonDomain);
        var pending = new Carrier();
        var signs = new IElement[] { new CarrierLocation(pending, -1), new CarrierLocation(pending, 0), new CarrierLocation(pending, 1) };
        var domain = S(signs); var rows = new List<Relation>();
        for (var request = 0; request < 3; request++)
            for (var held = 0; held < 3; held++)
                rows.Add(FiniteLaw.Row(comparisons[request], signs[held],
                    S(comparisons[request == held && request != 1 ? 1 : request])));
        var law = FiniteLaw.Create(receiver.ComparisonDomain, domain, receiver.ComparisonDomain, rows.ToArray());
        return FromSurface(S(new Placeholder(), receiver.PendingPort, receiver.Sensor, receiver.Owner,
            receiver.ComparisonBasis, receiver.ComparisonDomain, pending, domain, law));
    }

    public static PendingAdjustmentGate FromSurface(Relation surface) => new(surface);

    public Read Evaluate(Relation packet, bool compiled = true, bool pendingAvailable = true)
    {
        var valid = QualitativePublication.TryRead(packet, out var sensor, out var owner, out var occurrence,
            out var basis, out var domain, out var input, out _) &&
            ReferenceEquals(sensor, fields[2]) && ReferenceEquals(owner, fields[3]) &&
            ReferenceEquals(basis, fields[4]) && ReferenceEquals(domain, fields[5]);
        var port = (Relation)fields[1]; var at = (CarrierLocation)port.Left;
        var pending = pendingAvailable ? (long?)at.Ticks : null;
        var captured = pending is null ? StructuralConstraint.Open(port) :
            S(new Relation(port, new CarrierLocation(new Carrier(at.Carrier.UnitShape), pending.Value)));
        var right = pending is null ? StructuralConstraint.Open() : S(pendingStates[Math.Sign(pending.Value) + 1]);
        var left = valid ? input : StructuralConstraint.Open(packet);
        var read = compiled ? plan.Evaluate(left, right) : FiniteLaw.Evaluate(Law, left, right);
        FiniteLaw.TryReadResult(read, out var answer, out var residue, out _);
        var resolved = valid && Closed(answer, out var outputs) && outputs.Length == 1 && Entries(residue).Length == 0;
        if (Entries(residue).Length != 0 && Closed(answer, out var known))
            answer = StructuralConstraint.Open(known);
        if (!valid) answer = StructuralConstraint.Open();
        // Even a suppressed response consumes the same observation, so replay
        // cannot create a second physical request when the gate later changes.
        var result = QualitativePublication.Create(fields[2], fields[3], valid ? occurrence : new Placeholder(),
            fields[4], (Relation)fields[5], answer, fields[0]);
        var beforeMask = valid ? Mask(input) : 0; var afterMask = Mask(answer);
        var witness = new Relation(new Relation(Surface, packet), S(captured, read, result));
        return new(result, beforeMask, afterMask, pending, resolved,
            resolved && afterMask == 2 && beforeMask != 2, witness, read);
    }

    private int Mask(Relation support)
    {
        if (!Closed(support, out var values)) return 0;
        var mask = 0;
        foreach (var value in values)
        {
            var i = Array.FindIndex(comparisonStates, state => ReferenceEquals(state, value) ||
                state is CarrierLocation a && value is CarrierLocation b &&
                ReferenceEquals(a.Carrier, b.Carrier) && a.SameTickRefAs(b));
            if (i < 0) return 0;
            mask |= 1 << i;
        }
        return mask;
    }
    private static bool Coordinates(IElement[] values, Carrier basis) => values.Length == 3 &&
        values.Select((item, i) => item is CarrierLocation at && ReferenceEquals(at.Carrier, basis) && at.Ticks == i - 1).All(x => x);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] Entries(Relation scope) => Closed(scope, out var entries) ? entries :
        throw new ArgumentException("Expected a closed native scope.");
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
