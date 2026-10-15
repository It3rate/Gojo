using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A local question over an already exposed finite-law read. Response routes
/// are native outcome -> answer relations. A refinement port is requested only
/// when those premises do not support one complete answer. This helper has no
/// sensor classifier, hidden measurement or domain-specific outcome switch.
/// </summary>
public static class QualitativeQuestion
{
    // receipt = ((read -> response-routes) -> refinement-port) ->
    //           (answer-support -> (requested-ports -> proof-scope)).
    public static Relation Ask(Relation read, Relation routes, IElement refinementPort)
    {
        var question = new Relation(new Relation(read, routes), refinementPort);
        var proofs = new List<IElement> { read };
        var answers = new List<IElement>();
        var complete = FiniteLaw.TryReadResult(read, out var support, out var residue, out _)
            && FiniteLaw.ClosedEntries(support, out _) && FiniteLaw.Entries(residue).Length == 0;
        var known = support is not null && StructuralConstraint.TryReadScope(support, out var entries, out _, out _)
            ? entries : [];
        var impossible = support is not null && FiniteLaw.ClosedEntries(support, out var closed) && closed.Length == 0;

        foreach (var outcome in known)
        {
            var answerSlot = new Placeholder();
            var match = StructuralConstraint.Match(new Relation(outcome, answerSlot),
                StructuralConstraint.Closed(answerSlot), routes);
            proofs.Add(match);
            if (!StructuralConstraint.TryRead(match, out _, out var candidates, out _, out var unresolved))
            { complete = false; continue; }
            if (FiniteLaw.Entries(unresolved).Length != 0) complete = false;
            var offers = FiniteLaw.Entries(candidates);
            if (offers.Length == 0) complete = false;
            foreach (var entry in offers)
            {
                if (entry is not Relation candidate ||
                    !StructuralConstraint.TryReadCandidate(candidate, out _, out var instance, out _) ||
                    instance is not Relation response)
                { complete = false; continue; }
                if (!answers.Any(existing => FiniteLaw.SameParticipant(existing, response.Right)))
                    answers.Add(response.Right);
            }
        }

        var answerSupport = complete || impossible
            ? StructuralConstraint.Closed(answers.ToArray())
            : StructuralConstraint.Open(answers.ToArray());
        // A closed empty premise is a contradiction, not a reason to invent a
        // default answer or fetch a measurement automatically.
        var requests = impossible || complete && answers.Count == 1
            ? StructuralConstraint.Closed() : StructuralConstraint.Closed(refinementPort);
        return new Relation(question, new Relation(answerSupport,
            new Relation(requests, StructuralConstraint.Closed(proofs.ToArray()))));
    }

    public static bool TryRead(Relation receipt, out Relation answers,
        out Relation requests, out Relation proofs)
    {
        answers = requests = proofs = null!;
        if (receipt.Left is not Relation { Left: Relation { Left: Relation, Right: Relation } } ||
            receipt.Right is not Relation { Left: Relation support, Right: Relation rest } ||
            rest.Left is not Relation ports || rest.Right is not Relation evidence ||
            !StructuralConstraint.TryReadScope(support, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(ports, out _) || !FiniteLaw.ClosedEntries(evidence, out _)) return false;
        answers = support; requests = ports; proofs = evidence;
        return true;
    }

    public static bool TryReadAnswer(Relation receipt, out IElement answer)
    {
        answer = null!;
        if (!TryRead(receipt, out var answers, out var requests, out _) ||
            !FiniteLaw.ClosedEntries(answers, out var entries) || entries.Length != 1 ||
            FiniteLaw.Entries(requests).Length != 0) return false;
        answer = entries[0];
        return true;
    }

    /// <summary>
    /// Explicitly services only the requested port. A resolved question never
    /// calls the supplier. The returned witness connects the request and fresh
    /// evidence; it does not silently replace the earlier observation.
    /// </summary>
    public static Relation Service(Relation receipt, Func<IElement, Relation> supplier)
    {
        var serviced = new List<IElement>();
        if (TryRead(receipt, out _, out var requests, out _))
            foreach (var port in FiniteLaw.Entries(requests))
                serviced.Add(new Relation(port, supplier(port)));
        return new Relation(receipt, StructuralConstraint.Closed(serviced.ToArray()));
    }
}
