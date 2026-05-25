using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class RefinementReadTests
{
    [Fact]
    public void RefinementReadReadsParameterBindingWitnessResidueAndTimeline()
    {
        var parameter = Relation.Segment(new Carrier(), 0, 10);
        var binding = new Relation(parameter, Relation.Point(new Carrier(), 4));
        var readSurface = Relation.Segment(new Carrier(), 0, 1);
        var result = Relation.Point(new Carrier(), 7);
        var residue = Relation.Point(new Carrier(), 1);
        var witness = ReadWitness.Create(
            new Carrier(),
            binding,
            readSurface,
            result,
            residue);
        var time = new Carrier();
        var step = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            parameter,
            binding,
            result,
            residue);
        var timeline = TimeStepSequence.Create(new Carrier(), time, parameter, step);

        var refinement = RefinementRead.Create(
            new Carrier(),
            parameter,
            binding,
            witness,
            residue,
            timeline);

        Assert.True(RefinementRead.TryRead(
            refinement,
            out var readParameter,
            out var readBinding,
            out var readWitness,
            out var readResidue,
            out var readTimeline));
        Assert.Equal(parameter, readParameter);
        Assert.Equal(binding, readBinding);
        Assert.Equal(witness, readWitness);
        Assert.Equal(residue, readResidue);
        Assert.Equal(timeline, readTimeline);
        Assert.True(RefinementRead.TryReadWitness(refinement, out var embeddedWitness));
        Assert.Equal(witness, embeddedWitness);
        Assert.True(RefinementRead.TryReadTimeline(refinement, out var embeddedTimeline));
        Assert.Equal(timeline, embeddedTimeline);
    }
}
