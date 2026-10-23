using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Band = Gojo.Experiments.QualitativeExecution.QualitativeBand;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Substrate = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrate;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace QualitativeExecution.Tests;

public sealed class OrnamentSubstrateTests
{
    [Fact]
    public void Oblique_substrate_occurrences_retain_two_actual_additive_routes_and_distinct_sites()
    {
        var f = new Fixture(); var grid = Substrate.Create(f.Frame, f.Frame.Word(12, 0), f.Frame.Word(6, 10), f.Frame.Word(-3, 2), 4, 3);
        Assert.True(grid.Complete); Assert.Equal(12, grid.Landmarks.Count);
        var node = Substrate.At(grid, 3, 2); Assert.True(f.Frame.Vector(node.Position, out var point)); Assert.Equal(new Curve.Vector(45, 22), point);
        var body = (Relation)((Relation)node.Surface.Right).Right;
        Assert.True(f.Frame.Motion.TryReadResult((Relation)body.Left, out var horizontal, out _, out _));
        Assert.Same(horizontal, node.Position);
        var verticalAndAgreement = (Relation)body.Right;
        Assert.True(f.Frame.Motion.TryReadResult((Relation)verticalAndAgreement.Left, out var vertical, out _, out _));
        Assert.NotSame(horizontal, vertical); Assert.True(f.Frame.Vector(vertical, out var alternate)); Assert.Equal(point, alternate);
        Assert.Equal(grid.Landmarks.Count, grid.Landmarks.Select(n => n.Site).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(Substrate.Read(f.Frame, grid.Surface).Complete);
    }

    [Fact]
    public void Landmark_selection_uses_native_phases_and_words_rather_than_changed_diagnostic_fields()
    {
        var f = new Fixture(); var grid = f.Grid with { U = StructuralConstraint.Open(), Origin = f.Frame.Word(999, 0), Landmarks = [] };
        var node = Substrate.At(grid, 1, 1);
        Assert.True(f.Frame.Vector(node.Position, out var point)); Assert.Equal(new Curve.Vector(10, 10), point);
        Assert.Same(f.Grid.Landmarks[4].Surface, node.Surface);
    }

    [Fact]
    public void A_missing_or_dependent_basis_does_not_supply_a_two_dimensional_substrate()
    {
        var f = new Fixture();
        Assert.False(Substrate.Create(f.Frame, StructuralConstraint.Open(), f.Frame.Word(0, 10), f.Frame.Word(0, 0), 2, 2).Complete);
        Assert.False(Substrate.Create(f.Frame, f.Frame.Word(10, 0), f.Frame.Word(20, 0), f.Frame.Word(0, 0), 2, 2).Complete);
    }

    [Fact]
    public void A_forged_node_receipt_cannot_replace_the_actual_predecessor_even_at_equal_coordinates()
    {
        var f = new Fixture(); var items = E((Relation)f.Grid.Surface.Right); var node = (Relation)items[1];
        var body = (Relation)node.Right; var operations = (Relation)body.Right;
        var forged = f.Frame.Motion.Reduce(f.Frame.Word(0, 0), f.Grid.U);
        items[1] = new Relation(node.Left, new Relation(body.Left, new Relation(forged, operations.Right)));
        Assert.False(Substrate.Read(f.Frame, new Relation(f.Grid.Surface.Left, S(items))).Complete);
    }

    [Fact]
    public void Removing_a_route_agreement_or_controller_link_prevents_a_complete_lattice_read()
    {
        var f = new Fixture(); var items = E((Relation)f.Grid.Surface.Right); var node = (Relation)items[4];
        var body = (Relation)node.Right; var operations = (Relation)body.Right; var rest = (Relation)operations.Right;
        items[4] = new Relation(node.Left, new Relation(body.Left, new Relation(operations.Left, new Relation(rest.Left, S()))));
        Assert.False(Substrate.Read(f.Frame, new Relation(f.Grid.Surface.Left, S(items))).Complete);
        var request = (Relation)f.Grid.Surface.Left; var phaseAndLinks = (Relation)request.Right;
        var links = (Relation)phaseAndLinks.Right;
        var noLinks = new Relation(request.Left, new Relation(phaseAndLinks.Left, new Relation(S(), links.Right)));
        Assert.False(Substrate.Read(f.Frame, new Relation(noLinks, f.Grid.Surface.Right)).Complete);
    }

    [Fact]
    public void A_long_line_cross_event_retains_its_actual_continuous_envelopes_and_substrate_occurrence()
    {
        var f = new Fixture(); var node = Substrate.At(f.Grid, 1, 1);
        var horizontal = f.Envelope(f.Frame.Line(new Placeholder(), new Placeholder(), f.Frame.Word(-10, 10), f.Frame.Word(30, 10)));
        var vertical = f.Envelope(f.Frame.Line(new Placeholder(), new Placeholder(), f.Frame.Word(10, -10), f.Frame.Word(10, 30)));
        var crossed = Substrate.CrossEvent(f.Grid, node, horizontal, vertical);
        Assert.True(crossed.Crosses); Assert.False(crossed.Joined);
        var selection = (Relation)crossed.Surface.Left; Assert.Same(f.Grid.Surface, selection.Left); Assert.Same(node.Surface, selection.Right);
        var pairing = (Relation)((Relation)crossed.Surface.Right).Left; Assert.Same(horizontal, pairing.Left); Assert.Same(vertical, pairing.Right);
        Assert.True(Substrate.ReadCrossEvent(f.Frame, crossed.Surface).Crosses);
        var otherNode = Substrate.At(f.Grid, 0, 0);
        Assert.Null(Substrate.ReadCrossEvent(f.Frame, new Relation(new Relation(f.Grid.Surface, otherNode.Surface), crossed.Surface.Right)).Crosses);
    }

    [Fact]
    public void Curved_interior_crossing_stays_open_while_explicit_endpoint_visits_are_readable()
    {
        var f = new Fixture(); var node = Substrate.At(f.Grid, 1, 1);
        var curve = f.Frame.Cubic(node.Site, new Placeholder(), node.Position, f.Frame.Word(15, 20), f.Frame.Word(25, 20), f.Frame.Word(30, 10));
        var envelope = f.Envelope(curve); var vertical = f.Envelope(f.Frame.Line(new Placeholder(), new Placeholder(), f.Frame.Word(10, -10), f.Frame.Word(10, 30)));
        var visit = Substrate.Visit(f.Grid, node, envelope); Assert.True(visit.AtStart); Assert.False(visit.AtEnd); Assert.True(visit.JoinedToLandmark);
        Assert.Null(Substrate.CrossEvent(f.Grid, node, envelope, vertical).Crosses);
        var samePosition = f.Envelope(f.Frame.Cubic(new Placeholder(), new Placeholder(), node.Position, f.Frame.Word(15, 20), f.Frame.Word(25, 20), f.Frame.Word(30, 10)));
        var detached = Substrate.Visit(f.Grid, node, samePosition); Assert.True(detached.AtStart); Assert.False(detached.JoinedToLandmark);
    }

    [Fact]
    public void A_continuous_curved_chain_can_visit_one_landmark_from_both_sides_without_becoming_two_cell_motifs()
    {
        var f = new Fixture(); var node = Substrate.At(f.Grid, 1, 1); var aSite = new Placeholder(); var bSite = new Placeholder();
        var first = f.Frame.Cubic(new Placeholder(), node.Site, f.Frame.Word(0, 0), f.Frame.Word(2, 10), f.Frame.Word(8, 10), node.Position);
        var second = f.Frame.Cubic(node.Site, new Placeholder(), node.Position, f.Frame.Word(12, 10), f.Frame.Word(18, 10), f.Frame.Word(20, 20));
        var a = f.Envelope(first, sideAEnd: aSite, sideBEnd: bSite); var b = f.Envelope(second, sideAStart: aSite, sideBStart: bSite);
        Assert.True(Substrate.Visit(f.Grid, node, a).AtEnd); Assert.True(Substrate.Visit(f.Grid, node, b).AtStart);
        var joined = Parts.Seam(f.Frame, a, b); Assert.True(joined.Joined); Assert.True(joined.Coincident); Assert.True(joined.Tangent); Assert.True(joined.SideAgreement);
    }

    [Fact]
    public void A_foreign_lookalike_node_and_forged_visit_proof_do_not_supply_incidence()
    {
        var f = new Fixture(); var node = Substrate.At(f.Grid, 1, 1); var line = f.Envelope(f.Frame.Line(node.Site, new Placeholder(), node.Position, f.Frame.Word(20, 10)));
        var foreign = Substrate.Create(f.Frame, f.Grid.U, f.Grid.V, f.Grid.Origin, 3, 3); var other = Substrate.At(foreign, 1, 1);
        Assert.Null(Substrate.Visit(f.Grid, other, line).AtStart);
        var actual = Substrate.Visit(f.Grid, node, line); var differences = (Relation)actual.Surface.Right;
        Assert.Null(Substrate.ReadVisit(f.Frame, new Relation(actual.Surface.Left, new Relation(S(), differences.Right))).AtStart);
    }

    [Fact]
    public void Entire_curved_boundaries_match_by_translation_without_becoming_welded()
    {
        var f = new Fixture(); var source = f.Curve(0, 3); var target = f.Curve(20, 3); var map = Parts.Identity(f.Frame); var shift = f.Frame.Word(20, 0);
        var pair = Substrate.PairBoundary(f.Frame, source, target, map, shift);
        Assert.True(pair.Compatible); Assert.False(pair.Welded); Assert.Same(source, pair.Source); Assert.Same(target, pair.Target);
        Assert.Same(target, pair.SelectedTarget); Assert.Same(shift, pair.Translation); Assert.Equal(4, E((Relation)pair.Surface.Right).Length);
        Assert.True(Substrate.ReadBoundaryPair(f.Frame, map, pair.Surface).Compatible);
    }

    [Fact]
    public void Equal_boundary_endpoints_do_not_hide_a_different_interior_bulge()
    {
        var f = new Fixture(); var source = f.Curve(0, 3); var changed = f.Curve(20, 5);
        var pair = Substrate.PairBoundary(f.Frame, source, changed, Parts.Identity(f.Frame), f.Frame.Word(20, 0));
        Assert.False(pair.Compatible); Assert.False(pair.Welded);
    }

    [Fact]
    public void Opposed_boundary_traversal_needs_the_explicit_native_reverse_selection()
    {
        var f = new Fixture(); var source = f.Curve(0, 3); var target = f.Frame.Reverse(f.Curve(20, 3)); var map = Parts.Identity(f.Frame);
        Assert.False(Substrate.PairBoundary(f.Frame, source, target, map, f.Frame.Word(20, 0)).Compatible);
        var paired = Substrate.PairBoundary(f.Frame, source, target, map, f.Frame.Word(20, 0), reverseTarget: true);
        Assert.True(paired.Compatible); Assert.NotSame(target, paired.SelectedTarget); Assert.False(paired.Welded);
        var selection = (Relation)((Relation)paired.Surface.Left).Right; Assert.Same(target, selection.Left); Assert.Same(paired.SelectedTarget, selection.Right);
    }

    [Fact]
    public void Missing_substitution_or_control_correspondence_keeps_the_pair_unresolved()
    {
        var f = new Fixture(); var source = f.Curve(0, 3); var target = f.Curve(20, 3);
        var missing = new QualitativePatternTransform(f.Frame.Motion, f.Frame.Motion, S());
        Assert.Null(Substrate.PairBoundary(f.Frame, source, target, missing, f.Frame.Word(20, 0)).Compatible);
        var map = Parts.Identity(f.Frame); var valid = Substrate.PairBoundary(f.Frame, source, target, map, f.Frame.Word(20, 0));
        var rows = E((Relation)valid.Surface.Right);
        Assert.Null(Substrate.ReadBoundaryPair(f.Frame, map, new Relation(valid.Surface.Left, S(rows.Take(3).ToArray()))).Compatible);
    }

    [Fact]
    public void Forged_equal_coordinate_application_receipts_do_not_replace_actual_control_dependencies()
    {
        var f = new Fixture(); var source = f.Curve(0, 3); var target = f.Curve(20, 3); var map = Parts.Identity(f.Frame);
        var pair = Substrate.PairBoundary(f.Frame, source, target, map, f.Frame.Word(20, 0)); var rows = E((Relation)pair.Surface.Right);
        var first = (Relation)rows[0]; var operation = (Relation)first.Right; var rest = (Relation)operation.Right;
        var fakeSum = f.Frame.Motion.Reduce(f.Frame.Word(0, 0), pair.Translation);
        rows[0] = new Relation(first.Left, new Relation(operation.Left, new Relation(fakeSum, rest.Right)));
        Assert.Null(Substrate.ReadBoundaryPair(f.Frame, map, new Relation(pair.Surface.Left, S(rows))).Compatible);
    }

    [Fact]
    public void Literal_shared_boundary_sites_and_matched_geometry_are_a_separate_weld_read()
    {
        var f = new Fixture(); var source = f.Curve(0, 3);
        var same = Substrate.PairBoundary(f.Frame, source, source, Parts.Identity(f.Frame), f.Frame.Word(0, 0));
        Assert.True(same.Compatible); Assert.True(same.Welded);
        var detached = f.Curve(0, 3); var different = Substrate.PairBoundary(f.Frame, source, detached, Parts.Identity(f.Frame), f.Frame.Word(0, 0));
        Assert.True(different.Compatible); Assert.False(different.Welded);
    }

    [Fact]
    public void Native_transport_preserves_the_selected_landmark_and_its_actual_curve_endpoint_alias()
    {
        var f = new Fixture(); var node = Substrate.At(f.Grid, 1, 1);
        var envelope = f.Envelope(f.Frame.Line(node.Site, new Placeholder(), node.Position, f.Frame.Word(20, 10)));
        var visit = Substrate.Visit(f.Grid, node, envelope);
        var encoded = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("substrate-visit", visit.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(encoded), out var imported));
        var root = Assert.IsType<Relation>(imported.Root); var request = (Relation)root.Left; var selection = (Relation)request.Left;
        var lattice = (Relation)selection.Left; var selectedNode = (Relation)selection.Right;
        Assert.Contains(E((Relation)lattice.Right), item => ReferenceEquals(item, selectedNode));
        var site = ((Relation)selectedNode.Left).Left;
        var band = (Relation)request.Right; var sweep = (Relation)band.Left;
        var spine = (Relation)((Relation)((Relation)sweep.Left).Left).Right;
        Assert.Same(site, ((Relation)spine.Left).Left);
    }

