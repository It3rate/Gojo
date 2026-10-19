using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// A non-draining, explicit compromise between overlapping claims on actual
/// atomic interiors. No claim wins from its position in a scope. The result
/// retains rejected proposals; a separate partition read must still establish
/// connected cells, legal boundaries, embedding and periodic seam agreement.
/// </summary>
public sealed class InteriorBoundaryNegotiation
{
    // source = actual complex -> (proposed group claims -> selected resolutions)
    // claim = actual group identity -> Closed(actual atomic face ROWS)
    // resolution = actual contested face ROW -> chosen actual group identity
    // accepted = ((actual atom -> actual accepted claim) -> actual resolution
    //              or Closed()) -> Closed(actual competing claims)
    // rejected = (actual atom -> actual losing claim) ->
    //            (actual resolution -> actual accepted claim)
    // read = source -> (result claims -> (accepted ->
    //                    (blocked -> (open -> invalid))))
    public Relation Source { get; }
    public Relation Complex { get; }
    public Relation ProposedClaims { get; }
    public Relation Resolutions { get; }
    public Relation ResultClaims { get; }
    public Relation Accepted { get; }
    public Relation RejectedClaims { get; }
    public Relation Blocked { get; }
    public Relation Open { get; }
    public Relation Invalid { get; }
    public Relation Surface { get; }
    public bool IsSettled { get; }
    public InteriorBoundaryNegotiationSummary Summary { get; }

    public static InteriorBoundaryNegotiation Reconcile(Relation complex,
        Relation proposedGroupClaims, Relation resolutions) => new(complex, proposedGroupClaims, resolutions);

