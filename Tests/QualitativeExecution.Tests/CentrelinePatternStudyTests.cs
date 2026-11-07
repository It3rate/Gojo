using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.CentrelinePatternStudy;

namespace QualitativeExecution.Tests;

public sealed class CentrelinePatternStudyTests
{
    [Fact]
    public void Eighteen_source_rows_keep_their_separate_scopes_and_point_pens()
    {
        var rows = Study.Patterns();
        Assert.Equal(new[] { "A1", "A2", "A3", "A4", "A5", "A6", "B1", "B3", "B4", "B5", "B6", "B8", "CL1", "CL2", "CL5", "CL6", "CR3", "CR4" }, rows.Select(r => r.Id));
        Assert.All(rows, row => Assert.True(row.Metrics.Segments > 0));
        Assert.All(rows, row => Assert.Equal(0, row.Metrics.CollinearOverlaps));
        var dots = rows.Single(r => r.Id == "B8");
        Assert.Equal(8, dots.Metrics.Spots);
        Assert.All(dots.Paths.Where(p => p.Points.Length == 1), p => Assert.Empty(p.StepWord));
        Assert.Equal(dots.Metrics.Paths, dots.Metrics.Components);
        Assert.True(rows.Single(r => r.Id == "CL6").Metrics.ProjectedCrossings > rows.Single(r => r.Id == "B4").Metrics.ProjectedCrossings);
    }

    [Fact]
    public void Double_crosses_and_clipped_lattice_do_not_invent_edge_turns()
    {
        foreach (var id in new[] { "B4", "CL6" })
        {
            var r = Study.Read(Study.BuildSource(id));
            Assert.All(r.Paths, path => Assert.Equal(2, path.Points.Length));
            Assert.Equal(0, r.Metrics.LeftTurns); Assert.Equal(0, r.Metrics.RightTurns);
            Assert.Equal(0, r.Metrics.ClosedPaths); Assert.Equal(r.Metrics.Paths, r.Metrics.Components);
            Assert.True(r.Metrics.ProjectedContacts > 0);
        }
        var doubleCross = Study.Read(Study.BuildSource("B4"));
        Assert.Equal(10, doubleCross.Metrics.ProjectedCrossings);
        Assert.Equal(10, doubleCross.Groups.Single(g => g.Id == "lower").PathIds.Length);
        Assert.Equal(10, doubleCross.Groups.Single(g => g.Id == "upper").PathIds.Length);
    }

    [Fact]
    public void Framing_contributions_keep_contacts_without_duplicate_support()
    {
        foreach (var id in new[] { "B1", "B6", "CL1" })
        {
            var r = Study.Read(Study.BuildSource(id));
            Assert.Equal(0, r.Metrics.CollinearOverlaps);
            Assert.True(r.Metrics.ProjectedContacts > 0);
            Assert.True(r.Groups.Single(g => g.Id == "rails").PathIds.Length > 2);
        }
    }

    [Fact]
    public void Native_selections_overlap_by_reference_without_copying_geometry()
    {
        var f = Study.BuildSource("CR4"); var fields = E(f.Surface);
        var routes = E((Relation)fields[2]); var groups = E((Relation)fields[3]).Cast<Relation>().Select(E).ToArray();
        Assert.Equal(4, routes.Length); Assert.Equal(3, groups.Length);
        Assert.Same(routes[0], groups[0][0]); Assert.Same(routes[1], groups[0][1]);
        Assert.Same(routes[2], groups[1][0]); Assert.Same(routes[3], groups[1][1]);
        Assert.Equal(routes, groups[2]);
        var read = Study.Read(f);
        Assert.Equal(4, read.Metrics.Components); Assert.Equal(24, read.Metrics.ProjectedCrossings);
        Assert.Equal(42, read.Metrics.Segments);
        Assert.Equal(new[] { 0, 1 }, read.Groups[0].PathIds);
        Assert.Equal(new[] { 2, 3 }, read.Groups[1].PathIds);
    }

    [Fact]
    public void Shared_source_word_survives_independent_placement_and_expansion()
    {
        foreach (var variant in new[] { 0, 1, 2, 3, 4 })
        {
            var f = Study.BuildPair(variant); var applications = E((Relation)E(f.Surface)[4]).Cast<Relation>().ToArray();
            Assert.Equal(2, applications.Length);
            Assert.Same(((Relation)applications[0].Left).Left, ((Relation)applications[1].Left).Left);
            var routes = E((Relation)E(f.Surface)[2]);
            Assert.Same(routes[0], ((Relation)applications[0].Right).Right);
            Assert.Same(routes[1], ((Relation)applications[1].Right).Right);
        }
    }

    [Fact]
    public void Offsetting_a_partner_breaks_the_chosen_axis_but_not_translated_reflection()
    {
        var r = Study.Read(Study.BuildPair(1), includeSelectedPairQueries: true);
        Assert.False(r.PairReads[0].Exact);
        Assert.True(r.PairReads[1].Exact);
        Assert.Equal(new[] { 2, 1 }, r.PairReads[1].Translation);
        Assert.Equal(2, r.Metrics.Components);
    }

    [Fact]
    public void Unequal_vertical_expansion_is_not_a_rigid_reflected_copy()
    {
        var r = Study.Read(Study.BuildPair(2), includeSelectedPairQueries: true);
        Assert.False(r.PairReads[0].Exact); Assert.False(r.PairReads[1].Exact);
        Assert.Equal(2, r.PairReads[1].Matched);
        Assert.Equal(4, r.PairReads[1].Checked);
    }

