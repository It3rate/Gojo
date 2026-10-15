using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An experimental finite relation read. Domains, alternatives, admitted rows
/// and read evidence are native relations; this helper assigns no domain codes.
/// No carriers or numeric samples are created by this mechanism.
/// </summary>
public static class FiniteLaw
{
    // law = ((left-domain -> right-domain) -> output-domain) -> Closed(rows)
    // row = (left-participant -> right-participant) -> output-support
    // read = ((law -> (left-support -> right-support)) -> output-domain) ->
    //        (result-support -> (Closed(unresolved) -> Closed(consumed-rows)))
    // Every scope uses StructuralConstraint's seam/spine/tail grammar. An open
    // result admits an unaccounted tail; Closed() is a known empty alternative set.

    public static Relation Create(Relation leftDomain, Relation rightDomain,
        Relation outputDomain, params Relation[] rows) =>
        new(new Relation(new Relation(leftDomain, rightDomain), outputDomain),
            StructuralConstraint.Closed(rows));

    public static Relation Row(IElement left, IElement right, Relation outputSupport) =>
        new(new Relation(left, right), outputSupport);

    /// <summary>Reads the exact law surface and its finite closed domain scopes.
    /// Row applicability and output support are checked during evaluation.</summary>
    public static bool TryRead(Relation law, out Relation leftDomain,
        out Relation rightDomain, out Relation outputDomain, out Relation[] rows)
    {
        leftDomain = rightDomain = outputDomain = null!; rows = [];
        if (law.Left is not Relation { Left: Relation domains, Right: Relation output } ||
            domains.Left is not Relation left || domains.Right is not Relation right ||
            law.Right is not Relation rowScope || !ClosedEntries(rowScope, out var entries) ||
            entries.Any(entry => entry is not Relation) ||
            !DomainEntries(left, out _) || !DomainEntries(right, out _) ||
            !DomainEntries(output, out _)) return false;
        leftDomain = left; rightDomain = right; outputDomain = output;
        rows = entries.Cast<Relation>().ToArray();
        return true;
    }

    /// <summary>Interprets the native relation rows through the shared matcher.
    /// Known outcomes remain available alongside open or incompatible evidence.
    /// Missing rows in a closed law admit no outcomes, rather than an invented
    /// default state. This is observation, with no settlement or source mutation.</summary>
    public static Relation Evaluate(Relation law, Relation leftSupport, Relation rightSupport)
    {
        if (!TryRead(law, out var leftDomain, out var rightDomain, out var outputDomain, out _))
            return Unresolved(law, leftSupport, rightSupport, new Relation(law, new Placeholder()));

        DomainEntries(leftDomain, out var leftStates);
        DomainEntries(rightDomain, out var rightStates);
        DomainEntries(outputDomain, out var outputStates);
        var unresolved = new List<IElement>();
        var left = Admitted(leftSupport, leftDomain, leftStates, unresolved, out var leftEmpty);
        var right = Admitted(rightSupport, rightDomain, rightStates, unresolved, out var rightEmpty);
        var outcomes = new List<IElement>();
        var consumed = new List<IElement>();
        var leftSlot = new Placeholder();
        var rightSlot = new Placeholder();
        var outputSlot = new Placeholder();
        var pattern = new Relation(new Relation(leftSlot, rightSlot), outputSlot);
        var selected = StructuralConstraint.Closed(leftSlot, rightSlot, outputSlot);
        var offered = (Relation)law.Right;
        foreach (var a in left)
            foreach (var b in right)
            {
                var match = StructuralConstraint.Match(pattern, selected, offered,
                    new Relation(leftSlot, a), new Relation(rightSlot, b));
                if (!StructuralConstraint.TryRead(match, out _, out var candidates, out _, out var open))
                { unresolved.Add(match); continue; }
                ClosedEntries(open, out var pending);
                unresolved.AddRange(pending);
                ClosedEntries(candidates, out var entries);
                foreach (var entry in entries)
                {
                    if (entry is not Relation candidate ||
                        !StructuralConstraint.TryReadCandidate(candidate, out var participant, out _, out _) ||
                        participant is not Relation row || !TryReadRow(row, out _, out _, out var support))
                    { unresolved.Add(entry); continue; }
                    AddDistinct(consumed, row);
                    var values = Admitted(support, outputDomain, outputStates, unresolved, out _);
                    foreach (var value in values) AddDistinct(outcomes, value);
                }
            }

        // An impossible operand has no pairs, even if its other operand has an
        // unaccounted tail. Keep that evidence without manufacturing outcomes.
        var result = unresolved.Count > 0 && !leftEmpty && !rightEmpty
            ? StructuralConstraint.Open(outcomes.ToArray())
            : StructuralConstraint.Closed(outcomes.ToArray());
        return Witness(law, leftSupport, rightSupport, outputDomain, result,
            StructuralConstraint.Closed(unresolved.ToArray()), StructuralConstraint.Closed(consumed.ToArray()));
    }