    private InteriorBoundaryNegotiation(Relation complex, Relation proposedGroupClaims, Relation resolutions)
    {
        Complex = complex; ProposedClaims = proposedGroupClaims; Resolutions = resolutions;
        Source = new(complex, new Relation(proposedGroupClaims, resolutions));
        var invalid = new List<IElement>(); var open = new List<IElement>();
        var accepted = new List<IElement>(); var rejected = new List<IElement>();
        var atoms = Array.Empty<Relation>(); var domainReady = false;

        // Read only the advertised exact complex surface. This is membership
        // evidence, not a second hidden geometric or topological classifier.
        if (complex is { Left: Relation sites, Right: Relation { Left: Relation edges, Right: Relation faces } })
        {
            domainReady = Closed(sites, invalid, open, out _) &
                Closed(edges, invalid, open, out _) & Closed(faces, invalid, open, out var faceRows);
            if (domainReady)
            {
                atoms = faceRows.OfType<Relation>().ToArray();
                domainReady = atoms.Length == faceRows.Length && atoms.Length > 0 &&
                    N.Distinct(faceRows) && N.Distinct(atoms.Select(atom => atom.Left).ToArray());
                foreach (var atom in atoms)
                    if (atom.Right is not Relation boundary || !Closed(boundary, invalid, open, out _)) domainReady = false;
                if (!domainReady) invalid.Add(new Relation(complex, faces));
            }
        }
        else invalid.Add(complex);

        var claimsReady = Closed(proposedGroupClaims, invalid, open, out var claimEntries);
        var resolutionReady = Closed(resolutions, invalid, open, out var resolutionEntries);
        var validClaims = new List<Relation>();
        atoms = atoms.Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray();
        var owners = new Dictionary<Relation, List<Relation>>(ReferenceEqualityComparer.Instance);
        foreach (var atom in atoms) owners.Add(atom, []);
        if (domainReady && claimsReady)
        {
            var claimRows = claimEntries.OfType<Relation>().ToArray();
            foreach (var entry in claimEntries)
            {
                if (entry is not Relation { Right: Relation selected } claim)
                { invalid.Add(new Relation(entry, proposedGroupClaims)); continue; }
                if (claimRows.Count(other => N.Same(other.Left, claim.Left)) != 1)
                { invalid.Add(new Relation(claim, proposedGroupClaims)); continue; }
                if (!Closed(selected, invalid, open, out var selectedRows)) continue;
                var members = selectedRows.OfType<Relation>().ToArray();
                var valid = selectedRows.Length != 0 && members.Length == selectedRows.Length && N.Distinct(selectedRows);
                foreach (var row in selectedRows)
                    if (!atoms.Any(atom => ReferenceEquals(atom, row)))
                    { invalid.Add(new Relation(claim, row)); valid = false; }
                if (!valid) { invalid.Add(new Relation(claim, selected)); continue; }
                validClaims.Add(claim);
                foreach (var atom in members) owners[atom].Add(claim);
            }
        }

        var choices = new Dictionary<Relation, List<Relation>>(ReferenceEqualityComparer.Instance);
        if (domainReady && resolutionReady)
            foreach (var entry in resolutionEntries)
            {
                if (entry is not Relation resolution || resolution.Left is not Relation atom ||
                    !atoms.Any(actual => ReferenceEquals(actual, atom)))
                { invalid.Add(new Relation(entry, resolutions)); continue; }
                if (!choices.TryGetValue(atom, out var rows)) choices.Add(atom, rows = []);
                rows.Add(resolution);
                if (owners[atom].Count <= 1 || !owners[atom].Any(claim => N.Same(claim.Left, resolution.Right)))
                    invalid.Add(new Relation(resolution, N.S(owners[atom].Cast<IElement>().ToArray())));
            }

        var assigned = new Dictionary<Relation, List<Relation>>(ReferenceEqualityComparer.Instance);
        foreach (var claim in validClaims) assigned.Add(claim, []);
        var contested = 0; var acceptedAtoms = 0; var openAtoms = 0;
        foreach (var atom in atoms)
        {
            var participants = owners[atom];
            if (participants.Count == 0)
            { open.Add(new Relation(atom, proposedGroupClaims)); openAtoms++; continue; }
            if (participants.Count == 1)
            {
                var owner = participants[0]; assigned[owner].Add(atom); acceptedAtoms++;
                accepted.Add(new Relation(new Relation(new Relation(atom, owner), N.S()), N.S(owner)));
                continue;
            }
            contested++;
            var offers = N.S(participants.Cast<IElement>().ToArray());
            var offeredChoices = choices.TryGetValue(atom, out var actual) ? actual : [];
            if (!resolutionReady || offeredChoices.Count != 1 ||
                !participants.Any(claim => N.Same(claim.Left, offeredChoices[0].Right)))
            {
                if (offeredChoices.Count > 1) invalid.Add(new Relation(atom, N.S(offeredChoices.Cast<IElement>().ToArray())));
                open.Add(new Relation(new Relation(atom, offers), N.S(offeredChoices.Cast<IElement>().ToArray())));
                openAtoms++; continue;
            }
            var choice = offeredChoices[0]; var winner = participants.Single(claim => N.Same(claim.Left, choice.Right));
            assigned[winner].Add(atom); acceptedAtoms++;
            accepted.Add(new Relation(new Relation(new Relation(atom, winner), choice), offers));
            foreach (var loser in participants.Where(claim => !ReferenceEquals(claim, winner)))
                rejected.Add(new Relation(new Relation(atom, loser), new Relation(choice, winner)));
        }

        ResultClaims = N.S(validClaims.Where(claim => assigned[claim].Count > 0)
            .Select(claim => (IElement)new Relation(claim.Left, N.S(assigned[claim].Cast<IElement>().ToArray()))).ToArray());
        Accepted = N.S(accepted.ToArray()); RejectedClaims = N.S(rejected.ToArray());
        Invalid = N.S(invalid.ToArray()); Open = N.S(open.ToArray()); Blocked = N.S(rejected.Concat(invalid).ToArray());
        IsSettled = domainReady && claimsReady && resolutionReady && invalid.Count == 0 && open.Count == 0 && acceptedAtoms == atoms.Length;
        Surface = new(Source, new Relation(ResultClaims, new Relation(Accepted,
            new Relation(Blocked, new Relation(Open, Invalid)))));
        Summary = new(atoms.Length, claimEntries.Length, contested, acceptedAtoms,
            rejected.Count, openAtoms, invalid.Count, IsSettled);
    }

    private static bool Closed(Relation scope, List<IElement> invalid, List<IElement> open, out IElement[] entries)
    {
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail))
        { invalid.Add(scope); return false; }
        if (!seam.Equals(tail)) { open.Add(new Relation(scope, tail)); return false; }
        return true;
    }
}

/// <summary>Calculated negotiation diagnostics, not an aesthetic score.</summary>
public sealed record InteriorBoundaryNegotiationSummary(int AtomicFaces, int ProposedGroups,
    int ContestedAtoms, int AcceptedAtoms, int RejectedAtomClaims, int OpenAtoms,
    int InvalidWitnesses, bool IsSettled);
