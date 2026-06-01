using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class FocusSurfaceTests
{
    [Fact]
    public void PartialSpanReadsWholeSelectedComplementAndResidue()
    {
        var edge = new Carrier();
        var whole = Relation.Segment(edge, 0, 12);
        var selected = Relation.Segment(edge, 8, 12);
        var complement = Relation.Segment(edge, 0, 8);
        var residue = new Placeholder();

        var partial = FocusSurface.PartialSpan(
            new Carrier(),
            whole,
            selected,
            complement,
            residue);

        Assert.True(FocusSurface.TryReadPartialSpan(
            partial,
            out var readWhole,
            out var readSelected,
            out var readComplement,
            out var readResidue));
        Assert.Equal(whole, readWhole);
        Assert.Equal(selected, readSelected);
        Assert.Equal(complement, readComplement);
        Assert.Equal(residue, readResidue);
    }

    [Fact]
    public void FocusSurfaceCanSelectSubspansAcrossACornerAndBindReplacement()
    {
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var arcCarrier = new Carrier();

        var horizontalEdge = Relation.Segment(horizontal, 0, 10);
        var verticalEdge = Relation.Segment(vertical, 0, 8);
        var source = FocusSurface.Frame(new Carrier(), horizontalEdge, verticalEdge);

        var cornerJoin = new Relation(
            Relation.Point(horizontal, 10),
            Relation.Point(vertical, 0));
        var horizontalSelected = FocusSurface.PartialSpan(
            new Carrier(),
            horizontalEdge,
            Relation.Segment(horizontal, 7, 10),
            Relation.Segment(horizontal, 0, 7),
            Relation.Point(Carrier.Unknown, 0));
        var verticalSelected = FocusSurface.PartialSpan(
            new Carrier(),
            verticalEdge,
            Relation.Segment(vertical, 0, 3),
            Relation.Segment(vertical, 3, 8),
            Relation.Point(Carrier.Unknown, 0));
        var participation = FocusSurface.Frame(
            new Carrier(),
            horizontalSelected,
            verticalSelected);
        var complement = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(horizontal, 0, 7),
            Relation.Segment(vertical, 3, 8));

        var roundedArc = Relation.Segment(arcCarrier, 0, 4);
        var replacementBinding = FocusSurface.Bind(participation, roundedArc);
        var replacement = FocusSurface.BindingFrame(new Carrier(), replacementBinding);
        var firstTangent = FocusSurface.Bind(
            Relation.Point(horizontal, 7),
            Relation.Point(arcCarrier, 0));
        var secondTangent = FocusSurface.Bind(
            Relation.Point(vertical, 3),
            Relation.Point(arcCarrier, 4));
        var correspondence = FocusSurface.BindingFrame(new Carrier(), firstTangent, secondTangent);
        var residue = new Placeholder();

        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            cornerJoin,
            participation,
            complement,
            replacement,
            correspondence,
            residue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(FocusSurface.TryRead(
            focus,
            out var readSource,
            out var readBasis,
            out var readParticipation,
            out var readComplement,
            out var readReplacement,
            out var readCorrespondence,
            out var readResidue,
            out _));
        Assert.Equal(source, readSource);
        Assert.Equal(cornerJoin, readBasis);
        Assert.Equal(participation, readParticipation);
        Assert.Equal(complement, readComplement);
        Assert.Equal(residue, readResidue);

        Assert.True(FocusSurface.TryReadBindingFrame(readReplacement, out var replacements));
        Assert.Equal([replacementBinding], replacements);
        Assert.True(FocusSurface.TryReadBindingFrame(readCorrespondence, out var correspondences));
        Assert.Equal([firstTangent, secondTangent], correspondences);
    }

    [Fact]
    public void FocusStepUsesTimeStepSequenceForOrderedPartialStates()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var boundary = new Carrier();
        var sharpRectangle = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(boundary, 0, 10),
            Relation.Segment(boundary, 10, 20),
            Relation.Segment(boundary, 20, 30),
            Relation.Segment(boundary, 30, 40));
        var cornerA = Relation.Point(boundary, 10);
        var cornerB = Relation.Point(boundary, 20);
        var cornerAFocus = FocusSurface.Create(
            new Carrier(),
            sharpRectangle,
            cornerA,
            Relation.Segment(boundary, 8, 12));
        var afterCornerA = FocusSurface.Frame(
            new Carrier(),
            sharpRectangle,
            Relation.Segment(new Carrier(), 0, 4));
        var cornerBFocus = FocusSurface.Create(
            new Carrier(),
            afterCornerA,
            cornerB,
            Relation.Segment(boundary, 18, 22));
        var afterCornerB = FocusSurface.Frame(
            new Carrier(),
            afterCornerA,
            Relation.Segment(new Carrier(), 0, 4));

        var first = FocusSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            sharpRectangle,
            cornerAFocus,
            afterCornerA,
            Relation.Point(Carrier.Unknown, 0));
        var second = FocusSurface.Step(
            new Carrier(),
            clock,
            1,
            2,
            afterCornerA,
            cornerBFocus,
            afterCornerB,
            Relation.Point(Carrier.Unknown, 0));
        var timeline = FocusSurface.Timeline(new Carrier(), clock, sharpRectangle, first, second);

        Assert.True(TimeStepSequence.TryRead(timeline, out var origin, out var initial, out var steps));
        Assert.Equal(new CarrierLocation(clock, 0), origin);
        Assert.Equal(sharpRectangle, initial);
        Assert.Equal([first, second], steps);
        Assert.True(TimeStepSequence.TryReadStep(
            first,
            out _,
            out _,
            out _,
            out var before,
            out var transition,
            out var after,
            out _));
        Assert.Equal(sharpRectangle, before);
        Assert.Equal(cornerAFocus, transition);
        Assert.Equal(afterCornerA, after);
    }

    [Fact]
    public void FocusSurfaceCanCarryTimelineAndProjectToReadWitness()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var source = Relation.Segment(new Carrier(), 0, 10);
        var basis = Relation.Point(new Carrier(), 4);
        var participation = Relation.Segment(new Carrier(), 3, 5);
        var residue = new Placeholder();
        var step = FocusSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            source,
            basis,
            participation,
            residue);
        var timeline = FocusSurface.Timeline(new Carrier(), clock, source, step);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            basis,
            participation,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            residue,
            timeline);

        Assert.True(FocusSurface.TryReadTimeline(focus, out var readTimeline));
        Assert.Equal(timeline, readTimeline);
        Assert.True(FocusSurface.TryReadWitness(focus, new Carrier(), out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var readSelected,
            out var readSurface,
            out var result,
            out var readResidue,
            out var provenance));
        Assert.Equal(source, readSelected);
        Assert.Equal(basis, readSurface);
        Assert.Equal(participation, result);
        Assert.Equal(residue, readResidue);
        Assert.Equal(timeline, provenance);
    }

    [Fact]
    public void RepeatTemplateCanUseFocusSurfaceAsSelectedStructure()
    {
        var input = new Placeholder();
        var output = new Placeholder();
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 2, 4);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            input,
            selected,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Bind(selected, output),
            FocusSurface.Bind(input, output),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var ports = RepeatSurface.Ports(new Carrier(), input, output);

        var template = RepeatSurface.Template(new Carrier(), ports, focus, ports);

        Assert.True(RepeatSurface.TryReadTemplate(
            template,
            out var readInputs,
            out var readSelected,
            out var readOutputs));
        Assert.Equal(ports, readInputs);
        Assert.Equal(focus, readSelected);
        Assert.Equal(ports, readOutputs);
    }
}
