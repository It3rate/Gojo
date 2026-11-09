using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class ConstructionExchangeStudyTests
{
    [Fact]
    public void Four_step_walk_and_two_caps_exchange_outline_and_coarse_topology_but_not_flow()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 2, 2); var caps = Describe(frame, 4, 2, 2);
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, caps);
        Assert.True(comparison.Admissible); Assert.True(comparison.BoundaryAgrees);
        Assert.True(comparison.TopologyAgrees); Assert.True(comparison.BoundaryAndTopologyExchange);
        Assert.False(comparison.DirectedAgrees);
        Assert.Equal(4, E(walk.Execution.Sections).Length); Assert.Equal(6, E(caps.Execution.Sections).Length);
        var a = ConstructionExchangeStudy.Topology(frame, walk.Execution.Sections);
        var b = ConstructionExchangeStudy.Topology(frame, caps.Execution.Sections);
        Assert.Equal(1, a.Components); Assert.Equal(1, b.Components); Assert.True(a.Closed); Assert.True(b.Closed);
        Assert.NotEqual(IncidenceSites(a), IncidenceSites(b));
        Assert.Same(walk.Execution.Surface, ((Relation)E(comparison.Witness)[0]).Left);
        Assert.Same(caps.Execution.Surface, ((Relation)E(comparison.Witness)[0]).Right);
    }

    [Fact]
    public void Equal_turn_radial_candidate_rejects_stretch_even_when_its_generated_outline_matches()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 3, 2); var radial = Describe(frame, 2, 3, 2);
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, radial);
        Assert.True(comparison.BoundaryAgrees); Assert.True(comparison.TopologyAgrees);
        Assert.False(radial.Execution.Admissible); Assert.False(comparison.BoundaryExchange);
        Assert.NotEmpty(E(radial.Execution.Failed));
        var contour = Assert.IsType<QualitativeMarkedContour>(radial.Machine.Contour);
        Assert.True(contour.TryReadEqualTurns(contour.Evaluate(), out _));
        var obligations = E((Relation)radial.Machine.Surface.Right);
        var equality = Assert.IsType<Relation>(Assert.Single(E((Relation)obligations[4])));
        Assert.Same(radial.Machine.WidthRole, equality.Left); Assert.Same(radial.Machine.HeightRole, equality.Right);
        Assert.Contains(E(radial.Execution.Failed), item => item is Relation failure && ReferenceEquals(failure.Left, equality));
    }

    [Fact]
    public void Opening_the_equal_turn_requirement_keeps_radial_correspondence_and_admits_rectangular_read()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 3, 2); var radial = Describe(frame, 3, 3, 2);
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, radial);
        Assert.True(radial.Execution.Admissible); Assert.True(comparison.BoundaryAndTopologyExchange);
        var contour = Assert.IsType<QualitativeMarkedContour>(radial.Machine.Contour);
        var receipt = contour.Evaluate();
        Assert.True(contour.TryReadClosedContour(receipt, out _));
        Assert.False(contour.TryReadEqualTurns(receipt, out _));
        Assert.True(contour.TryReadResult(receipt, out _, out _, out var unresolved));
        Assert.NotEmpty(E(unresolved));
        var drawnSites = E(radial.Execution.Sections).Cast<Relation>().Select(frame.Read).Select(section => section.Start).ToArray();
        Assert.All(E(contour.Markers), marker => Assert.Contains(drawnSites, site => ReferenceEquals(marker, site)));
        Assert.All(E(contour.RadialAttachments).Cast<Relation>(), attachment =>
            Assert.Same(contour.ReachSupport, attachment.Right));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Collapsed_radial_markers_leave_the_geometric_calibration_unresolved(int option)
    {
        var frame = Frame(); var machine = ConstructionExchangeMachine.Create(frame, option);
        var execution = machine.Execute(machine.Publish(0, 2));
        Assert.False(execution.Admissible); Assert.NotEmpty(E(execution.Unresolved));
        Assert.Equal(4, E(execution.Sections).Length);
        var sections = E(execution.Sections).Cast<Relation>().Select(frame.Read).ToArray();
        Assert.Equal(2, sections.Count(section => section.Controls[0] == section.Controls[1]));
        Assert.Equal(4, sections.Select(section => section.Start).Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void A_zero_notch_read_preserves_six_phases_then_exposes_new_boundary_when_increased()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 3, 2); var zero = Describe(frame, 5, 3, 2);
        var exposed = Describe(frame, 5, 3, 2, 1);
        var first = ConstructionExchangeStudy.Compare(frame, walk, zero);
        var second = ConstructionExchangeStudy.Compare(frame, walk, exposed);
        Assert.True(first.BoundaryAndTopologyExchange); Assert.True(first.DirectedAgrees);
        Assert.Equal(6, E(zero.Execution.Sections).Length); Assert.Equal(2, zero.ZeroSections);
        var zeroRead = BoundaryExchangeRead.Evaluate(frame, zero.Execution.Sections, walk.Execution.Sections);
        Assert.Equal(2, E(zeroRead.SourceZero).Length);
        foreach (var section in E(zeroRead.SourceZero).Cast<Relation>())
        {
            var read = frame.Read(section);
            Assert.NotSame(read.Start, read.End); Assert.Equal(read.Controls[0], read.Controls[1]);
        }
        Assert.False(second.BoundaryAgrees); Assert.True(second.TopologyAgrees);
        Assert.True(exposed.Execution.Admissible); Assert.Equal(6, E(exposed.Execution.Sections).Length);
        Assert.Equal(0, exposed.ZeroSections);
    }

    [Fact]
    public void Missing_open_or_foreign_extent_does_not_supply_a_zero_measurement()
    {
        var frame = Frame(); var machine = ConstructionExchangeMachine.Create(frame, 0);
        var rows = E(machine.Publish(2, 2));
        var foreign = new Placeholder();
        foreach (var bad in new[]
        {
            S(rows[0], rows[2]), StructuralConstraint.Open(rows),
            S(new Relation(machine.WidthRole, StructuralConstraint.Open()), rows[1], rows[2]),
            S(new Relation(machine.WidthRole, S(foreign)), rows[1], rows[2]),
            S(rows[0], rows[0], rows[2])
        })
        {
            var execution = machine.Execute(bad);
            Assert.False(execution.Admissible); Assert.NotEmpty(E(execution.Unresolved));
            Assert.Empty(E(execution.Sections)); Assert.Empty(E(execution.Positions));
        }
    }

    [Fact]
    public void Detached_rails_match_ink_but_do_not_acquire_connected_topology()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 2, 2); var detached = Describe(frame, 7, 2, 2);
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, detached);
        Assert.True(comparison.BoundaryExchange); Assert.False(comparison.TopologyAgrees);
        Assert.False(comparison.BoundaryAndTopologyExchange);
        var topology = ConstructionExchangeStudy.Topology(frame, detached.Execution.Sections);
        Assert.Equal(4, topology.Components); Assert.False(topology.Closed);
        var ends = E(detached.Execution.Sections).Cast<Relation>().Select(frame.Read)
            .SelectMany(read => new[] { read.Start, read.End }).ToArray();
        Assert.Equal(8, ends.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(topology.Branches, degree => Assert.Equal(1, degree));
    }

    [Fact]
    public void New_candidate_publications_preserve_previous_execution_and_shared_site_correspondence()
    {
        var frame = Frame(); var machine = ConstructionExchangeMachine.Create(frame, 5);
        var initialPublication = machine.Publish(2, 2, 0);
        var old = machine.Execute(initialPublication);
        var positions = E(old.Positions).Cast<Relation>().ToArray();
        var sections = E(old.Sections).Cast<Relation>().ToArray();
        var before = sections.Select(frame.Read).Select(read => read.Controls.ToArray()).ToArray();
        var next = machine.Execute(machine.Publish(3, 2, 1));
        var replay = machine.Execute(initialPublication);
        Assert.Same(initialPublication, ((Relation)old.Surface.Left).Right);
        Assert.Same(machine.Surface, ((Relation)old.Surface.Left).Left);
        Assert.Equal(6, sections.Length); Assert.Equal(6, E(next.Sections).Length);
        for (var i = 0; i < sections.Length; i++)
        {
            Assert.Equal(before[i], frame.Read(sections[i]).Controls);
            Assert.Same(sections[i], E(old.Sections)[i]);
            Assert.Same(frame.Read(sections[i]).Start, frame.Read((Relation)E(next.Sections)[i]).Start);
        }
        for (var i = 0; i < positions.Length; i++) Assert.Same(positions[i], E(old.Positions)[i]);
        Assert.True(BoundaryExchangeRead.Evaluate(frame, old.Sections, replay.Sections, directed: true).Equivalent);
        Assert.False(BoundaryExchangeRead.Evaluate(frame, old.Sections, next.Sections).Equivalent);
    }

    [Fact]
    public void A_favourable_display_summary_does_not_authorize_a_failed_native_exchange()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 3, 2); var radial = Describe(frame, 2, 3, 2);
        Assert.False(radial.Execution.Admissible);
        var misleading = radial with { Admissible = true, Failed = 0, Unresolved = 0 };
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, misleading);
        Assert.True(comparison.BoundaryAgrees); Assert.False(comparison.Admissible);
        Assert.False(comparison.BoundaryExchange);
    }

    private static ConstructionExchangeStudy.Shape Describe(Curve.Frame frame, int option, int width, int height, int notch = 0) =>
        ConstructionExchangeStudy.Describe(ConstructionExchangeMachine.Create(frame, option),
            ConstructionExchangeStudy.Machines[option], "test", width, height, notch);

    [Fact]
    public void Radial_realization_retains_executable_reflections_and_their_actual_comparisons()
    {
        var frame = Frame(); var radial = Describe(frame, 3, 3, 2);
        var program = E((Relation)radial.Machine.Surface.Right);
        var retained = E((Relation)program[5]);
        var basis = Assert.IsType<Relation>(retained[0]);
        var calibration = E((Relation)basis.Right);
        var maps = E((Relation)calibration[2]).Cast<Relation>().ToArray();
        Assert.Equal(new byte[] { 0, 2, 6, 4 }, maps.Select(m => CentrelineAction.Encode(frame, (Relation)m.Right)));
        foreach (var map in maps)
        {
            var evidence = E(radial.Execution.Matched).OfType<Relation>().Single(row =>
                row.Left is Relation cause && ReferenceEquals(cause.Left, basis) && ReferenceEquals(cause.Right, map));
            Assert.True(CentrelineAction.TryReadMatch(frame, (Relation)evidence.Right, out var read));
            Assert.True(read.GeometryExact); Assert.True(read.SitePatternExact);
        }
    }

    [Fact]
    public void Native_archive_restores_the_selected_boundary_read_without_machine_labels()
    {
        var frame = Frame(); var walk = Describe(frame, 0, 2, 2); var caps = Describe(frame, 4, 2, 2);
        var comparison = ConstructionExchangeStudy.Compare(frame, walk, caps);
        var encoded = GojoNumericFormat.Encode(comparison.Witness);
        var root = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(encoded)));
        var fields = E(root);
        var boundary = Assert.IsType<Relation>(fields[1]);
        var contract = Assert.IsType<Relation>(boundary.Left);
        var request = Assert.IsType<Relation>(contract.Left);
        var restoredFrame = Curve.Frame.FromSurface(Assert.IsType<Relation>(request.Left));
        Assert.NotSame(frame.Surface, restoredFrame.Surface);
        Assert.True(BoundaryExchangeRead.TryRead(restoredFrame, boundary, out var read));
        Assert.True(read.Equivalent);
        Assert.True(BoundaryExchangeRead.TryRead(restoredFrame, Assert.IsType<Relation>(fields[2]), out var directed));
        Assert.False(directed.Equivalent);
    }
    private static int IncidenceSites(ConstructionExchangeStudy.TopologyRead topology) =>
        E((Relation)topology.Witness.Right).Cast<Relation>().Sum(group => E(group).Length);
    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