    /// <summary>Reads only the exact witness surface. Result may be an open
    /// scope; residue and provenance are closed evidence scopes.</summary>
    public static bool TryReadResult(Relation read, out Relation support,
        out Relation residue, out Relation provenance)
    {
        support = residue = provenance = null!;
        if (read.Left is not Relation { Left: Relation source, Right: Relation } ||
            source.Left is not Relation || source.Right is not Relation { Left: Relation, Right: Relation } ||
            read.Right is not Relation { Left: Relation result, Right: Relation remainder } ||
            remainder.Left is not Relation pending || remainder.Right is not Relation evidence ||
            !StructuralConstraint.TryReadScope(result, out _, out _, out _) ||
            !ClosedEntries(pending, out _) || !ClosedEntries(evidence, out _)) return false;
        support = result; residue = pending; provenance = evidence;
        return true;
    }

    /// <summary>Inspects the exposed scope only, retaining open/closed status in
    /// the original scope. This convenience throws for malformed programmer input.</summary>
    public static IElement[] Entries(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)
            ? entries : throw new ArgumentException("Expected a finite scope surface.", nameof(scope));

    /// <summary>Connects publication, frame correspondence or other source
    /// evidence to a completed read without replacing its admitted results.
    /// The preceding read remains available as the provenance's source.</summary>
    public static Relation WithProvenance(Relation read, IElement evidence)
    {
        if (!TryReadResult(read, out var result, out var residue, out var provenance))
            throw new ArgumentException("Expected a finite-law read witness.", nameof(read));
        var connected = StructuralConstraint.Closed([.. Entries(provenance), new Relation(read, evidence)]);
        return new Relation(read.Left, new Relation(result, new Relation(residue, connected)));
    }

    internal static Relation Witness(Relation law, Relation left, Relation right,
        Relation outputDomain, Relation result, Relation residue, Relation provenance) =>
        new(new Relation(new Relation(law, new Relation(left, right)), outputDomain),
            new Relation(result, new Relation(residue, provenance)));

    internal static Relation Unresolved(Relation law, Relation left, Relation right, IElement evidence)
    {
        var domain = TryRead(law, out _, out _, out var output, out _) ? output : StructuralConstraint.Open();
        return Witness(law, left, right, domain, StructuralConstraint.Open(),
            StructuralConstraint.Closed(evidence), StructuralConstraint.Closed());
    }

    internal static bool TryReadRow(Relation row, out IElement left, out IElement right, out Relation output)
    {
        left = right = null!; output = null!;
        if (row.Left is not Relation pair || row.Right is not Relation support) return false;
        left = pair.Left; right = pair.Right; output = support;
        return true;
    }

    internal static bool ClosedEntries(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);

    internal static bool DomainEntries(Relation domain, out IElement[] entries)
    {
        if (!ClosedEntries(domain, out entries)) return false;
        for (var i = 0; i < entries.Length; i++)
            for (var j = 0; j < i; j++)
                if (SameParticipant(entries[i], entries[j])) return false;
        return true;
    }

    internal static bool SameParticipant(IElement left, IElement right) =>
        ReferenceEquals(left, right) || left is Placeholder a && right is Placeholder b && a.Equals(b) ||
        left is CarrierLocation x && right is CarrierLocation y &&
        x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);

    internal static int IndexOf(IElement[] states, IElement participant)
    {
        for (var i = 0; i < states.Length; i++)
            if (SameParticipant(states[i], participant)) return i;
        return -1;
    }

    private static IElement[] Admitted(Relation support, Relation domain, IElement[] states,
        List<IElement> unresolved, out bool definitelyEmpty)
    {
        definitelyEmpty = false;
        if (!StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail))
        { unresolved.Add(new Relation(support, domain)); return []; }
        var selected = new List<IElement>();
        foreach (var entry in entries)
            if (IndexOf(states, entry) < 0) unresolved.Add(new Relation(entry, domain));
            else AddDistinct(selected, entry);
        if (!seam.Equals(tail)) unresolved.Add(new Relation(support, tail));
        definitelyEmpty = entries.Length == 0 && seam.Equals(tail);
        return selected.ToArray();
    }

    private static void AddDistinct(List<IElement> entries, IElement entry)
    {
        if (!entries.Any(existing => SameParticipant(existing, entry))) entries.Add(entry);
    }
}
