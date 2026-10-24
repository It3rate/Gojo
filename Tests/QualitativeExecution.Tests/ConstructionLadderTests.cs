using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ConstructionLadderTests
{
    [Fact]
    public void RestoredReflectionConsumesEarlierSiteAndMovesOppositelyWithoutMergingIdentity()
    {
        var source = Fixture(); source["repeats"] = new JsonArray();
        source["sites"]![6]!["x"] = JsonNode.Parse("""{"origin":"b","from":"p","to":"b","numerator":1,"denominator":1}""");
        source["sites"]![7]!["x"] = JsonNode.Parse("""{"origin":"b","from":"q","to":"b","numerator":1,"denominator":1}""");
        var program = Restore(Import(source));
        Assert.True(ConstructionLadder.TryExecute(program, out var before));
        Assert.Equal(175, before.Sites[6].Position.X);
        Assert.Contains(4, before.Sites[6].Dependencies);
        Assert.Contains(1, before.Sites[6].Dependencies);
        Assert.NotSame(before.Sites[4].Event, before.Sites[6].Event);
        before.Parameters[0].Set(40);
        Assert.True(ConstructionLadder.TryExecute(program, out var after));
        Assert.Equal(160, after.Sites[6].Position.X);
        Assert.Equal(200, after.Sites[4].Position.X + after.Sites[6].Position.X);
        Assert.Equal(before.Sites[6].Position.Y, after.Sites[6].Position.Y);
        Assert.Equal(175, before.Sites[6].Position.X);
    }

    [Fact]
    public void RestoredProgramRecomputesDependentCoordinatesWhenLiveParameterReceivesDelta()
    {
        var program = Restore(Import(Fixture())); Assert.True(ConstructionLadder.TryExecute(program, out var first));
        Assert.Equal(25, first.Sites[4].Position.X); Assert.Equal(65, first.Sites[6].Position.X);
        first.Parameters[0].Set(40); Assert.Equal(0, first.Parameters[0].Tension);
        Assert.True(ConstructionLadder.TryExecute(program, out var second));
        Assert.Equal(40, second.Sites[4].Position.X); Assert.Equal(80, second.Sites[6].Position.X);
        Assert.Equal(25, first.Sites[4].Position.X); // Previous calculated geometry remains a prior observation.
        Assert.Same(first.Parameters[0], second.Parameters[0]);
        Assert.Equal(first.Sites[4].Event, second.Sites[4].Event);
        Assert.True(ReadWitness.TryRead(first.Sites[4].Witness, out _, out var coordinates, out _, out _, out _));
        Assert.True(((Relation)coordinates).TryReadOrderedGroup(out var coordinateWitnesses));
        Assert.True(ReadWitness.TryRead((Relation)coordinateWitnesses[0], out _, out _, out _, out _, out var provenance));
        Assert.True(((Relation)provenance).TryReadOrderedGroup(out var captured));
        Assert.Equal(25, Assert.IsType<CarrierLocation>(captured[3]).Ticks);
    }

    [Fact]
    public void RepeatedTranslationConsumesTemplateSitesAndKeepsSeparateNewIdentities()
    {
        Assert.True(ConstructionLadder.TryExecute(Restore(Import(Fixture())), out var read));
        Assert.Contains(4, read.Sites[6].Dependencies); Assert.Contains(5, read.Sites[7].Dependencies);
        Assert.NotSame(read.Sites[4].Event, read.Sites[6].Event);
        Assert.Equal(read.Edges[1].Controls.Length, read.Edges[2].Controls.Length);
        Assert.True(RepeatSurface.TryRead(Assert.Single(read.Repeats), out _, out var controller, out _, out _, out _, out var history));
        Assert.True(RepeatSurface.TryReadSpan(controller, out var begin, out var end)); Assert.Equal(2, end.Ticks - begin.Ticks);
        Assert.True(TimeStepSequence.TryRead(Assert.IsType<Relation>(history), out _, out _, out var steps)); Assert.Equal(2, steps.Length);
    }

    [Fact]
    public void CoarseRungAndRetiredGuideKeepTheSameCurveWhileRefinementCarriesActualSource()
    {
        Assert.True(ConstructionLadder.TryExecute(Restore(Import(Fixture())), out var read));
        Assert.Same(read.Rungs[0].Visible[0], read.Rungs[2].Guides[0]);
        Assert.True(StructuralLadderSurface.TryReadRung(read.Rungs[1].Surface, out var source, out var focus, out _, out _, out _, out _, out _, out _));
        Assert.True(StructuralLadderSurface.TryReadRung(read.Rungs[0].Surface, out _, out _, out var coarse, out _, out _, out _, out _, out _));
        Assert.Same(coarse, source); Assert.True(FocusSurface.TryRead(Assert.IsType<Relation>(focus), out var focusSource, out _, out _, out _, out _, out _, out _, out _));
        Assert.Same(coarse, focusSource);
    }

    [Fact]
    public void OutsideWindowIsFailedVisibleEvidenceAndIsNeverClamped()
    {
        var source = Fixture(); source["parameters"]![0]!["value"] = 90;
        Assert.True(ConstructionLadder.TryExecute(Import(source), out var read));
        Assert.Equal(130, read.Sites[6].Position.X); Assert.False(Assert.Single(read.Constraints).Satisfied);
        Assert.True(ReadWitness.TryRead(read.Constraints[0].Witness, out _, out _, out _, out var residue, out _));
        Assert.IsType<Relation>(residue);
    }

    [Fact]
    public void AffineDisplacementKeepsFractionalResidueInNativeWitness()
    {
        var source = Fixture(); source["sites"]![4]!["x"]!["numerator"] = 1; source["sites"]![4]!["x"]!["denominator"] = 3;
        Assert.True(ConstructionLadder.TryExecute(Restore(Import(source)), out var read)); Assert.Equal(33, read.Sites[4].Position.X);
        Assert.True(ReadWitness.TryRead(read.Sites[4].Witness, out _, out var coordinates, out _, out _, out _));
        Assert.True(((Relation)coordinates).TryReadOrderedGroup(out var witnesses));
        Assert.True(ReadWitness.TryRead((Relation)witnesses[0], out _, out _, out _, out var residue, out _));
        Assert.IsType<Relation>(residue); // 1/300 of the source parameter frame, not a silent integer zero.
    }

    [Fact]
    public void FutureRungReferenceIsRejectedEvenWhenSourceRowAppearsEarlierInArray()
    {
        var source = Fixture(); source["sites"]![0]!["at"] = 2;
        Assert.Throws<InvalidDataException>(() => Import(source));
    }

    [Fact]
    public void RungCannotPublishAFutureEdgeOrOmitItsIntroduction()
    {
        var source = Fixture(); source["rungs"]![0]!["visible"] = new JsonArray("copied");
        Assert.Throws<InvalidDataException>(() => Import(source));
        source = Fixture(); source["rungs"]![1]!["introduced"] = new JsonArray();
        Assert.Throws<InvalidDataException>(() => Import(source));
    }

    [Fact]
    public void EqualCoordinatesDoNotMergeAuthoredSiteIdentities()
    {
        var source = Fixture(); source["parameters"]![1]!["value"] = 0;
        Assert.True(ConstructionLadder.TryExecute(Restore(Import(source)), out var read));
        Assert.Equal(read.Sites[4].Position, read.Sites[6].Position); Assert.NotSame(read.Sites[4].Event, read.Sites[6].Event);
    }

    [Fact]
    public void NonconvexAndDegenerateBoundariesRemainOpen()
    {
        Assert.Null(ConstructionLadder.ConvexContains([new(0, 0), new(10, 0), new(5, 5), new(10, 10), new(0, 10)], [new(2, 2)]));
        Assert.Null(ConstructionLadder.ConvexContains([new(0, 0), new(0, 0), new(10, 10)], [new(2, 2)]));
        Assert.True(ConstructionLadder.ConvexContains([new(0, 0), new(10, 0), new(10, 10), new(0, 10)], [new(0, 5)]));
        Assert.Null(ConstructionLadder.ConvexContains([new(0, 10), new(6, -8), new(-10, 3), new(10, 3), new(-6, -8)], [new(0, 0)]));
    }

    [Fact]
    public void ALookalikeTranslatedDrawingWithoutTemplateOriginsIsNotARepeat()
    {
        var source = Fixture(); source["sites"]![6]!["x"] = 65; source["sites"]![6]!["y"] = 30;
        Assert.Throws<InvalidDataException>(() => Import(source));
    }

    [Fact]
    public void DroppingSourceStrokesWithoutSelectionIsRejected()
    {
        var source = Fixture(); source["rungs"]![1]!["selected"] = new JsonArray();
        Assert.Throws<InvalidDataException>(() => Import(source));
    }

    [Fact]
    public void CircularConstructionRetainsExactPartitionAndOpenSampledCalibration()
    {
        var source = Fixture(); source["circular"] = JsonNode.Parse("""[{"id":"radials","at":0,"center":"a","ring":["a","b","c","d"]}]""");
        Assert.True(ConstructionLadder.TryExecute(Restore(Import(source)), out var read));
        Assert.True(ReadWitness.TryRead(Assert.Single(read.Circular), out _, out var partitionWitness, out _, out var residue, out _));
        Assert.IsType<Placeholder>(residue);
        Assert.True(ReadWitness.TryRead((Relation)partitionWitness, out var selected, out _, out _, out _, out _));
        Assert.True(ContourConstruction.TryReadPartition((Relation)selected, out _));
    }

    private static Relation Import(JsonNode source)
    { using var document = JsonDocument.Parse(source.ToJsonString()); return ConstructionLadder.Import(document.RootElement); }
    private static Relation Restore(Relation program)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("test", program));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
    private static JsonNode Fixture() => JsonNode.Parse("""
    {"id":"window","viewBox":[0,0,150,120],"parameters":[{"id":"left","value":25},{"id":"spacing","value":40}],
     "sites":[
      {"id":"a","at":0,"x":0,"y":0},{"id":"b","at":0,"x":100,"y":0},
      {"id":"c","at":0,"x":100,"y":100},{"id":"d","at":0,"x":0,"y":100},
      {"id":"p","at":1,"x":{"from":"a","to":"b","numerator":"left","denominator":100},"y":30},
      {"id":"q","at":1,"x":{"from":"a","to":"b","numerator":"left","denominator":100},"y":70},
      {"id":"r","at":2,"x":{"origin":"p","from":"a","to":"b","numerator":"spacing","denominator":100},"y":{"from":"p","to":"p","numerator":0,"denominator":1}},
      {"id":"s","at":2,"x":{"origin":"q","from":"a","to":"b","numerator":"spacing","denominator":100},"y":{"from":"q","to":"q","numerator":0,"denominator":1}}],
     "edges":[{"id":"base","at":0,"controls":["a","b"]},{"id":"template","at":1,"controls":["p","q"]},{"id":"copied","at":2,"controls":["r","s"]}],
     "rungs":[
      {"id":"rough","visible":["base"],"guides":[],"selected":[],"introduced":["base"]},
      {"id":"window","visible":["template"],"guides":["base"],"selected":["base"],"introduced":["template"]},
      {"id":"repeat","visible":["template","copied"],"guides":["base"],"selected":[],"introduced":["copied"]}],
     "constraints":[{"id":"inside","at":2,"container":["a","b","c","d"],"edges":["template","copied"]}],
     "repeats":[{"id":"row","at":2,"source":["template"],"instances":[["template"],["copied"]]}],"circular":[]}
    """)!;
}
