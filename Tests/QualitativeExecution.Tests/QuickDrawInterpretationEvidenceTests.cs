using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QuickDrawInterpretationEvidenceTests
{
    [Fact]
    public void TransportRetainsActualAcquisitionToProposalRowsAndFullUnaccountedIntervals()
    {
        var surface = Build(); var restored = Restore(surface);
        Assert.True(QuickDrawInterpretationEvidence.TryRead(restored, out var read));
        Assert.Equal(2, read.Acquisition.StrokeCount); Assert.Equal(5, read.Acquisition.Samples.Count);
        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, read.Acquisition.Samples.Select(sample => (int)sample.TimeNumerator!));
        Assert.Equal(5, read.Acquisition.Samples.Select(sample => sample.Event).Distinct().Count());
        Assert.Equal(new BigInteger(12), read.Acquisition.Samples[3].XNumerator);
        Assert.Equal(new BigInteger(10), read.Acquisition.Samples[2].XNumerator); // Acquired pen-lift gap survives.
        Assert.NotSame(read.Spans[0].Edge.Controls[^1].Surface, read.Spans[1].Edge.Controls[0].Surface);
        Assert.Equal(read.Spans[0].Edge.Controls[^1].Position, read.Spans[1].Edge.Controls[0].Position);
        foreach (var span in read.Spans)
        {
            Assert.True(ReadWitness.TryRead(span.Witness, out var source, out var basis, out var target, out var residue, out _));
            Assert.Same(span.Selection, source); Assert.Same(source, residue); Assert.Same(read.Calibration, basis);
            Assert.Same(span.Edge.Surface, target);
            Assert.Same(read.Construction.Edges[span.Edge.Index].Surface, target);
        }
        Assert.Equal(3, Entries(read.Spans[0].Selection.Right).Length); // Bowed middle sample is not explained away.
        Assert.Equal(new BigInteger(2), read.Acquisition.Samples[1].YNumerator);
        Assert.True(ConstructionLadder.TryExecute(Restore(read.Program), out var targetOnly));
        Assert.Equal(read.Construction.Sites.Select(site => site.Position), targetOnly.Sites.Select(site => site.Position));
    }

    [Fact]
    public void EqualLookingSampleRowCannotStandInForTheActualAcquiredEvent()
    {
        var root = Build(); var fields = Entries(root); var witnesses = Entries(fields[3]);
        var original = (Relation)witnesses[0];
        ReadWitness.TryRead(original, out var selected, out var basis, out var target, out _, out var provenance);
        var selection = (Relation)selected; var samples = Entries(selection.Right); var middle = (Relation)samples[1];
        samples[1] = new Relation(middle.Left, middle.Right);
        var copiedSelection = new Relation(selection.Left, G(samples));
        witnesses[0] = ReadWitness.Create(new Carrier(), copiedSelection, basis, target, copiedSelection, provenance);
        fields[3] = G(witnesses);
        Assert.False(QuickDrawInterpretationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void EqualLookingTargetEdgeCannotBorrowTheRealEdgeCorrespondence()
    {
        var fields = Entries(Build()); var witnesses = Entries(fields[3]);
        ReadWitness.TryRead((Relation)witnesses[0], out var source, out var basis, out var target, out var residue, out var provenance);
        var edge = (Relation)target;
        witnesses[0] = ReadWitness.Create(new Carrier(), source, basis, new Relation(edge.Left, edge.Right), residue, provenance);
        fields[3] = G(witnesses);
        Assert.False(QuickDrawInterpretationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void MissingIntervalsAndReorderedStrokeEvidenceAreNotCompleteCoverage()
    {
        var fields = Entries(Build()); var witnesses = Entries(fields[3]);
        fields[3] = G(witnesses[1]);
        Assert.False(QuickDrawInterpretationEvidence.TryRead(G(fields), out _));
        fields[3] = G(witnesses.Reverse().ToArray());
        Assert.False(QuickDrawInterpretationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void DroppingReadResidueCannotAssertThatTheMeasuredTraceWasFullyExplained()
    {
        var fields = Entries(Build()); var witnesses = Entries(fields[3]);
        ReadWitness.TryRead((Relation)witnesses[0], out var source, out var basis, out var target, out _, out var provenance);
        witnesses[0] = ReadWitness.Create(new Carrier(), source, basis, target, G(), provenance);
        fields[3] = G(witnesses);
        Assert.False(QuickDrawInterpretationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void EqualCoordinatePenLiftDoesNotAuthorizeWeldingTargetEndpointIdentity()
    {
        using var document = JsonDocument.Parse(Recipe(weld: true));
        var acquisition = Acquire(); var program = ConstructionLadder.Import(document.RootElement);
        Assert.Throws<InvalidDataException>(() => QuickDrawInterpretationEvidence.Create(acquisition, program, [0, 1],
            [new(0, 0, 2, 1), new(1, 0, 1, 2)]));
    }

    [Fact]
    public void EnvelopeCalibrationRequiresDistinctSitesAndPositiveTwoDimensionalExtent()
    {
        using var document = JsonDocument.Parse(Recipe());
        var program = ConstructionLadder.Import(document.RootElement);
        foreach (var sites in new[] { new[] { 0, 0 }, new[] { 1, 0 } })
            Assert.Throws<InvalidDataException>(() => QuickDrawInterpretationEvidence.Create(Acquire(), program, sites,
                [new(0, 0, 2, 1), new(1, 0, 1, 2)]));
    }

    [Fact]
    public void OriginalSourceTextPreservesExactDecimalBeyondPythonFloatPrecision()
    {
        var source = "{\"key_id\":\"fixture\",\"drawing\":[[[0,5,10],[0,2,0],[5,4,3]],[[12.0000000000000001,15],[0,10],[2,1]]]}";
        using var recipe = JsonDocument.Parse(Recipe());
        var packet = JsonSerializer.Serialize(new { sourceOriginalJson = source,
            source = new { key_id = "fixture", drawing = Array.Empty<object>() },
            observation = new { sourceId = "fixture", spans = new[] {
                new { id = "a", strokeIndex = 0, startIndex = 0, endIndex = 2 },
                new { id = "b", strokeIndex = 1, startIndex = 0, endIndex = 1 } } },
            program = recipe.RootElement });
        using var document = JsonDocument.Parse(packet);
        var restored = Restore(QuickDrawInterpretationEvidence.Import(document.RootElement));
        Assert.True(QuickDrawInterpretationEvidence.TryRead(restored, out var read));
        Assert.Equal(BigInteger.Parse("120000000000000001"), read.Acquisition.Samples[3].XNumerator);
        Assert.Equal(BigInteger.Parse("10000000000000000"), read.Acquisition.Samples[3].XDenominator);
    }

    private static NativeAcquisition Acquire() => new(QuickDrawRecord.Parse(
        "{\"key_id\":\"fixture\",\"drawing\":[[[0,5,10],[0,2,0],[5,4,3]],[[12,15],[0,10],[2,1]]]}"));
    private static Relation Build()
    {
        using var document = JsonDocument.Parse(Recipe());
        return QuickDrawInterpretationEvidence.Create(Acquire(), ConstructionLadder.Import(document.RootElement), [0, 1],
            [new(0, 0, 2, 1), new(1, 0, 1, 2)]);
    }
    private static string Recipe(bool weld = false)
    {
        object Coordinate(string origin, int numerator) => new { origin, from = "envelope-a", to = "envelope-b", numerator, denominator = 100 };
        return JsonSerializer.Serialize(new {
            id = "fixture", family = "fixture", viewBox = new[] { 0, 0, 100, 100 }, parameters = Array.Empty<object>(),
            sites = new object[] {
                new { id = "envelope-a", at = 0, x = 0, y = 0 }, new { id = "envelope-b", at = 0, x = 100, y = 100 },
                new { id = "a", at = 2, x = Coordinate("envelope-a", 10), y = Coordinate("envelope-a", 10) },
                new { id = "b", at = 2, x = Coordinate("a", 80), y = Coordinate("a", 0) },
                new { id = "c", at = 3, x = Coordinate("b", 0), y = Coordinate("b", 0) },
                new { id = "d", at = 3, x = Coordinate("c", 0), y = Coordinate("c", 80) } },
            edges = new[] { new { id = "rough", at = 0, controls = new[] { "envelope-a", "envelope-b" } },
                new { id = "one", at = 2, controls = new[] { "a", "b" } },
                new { id = "two", at = 3, controls = new[] { weld ? "b" : "c", "d" } } },
            rungs = new[] {
                new { id = "rough", visible = new[] { "rough" }, guides = Array.Empty<string>(), selected = Array.Empty<string>(), introduced = new[] { "rough" } },
                new { id = "outline", visible = new[] { "rough" }, guides = Array.Empty<string>(), selected = Array.Empty<string>(), introduced = Array.Empty<string>() },
                new { id = "first", visible = new[] { "one" }, guides = new[] { "rough" }, selected = new[] { "rough" }, introduced = new[] { "one" } },
                new { id = "second", visible = new[] { "one", "two" }, guides = new[] { "rough" }, selected = Array.Empty<string>(), introduced = new[] { "two" } } },
            constraints = Array.Empty<object>(), repeats = Array.Empty<object>(), circular = Array.Empty<object>(),
            sourceMapping = new { sourceId = "fixture", envelopeSiteIds = new[] { "envelope-a", "envelope-b" }, spanMap = new { a = "one", b = "two" } }
        });
    }
    private static Relation Restore(Relation surface)
    {
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
    private static IElement[] Entries(IElement element)
    { Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var entries)); return entries; }
    private static Relation G(params IElement[] entries) => Relation.OrderedGroup(new Carrier(), entries);
}
