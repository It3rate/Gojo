using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Selects one proposed continuation from an exposed finite proposal. This is
/// an operation on possible actions, not a refinement of an observation of the
/// world and not evidence that an unknown physical state became known. The
/// caller supplies the candidate and sampling evidence; no randomness, retry,
/// branch expansion or effect execution is hidden in this helper.
/// </summary>
public static class FiniteChoice
{
    // selection = ((proposal -> chooser) -> candidate) -> sampling-evidence
    // receipt = selection -> ((command -> accepted-selections) ->
    //                         (blocked-selections -> (unresolved -> checks)))
    // command has exactly the original proposal's publication context, with
    // Closed(candidate) support and (selection -> checks) as operation evidence.
    // The whole proposal stays connected through selection. A selection offers
    // one continuation; its actuator must still account for any actual effect.

    public static Relation Select(Relation proposal, IElement chooser,
        IElement suppliedCandidate, IElement samplingEvidence)
    {
        var selection = new Relation(new Relation(new Relation(proposal, chooser), suppliedCandidate), samplingEvidence);
        var accepted = StructuralConstraint.Closed();
        var blocked = StructuralConstraint.Closed();
        var unresolved = StructuralConstraint.Closed();
        var checks = new List<IElement>();
        var command = proposal;
        if (Authorize(proposal, suppliedCandidate, checks, out var pending))
        {
            accepted = StructuralConstraint.Closed(selection);
            var proof = StructuralConstraint.Closed(checks.ToArray());
            command = new Relation(proposal.Left,
                new Relation(StructuralConstraint.Closed(suppliedCandidate), new Relation(selection, proof)));
            return Receipt(proof);
        }
        var reason = new Relation(selection, StructuralConstraint.Closed(checks.ToArray()));
        if (pending) unresolved = StructuralConstraint.Closed(reason);
        else blocked = StructuralConstraint.Closed(selection);
        return Receipt(StructuralConstraint.Closed(checks.ToArray()));

        Relation Receipt(Relation proof) => new(selection,
            new Relation(new Relation(command, accepted),
                new Relation(blocked, new Relation(unresolved, proof))));
    }

    /// <summary>Inspects successful and refused selection receipts. An inspected
    /// success field is not itself authority; use TryReadSelected for a command.</summary>
    public static bool TryReadSelection(Relation receipt, out Relation proposal,
        out IElement chooser, out IElement candidate, out IElement samplingEvidence,
        out Relation command, out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        proposal = command = accepted = blocked = unresolved = null!;
        chooser = candidate = samplingEvidence = null!;
        if (receipt.Left is not Relation { Left: Relation { Left: Relation origin } choice } selection ||
            origin.Left is not Relation offered ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation refusal } ||
            result.Left is not Relation selected || result.Right is not Relation admitted ||
            refusal.Left is not Relation refused || refusal.Right is not Relation remainder ||
            remainder.Left is not Relation pending || remainder.Right is not Relation checks ||
            !FiniteLaw.ClosedEntries(admitted, out _) || !FiniteLaw.ClosedEntries(refused, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(checks, out _)) return false;
        proposal = offered; chooser = origin.Right; candidate = choice.Right; samplingEvidence = selection.Right;
        command = selected; accepted = admitted; blocked = refused; unresolved = pending;
        return true;
    }

    /// <summary>Rechecks the original proposal's closed nonempty support and
    /// candidate membership, the unchanged native context, singleton command,
    /// and connected operation evidence. It does not trust copied success fields
    /// or certify fairness or unpredictability of the caller's sampling.</summary>
    public static bool TryReadSelected(Relation receipt, out Relation command)
    {
        command = null!;
        if (!TryReadSelection(receipt, out var proposal, out _, out var candidate, out _,
                out var selected, out var accepted, out var blocked, out var unresolved) ||
            receipt.Left is not Relation selection ||
            FiniteLaw.Entries(accepted) is not [var admitted] || !ReferenceEquals(admitted, selection) ||
            FiniteLaw.Entries(blocked).Length != 0 || FiniteLaw.Entries(unresolved).Length != 0 ||
            !Authorize(proposal, candidate, [], out _) ||
            !ReferenceEquals(selected.Left, proposal.Left) ||
            !QualitativePublication.TryRead(selected, out _, out _, out _, out _, out _, out var support, out var evidence) ||
            !FiniteLaw.ClosedEntries(support, out var entries) || entries.Length != 1 ||
            !FiniteLaw.SameParticipant(entries[0], candidate) ||
            evidence is not Relation operation || !ReferenceEquals(operation.Left, selection) ||
            receipt.Right is not Relation { Right: Relation { Right: Relation remainder } } ||
            !ReferenceEquals(operation.Right, remainder.Right) ||
            remainder.Right is not Relation checks || !ConnectedChecks(checks, proposal, candidate)) return false;
        command = selected;
        return true;
    }

    private static bool Authorize(Relation proposal, IElement candidate,
        List<IElement> checks, out bool pending)
    {
        pending = false;
        if (!QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out var domain,
                out var support, out _))
        { pending = true; checks.Add(proposal); return false; }
        checks.Add(new Relation(proposal, new Relation(domain, support)));
        if (!FiniteLaw.ClosedEntries(support, out var alternatives))
        { pending = true; checks.Add(support); return false; }
        if (alternatives.Length == 0) { checks.Add(support); return false; }
        var admitted = true;
        // A valid known candidate cannot hide a foreign alternative in the
        // original proposal. The exposed finite action contract stays whole.
        foreach (var alternative in alternatives)
            admitted &= Member(alternative, domain, checks);
        admitted &= Member(candidate, domain, checks);
        admitted &= Member(candidate, support, checks);
        return admitted;
    }

    private static bool Member(IElement candidate, Relation support, List<IElement> checks)
    {
        var read = StructuralConstraint.Match(candidate, StructuralConstraint.Closed(), support);
        checks.Add(read);
        return StructuralConstraint.TryRead(read, out _, out var candidates, out _, out var unresolved) &&
            FiniteLaw.Entries(candidates).Length != 0 && FiniteLaw.Entries(unresolved).Length == 0;
    }

    private static bool ConnectedChecks(Relation checks, Relation proposal, IElement candidate)
    {
        if (!QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out var domain,
                out var support, out _) || !FiniteLaw.ClosedEntries(support, out var alternatives) ||
            !FiniteLaw.ClosedEntries(checks, out var proofs) || proofs.Length != alternatives.Length + 3 ||
            proofs[0] is not Relation contract || !ReferenceEquals(contract.Left, proposal) ||
            contract.Right is not Relation scopes || !ReferenceEquals(scopes.Left, domain) ||
            !ReferenceEquals(scopes.Right, support)) return false;
        for (var i = 0; i < alternatives.Length; i++)
            if (!MembershipRequest(proofs[i + 1], alternatives[i], domain)) return false;
        return MembershipRequest(proofs[^2], candidate, domain) && MembershipRequest(proofs[^1], candidate, support);
    }

    private static bool MembershipRequest(IElement proof, IElement candidate, Relation support) =>
        proof is Relation read &&
        StructuralConstraint.TryRead(read, out var request, out _, out _, out _) &&
        ReferenceEquals(request.Right, support) &&
        request.Left is Relation { Left: Relation pattern, Right: Relation initial } &&
        FiniteLaw.SameParticipant(pattern.Left, candidate) &&
        pattern.Right is Relation variables && FiniteLaw.ClosedEntries(variables, out var slots) && slots.Length == 0 &&
        FiniteLaw.ClosedEntries(initial, out var bindings) && bindings.Length == 0;
}