    [Fact]
    public void Reversed_traversal_needs_an_explicit_reverse_correspondence()
    {
        var r = Study.Read(Study.BuildPair(3), includeSelectedPairQueries: true);
        Assert.False(r.PairReads[1].Exact); Assert.True(r.PairReads[2].Exact);
        Assert.Equal("vertical-reflection+reverse", r.PairReads[2].Transform);
        Assert.Equal(2, r.Metrics.Components);
    }

    [Fact]
    public void Axis_dwell_is_exact_reflection_with_overlap_and_separate_ownership()
    {
        var plain = Study.Read(Study.BuildPair(), includeSelectedPairQueries: true);
        var dwell = Study.Read(Study.BuildPair(4), includeSelectedPairQueries: true);
        Assert.True(plain.PairReads[0].Exact); Assert.True(dwell.PairReads[0].Exact);
        Assert.Equal(0, plain.Metrics.CollinearOverlaps); Assert.Equal(1, dwell.Metrics.CollinearOverlaps);
        Assert.Equal(2, dwell.Metrics.Components); Assert.Equal(0, dwell.Metrics.ProjectedCrossings);
        Assert.NotEqual(dwell.Paths[0].SiteIds[0], dwell.Paths[1].SiteIds[0]);
    }

    [Fact]
    public void Continuous_inward_outward_winding_differs_from_finite_cell_coils()
    {
        var winding = Study.Read(Study.BuildSource("A5"));
        Assert.Equal(3, winding.Metrics.Components);
        Assert.Single(winding.Groups.Single(g => g.Id == "course").PathIds);
        Assert.Equal(56, winding.Paths[0].Points.Length - 1);
        var coils = Study.Read(Study.BuildSource("CL2"));
        Assert.Equal(0, coils.Metrics.ClosedPaths); Assert.Equal(15, coils.Metrics.Components);
        Assert.Equal(6, coils.Groups.Single(g => g.Id == "spirals").PathIds.Length);
        Assert.Contains(coils.PairReads, p => p.First == 0 && p.Second == 1 && p.Transform == "half-turn+translation" && p.Exact);
        var medallions = Study.Read(Study.BuildSource("A6"));
        Assert.Equal(9, medallions.Metrics.ClosedPaths);
        Assert.All(medallions.Paths.Where(p => p.Closed), p => Assert.Equal(4, p.TurnWord.Split(' ').Length));
        Assert.All(medallions.Groups.Single(g => g.Id == "hooks").PathIds, id => Assert.False(medallions.Paths[id].Closed));
    }

    [Fact]
    public void A_T_contact_is_visible_without_joining_the_material_routes()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var f = Fixture(frame,
            Route(frame.Line(new Placeholder(), new Placeholder(), frame.Word(0,0), frame.Word(4,0))),
            Route(frame.Line(new Placeholder(), new Placeholder(), frame.Word(2,0), frame.Word(2,2))));
        var r = Study.Read(f);
        Assert.Equal(1, r.Metrics.ProjectedContacts); Assert.Equal(0, r.Metrics.ProjectedCrossings);
        Assert.Equal(0, r.Metrics.CollinearOverlaps); Assert.Equal(2, r.Metrics.Components);
    }

    [Fact]
    public void U_turn_is_not_straight_continuation()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var mid = new Placeholder();
        var f = Fixture(frame, Route(
            frame.Line(new Placeholder(), mid, frame.Word(0,0), frame.Word(3,0)),
            frame.Line(mid, new Placeholder(), frame.Word(3,0), frame.Word(1,0))));
        var r = Study.Read(f);
        Assert.Equal(1, r.Metrics.UTurns); Assert.Equal(0, r.Metrics.StraightSteps);
        Assert.Equal("U", r.Paths[0].TurnWord);
    }

    [Fact]
    public void A_shared_site_at_incompatible_positions_does_not_supply_false_connectivity()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame()); var site = new Placeholder();
        var f = Fixture(frame,
            Route(frame.Line(site, new Placeholder(), frame.Word(0,0), frame.Word(1,0))),
            Route(frame.Line(site, new Placeholder(), frame.Word(2,0), frame.Word(3,0))));
        Assert.Throws<ArgumentException>(() => Study.Read(f));
    }

    [Fact]
    public void A_selection_cannot_reach_a_foreign_route()
    {
        var f = Study.BuildPair(); var fields = E(f.Surface); var foreign = Study.BuildPair();
        fields[3] = S(S(E((Relation)E(foreign.Surface)[2])[0]));
        var invalid = new Study.Fixture(f.Frame, S(fields), f.Roles, ["foreign"], ["foreign"]);
        Assert.Throws<ArgumentException>(() => Study.Read(invalid));
    }

    [Fact]
    public void Renaming_a_pattern_does_not_select_additional_queries_or_change_its_facts()
    {
        var f = Study.BuildPair(); var a = Study.Read(f, id: "ordinary"); var b = Study.Read(f, id: "paired-name-is-not-a-law");
        Assert.Equal(a.Metrics, b.Metrics); Assert.Equal(a.PairReads.Length, b.PairReads.Length);
        Assert.Equal(a.PairReads.Select(p => p.Transform), b.PairReads.Select(p => p.Transform));
    }

    [Fact]
    public void An_empty_comparison_is_explicitly_outside_the_reader_contract()
    {
        var p = new Study.PathRead(0, "", [], false, "", "", []);
        Assert.Throws<ArgumentException>(() => Study.Compare(p, p, "translation"));
    }

    private static Relation Route(params Relation[] sections) => S(S(sections), S());
    private static Study.Fixture Fixture(Curve.Frame frame, params Relation[] routes) =>
        new(frame, S(frame.Surface, frame.Word(4,0), S(routes), S(S(routes)), S()),
            routes.Select((_, i) => "route " + i).ToArray(), ["all"], ["all"]);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var items, out _, out _) ? items.ToArray() : [];
}
