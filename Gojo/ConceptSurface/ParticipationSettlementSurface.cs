using Gojo.Core;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Relation-shaped witness for support, overlap, cutout, absence, and other
/// boolean-style reads over occupied participation. The helper creates and
/// reads ordinary Gojo structure; it does not own the operation meaning.
/// </summary>
public static class ParticipationSettlementSurface
{
    /// <summary>
    /// Creates a participation-settlement read:
    /// selected participants, scope, boundary evidence, participation basis,
    /// partitions, occupied lanes, support, tension, exclusive, uncovered,
    /// result participation, residue/tension, and provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement selectedParticipants,
        IElement scope,
        IElement boundaryEvidence,
        IElement participationBasis,
        IElement partitions,
        IElement occupiedLaneEvidence,
        IElement support,
        IElement tension,
        IElement exclusive,
        IElement uncovered,
        IElement resultParticipation,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            selectedParticipants ?? ConceptSurface.UnknownPoint(),
            scope ?? ConceptSurface.UnknownPoint(),
            boundaryEvidence ?? ConceptSurface.UnknownPoint(),
            participationBasis ?? ConceptSurface.UnknownPoint(),
            partitions ?? ConceptSurface.UnknownPoint(),
            occupiedLaneEvidence ?? ConceptSurface.UnknownPoint(),
            support ?? ConceptSurface.UnknownPoint(),
            tension ?? ConceptSurface.UnknownPoint(),
            exclusive ?? ConceptSurface.UnknownPoint(),
            uncovered ?? ConceptSurface.UnknownPoint(),
            resultParticipation ?? ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint(),
            provenance ?? ConceptSurface.UnknownPoint());
    }

    /// <summary>
    /// Builds a single-cell settlement. This is the shared low-resolution shape
    /// for grade-0 site occupancy and higher-grade cell/fragment occupancy.
    /// </summary>
    public static Relation CreateCellSettlement(
        Carrier root,
        IElement selectedParticipants,
        IElement scope,
        IElement boundaryEvidence,
        Relation participationBasis,
        params Relation[] occupiedValues)
    {
        var values = occupiedValues ?? [];
        var basis = participationBasis ?? ConceptSurface.UnknownPoint();
        var legal = new List<Relation>();
        var residue = new List<IElement>();

        foreach (var value in values)
        {
            if (participationBasis is not null &&
                BoundaryFlowValueSurface.TryReadValue(value, participationBasis, out var read) &&
                read.IsLegal)
            {
                legal.Add(value);
            }
            else
            {
                residue.Add(value);
            }
        }

        var selected = selectedParticipants ?? Group(root, values);
        var cell = boundaryEvidence ?? scope ?? ConceptSurface.UnknownPoint();
        var support = Group(root, legal);
        var tension = legal.Count > 1 ? Group(root, legal) : EmptyGroup(root);
        var exclusive = legal.Count == 1 ? Group(root, legal) : EmptyGroup(root);
        var uncovered = legal.Count == 0 ? Group(root, cell) : EmptyGroup(root);
        var residueGroup = residue.Count == 0 ? EmptyGroup(root) : Group(root, residue);

        return Create(
            ConceptSurface.RootOrNew(root),
            selected,
            scope ?? cell,
            cell,
            basis,
            Group(root, cell),
            Group(root, values),
            support,
            tension,
            exclusive,
            uncovered,
            support,
            residueGroup,
            selected);
    }

    /// <summary>
    /// Convenience wrapper for grade-0 point/site occupancy. Bare sites do not
    /// participate unless occupied flow/value surfaces are supplied.
    /// </summary>
    public static Relation CreateSiteOccupancy(
        Carrier root,
        Relation site,
        Relation participationBasis,
        params Relation[] occupiedValues) =>
        CreateCellSettlement(
            ConceptSurface.RootOrNew(root),
            Group(root, occupiedValues ?? []),
            site ?? ConceptSurface.UnknownPoint(),
            site ?? ConceptSurface.UnknownPoint(),
            participationBasis,
            occupiedValues ?? []);

    /// <summary>
    /// Creates a scoped grade-1 support settlement using the existing core
    /// relation span reads as partition and projection evidence.
    /// </summary>
    public static Relation CreateSegmentSupportWithBasis(
        Carrier root,
        Carrier reader,
        Relation scope,
        Relation participationBasis,
        params Relation[] relations)
    {
        var selectedRelations = relations ?? [];
        var basis = participationBasis ?? SegmentSupportBasis(root, reader, scope);
        var supportStates = RelationReads.SupportStatesWithinOn(reader, scope, selectedRelations);
        var uncoveredSpans = RelationReads.UncoveredSpansOn(reader, scope, selectedRelations);
        var support = RelationReads.SupportWithinOn(reader, scope, selectedRelations);
        var tension = RelationReads.TensionWithinOn(reader, scope, selectedRelations);
        var exclusive = RelationReads.ExclusiveWithinOn(reader, scope, selectedRelations);

        return Create(
            ConceptSurface.RootOrNew(root),
            Group(root, selectedRelations),
            scope ?? ConceptSurface.UnknownPoint(),
            BoundaryEvidenceOn(root, reader, scope, selectedRelations),
            basis,
            Group(root, supportStates.Cast<IElement>().Concat(uncoveredSpans)),
            Group(root, supportStates),
            Group(root, support),
            Group(root, tension),
            Group(root, exclusive),
            Group(root, uncoveredSpans),
            Group(root, support),
            EmptyGroup(root),
            Group(root, selectedRelations));
    }

    public static Relation CreateSegmentSupport(
        Carrier root,
        Carrier reader,
        Relation scope,
        params Relation[] relations) =>
        CreateSegmentSupportWithBasis(
            ConceptSurface.RootOrNew(root),
            reader,
            scope,
            SegmentSupportBasis(root, reader, scope),
            relations ?? []);

    public static bool TryRead(
        Relation settlement,
        out ParticipationSettlementRead read)
    {
        if (ConceptSurfaceReader.TryReadRootElements(settlement, out var elements) &&
            elements.Length >= 13)
        {
            read = new ParticipationSettlementRead(
                elements[0],
                elements[1],
                elements[2],
                elements[3],
                elements[4],
                elements[5],
                elements[6],
                elements[7],
                elements[8],
                elements[9],
                elements[10],
                elements[11],
                elements[12]);
            return true;
        }

        read = default;
        return false;
    }

    /// <summary>
    /// Projects a participation settlement into the common read-witness shape.
    /// </summary>
    public static bool TryReadWitness(
        Relation settlement,
        Carrier root,
        out Relation witness)
    {
        if (TryRead(settlement, out var read))
        {
            witness = ReadWitness.Create(
                ConceptSurface.RootOrNew(root),
                read.SelectedParticipants,
                read.ParticipationBasis,
                read.ResultParticipation,
                read.ResidueOrTension,
                read.Provenance);
            return true;
        }

        witness = null!;
        return false;
    }

    private static Relation SegmentSupportBasis(
        Carrier root,
        Carrier reader,
        IElement selected)
    {
        return BoundaryFlowLaneBasis.Create(
            ConceptSurface.RootOrNew(root),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            selected ?? reader ?? Carrier.Unknown);
    }

    private static Relation BoundaryEvidenceOn(
        Carrier root,
        Carrier reader,
        Relation? scope,
        Relation[] relations)
    {
        var boundaryPoints = RelationReads.BoundaryPointsOn(reader, relations ?? [])
            .Cast<IElement>()
            .ToArray();

        if (scope is not null)
        {
            var scopeBoundary = scope.BoundariesOn(reader);
            boundaryPoints = [
                scopeBoundary.LeftLocation,
                .. boundaryPoints,
                scopeBoundary.RightLocation
            ];
        }

        return Group(root, boundaryPoints);
    }

    private static Relation EmptyGroup(Carrier root) =>
        Group(root, []);

    private static Relation Group(Carrier root, params IElement[] elements) =>
        Group(root, elements.AsEnumerable());

    private static Relation Group(Carrier root, IEnumerable<IElement> elements) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            [.. (elements ?? []).Select(element => element ?? ConceptSurface.UnknownPoint())]);
}

/// <summary>
/// Compact read of a participation-settlement relation. The properties are
/// helper reads over the relation slots; the source meaning stays structural.
/// </summary>
public readonly record struct ParticipationSettlementRead(
    IElement SelectedParticipants,
    IElement Scope,
    IElement BoundaryEvidence,
    IElement ParticipationBasis,
    IElement Partitions,
    IElement OccupiedLaneEvidence,
    IElement Support,
    IElement Tension,
    IElement Exclusive,
    IElement Uncovered,
    IElement ResultParticipation,
    IElement ResidueOrTension,
    IElement Provenance);