    [Fact]
    public void Substrate_and_full_curve_correspondence_introduce_no_measured_holder_state()
    {
        var f = new Fixture(); var pair = Substrate.PairBoundary(f.Frame, f.Curve(0, 3), f.Curve(20, 3), Parts.Identity(f.Frame), f.Frame.Word(20, 0));
        var root = new Relation(f.Grid.Surface, pair.Surface); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var work = new Stack<IElement>(); work.Push(root);
        while (work.Count != 0)
        {
            var item = work.Pop(); if (!seen.Add(item)) continue;
            if (item is Carrier carrier) { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); }
            if (item is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); }
        }
    }

    private sealed class Fixture
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        public Substrate.Lattice Grid { get; }
        public Fixture() => Grid = Substrate.Create(Frame, Frame.Word(10, 0), Frame.Word(0, 10), Frame.Word(0, 0), 3, 3);
        public Relation Curve(int x, int bulge) => Frame.Cubic(new Placeholder(), new Placeholder(), Frame.Word(x, 0), Frame.Word(x + bulge, 3), Frame.Word(x + bulge, 7), Frame.Word(x, 10));
        public Relation Envelope(Relation spine, IElement? sideAStart = null, IElement? sideAEnd = null, IElement? sideBStart = null, IElement? sideBEnd = null)
        {
            var count = Frame.Read(spine).Controls.Length;
            var sweep = Band.Read(Frame, Band.Sweep(Frame, spine, Enumerable.Range(0, count).Select(_ => Frame.Word(0, 1)).ToArray(),
                Enumerable.Range(0, count).Select(_ => Frame.Word(0, -1)).ToArray(), sideAStart, sideAEnd, sideBStart, sideBEnd));
            return Band.BandEnvelope(sweep.Surface, Band.Continue(Frame, sweep, true), Band.Continue(Frame, sweep, false), Band.Occupy(sweep));
        }
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var items, out var start, out var end)); Assert.Equal(start, end); return items; }
}
