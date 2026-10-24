using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QuickDrawGestaltEvidenceTests
{
    [Fact]
    public void ManyToManyPassesRetainActualSourceIntervalsAndReplayWithoutSourceMetadata()
    {
        var read = Read(Restore(Build()));
        Assert.Equal(2, read.Acquisition.StrokeCount); Assert.Equal(4, read.Acquisition.Samples.Count);
        Assert.Equal(2, read.Intervals.Length); Assert.Equal(3, read.Passes.Length);
        Assert.Equal(2, Assert.Single(read.Passes[1].Links).Source.Length);
        Assert.Single(read.Passes[1].Links[0].Target);
        Assert.Equal(2, Assert.Single(read.Passes[2].Links).Target.Length);
        foreach (var link in read.Passes.SelectMany(p => p.Links))
        {
            Assert.True(ReadWitness.TryRead(link.Witness, out var selected, out _, out var target, out var residue, out _));
            Assert.Same(selected, residue);
            Assert.Equal(link.Source.Select(i => i.Selection), Entries(selected));
            Assert.Equal(link.Target.Select(e => e.Surface), Entries(target));
        }
        var events = read.Acquisition.Samples.Select(s => s.Event).ToArray();
        Assert.All(read.Construction.Sites, site => Assert.DoesNotContain(events, e => ReferenceEquals(e, site.Event)));
        Assert.True(ConstructionLadder.TryExecute(Restore(read.Program), out var standalone));
        Assert.Equal(read.Construction.Sites.Select(s => s.Position), standalone.Sites.Select(s => s.Position));
    }

    [Fact]
    public void UnmatchedIntervalsAndActiveResidueGuidesAreDifferentValidReads()
    {
        var unmatched = Read(Restore(Build(unmatched: true)));
        Assert.Single(unmatched.Passes[1].Unmatched); Assert.Empty(unmatched.Passes[1].Residue);
        var residue = Read(Restore(Build(activeResidue: true)));
        Assert.Empty(residue.Passes[1].Unmatched); Assert.Single(residue.Passes[1].Residue);
        Assert.Same(residue.Construction.Edges[2], residue.Passes[1].Residue[0]);
    }

    [Fact]
    public void HistoricalGuidesCannotSilentlySupplyActiveSourceCoverage()
    { Assert.Throws<InvalidDataException>(() => Build(historicalAsActive: true)); }

    [Fact]
    public void MatchedSourceCannotAlsoBeDeclaredUnmatched()
    { Assert.Throws<InvalidDataException>(() => Build(duplicateUnmatched: true)); }

    [Fact]
    public void NewCoordinatesMustConsumeEarlierConstruction()
    { Assert.Throws<InvalidDataException>(() => Build(literalLater: true)); }

    [Fact]
    public void LookalikeIntervalCannotBorrowAnActualAcquiredSelection()
    {
        var root = Entries(Build()); var passes = Entries(root[4]); var pass = Entries(passes[1]);
        var output = Entries(pass[2]); var links = Entries(output[0]); var link = Entries(links[0]);
        var selected = Entries(link[0]); var actual = (Relation)selected[0]; selected[0] = new Relation(actual.Left, actual.Right);
        link[0] = G(selected); link[3] = link[0]; links[0] = G(link); output[0] = G(links); pass[2] = G(output); passes[1] = G(pass); root[4] = G(passes);
        Assert.False(QuickDrawGestaltEvidence.TryRead(G(root), out _));
    }

    [Fact]
    public void LookalikeTargetEdgeCannotReplaceTheActualProgramRow()
    {
        var root = Entries(Build()); var passes = Entries(root[4]); var pass = Entries(passes[1]);
        var output = Entries(pass[2]); var links = Entries(output[0]); var link = Entries(links[0]);
        var target = Entries(link[2]); var actual = (Relation)target[0]; target[0] = new Relation(actual.Left, actual.Right);
        link[2] = G(target); links[0] = G(link); output[0] = G(links); pass[2] = G(output); passes[1] = G(pass); root[4] = G(passes);
        Assert.False(QuickDrawGestaltEvidence.TryRead(G(root), out _));
    }

    [Fact]
    public void SourceIntervalsRemainResidueEvenWhenSimplifiedGeometryLooksComplete()
    {
        var root = Entries(Build()); var passes = Entries(root[4]); var pass = Entries(passes[1]);
        var output = Entries(pass[2]); var links = Entries(output[0]); var link = Entries(links[0]);
        link[3] = G(); links[0] = G(link); output[0] = G(links); pass[2] = G(output); passes[1] = G(pass); root[4] = G(passes);
        Assert.False(QuickDrawGestaltEvidence.TryRead(G(root), out _));
    }

    private static Relation Build(bool unmatched = false, bool activeResidue = false, bool historicalAsActive = false, bool duplicateUnmatched = false, bool literalLater = false)
    {
        object Coordinate(string origin, string from, string to, int numerator = 0, int denominator = 1) => new { origin, from, to, numerator, denominator };
        object Link(string[] spans, params string[] edges) => new { spanIds = spans, edgeIds = edges };
        object Pass(int rung, object[] links, string[]? missing = null, string[]? residue = null) => new {
            rung, links, unmatchedSpanIds = missing ?? [], inferredEdgeIds = Array.Empty<string>(), residueEdgeIds = residue ?? [] };
        var middleLinks = unmatched || activeResidue || historicalAsActive ? new[] { Link(["s0"], "merged") } : new[] { Link(["s0", "s1"], "merged") };
        if (activeResidue || historicalAsActive) middleLinks = middleLinks.Append(Link(["s1"], "e1")).ToArray();
        var program = new {
            id = "fixture", viewBox = new[] { 0, 0, 100, 100 }, parameters = Array.Empty<object>(),
            sites = new object[] {
                new { id = "page-origin", at = 0, x = 0, y = 0 }, new { id = "page-extent", at = 0, x = 100, y = 100 },
                new { id = "a", at = 0, x = 0, y = 0 }, new { id = "b", at = 0, x = 10, y = 0 },
                new { id = "c", at = 0, x = 12, y = 0 }, new { id = "d", at = 0, x = 20, y = 10 },
                new { id = "m0", at = 1, x = Coordinate("a", "a", "d"), y = Coordinate("a", "a", "d") },
                new { id = "m1", at = 1, x = literalLater ? (object)20 : Coordinate("a", "a", "d", 1), y = Coordinate("a", "a", "d", 1) },
                new { id = "n0", at = 2, x = Coordinate("m0", "m0", "m1"), y = Coordinate("m0", "m0", "m1") },
                new { id = "n1", at = 2, x = Coordinate("m0", "m0", "m1", 1, 2), y = Coordinate("m0", "m0", "m1", 1, 2) },
                new { id = "n2", at = 2, x = Coordinate("m1", "m0", "m1"), y = Coordinate("m1", "m0", "m1") } },
            edges = new[] {
                new { id = "envelope", at = 0, controls = new[] { "page-origin", "page-extent" } },
                new { id = "e0", at = 0, controls = new[] { "a", "b" } }, new { id = "e1", at = 0, controls = new[] { "c", "d" } },
                new { id = "merged", at = 1, controls = new[] { "m0", "m1" } },
                new { id = "split0", at = 2, controls = new[] { "n0", "n1" } }, new { id = "split1", at = 2, controls = new[] { "n1", "n2" } } },
            rungs = new[] {
                new { id = "baseline", visible = new[] { "e0", "e1" }, guides = new[] { "envelope" }, selected = Array.Empty<string>(), introduced = new[] { "envelope", "e0", "e1" } },
                new { id = "merge", visible = new[] { "merged" }, guides = new[] { "envelope", "e0", "e1" }, selected = new[] { "e0", "e1" }, introduced = new[] { "merged" } },
                new { id = "split", visible = new[] { "split0", "split1" }, guides = new[] { "envelope", "e0", "e1", "merged" }, selected = new[] { "merged" }, introduced = new[] { "split0", "split1" } } },
            constraints = Array.Empty<object>(), repeats = Array.Empty<object>(), circular = Array.Empty<object>(),
            sourceMapping = new { sourceId = "fixture", envelopeSiteIds = new[] { "page-origin", "page-extent" }, passes = new[] {
                Pass(0, [Link(["s0"], "e0"), Link(["s1"], "e1")]),
                Pass(1, middleLinks, unmatched || duplicateUnmatched ? ["s1"] : [], activeResidue ? ["e1"] : []),
                Pass(2, [Link(["s0", "s1"], "split0", "split1")]) } }
        };
        using var packet = JsonDocument.Parse(JsonSerializer.Serialize(new {
            source = new { key_id = "fixture", drawing = new[] { new[] { new[] { 0, 10 }, new[] { 0, 0 }, new[] { 3, 2 } }, new[] { new[] { 12, 20 }, new[] { 0, 10 }, new[] { 1, 0 } } } },
            observation = new { sourceId = "fixture", spans = new[] { new { id = "s0", strokeIndex = 0, startIndex = 0, endIndex = 1 }, new { id = "s1", strokeIndex = 1, startIndex = 0, endIndex = 1 } } }, program }));
        return QuickDrawGestaltEvidence.Import(packet.RootElement);
    }
    private static QuickDrawGestaltEvidence.Read Read(Relation surface) { Assert.True(QuickDrawGestaltEvidence.TryRead(surface, out var read)); return read; }
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored)); return Assert.IsType<Relation>(restored.Root);
    }
    private static IElement[] Entries(IElement element) { Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var fields)); return fields; }
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
}
