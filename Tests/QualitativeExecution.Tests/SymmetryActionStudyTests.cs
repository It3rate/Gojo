using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.SymmetryActionStudy;
using Atlas = Gojo.Experiments.QualitativeFrieze.Composition.CentrelinePatternStudy;

namespace QualitativeExecution.Tests;

public sealed class SymmetryActionStudyTests
{
    [Fact]
    public void Overlapping_T_readings_reuse_actual_sections_and_preserve_directed_order()
    {
        var readings = Study.TReadings();
        Assert.Equal(new[] { 3, 7 }, readings.Select(r => r.SharedCount));
        Assert.All(readings, r => { Assert.True(r.Exact); Assert.True(r.SitePatternExact); Assert.Equal(4, r.Code); });
        foreach (var r in readings)
        {
            var native = Study.BuildT(r.Pattern);
            var selections = E((Relation)((Relation)native.Surface.Left).Right).Cast<Relation>().Select(E).ToArray();
            var retainedShared = E((Relation)((Relation)((Relation)((Relation)native.Surface.Right).Right).Right).Right);
            Assert.Equal(r.SharedCount, retainedShared.Length);
            Assert.All(retainedShared, section => { Assert.Contains(selections[0], x => ReferenceEquals(x, section));
                Assert.Contains(selections[1], x => ReferenceEquals(x, section)); });
            var f = Atlas.BuildSource(r.Pattern);
            var route = E((Relation)E(f.Surface)[2])[0];
            var sections = E((Relation)E((Relation)route)[0]);
            var a = r.SourceIndices.Select(i => sections[i]).ToArray();
            var b = r.TargetIndices.Select(i => sections[i]).ToArray();
            Assert.Equal(r.SharedCount, a.Count(x => b.Any(y => ReferenceEquals(x, y))));
            for (var i = 0; i < r.Source.Length; i++)
            {
                Assert.Equal(r.Source[i][0] + r.Offset[0], r.Output[i][0]);
                Assert.Equal(-r.Source[i][1] + r.Offset[1], r.Output[i][1]);
            }
        }
    }

    [Fact]
    public void Recurrence_uses_its_previous_prediction_without_reading_target_back_into_input()
    {
        var f = Atlas.BuildSource("A2");
        var engine = Assert.Single(Study.FindEngines(f, "A2").Where(e => e.Read.Code == 4));
        Assert.Equal(4, engine.Read.SeedSegments); Assert.Equal(10, engine.Read.Occurrences);
        Relation current = engine.Seed;
        foreach (var application in engine.Applications)
        {
            Assert.True(CentrelineAction.TryReadApplication(f.Frame, application, out var read));
            Assert.Same(current, read.Source); Assert.Same(engine.Action, read.Action);
            Assert.True(read.Complete); current = read.Output;
        }
        Assert.All(engine.Matches, witness =>
        { Assert.True(CentrelineAction.TryReadMatch(f.Frame, witness, out var match)); Assert.True(match.GeometryExact); Assert.True(match.SitePatternExact); });
    }

    [Theory]
    [InlineData("A5", 14)]
    [InlineData("B6", 8)]
    [InlineData("CL5", 1)]
    public void Shortest_tested_seed_retains_different_levels_of_instruction(string id, int shortest)
    {
        var engines = Study.FindEngines(Atlas.BuildSource(id), id);
        Assert.NotEmpty(engines);
        Assert.Equal(shortest, engines.Min(e => e.Read.SeedSegments));
        Assert.All(engines, e => { Assert.True(e.Read.Exact); Assert.True(e.Read.SitePatternExact);
            Assert.Equal(e.Read.Occurrences - 1, e.Read.ValidatedApplications); });
    }

    [Fact]
    public void A_shorter_geometry_only_repeat_does_not_hide_a_longer_valid_alias_repeat()
    {
        var f = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder(), d = new Placeholder(),
            e = new Placeholder(), g = new Placeholder(), h = new Placeholder();
        var sites = new[] { a, b, a, c, d, e, d, g, h };
        var sections = Enumerable.Range(0, 8).Select(i => (IElement)f.Line(sites[i], sites[i + 1],
            f.Word(i % 2, 0), f.Word((i + 1) % 2, 0))).ToArray();
        var route = StructuralConstraint.Closed(StructuralConstraint.Closed(sections), StructuralConstraint.Closed());
        var fixture = new Atlas.Fixture(f, StructuralConstraint.Closed(f.Surface, f.Word(0, 0),
            StructuralConstraint.Closed(route), StructuralConstraint.Closed(), StructuralConstraint.Closed()), ["returning course"], [], []);
        var found = Assert.Single(Study.FindEngines(fixture, "alias-control").Where(r => r.Read.Code == 0));
        Assert.Equal(4, found.Read.SeedSegments);
        Assert.True(found.Read.SitePatternExact);
    }

    [Fact]
    public void Negative_controls_separate_extent_order_and_native_handoff_failures()
    {
        var controls = Study.Controls().ToDictionary(c => c.Id);
        Assert.True(controls["exact"].Exact); Assert.True(controls["exact"].SitePatternExact);
        Assert.False(controls["reach-change"].Exact);
        Assert.True(controls["unjoined"].Exact); Assert.False(controls["unjoined"].SitePatternExact);
        Assert.False(controls["reverse-missing"].Exact);
        Assert.True(controls["reverse-explicit"].Exact); Assert.True(controls["reverse-explicit"].SitePatternExact);
    }

    [Fact]
    public void Translated_pairs_and_reflected_pairs_keep_their_distinct_native_actions()
    {
        var shifted = Study.FindPairs(Atlas.BuildSource("B3"), "B3");
        Assert.Contains(shifted, p => p.First == 0 && p.Second == 1 && p.Code == 0 && p.Offset.SequenceEqual(new[] { 3, 0 }));
        var weave = Study.FindPairs(Atlas.BuildSource("CR4"), "CR4");
        Assert.Contains(weave, p => p.First == 0 && p.Second == 1 && p.Code == 4);
        Assert.Contains(weave, p => p.First == 2 && p.Second == 3 && p.Code == 4);
        Assert.DoesNotContain(weave, p => p.First < 2 && p.Second >= 2);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
