using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Checks supplied finite equations against their connected native law. This
/// is neither law discovery nor a universal algebra prover. Each success or
/// counterexample retains its obligation, supports and native evaluation reads.
/// </summary>
public static class FiniteLawConformance
{
    // expression = support-scope | law -> (left-expression -> right-expression)
    // equation = left-expression -> right-expression
    // report = (law -> obligations-scope) ->
    //          (Closed(passed) -> (Closed(violated) -> Closed(unresolved)))
    // obligation-witness = equation ->
    //          ((left-support -> right-support) -> Closed(evaluation-proofs))
    // Only applications of the selected law are admitted. Its finite domain
    // participants ground leaf identities; comparisons use its output domain.

    public static Relation Apply(Relation law, Relation leftExpression, Relation rightExpression) =>
        new(law, new Relation(leftExpression, rightExpression));

    public static Relation Equation(Relation leftExpression, Relation rightExpression) =>
        new(leftExpression, rightExpression);

    /// <summary>
    /// Compares complete nonempty output supports as identity sets. An open
    /// obligation scope stays unresolved even when all supplied entries pass.
    /// Missing rows, open inputs, foreign identities and malformed expressions
    /// cannot obtain a vacuous success.
    /// </summary>
    public static Relation Check(Relation law, Relation obligations)
    {
        var passed = new List<IElement>();
        var violated = new List<IElement>();
        var unresolved = new List<IElement>();
        var validLaw = FiniteLaw.TryRead(law, out var leftDomain, out var rightDomain,
            out var outputDomain, out _);
        var domains = validLaw
            ? FiniteLaw.Entries(leftDomain).Concat(FiniteLaw.Entries(rightDomain))
                .Concat(FiniteLaw.Entries(outputDomain)).ToArray()
            : [];
        var outputs = validLaw ? FiniteLaw.Entries(outputDomain) : [];
        if (!StructuralConstraint.TryReadScope(obligations, out var entries, out var seam, out var tail))
            unresolved.Add(obligations);
        else
        {
            if (!seam.Equals(tail)) unresolved.Add(new Relation(obligations, tail));
            foreach (var entry in entries)
            {
                var proofs = new List<IElement>();
                Relation left = StructuralConstraint.Open(), right = StructuralConstraint.Open();
                var complete = validLaw && entry is Relation { Left: Relation, Right: Relation };
                if (entry is Relation { Left: Relation a, Right: Relation b } && validLaw)
                {
                    var leftComplete = Evaluate(law, a, domains, new HashSet<Relation>(ReferenceEqualityComparer.Instance),
                        proofs, out left);
                    var rightComplete = Evaluate(law, b, domains, new HashSet<Relation>(ReferenceEqualityComparer.Instance),
                        proofs, out right);
                    complete = leftComplete && rightComplete && Admitted(left, outputs) && Admitted(right, outputs);
                }
                var witness = new Relation(entry, new Relation(new Relation(left, right),
                    StructuralConstraint.Closed(proofs.ToArray())));
                if (!complete) unresolved.Add(witness);
                else if (SameSet(FiniteLaw.Entries(left), FiniteLaw.Entries(right))) passed.Add(witness);
                else violated.Add(witness);
            }
        }
        if (!validLaw) unresolved.Add(law);
        return new Relation(new Relation(law, obligations),
            new Relation(StructuralConstraint.Closed(passed.ToArray()),
                new Relation(StructuralConstraint.Closed(violated.ToArray()),
                    StructuralConstraint.Closed(unresolved.ToArray()))));
    }

    public static bool TryReadReport(Relation report, out Relation law, out Relation obligations,
        out Relation passed, out Relation violated, out Relation unresolved)
    {
        law = obligations = passed = violated = unresolved = null!;
        if (report.Left is not Relation { Left: Relation source, Right: Relation claims } ||
            report.Right is not Relation { Left: Relation accepted, Right: Relation rest } ||
            rest.Left is not Relation counterexamples || rest.Right is not Relation pending ||
            !FiniteLaw.ClosedEntries(accepted, out _) || !FiniteLaw.ClosedEntries(counterexamples, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _)) return false;
        law = source; obligations = claims; passed = accepted;
        violated = counterexamples; unresolved = pending;
        return true;
    }

    public static bool TryReadObligation(Relation witness, out IElement obligation,
        out Relation leftSupport, out Relation rightSupport, out Relation proofs)
    {
        obligation = null!; leftSupport = rightSupport = proofs = null!;
        if (witness.Right is not Relation { Left: Relation supports, Right: Relation evidence } ||
            supports.Left is not Relation left || supports.Right is not Relation right ||
            !StructuralConstraint.TryReadScope(left, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(right, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(evidence, out _)) return false;
        obligation = witness.Left; leftSupport = left; rightSupport = right; proofs = evidence;
        return true;
    }

    private static bool Evaluate(Relation law, Relation expression, IElement[] domain,
        HashSet<Relation> path, List<IElement> proofs, out Relation support)
    {
        support = StructuralConstraint.Open();
        if (!path.Add(expression)) { proofs.Add(expression); return false; }
        try
        {
            if (StructuralConstraint.TryReadScope(expression, out _, out _, out _))
            {
                support = expression;
                if (Admitted(expression, domain)) return true;
                proofs.Add(new Relation(expression, StructuralConstraint.Closed(domain)));
                return false;
            }
            if (!ReferenceEquals(expression.Left, law) ||
                expression.Right is not Relation { Left: Relation leftExpression, Right: Relation rightExpression })
            { proofs.Add(expression); return false; }

            var leftComplete = Evaluate(law, leftExpression, domain, path, proofs, out var left);
            var rightComplete = Evaluate(law, rightExpression, domain, path, proofs, out var right);
            var read = FiniteLaw.Evaluate(law, left, right);
            proofs.Add(read);
            if (!FiniteLaw.TryReadResult(read, out support, out var residue, out _)) return false;
            var complete = leftComplete && rightComplete &&
                FiniteLaw.ClosedEntries(residue, out var pending) && pending.Length == 0 &&
                FiniteLaw.ClosedEntries(support, out var results) && results.Length > 0;

            // The ordinary interpreter deliberately admits no outcomes for a
            // missing row. Certification must additionally rule out holes in
            // each requested pair, including holes hidden by other outcomes.
            if (leftComplete && rightComplete)
                foreach (var a in FiniteLaw.Entries(left))
                    foreach (var b in FiniteLaw.Entries(right))
                    {
                        var pair = FiniteLaw.Evaluate(law, StructuralConstraint.Closed(a), StructuralConstraint.Closed(b));
                        if (!FiniteLaw.TryReadResult(pair, out var pairSupport, out var pairResidue, out _) ||
                            !FiniteLaw.ClosedEntries(pairSupport, out var values) || values.Length == 0 ||
                            !FiniteLaw.ClosedEntries(pairResidue, out var remaining) || remaining.Length != 0)
                        { proofs.Add(pair); complete = false; }
                    }
            return complete;
        }
        finally { path.Remove(expression); }
    }

    private static bool Admitted(Relation support, IElement[] domain) =>
        FiniteLaw.ClosedEntries(support, out var entries) && entries.Length > 0 &&
        entries.All(entry => FiniteLaw.IndexOf(domain, entry) >= 0);

    private static bool SameSet(IElement[] left, IElement[] right) =>
        left.All(item => FiniteLaw.IndexOf(right, item) >= 0) &&
        right.All(item => FiniteLaw.IndexOf(left, item) >= 0);
}
