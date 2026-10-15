using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Detector publication, without a measured scalar:
/// (source -> event) -> ((view -> (boundary -> resolution)) ->
///                     ((side support -> trend support) -> evidence)).
/// The event connects the two published supports. This is a sensor contract,
/// not a claim that a trend can be recovered from one threshold category.
/// </summary>
public static class DetectorObservation
{
    public static Relation Create(IElement source, IElement eventIdentity,
        Relation view, IElement boundary, IElement resolution,
        Relation sides, Relation trends, IElement evidence) =>
        new(new Relation(source, eventIdentity),
            new Relation(new Relation(view, new Relation(boundary, resolution)),
                new Relation(new Relation(sides, trends), evidence)));

    public static bool TryRead(Relation observation, out IElement source,
        out IElement eventIdentity, out Relation view, out Relation sides,
        out Relation trends, out IElement evidence) =>
        TryReadContract(observation, out source, out eventIdentity, out view,
            out _, out _, out sides, out trends, out evidence);

    internal static bool TryReadContract(Relation observation, out IElement source,
        out IElement eventIdentity, out Relation view, out IElement boundary,
        out IElement resolution, out Relation sides, out Relation trends,
        out IElement evidence)
    {
        source = eventIdentity = boundary = resolution = evidence = null!;
        view = sides = trends = null!;
        if (observation.Left is not Relation identity ||
            observation.Right is not Relation body ||
            body.Left is not Relation { Left: Relation frame, Right: Relation contract } ||
            body.Right is not Relation { Left: Relation supports } publication ||
            supports.Left is not Relation sideSupport ||
            supports.Right is not Relation trendSupport ||
            !StructuralConstraint.TryReadScope(sideSupport, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(trendSupport, out _, out _, out _)) return false;
        source = identity.Left; eventIdentity = identity.Right;
        view = frame; boundary = contract.Left; resolution = contract.Right;
        sides = sideSupport; trends = trendSupport; evidence = publication.Right;
        return true;
    }

    /// <summary>
    /// Explicitly intersect closed publications only for the same source,
    /// observation event and detector contract. Conflicting or open joins retain
    /// the offered pair as the returned structural disagreement.
    /// </summary>
    public static bool TryCombine(Relation sideObservation, Relation trendObservation,
        out Relation combined)
    {
        combined = new Relation(sideObservation, trendObservation);
        if (!TryReadContract(sideObservation, out var source, out var eventIdentity,
                out var view, out var boundary, out var resolution, out var sides, out var initialTrends, out _) ||
            !TryReadContract(trendObservation, out var otherSource, out var otherEvent,
                out var otherView, out var otherBoundary, out var otherResolution,
                out var otherSides, out var trends, out _) ||
            !ReferenceEquals(source, otherSource) || !ReferenceEquals(eventIdentity, otherEvent) ||
            !ReferenceEquals(view, otherView) || !ReferenceEquals(boundary, otherBoundary) ||
            !ReferenceEquals(resolution, otherResolution) ||
            !IntersectClosed(sides, otherSides, out var acceptedSides) ||
            !IntersectClosed(initialTrends, trends, out var acceptedTrends)) return false;
        combined = Create(source, eventIdentity, view, boundary, resolution,
            acceptedSides, acceptedTrends, combined);
        return true;
    }

    private static bool IntersectClosed(Relation first, Relation second, out Relation intersection)
    {
        intersection = StructuralConstraint.Closed();
        if (!StructuralConstraint.TryReadScope(first, out var left, out var leftSeam, out var leftTail) ||
            !StructuralConstraint.TryReadScope(second, out var right, out var rightSeam, out var rightTail) ||
            !leftSeam.Equals(leftTail) || !rightSeam.Equals(rightTail)) return false;
        var shared = left.Where(value => right.Any(other => FiniteLaw.SameParticipant(value, other))).ToArray();
        intersection = StructuralConstraint.Closed(shared);
        return shared.Length > 0;
    }
}
