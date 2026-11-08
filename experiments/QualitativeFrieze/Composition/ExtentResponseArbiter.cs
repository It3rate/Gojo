using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Two published comparisons offer one response to the same extent owner.
/// Each offer must resolve to a singleton before it can fill a response slot.
/// A native finite law selects agreement or an explicitly ordered preference;
/// unresolved and unselected offers remain local read residue, not tension.
/// This is an authored selector, not a compromise solver or learned priority.
/// </summary>
public sealed class ExtentResponseArbiter
{
    // Surface = Closed(public identity, receiver sensor, receiver owner,
    //   comparison basis, comparison domain, input domain, output domain, law)
    // input domain = Closed(decrease, equal, increase, unfilled candidate slot)
    // output domain = Closed(decrease, equal, increase, unfilled response slot)
    // The unfilled slots describe absence of a settled response. They do not
    // assert a fourth physical comparison or turn unknown comparison into zero.
    // candidate read = publication -> (singleton eligibility -> candidate support)
    // witness = (Surface -> Closed(first, second)) ->
    //   Closed(candidate reads, law read, outgoing packet, retained offers)
    // No input packet, sample, source graph, or witness enters outgoing evidence.

    private readonly IElement identity;
    private readonly IElement sensor;
    private readonly IElement owner;
    private readonly Carrier basis;
    private readonly Relation comparisonDomain;
    private readonly IElement[] comparisons;
    private readonly IElement vacancy;
    private readonly IElement unfilledResponse;
    private readonly FiniteLawPlan plan;

    public Relation Surface { get; }
    public Relation Law { get; }

    public sealed record ArbitrationRead(Relation Packet, byte Mask, bool Resolved,
        bool FirstEligible, bool SecondEligible, bool Conflict, Relation Residue,
        Relation Witness, Relation LawRead);

