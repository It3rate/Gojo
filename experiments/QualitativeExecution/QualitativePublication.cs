using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A non-draining read at an explicit basis, domain and observation event.
/// Resolution transport preserves the observation identity; schedule delivery
/// records a separate receiving event. Neither operation creates a new sample.
/// </summary>
public static class QualitativePublication
{
    // publication = ((source -> (subject -> event)) -> (basis -> domain)) ->
    //               (support -> evidence)
    // application = (publication -> connection/read) ->
    //               (result -> (blocked -> checks))
    public static Relation Create(IElement source, IElement subject, IElement eventIdentity,
        IElement basis, Relation domain, Relation support, IElement? evidence = null) =>
        new(new Relation(new Relation(source, new Relation(subject, eventIdentity)),
                new Relation(basis, domain)),
            new Relation(support, evidence ?? StructuralConstraint.Closed()));

    public static bool TryRead(Relation publication, out IElement source, out IElement subject,
        out IElement eventIdentity, out IElement basis, out Relation domain,
        out Relation support, out IElement evidence)
    {
        source = subject = eventIdentity = basis = evidence = null!;
        domain = support = null!;
        if (publication.Left is not Relation
            { Left: Relation { Right: Relation observation } origin, Right: Relation contract } ||
            contract.Right is not Relation scope || !FiniteLaw.DomainEntries(scope, out _) ||
            publication.Right is not Relation { Left: Relation alternatives } read ||
            !StructuralConstraint.TryReadScope(alternatives, out _, out _, out _)) return false;
        source = origin.Left; subject = observation.Left; eventIdentity = observation.Right;
        basis = contract.Left; domain = scope; support = alternatives; evidence = read.Right;
        return true;
    }

    public static bool TryForward(ResolutionCorrespondence connection, Relation publication,
        out Relation result, out Relation witness) =>
        Transport(connection, publication, false, out result, out witness);

    public static bool TryBackward(ResolutionCorrespondence connection, Relation publication,
        out Relation result, out Relation witness) =>
        Transport(connection, publication, true, out result, out witness);

    /// <summary>
    /// Accepts an admitted nonempty narrowing for this same observation and
    /// contract. A conflicting measurement retains both claims and the old read.
    /// This checks compatibility, not the external truth of either supplier.
    /// </summary>
    public static bool TryRefine(Relation earlier, Relation offered,
        out Relation accepted, out Relation witness)
    {
        accepted = earlier;
        var checks = new List<IElement>();
        var compatible = TryRead(earlier, out var source, out var subject, out var eventIdentity,
                out var basis, out var domain, out var oldSupport, out _) &&
            TryRead(offered, out var otherSource, out var otherSubject, out var otherEvent,
                out var otherBasis, out var otherDomain, out _, out _) &&
            FiniteLaw.SameParticipant(source, otherSource) &&
            FiniteLaw.SameParticipant(subject, otherSubject) &&
            FiniteLaw.SameParticipant(eventIdentity, otherEvent) &&
            FiniteLaw.SameParticipant(basis, otherBasis) && ReferenceEquals(domain, otherDomain);
        if (compatible)
        {
            TryRead(offered, out _, out _, out _, out _, out _, out var newSupport, out _);
            compatible = Narrower(oldSupport!, newSupport, domain!, checks);
            if (compatible)
                accepted = Create(source!, subject!, eventIdentity!, basis!, domain!, newSupport,
                    new Relation(new Relation(earlier, offered), StructuralConstraint.Closed(checks.ToArray())));
        }
        witness = new Relation(new Relation(earlier, offered), new Relation(accepted,
            new Relation(compatible ? StructuralConstraint.Closed() : StructuralConstraint.Closed(offered),
                StructuralConstraint.Closed(checks.ToArray()))));
        return compatible;
    }

    private static bool Transport(ResolutionCorrespondence connection, Relation publication,
        bool backward, out Relation result, out Relation witness)
    {
        result = publication;
        var fromBasis = backward ? connection.TargetBasis : connection.SourceBasis;
        var fromDomain = backward ? connection.TargetDomain : connection.SourceDomain;
        var toBasis = backward ? connection.SourceBasis : connection.TargetBasis;
        var toDomain = backward ? connection.SourceDomain : connection.TargetDomain;
        var admitted = TryRead(publication, out var source, out var subject, out var eventIdentity,
                out var basis, out var domain, out var support, out _) &&
            FiniteLaw.SameParticipant(basis, fromBasis) && ReferenceEquals(domain, fromDomain);
        IElement proof = connection.Surface;
        if (admitted)
        {
            var read = backward ? connection.ReadBackward(support!, publication)
                : connection.ReadForward(support!, publication);
            proof = read;
            admitted = connection.TryReadResult(read, out var mapped, out var blocked, out var pending, out _);
            if (admitted)
            {
                // A known mapped branch cannot erase a rejected foreign branch
                // or an unfilled mapping and masquerade as a complete read.
                var incomplete = !FiniteLaw.ClosedEntries(blocked, out var rejected) || rejected.Length != 0 ||
                    !FiniteLaw.ClosedEntries(pending, out var unresolved) || unresolved.Length != 0;
                var impossibleInput = FiniteLaw.ClosedEntries(support!, out var inputs) && inputs.Length == 0;
                if (incomplete && !impossibleInput && FiniteLaw.ClosedEntries(mapped, out var known))
                    mapped = StructuralConstraint.Open(known);
                result = Create(source!, subject!, eventIdentity!, toBasis, toDomain, mapped, read);
            }
        }
        witness = new Relation(new Relation(publication, connection.Surface),
            new Relation(result, new Relation(admitted ? StructuralConstraint.Closed() :
                StructuralConstraint.Closed(publication), proof)));
        return admitted;
    }

    private static bool Narrower(Relation earlier, Relation offered, Relation domain, List<IElement> checks)
    {
        if (!FiniteLaw.ClosedEntries(offered, out var entries) || entries.Length == 0 ||
            !StructuralConstraint.TryReadScope(earlier, out var oldEntries, out var seam, out var tail)) return false;
        var compatible = true;
        foreach (var entry in oldEntries) compatible &= Member(entry, domain, checks);
        foreach (var entry in entries)
        {
            compatible &= Member(entry, domain, checks);
            if (seam.Equals(tail)) compatible &= Member(entry, earlier, checks);
        }
        return compatible;
    }

    private static bool Member(IElement participant, Relation scope, List<IElement> checks)
    {
        var read = StructuralConstraint.Match(participant, StructuralConstraint.Closed(), scope);
        checks.Add(read);
        return StructuralConstraint.TryRead(read, out _, out var candidates, out _, out var pending) &&
            FiniteLaw.Entries(candidates).Length > 0 && FiniteLaw.Entries(pending).Length == 0;
    }
}
