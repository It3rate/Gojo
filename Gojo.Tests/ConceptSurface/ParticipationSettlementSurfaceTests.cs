using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Interpretation;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Tests.ConceptSurface;

public sealed class ParticipationSettlementSurfaceTests
{
    [Fact]
    public void SettlementSurfaceKeepsAllReadSlotsVisible()
    {
        var selected = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 1));
        var scope = Relation.Segment(new Carrier(), 0, 10);
        var boundary = Relation.OrderedGroup(new Carrier(), scope);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            scope);
        var partitions = Relation.OrderedGroup(new Carrier(), scope);
        var occupied = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 1));
        var support = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 2));
        var tension = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 3));
        var exclusive = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 4));
        var uncovered = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 5));
        var result = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 6));
        var residue = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 7));
        var provenance = Relation.OrderedGroup(new Carrier(), selected, basis);

        var settlement = ParticipationSettlementSurface.Create(
            new Carrier(),
            selected,
            scope,
            boundary,
            basis,
            partitions,
            occupied,
            support,
            tension,
            exclusive,
            uncovered,
            result,
            residue,
            provenance);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Equal(selected, read.SelectedParticipants);
        Assert.Equal(scope, read.Scope);
        Assert.Equal(boundary, read.BoundaryEvidence);
        Assert.Equal(basis, read.ParticipationBasis);
        Assert.Equal(partitions, read.Partitions);
        Assert.Equal(occupied, read.OccupiedLaneEvidence);
        Assert.Equal(support, read.Support);
        Assert.Equal(tension, read.Tension);
        Assert.Equal(exclusive, read.Exclusive);
        Assert.Equal(uncovered, read.Uncovered);
        Assert.Equal(result, read.ResultParticipation);
        Assert.Equal(residue, read.ResidueOrTension);
        Assert.Equal(provenance, read.Provenance);
    }

    [Fact]
    public void BareCoincidentPointSitesAreUncoveredUntilFlowOccupiesTheSite()
    {
        var siteCarrier = new Carrier();
        var tick = new TickRef(5);
        var firstSite = new Relation(new CarrierLocation(siteCarrier, tick), siteCarrier);
        var secondSite = new Relation(new CarrierLocation(siteCarrier, tick), siteCarrier);
        var selectedBareSites = Relation.OrderedGroup(new Carrier(), firstSite, secondSite);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            firstSite);

        var settlement = ParticipationSettlementSurface.CreateCellSettlement(
            new Carrier(),
            selectedBareSites,
            firstSite,
            firstSite,
            basis);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Support)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Tension)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Exclusive)));
        Assert.Equal([firstSite], Elements(Assert.IsType<Relation>(read.Uncovered)));
    }

    [Fact]
    public void OccupiedPointSiteReadsAsSupportAndExclusiveParticipation()
    {
        var site = Relation.Point(new Carrier(), 0);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            site);
        var value = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(new Carrier(), 1),
            BoundaryFlowLane.WholeForward,
            basis,
            Relation.Point(Carrier.Unknown, 0));

        var settlement = ParticipationSettlementSurface.CreateSiteOccupancy(
            new Carrier(),
            site,
            basis,
            value);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Equal([value], Elements(Assert.IsType<Relation>(read.Support)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Tension)));
        Assert.Equal([value], Elements(Assert.IsType<Relation>(read.Exclusive)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Uncovered)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.ResidueOrTension)));
    }

    [Fact]
    public void OpposedAreaLikeCellOccupancyStaysAsTensionInsteadOfDisappearing()
    {
        var x = new Carrier();
        var y = new Carrier();
        var cell = Relation.OrderedGroup(
            new Carrier(),
            Relation.Segment(x, 0, 5),
            Relation.Segment(y, 0, 5));
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            cell);
        var fill = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(new Carrier(), 1),
            BoundaryFlowLane.WholeForward,
            basis,
            Relation.Point(Carrier.Unknown, 0));
        var antiFill = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(new Carrier(), 1),
            BoundaryFlowLane.WholeInverse,
            basis,
            Relation.Point(Carrier.Unknown, 0));

        var settlement = ParticipationSettlementSurface.CreateCellSettlement(
            new Carrier(),
            Relation.OrderedGroup(new Carrier(), fill, antiFill),
            cell,
            cell,
            basis,
            fill,
            antiFill);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Equal([fill, antiFill], Elements(Assert.IsType<Relation>(read.Support)));
        Assert.Equal([fill, antiFill], Elements(Assert.IsType<Relation>(read.Tension)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Exclusive)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Uncovered)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.ResidueOrTension)));
    }

    [Fact]
    public void IncompatibleCellLaneRemainsAsResidue()
    {
        var cell = Relation.OrderedGroup(new Carrier(), Relation.Point(new Carrier(), 0));
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.Both,
            cell);
        var noFlowValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(new Carrier(), 1),
            BoundaryFlowLane.WholeNone,
            basis,
            Relation.Point(Carrier.Unknown, 0));

        var settlement = ParticipationSettlementSurface.CreateCellSettlement(
            new Carrier(),
            Relation.OrderedGroup(new Carrier(), noFlowValue),
            cell,
            cell,
            basis,
            noFlowValue);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Support)));
        Assert.Equal([cell], Elements(Assert.IsType<Relation>(read.Uncovered)));
        Assert.Equal([noFlowValue], Elements(Assert.IsType<Relation>(read.ResidueOrTension)));
    }

    [Fact]
    public void SegmentSupportSettlementWrapsExistingSpanParticipationReads()
    {
        var carrier = new Carrier();
        var scope = Relation.Segment(carrier, 0, 10);
        var forward = Relation.Segment(carrier, 0, 6);
        var reverse = Relation.Segment(carrier, 10, 4);

        var settlement = ParticipationSettlementSurface.CreateSegmentSupport(
            new Carrier(),
            carrier,
            scope,
            forward,
            reverse);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Equal(
            RelationReads.SupportWithinOn(carrier, scope, forward, reverse),
            Elements(Assert.IsType<Relation>(read.Support)));
        Assert.Equal(
            RelationReads.TensionWithinOn(carrier, scope, forward, reverse),
            Elements(Assert.IsType<Relation>(read.Tension)));
        Assert.Equal(
            RelationReads.ExclusiveWithinOn(carrier, scope, forward, reverse),
            Elements(Assert.IsType<Relation>(read.Exclusive)));
        Assert.Empty(Elements(Assert.IsType<Relation>(read.Uncovered)));

        var basis = Assert.IsType<Relation>(read.ParticipationBasis);
        Assert.True(BoundaryFlowLaneBasis.TryReadLegalLanes(
            basis,
            out var exposure,
            out var lanes));
        Assert.Equal(new BoundaryFlowExposure(BoundaryExposure.BoundedWhole, FlowExposure.Both), exposure);
        Assert.Equal("SF|SI", string.Join("|", lanes.Select(lane => lane.Label)));
    }

    [Fact]
    public void ParticipationSupportSettlementUsesTheSameSegmentWitness()
    {
        var carrier = new Carrier();
        var scope = Relation.Segment(carrier, 0, 10);
        var left = Relation.Segment(carrier, 0, 4);
        var right = Relation.Segment(carrier, 6, 10);

        var settlement = ParticipationSupportReads.SettlementOn(carrier, scope, left, right);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.Equal(
            ParticipationSupportReads.SupportOn(carrier, left, right),
            Elements(Assert.IsType<Relation>(read.ResultParticipation)));
        Assert.Equal(
            ParticipationSupportReads.UncoveredWithinOn(carrier, scope, left, right),
            Elements(Assert.IsType<Relation>(read.Uncovered)));
    }

    [Fact]
    public void SettlementCanProjectToCommonReadWitness()
    {
        var carrier = new Carrier();
        var scope = Relation.Segment(carrier, 0, 3);
        var covered = Relation.Segment(carrier, 0, 3);
        var settlement = ParticipationSettlementSurface.CreateSegmentSupport(
            new Carrier(),
            carrier,
            scope,
            covered);

        Assert.True(ParticipationSettlementSurface.TryRead(settlement, out var read));
        Assert.True(ParticipationSettlementSurface.TryReadWitness(settlement, new Carrier(), out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var readSurface,
            out var result,
            out var residue,
            out var provenance));
        Assert.Equal(read.SelectedParticipants, selected);
        Assert.Equal(read.ParticipationBasis, readSurface);
        Assert.Equal(read.ResultParticipation, result);
        Assert.Equal(read.ResidueOrTension, residue);
        Assert.Equal(read.Provenance, provenance);
    }

    private static IElement[] Elements(Relation group)
    {
        var holder = Assert.IsType<Carrier>(group.Right);
        Assert.True(group.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }
}