    private ExtentResponseArbiter(Relation surface)
    {
        if (!Closed(surface, out var entries) || entries is not
            [Placeholder publicIdentity, Placeholder inputSensor, Placeholder inputOwner,
             Carrier comparisonBasis, Relation domain, Relation inputs, Relation outputs, Relation law] ||
            !Closed(domain, out var signs) || signs.Length != 3 ||
            signs.Where((item, i) => item is not CarrierLocation at ||
                !ReferenceEquals(at.Carrier, comparisonBasis) || at.Ticks != i - 1).Any() ||
            !Closed(inputs, out var offered) || offered is not [_, _, _, Placeholder missing] ||
            !Closed(outputs, out var responses) || responses is not [_, _, _, Placeholder unresolved] ||
            Same(missing, unresolved) ||
            !signs.Select((item, i) => Same(item, offered[i]) && Same(item, responses[i])).All(same => same) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, inputs) || !ReferenceEquals(right, inputs) || !ReferenceEquals(output, outputs))
            throw new ArgumentException("Expected the exact two-offer response surface.", nameof(surface));
        plan = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("The selector law is not a finite closed law.", nameof(surface));
        Surface = surface; Law = law; identity = publicIdentity; sensor = inputSensor;
        owner = inputOwner; basis = comparisonBasis; comparisonDomain = domain;
        comparisons = signs; vacancy = missing; unfilledResponse = unresolved;
    }

    /// <summary>Names select supplied native rows during construction only.
    /// "first" and "second" prefer a settled response from the named input,
    /// falling back to the other only when the preferred slot is unfilled.</summary>
    public static ExtentResponseArbiter Create(CoupledExtentMachine receiver, string policy = "agreement")
    {
        if (policy is not ("agreement" or "first" or "second"))
            throw new ArgumentException("Select agreement, first, or second for this supplied fixture.", nameof(policy));
        if (!Closed(receiver.ComparisonDomain, out var signs))
            throw new ArgumentException("The receiver must expose a closed comparison domain.", nameof(receiver));
        var missing = new Placeholder(); var unresolved = new Placeholder();
        var candidates = new IElement[] { signs[0], signs[1], signs[2], missing };
        var responses = new IElement[] { signs[0], signs[1], signs[2], unresolved };
        var inputs = S(candidates); var outputs = S(responses);
        var rows = new List<Relation>();
        for (var a = 0; a < 4; a++)
            for (var b = 0; b < 4; b++)
            {
                var selected = policy == "agreement" ? a == b && a != 3 ? a : 3 :
                    policy == "first" ? a != 3 ? a : b : b != 3 ? b : a;
                rows.Add(FiniteLaw.Row(candidates[a], candidates[b], S(responses[selected])));
            }
        var law = FiniteLaw.Create(inputs, inputs, outputs, rows.ToArray());
        return new(S(new Placeholder(), receiver.Sensor, receiver.Owner, receiver.ComparisonBasis,
            receiver.ComparisonDomain, inputs, outputs, law));
    }

    public static ExtentResponseArbiter FromSurface(Relation surface) => new(surface);
    public byte[] Encode() => GojoNumericFormat.Encode(Surface);
    public static ExtentResponseArbiter Decode(ReadOnlySpan<byte> bytes) =>
        GojoNumericFormat.Decode(bytes) is [Relation surface] ? FromSurface(surface) :
            throw new ArgumentException("Expected one response-selector root.", nameof(bytes));

    /// <summary>Observation only. Two source events remain in the private
    /// witness; the outgoing response has a fresh event identity. Reading this
    /// method again is a new selection event, not replay of an old command.
    /// A caller that retries delivery must reuse the returned Packet.</summary>
    public ArbitrationRead Evaluate(Relation firstPacket, Relation secondPacket,
        bool compiled = true, IElement? eventIdentity = null)
    {
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder)
            throw new ArgumentException("An outgoing event must be an opaque identity.", nameof(eventIdentity));
        var first = ReadCandidate(firstPacket); var second = ReadCandidate(secondPacket);
        var firstInput = S(first.State); var secondInput = S(second.State);
        var read = compiled ? plan.Evaluate(firstInput, secondInput) : FiniteLaw.Evaluate(Law, firstInput, secondInput);
        IElement? selected = null;
        if (FiniteLaw.TryReadResult(read, out var answers, out var remainder, out _) &&
            Closed(answers, out var outcomes) && outcomes.Length == 1 &&
            Closed(remainder, out var pending) && pending.Length == 0 &&
            !Same(outcomes[0], unfilledResponse) && comparisons.Any(sign => Same(sign, outcomes[0])))
            selected = outcomes[0];
        var mask = selected is null ? (byte)0 : (byte)(1 << Array.FindIndex(comparisons, sign => Same(sign, selected)));
        var support = selected is null ? StructuralConstraint.Open() : S(selected);
        var packet = QualitativePublication.Create(sensor, owner, occurrence, basis, comparisonDomain, support, identity);
        var retained = new List<IElement>();
        if (selected is null || !first.Eligible || !Same(first.State, selected)) retained.Add(firstPacket);
        if (selected is null || !second.Eligible || !Same(second.State, selected)) retained.Add(secondPacket);
        var residue = S(retained.ToArray());
        var witness = new Relation(new Relation(Surface, S(firstPacket, secondPacket)),
            S(S(first.Witness, second.Witness), read, packet, residue));
        return new(packet, mask, selected is not null, first.Eligible, second.Eligible,
            first.Eligible && second.Eligible && !Same(first.State, second.State), residue, witness, read);
    }

    private (IElement State, bool Eligible, Relation Witness) ReadCandidate(Relation packet)
    {
        var eligible = QualitativePublication.TryRead(packet, out var source, out var subject,
            out _, out var packetBasis, out var domain, out var support, out _) &&
            Same(source, sensor) && Same(subject, owner) && Same(packetBasis, basis) &&
            ReferenceEquals(domain, comparisonDomain) && Closed(support, out var candidates) &&
            candidates.Length == 1 && comparisons.Any(sign => Same(sign, candidates[0]));
        var candidate = vacancy;
        if (eligible)
        {
            QualitativePublication.TryRead(packet, out _, out _, out _, out _, out _, out var supported, out _);
            Closed(supported, out var offers);
            candidate = comparisons.Single(sign => Same(sign, offers[0]));
        }
        // Keeping the original packet explains whether an unfilled candidate
        // came from missing support, alternatives, empty support or wrong scope.
        var eligibility = eligible ? S(candidate) : StructuralConstraint.Open(packet);
        return (candidate, eligible, new Relation(packet, new Relation(eligibility, S(candidate))));
    }

    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
