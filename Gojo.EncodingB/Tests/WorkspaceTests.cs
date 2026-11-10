using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class WorkspaceTests
{
    private static readonly SiteGrammar Markers = SiteGrammar.Create("markers")
        .Plane("marker", "value", "no marker", "marker present")
        .Build();

    [Fact]
    public void SelectionMachinesAreSmallAndRecordWhatMadeThem()
    {
        var tile = new Tile("strip", Markers, 64);
        var all = SelectionScope.Sites(tile);
        Assert.Equal(0x5555555555555555UL, SelectionMachines.EverySecond(all).Members);
        var sparse = SelectionMachines.These(all, Bits.Parse("1101011"), "picked");
        Assert.Equal("1001001", SelectionMachines.EverySecond(sparse).Pattern(7));
        var within = SelectionMachines.Within(all, Bits.Of(10), 3, Adjacency.Chain(64));
        Assert.Equal(Bits.Low(7) << 7, within.Members);
        var (grown, steps) = SelectionMachines.Grow(SelectionScope.Sites(new Tile("ring", Markers, 8)), Bits.Of(0), Adjacency.Ring(8));
        Assert.Equal(Bits.Low(8), grown.Members);
        Assert.Equal(4, steps);
        Assert.Contains("every second", SelectionMachines.EverySecond(all).Machine);
    }

    [Fact]
    public void OnlyClosedSelectionsSupportExclusionAndOnlyWithinScope()
    {
        var tile = new Tile("t", Markers, 6);
        var scope = SelectionScope.Sites(tile) with { Members = Bits.Low(4) };
        var open = SelectionMachines.These(scope, Bits.Of(0), "these");
        var closed = SelectionMachines.Where(scope, Bits.Of(0), "marked");
        Assert.Equal(MembershipRead.NoClaim, open.Read(1));
        Assert.Equal(MembershipRead.ExcludedWithinScope, closed.Read(1));
        Assert.Equal(MembershipRead.NoClaim, closed.Read(5));
        Assert.Equal(MembershipRead.Selected, closed.Read(0));
    }

    // House test 1: sibling workspaces read one shared seam in opposite directions.
    [Fact]
    public void RoofAndBodyShareTheSeamWithoutCopyingOrReversingIt()
    {
        var house = new Tile("house", Markers, 6, ["left eave", "right eave", "apex", "right floor", "left floor", "landmark"]);
        var edges = house.AddRowSet("edges");
        var seam = edges.Add(0, 1);
        var roofEdges = new[] { seam, edges.Add(1, 2), edges.Add(2, 0) };
        var bodyEdges = new[] { seam, edges.Add(1, 3), edges.Add(3, 4), edges.Add(4, 0) };
        var rowScope = SelectionScope.Rows(house, "edges");
        var roofRows = SelectionMachines.These(rowScope, Bits.Of(roofEdges), "roof contour");
        var bodyRows = SelectionMachines.These(rowScope, Bits.Of(bodyEdges), "body contour");
        var roof = new TrialWorkspace("roof", house, SelectionMachines.SitesOf(roofRows));
        var body = new TrialWorkspace("body", house, SelectionMachines.SitesOf(bodyRows));
        roof.Retain(roofRows);
        body.Retain(bodyRows);

        Assert.Equal([0, 1], roof.Traverse("edges", seam, reversed: false)!);
        Assert.Equal([1, 0], body.Traverse("edges", seam, reversed: true)!);
        Assert.Equal([0, 1], edges.Row(seam));
        Assert.Null(roof.Traverse("edges", bodyEdges[1], reversed: false));

        edges.Insert(seam, 1, 5);
        Assert.Equal([0, 5, 1], roof.Traverse("edges", seam, reversed: false)!);
        Assert.Equal([1, 5, 0], body.Traverse("edges", seam, reversed: true)!);
    }

    private static bool Closed(IReadOnlyList<int[]> rows, int[] path) =>
        rows.Any(r => r.Length == 2 && r[0] == path[^1] && r[1] == path[0]);

    // House test 2: an introduced closure is marked introduced; the source stays open.
    [Fact]
    public void AnIntroducedClosureNeverBecomesSourceEvidence()
    {
        var sketch = new Tile("sketch", Markers, 4);
        sketch.AddRowSet("strokes").Add(0, 1, 2, 3);
        var ws = new TrialWorkspace("closer", sketch, SelectionMachines.All(SelectionScope.Sites(sketch)));
        int[] path = [0, 1, 2, 3];
        ws.IntroduceRow("strokes", [3, 0], "close a nearly closed stroke");
        Assert.True(Closed(ws.RowsOf("strokes"), path));
        Assert.False(Closed(sketch.Rows("strokes") is var rows ? Enumerable.Range(0, rows.Count).Select(rows.Row).ToArray() : [], path));

        var made = ws.Introduce(new Dictionary<string, bool> { ["marker"] = true });
        Assert.Equal(SiteProvenance.Introduced, ws.ProvenanceOf(made));
        Assert.False(ws.Observe("marker", made));
        Assert.False(Bits.Get(ws.Support("marker").Sites, made));
        Assert.True(ws.ReadBit("marker", made));
        Assert.Equal(4, sketch.SiteCount);
    }

    // House test 3: adoption is an explicit, validated edit with adoption provenance.
    [Fact]
    public void AdoptionIsValidatedAndLabelled()
    {
        var sketch = new Tile("sketch", Markers, 4);
        sketch.AddRowSet("strokes").Add(0, 1, 2, 3);
        var ws = new TrialWorkspace("closer", sketch, SelectionMachines.All(SelectionScope.Sites(sketch)));
        var closure = ws.IntroduceRow("strokes", [3, 0], "close a nearly closed stroke");

        sketch.AddRowSet("notes");
        var stale = ws.Adopt(closure, uses: 3);
        Assert.False(stale.Committed);
        Assert.Contains("stale", stale.Read);
        Assert.Contains(closure, ws.IntroducedRows);
        Assert.Equal(1, sketch.Rows("strokes").Count);

        ws.Capture();
        var adopted = ws.Adopt(closure, uses: 3);
        Assert.True(adopted.Committed);
        var strokes = sketch.Rows("strokes");
        Assert.Equal([3, 0], strokes.Row(1));
        Assert.Contains("introduced by rule 'close a nearly closed stroke'", strokes.Provenance(1));
        Assert.Contains("adopted after 3 uses", strokes.Provenance(1));
        Assert.DoesNotContain("observed", strokes.Provenance(1));
        Assert.Empty(ws.IntroducedRows);
    }

    // House test 4: four of six markers read as a rectangle without changing a marker.
    [Fact]
    public void SelectingFourOfSixMarkersReadsARectangle()
    {
        var contour = new Tile("contour", Markers, 6);
        contour.Seed("marker", "111111");
        var version = contour.Version;
        var ring = SelectionScope.Sites(contour);
        var rectangle = SelectionMachines.OmitOppositePair(ring, 1);
        Assert.Equal("101101", rectangle.Pattern(6));
        var gaps = rectangle.RingGaps(6);
        Assert.Equal([2, 1, 2, 1], gaps);
        Assert.True(gaps[0] == gaps[2] && gaps[1] == gaps[3]);

        var arbitrary = SelectionMachines.These(ring, Bits.Parse("100111"), "omit markers 1 and 2");
        var other = arbitrary.RingGaps(6);
        Assert.False(other[0] == other[2] && other[1] == other[3]);
        Assert.Equal(version, contour.Version);

        var diagonal = SelectionMachines.EverySecond(rectangle);
        Assert.Same(rectangle, diagonal.Parent);
        Assert.Equal("100100", diagonal.Pattern(6));
    }

    // House test 5: looking away does not stop flow; partial attention cannot commit material.
    [Fact]
    public void ObservationNeverStopsFlowAndCommitsNeedTheWholeAccountingScope()
    {
        Tile Make()
        {
            var tile = new Tile("valve", Passage.Grammar, 4, ["S", "C", "T", "elsewhere"]);
            tile.Seed("occupied", "1000");
            tile.Seed("open", "0100");
            return tile;
        }
        var rows = Passage.Rows("handoff", [[0, 1, 2]]);

        var source = Make();
        var looker = new PredictionWorkspace("looker", source, SelectionMachines.These(SelectionScope.Sites(source), Bits.Of(3), "elsewhere"));
        rows.Apply(source, rows.AllRows);
        Assert.Equal("0010", Bits.Format(source.Read("occupied"), 4));
        Assert.Null(looker.ReadBit("occupied", 2));

        source = Make();
        var partial = new TrialWorkspace("partial", source, SelectionMachines.These(SelectionScope.Sites(source), Bits.Parse("1100"), "S and C"));
        var refused = partial.CommitTransition(rows, rows.AllRows);
        Assert.False(refused.Committed);
        Assert.Equal(Bits.Of(2), refused.MissingSites);
        Assert.Equal("1000", Bits.Format(source.Read("occupied"), 4));

        var whole = new TrialWorkspace("whole", source, SelectionMachines.These(SelectionScope.Sites(source), Bits.Parse("1110"), "the passage"));
        source.Supply("open", 3, true);
        var stale = whole.CommitTransition(rows, rows.AllRows);
        Assert.False(stale.Committed);
        Assert.Contains("stale", stale.Read);
        whole.Capture();
        var done = whole.CommitTransition(rows, rows.AllRows);
        Assert.True(done.Committed);
        Assert.Equal("0010", Bits.Format(source.Read("occupied"), 4));
    }

    [Fact]
    public void LocalWorkCopiesOnWriteAndLeavesTheSourceAlone()
    {
        var source = new Tile("valve", Passage.Grammar, 3);
        source.Seed("occupied", "100");
        source.Seed("open", "010");
        var version = source.Version;
        var ws = new TrialWorkspace("trial", source, SelectionMachines.All(SelectionScope.Sites(source)));
        Assert.Equal(SiteProvenance.ReferencedLive, ws.ProvenanceOf(0));
        ws.ApplyLocal(Passage.Rows("handoff", [[0, 1, 2]]), 1);
        Assert.Equal(SiteProvenance.Diverged, ws.ProvenanceOf(0));
        Assert.Equal(SiteProvenance.Diverged, ws.ProvenanceOf(2));
        Assert.Equal(SiteProvenance.ReferencedLive, ws.ProvenanceOf(1));
        Assert.Equal(0b101UL, ws.K0 & ws.K1);
        Assert.True(ws.ReadBit("occupied", 2));
        Assert.False(source.Bit("occupied", 2));
        Assert.Equal(version, source.Version);
    }

    [Fact]
    public void LawsRefuseToReadUnselectedSites()
    {
        var source = new Tile("valve", Passage.Grammar, 3);
        var ws = new TrialWorkspace("partial", source, SelectionMachines.These(SelectionScope.Sites(source), 0b011, "S and C"));
        Assert.Throws<InvalidOperationException>(() => ws.ApplyLocal(Passage.Rows("handoff", [[0, 1, 2]]), 1));

        var sums = new Tile("sums", Odv.Grammar, 3);
        var reader = new TrialWorkspace("reader", sums, SelectionMachines.These(SelectionScope.Sites(sums), 0b011, "a and b"));
        var add = new LawRows("a+b=r", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[0, 1, 2]]);
        Assert.Throws<InvalidOperationException>(() => reader.Propagate([add]));
    }

    [Fact]
    public void APredictionWorkspaceForecastsAndIsResolvedButNeverCommits()
    {
        var source = new Tile("valve", Passage.Grammar, 3);
        source.Seed("occupied", "100");
        source.Seed("open", "010");
        var rows = Passage.Rows("handoff", [[0, 1, 2]]);
        var forecaster = new PredictionWorkspace("forecast", source, SelectionMachines.All(SelectionScope.Sites(source)));
        var forecast = forecaster.Forecast(rows, 1, "the gate stays open");
        Assert.Equal(Bits.Of(2), forecast.Predicted["occupied"]);
        Assert.Equal("100", Bits.Format(source.Read("occupied"), 3));

        source.Supply("open", 1, false);
        rows.Apply(source, rows.AllRows);
        var resolution = forecaster.Resolve(forecast);
        Assert.False(resolution.Matched);
        Assert.Equal(0b101UL, resolution.Mismatch["occupied"]);
        Assert.DoesNotContain(typeof(PredictionWorkspace).GetMethods(), m => m.Name.StartsWith("Commit") || m.Name == "Adopt");
    }
}
