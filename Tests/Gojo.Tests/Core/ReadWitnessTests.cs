using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class ReadWitnessTests
{
    [Fact]
    public void ReadWitnessKeepsSelectedReadResultResidueAndProvenanceVisible()
    {
        var selected = Relation.Point(new Carrier(), 2);
        var readSurface = Relation.Segment(new Carrier(), 0, 10);
        var result = Relation.Point(new Carrier(), 7);
        var residue = Relation.Point(new Carrier(), 1);
        var provenance = Relation.OrderedGroup(new Carrier(), selected, readSurface);

        var witness = ReadWitness.Create(
            new Carrier(),
            selected,
            readSurface,
            result,
            residue,
            provenance);

        Assert.True(ReadWitness.TryRead(
            witness,
            out var readSelected,
            out var readSurfaceRead,
            out var readResult,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(selected, readSelected);
        Assert.Equal(readSurface, readSurfaceRead);
        Assert.Equal(result, readResult);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);
    }

    [Fact]
    public void ReadWitnessCanCarryTimelineAsProvenance()
    {
        var time = new Carrier();
        var selected = Relation.Point(new Carrier(), 0);
        var step = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            selected,
            Relation.Segment(new Carrier(), 0, 1),
            Relation.Point(new Carrier(), 1),
            Relation.Point(Carrier.Unknown, 0));
        var timeline = TimeStepSequence.Create(new Carrier(), time, selected, step);
        var witness = ReadWitness.Create(
            new Carrier(),
            selected,
            step,
            selected,
            Relation.Point(Carrier.Unknown, 0),
            timeline);

        Assert.True(ReadWitness.TryReadTimeline(witness, out var readTimeline));
        Assert.Equal(timeline, readTimeline);
    }

    [Fact]
    public void ReadWitnessCanBeReadFromEmbeddedSlot()
    {
        var selected = Relation.Point(new Carrier(), 2);
        var witness = ReadWitness.Create(
            new Carrier(),
            selected,
            Relation.Segment(new Carrier(), 0, 10),
            Relation.Point(new Carrier(), 7),
            Relation.Point(Carrier.Unknown, 0),
            selected);
        var surface = Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(Carrier.Unknown, 0),
            witness);

        Assert.True(ReadWitness.TryReadEmbedded(surface, 1, out var embedded));
        Assert.Equal(witness, embedded);
        Assert.False(ReadWitness.TryReadEmbedded(surface, 0, out _));
    }
}
