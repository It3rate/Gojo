using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class BasisReadSurfaceTests
{
    [Fact]
    public void BasisReadKeepsAxisFaceLaneMetricResidueAndProvenanceVisible()
    {
        var selected = new Carrier();
        var axis = Relation.Segment(selected, 0, 10);
        var laneBasis = Relation.OrderedGroup(new Carrier(), axis);
        var metric = Relation.Point(selected, 4);
        var residue = Relation.Point(Carrier.Unknown, 0);
        var provenance = Relation.OrderedGroup(new Carrier(), axis, laneBasis);

        var read = BasisReadSurface.Create(
            new Carrier(),
            selected,
            axis,
            laneBasis,
            metric,
            residue,
            provenance);

        Assert.True(BasisReadSurface.TryRead(
            read,
            out var readSelected,
            out var readAxis,
            out var readLaneBasis,
            out var readMetric,
            out var readResidue,
            out var readProvenance));
        Assert.Same(selected, Assert.IsType<Carrier>(readSelected));
        Assert.Equal(axis, readAxis);
        Assert.Equal(laneBasis, readLaneBasis);
        Assert.Equal(metric, readMetric);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);
    }

    [Fact]
    public void BasisReadProjectsToCommonReadWitness()
    {
        var selected = new Carrier();
        var axis = Relation.Segment(selected, 0, 10);
        var laneBasis = Relation.OrderedGroup(new Carrier(), axis);
        var residue = Relation.Point(Carrier.Unknown, 0);
        var provenance = Relation.OrderedGroup(new Carrier(), axis, laneBasis);
        var read = BasisReadSurface.Create(
            new Carrier(),
            selected,
            axis,
            laneBasis,
            Relation.Point(selected, 4),
            residue,
            provenance);

        Assert.True(BasisReadSurface.TryReadWitness(read, new Carrier(), out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var witnessedSelected,
            out var readSurface,
            out var result,
            out var residueOrTension,
            out var witnessedProvenance));
        Assert.Same(selected, Assert.IsType<Carrier>(witnessedSelected));
        Assert.Equal(axis, readSurface);
        Assert.Equal(laneBasis, result);
        Assert.Equal(residue, residueOrTension);
        Assert.Equal(provenance, witnessedProvenance);
    }
}
