using Gojo.Core;
using GojoCAD.GCode;

namespace GojoCAD.Tests;

public sealed class GCodeParserTests
{
    [Fact]
    public void ParsesLinearMotionWithPositiveAndInverseAxisFlow()
    {
        var program = GCodeParser.Parse(
            """
            G21 G90
            G0 Z5
            G0 X0 Y0
            G1 Z-1
            G1 X10 Y0
            G1 X5 Y0
            """,
            new GCodeParseOptions(CoordinateTicksPerUnit: 10, ArcTicksPerTurn: 360));

        Assert.True(GCodeStructure.TryReadProgram(program, out var basis, out var elements));
        Assert.True(GCodeStructure.TryReadBasis(basis, out var basisRead));
        Assert.Equal(10, basisRead.XTicksPerUnit);
        var lines = ReadLineMotions(elements);

        Assert.Equal(2, lines.Count);
        var first = lines[0];
        var firstSegment = Assert.Single(GraphicReadGraph.From(first.Segment).ReadsOf<SharedFrameSegmentRead>());
        Assert.Equal(0, firstSegment.Start.Coordinates[0].Ticks);
        Assert.Equal(100, firstSegment.End.Coordinates[0].Ticks);
        Assert.True(GCodeStructure.TryReadAxisFlow(first.XFlow, out var xCarrier, out var xTicks, out var isInverse));
        Assert.True(xCarrier.SameIdentityAs(basisRead.XAxis));
        Assert.False(isInverse);
        Assert.Equal(100, xTicks);

        var second = lines[1];
        Assert.True(GCodeStructure.TryReadAxisFlow(second.XFlow, out xCarrier, out xTicks, out isInverse));
        Assert.True(xCarrier.SameIdentityAs(basisRead.XAxis));
        Assert.True(isInverse);
        Assert.Equal(50, xTicks);
    }

    [Fact]
    public void ParsesCounterClockwiseArcWithRadialAndCircumferenceEvidence()
    {
        var program = GCodeParser.Parse(
            """
            G21 G90
            G0 X10 Y0
            G1 Z-1
            G3 X0 Y10 I-10 J0
            """,
            new GCodeParseOptions(CoordinateTicksPerUnit: 10, ArcTicksPerTurn: 360));

        Assert.True(GCodeStructure.TryReadProgram(program, out var basis, out var elements));
        Assert.True(GCodeStructure.TryReadBasis(basis, out var basisRead));
        var arc = Assert.Single(ReadArcMotions(elements));
        Assert.Equal(GCodeStructure.CounterClockwiseArc, arc.Command.Ticks);
        Assert.False(arc.Command.Carrier.IsInverse);

        var arcRead = Assert.Single(GraphicReadGraph.From(arc.RenderedCircumference).ReadsOf<SharedFrameArcRead>());
        Assert.Equal(0, arcRead.Center.Coordinates[0].Ticks);
        Assert.Equal(0, arcRead.Center.Coordinates[1].Ticks);
        Assert.Equal(100, arcRead.Start.Coordinates[0].Ticks);
        Assert.Equal(0, arcRead.Start.Coordinates[1].Ticks);
        Assert.Equal(0, arcRead.End.Coordinates[0].Ticks);
        Assert.Equal(100, arcRead.End.Coordinates[1].Ticks);

        Assert.True(GCodeStructure.TryReadAxisFlow(arc.CircumferenceFlow, out _, out var arcTicks, out var isClockwiseFace));
        Assert.False(isClockwiseFace);
        Assert.Equal(90, arcTicks);
        Assert.True(GCodeStructure.TryReadAxisFlow(arc.XFlow, out var xCarrier, out var xTicks, out var xInverse));
        Assert.True(xCarrier.SameIdentityAs(basisRead.XAxis));
        Assert.True(xInverse);
        Assert.Equal(100, xTicks);
    }

    [Fact]
    public void ParsesFullCircleArcWithoutEndpointDelta()
    {
        var program = GCodeParser.Parse(
            """
            G21 G90
            G1 Z-1
            G2 X0 Y0 I10 J0
            """,
            new GCodeParseOptions(CoordinateTicksPerUnit: 10, ArcTicksPerTurn: 720));

        Assert.True(GCodeStructure.TryReadProgram(program, out _, out var elements));
        var arc = Assert.Single(ReadArcMotions(elements));
        var arcPieces = GraphicReadGraph.From(arc.RenderedCircumference).ReadsOf<SharedFrameArcRead>().ToArray();

        Assert.Equal(4, arcPieces.Length);
        Assert.True(GCodeStructure.TryReadAxisFlow(arc.CircumferenceFlow, out _, out var ticks, out var isClockwiseFace));
        Assert.True(isClockwiseFace);
        Assert.Equal(720, ticks);
    }

    [Fact]
    public void ToolChangeAndLiftedPenRemainInspectableWithoutDrawingStroke()
    {
        var program = GCodeParser.Parse(
            """
            G21 G90
            T2 M6
            G0 Z5
            G1 X10 Y0
            """,
            new GCodeParseOptions(CoordinateTicksPerUnit: 10, ArcTicksPerTurn: 360));

        Assert.True(GCodeStructure.TryReadProgram(program, out _, out var elements));
        var stroke = Assert.Single(elements.OfType<Relation>(), relation =>
            GCodeStructure.TryReadStrokeChange(relation, out _, out _, out _));
        Assert.True(GCodeStructure.TryReadStrokeChange(stroke, out _, out var before, out var after));
        Assert.Equal(0, before.Ticks);
        Assert.Equal(2, after.Ticks);
        Assert.Empty(ReadLineMotions(elements));
        Assert.Single(elements.OfType<Relation>(), relation =>
            GCodeStructure.TryReadTravelMotion(relation, out _, out _, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void CoordinateResolutionComesFromParsedBasis()
    {
        var coarse = GCodeParser.Parse(
            "G21 G90 G1 X1 Y2",
            new GCodeParseOptions(CoordinateTicksPerUnit: 10, ArcTicksPerTurn: 360));
        var refined = GCodeParser.Parse(
            "G21 G90 G1 X1 Y2",
            new GCodeParseOptions(CoordinateTicksPerUnit: 100, ArcTicksPerTurn: 360));

        var coarseLine = Assert.Single(ReadLineMotions(ReadProgramElements(coarse)));
        var refinedLine = Assert.Single(ReadLineMotions(ReadProgramElements(refined)));
        var coarseSegment = Assert.Single(GraphicReadGraph.From(coarseLine.Segment).ReadsOf<SharedFrameSegmentRead>());
        var refinedSegment = Assert.Single(GraphicReadGraph.From(refinedLine.Segment).ReadsOf<SharedFrameSegmentRead>());

        Assert.Equal(10, coarseSegment.End.Coordinates[0].Ticks);
        Assert.Equal(20, coarseSegment.End.Coordinates[1].Ticks);
        Assert.Equal(100, refinedSegment.End.Coordinates[0].Ticks);
        Assert.Equal(200, refinedSegment.End.Coordinates[1].Ticks);
    }

    private static IElement[] ReadProgramElements(Relation program)
    {
        Assert.True(GCodeStructure.TryReadProgram(program, out _, out var elements));
        return elements;
    }

    private static List<LineMotionRead> ReadLineMotions(IReadOnlyList<IElement> elements)
    {
        var lines = new List<LineMotionRead>();
        foreach (var relation in elements.OfType<Relation>())
        {
            if (GCodeStructure.TryReadLineMotion(
                    relation,
                    out var command,
                    out var pen,
                    out var tool,
                    out var feed,
                    out var segment,
                    out var xFlow,
                    out var yFlow))
            {
                lines.Add(new LineMotionRead(command, pen, tool, feed, segment, xFlow, yFlow));
            }
        }

        return lines;
    }

    private static List<ArcMotionRead> ReadArcMotions(IReadOnlyList<IElement> elements)
    {
        var arcs = new List<ArcMotionRead>();
        foreach (var relation in elements.OfType<Relation>())
        {
            if (GCodeStructure.TryReadArcMotion(
                    relation,
                    out var command,
                    out var pen,
                    out var tool,
                    out var feed,
                    out var renderedCircumference,
                    out var center,
                    out var start,
                    out var end,
                    out var startRadius,
                    out var endRadius,
                    out var circumferenceFlow,
                    out var xFlow,
                    out var yFlow))
            {
                arcs.Add(new ArcMotionRead(
                    command,
                    pen,
                    tool,
                    feed,
                    renderedCircumference,
                    center,
                    start,
                    end,
                    startRadius,
                    endRadius,
                    circumferenceFlow,
                    xFlow,
                    yFlow));
            }
        }

        return arcs;
    }

    private sealed record LineMotionRead(
        CarrierLocation Command,
        CarrierLocation Pen,
        CarrierLocation Tool,
        CarrierLocation Feed,
        Relation Segment,
        Relation XFlow,
        Relation YFlow);

    private sealed record ArcMotionRead(
        CarrierLocation Command,
        CarrierLocation Pen,
        CarrierLocation Tool,
        CarrierLocation Feed,
        IElement RenderedCircumference,
        Relation Center,
        Relation Start,
        Relation End,
        Relation StartRadius,
        Relation EndRadius,
        Relation CircumferenceFlow,
        Relation XFlow,
        Relation YFlow);
}
