using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class ConstructionRangeTests
{
    private static readonly Lazy<ConstructionRangeStudy.Report> Study = new(() => ConstructionRangeStudy.Explore());

    [Fact]
    public void Shared_extent_obligation_rejects_stretch_without_rewriting_the_free_program()
    {
        var frame = Frame(); var free = ConstructionExchangeMachine.Create(frame, 0);
        var original = free.Surface; var tied = free.WithEqualExtents();
        var freeRead = free.Execute(free.Publish(3, 2)); var tiedRead = tied.Execute(tied.Publish(3, 2));
        Assert.Same(original, free.Surface); Assert.True(freeRead.Admissible); Assert.False(tiedRead.Admissible);
        Assert.Empty(E(freeRead.Failed)); Assert.Single(E(tiedRead.Failed));
        var program = E((Relation)tied.Surface.Right);
        var equality = Assert.IsType<Relation>(Assert.Single(E((Relation)program[4])));
        Assert.Same(tied.WidthRole, equality.Left); Assert.Same(tied.HeightRole, equality.Right);
        Assert.Contains(E(tiedRead.Failed), item => item is Relation failure && ReferenceEquals(failure.Left, equality));
        Assert.True(BoundaryExchangeRead.Evaluate(frame, freeRead.Sections, tiedRead.Sections).Equivalent);
        // The illustrative output is not admitted merely because its ink agrees.
        Assert.Empty(E((Relation)E((Relation)free.Surface.Right)[4]));
        Assert.True(tied.Execute(tied.Publish(2, 2)).Admissible);
    }

    [Fact]
    public void Closed_finite_publication_inventory_distinguishes_attempts_admission_and_distinct_outputs()
    {
        var report = Study.Value;
        Assert.Equal(10, report.Families.Length); Assert.Equal(176, report.Samples.Length);
        Assert.Equal(152, report.Samples.Count(s => s.Admissible)); Assert.Equal(90, report.Comparisons.Length);
        Assert.Equal(24, report.Samples.Count(s => s.Failed > 0)); Assert.All(report.Samples, s => Assert.Equal(0, s.Unresolved));
        foreach (var family in report.Families)
        {
            Assert.True(StructuralConstraint.TryReadScope(family.InputDomain, out var publications, out var seam, out var tail));
            Assert.Same(seam, tail);
            var samples = report.Samples.Where(s => s.Family == family.Id).ToArray();
            Assert.Equal(publications.Length, samples.Length);
            Assert.All(samples, s => Assert.Contains(publications, p => ReferenceEquals(p, s.Publication)));
        }
        Assert.Equal(4, Range("tied-square").UniqueBoundaries);
        Assert.Equal(16, Range("free-walk").UniqueBoundaries);
        Assert.Equal(32, Range("notch-range").UniqueBoundaries);
        Assert.Contains("only", report.Limits[0]);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(report));
        Assert.Equal(10, document.RootElement.GetProperty("Families").GetArrayLength());
    }

    [Fact]
    public void Six_equal_steps_reuse_one_actual_extent_role_and_close_six_distinct_sites()
    {
        var frame = Frame(); var machine = ConstructionExchangeMachine.Create(frame, 9);
        var scopes = E((Relation)machine.Surface.Right).Cast<Relation>().ToArray();
        var advances = E(scopes[2]).Cast<Relation>().ToArray();
        Assert.Equal(6, advances.Length);
        var routes = advances.Select(a => (Relation)a.Left).ToArray();
        Assert.Equal(6, routes.Select(r => r.Left).Distinct(ReferenceEqualityComparer.Instance).Count());
        for (var i = 0; i < routes.Length; i++) Assert.Same(routes[i].Right, routes[(i + 1) % routes.Length].Left);
        IElement[] expected = [frame.Motion.Right, frame.Motion.Right, frame.Motion.Up,
            frame.Motion.Left, frame.Motion.Left, frame.Motion.Down];
        for (var i = 0; i < advances.Length; i++)
        {
            var terms = E((Relation)advances[i].Right).Cast<Relation>().ToArray(); Assert.Equal(2, terms.Length);
            Assert.All(terms, term => { Assert.Same(machine.WidthRole, term.Right); Assert.Same(expected[i], term.Left); });
        }
        var seed = Assert.IsType<Relation>(Assert.Single(E(scopes[1])));
        Assert.All(E((Relation)seed.Right).Cast<Relation>(), term => Assert.Same(machine.WidthRole, term.Right));
        Assert.Empty(E(scopes[4])); // Equal participation comes from the shared role, not a ratio/equality tag.

        var first = machine.Execute(machine.Publish(2, 1)); var unusedHeightChanged = machine.Execute(machine.Publish(2, 4));
        Assert.True(first.Admissible); Assert.True(unusedHeightChanged.Admissible);
        Assert.Single(E(first.Matched)); Assert.Equal(6, E(first.Positions).Length); Assert.Equal(6, E(first.Sections).Length);
        Assert.True(BoundaryExchangeRead.Evaluate(frame, first.Sections, unusedHeightChanged.Sections, true).Equivalent);
        var sections = E(first.Sections).Cast<Relation>().Select(frame.Read).ToArray();
        Assert.All(sections, s =>
        { var delta = s.Controls[1] - s.Controls[0]; Assert.Equal(4, (int)(BigInteger.Abs(delta.X) + BigInteger.Abs(delta.Y))); });
        Assert.True(ConstructionExchangeStudy.Topology(frame, first.Sections).Closed);
        var walk = ConstructionExchangeMachine.Create(frame, 0);
        Assert.True(BoundaryExchangeRead.Evaluate(frame, first.Sections, walk.Execute(walk.Publish(4, 2)).Sections, true).Equivalent);
    }

    [Fact]
    public void Six_equal_step_range_has_four_fixed_aspect_scales_and_only_two_tested_exchange_sizes()
    {
        var range = Range("six-equal-steps");
        Assert.Equal(16, range.Attempts); Assert.Equal(16, range.Admitted); Assert.Equal(4, range.UniqueBoundaries);
        Assert.Equal(4, range.UniqueDirected); Assert.Equal(24, range.DuplicateReads.Length);
        foreach (var family in new[] { "free-walk", "repeated-corner", "radial-open", "latent-notch", "notch-range" })
            foreach (var directed in new[] { false, true })
            {
                var comparison = Comparison(family, "six-equal-steps", directed);
                Assert.Equal(2, comparison.Intersection); Assert.Equal(2, comparison.SecondOnly);
                Assert.False(comparison.FirstSubsetOfSecond); Assert.False(comparison.SecondSubsetOfFirst);
            }
        foreach (var family in new[] { "tied-square", "radial-equal" })
            Assert.Equal(0, Comparison(family, "six-equal-steps", false).Intersection);
        foreach (var family in new[] { "rails", "caps" })
        {
            Assert.Equal(2, Comparison(family, "six-equal-steps", false).Intersection);
            Assert.Equal(0, Comparison(family, "six-equal-steps", true).Intersection);
        }
        Assert.All(Study.Value.Samples.Where(s => s.Family == "six-equal-steps"), s =>
        { Assert.Equal(6, s.Sections); Assert.Equal(0, s.ZeroSections); Assert.True(s.Admissible); });
    }

    [Fact]
    public void Factoring_an_already_free_rectangle_into_a_repeated_corner_does_not_expand_its_range()
    {
        var report = Study.Value;
        foreach (var directed in new[] { false, true })
        {
            var read = Comparison("free-walk", "repeated-corner", directed);
            Assert.Equal(16, read.Intersection); Assert.True(read.FirstSubsetOfSecond); Assert.True(read.SecondSubsetOfFirst);
            Assert.Null(read.FirstOnlySample); Assert.Null(read.SecondOnlySample);
        }
        foreach (var sample in report.Samples.Where(s => s.Family == "repeated-corner"))
        {
            var repeat = Assert.IsType<ConstructionRangeStudy.Repetition>(sample.Repeat);
            Assert.Equal(2, E(sample.Execution.Sections).Length); Assert.Equal(4, E(sample.Output).Length);
            Assert.True(CentrelineAction.TryBits(report.Frame, repeat.Action, out var bits)); Assert.Equal(6, bits);
            Assert.True(repeat.Join.TryReadPassed(repeat.Join.Evaluate())); Assert.Equal(2, E(repeat.Join.Accepted).Length);
            Assert.False(ConstructionExchangeStudy.Topology(report.Frame, repeat.Source).Closed);
            var connected = ConstructionExchangeStudy.Topology(report.Frame, repeat.Sections);
            Assert.True(connected.Closed); Assert.Equal(1, connected.Components);
            Assert.True(CentrelineAction.TryReadApplication(report.Frame, repeat.Application, out var applied));
            Assert.True(applied.Complete); Assert.Same(repeat.Source, applied.Source);
        }
    }

    [Fact]
    public void Releasing_actual_equalities_enlarges_the_sampled_range_but_open_radial_angles_are_not_fully_enumerated()
    {
        foreach (var (a, b) in new[] { ("tied-square", "free-walk"), ("radial-equal", "radial-open") })
        {
            var comparison = Comparison(a, b, false);
            Assert.Equal(4, comparison.Intersection); Assert.Equal(12, comparison.SecondOnly);
            Assert.True(comparison.FirstSubsetOfSecond); Assert.False(comparison.SecondSubsetOfFirst);
            Assert.Equal(12, Range(a).Rejected); Assert.Equal(0, Range(b).Rejected);
            Assert.NotNull(comparison.SecondOnlySample); Assert.False(comparison.RepresentativeMismatch!.Equivalent);
        }
        Assert.Contains(Study.Value.Limits, text => text.Contains("other possible realizations"));
        var small = ConstructionRangeStudy.Explore(1);
        var bounded = small.Comparisons.Single(c => c.First == "tied-square" && c.Second == "free-walk" && !c.Directed);
        Assert.True(bounded.FirstSubsetOfSecond); Assert.True(bounded.SecondSubsetOfFirst);
        // Equality on the smaller offered inventory must not become a universal claim.
        Assert.Contains("1 through 1", small.Domain);
    }

    [Fact]
    public void Shared_ink_range_does_not_erase_different_flow_constructions()
    {
        foreach (var other in new[] { "rails", "caps" })
        {
            var ink = Comparison("free-walk", other, false); var flow = Comparison("free-walk", other, true);
            Assert.Equal(16, ink.Intersection); Assert.True(ink.FirstSubsetOfSecond); Assert.True(ink.SecondSubsetOfFirst);
            Assert.Equal(0, flow.Intersection); Assert.Equal(16, flow.FirstOnly); Assert.Equal(16, flow.SecondOnly);
            Assert.False(flow.FirstSubsetOfSecond); Assert.False(flow.SecondSubsetOfFirst);
        }
    }

    [Fact]
    public void A_latent_zero_read_notch_retains_participation_and_grows_a_strictly_larger_sampled_range()
    {
        var latent = Comparison("free-walk", "latent-notch", true);
        Assert.Equal(16, latent.Intersection); Assert.True(latent.FirstSubsetOfSecond); Assert.True(latent.SecondSubsetOfFirst);
        var expanded = Comparison("latent-notch", "notch-range", false);
        Assert.Equal(16, expanded.Intersection); Assert.Equal(16, expanded.SecondOnly);
        Assert.True(expanded.FirstSubsetOfSecond); Assert.False(expanded.SecondSubsetOfFirst);
        foreach (var sample in Study.Value.Samples.Where(s => s.Family == "latent-notch"))
        {
            Assert.Equal(6, sample.Sections); Assert.Equal(2, sample.ZeroSections);
            Assert.Equal(2, E(sample.AtomRead!.SourceZero).Length);
            foreach (var section in E(sample.AtomRead.SourceZero).Cast<Relation>())
            {
                var read = Study.Value.Frame.Read(section);
                Assert.NotSame(read.Start, read.End); Assert.Equal(read.Controls[0], read.Controls[^1]);
            }
        }
    }

    [Fact]
    public void Each_positive_range_correspondence_has_recheckable_native_boundary_evidence()
    {
        var report = Study.Value;
        foreach (var comparison in report.Comparisons)
        {
            Assert.Equal(comparison.Intersection, comparison.IntersectionReads.Length);
            Assert.Equal(comparison.FirstCount, comparison.Intersection + comparison.FirstOnly);
            Assert.Equal(comparison.SecondCount, comparison.Intersection + comparison.SecondOnly);
            foreach (var witness in comparison.IntersectionReads)
            {
                Assert.True(BoundaryExchangeRead.TryRead(report.Frame, witness.Surface, out var read));
                Assert.True(read.Equivalent); Assert.Empty(E(read.Unresolved));
            }
            if (comparison.RepresentativeMismatch is { } mismatch)
            {
                Assert.True(BoundaryExchangeRead.TryRead(report.Frame, mismatch.Surface, out var read));
                Assert.False(read.Equivalent);
            }
        }
    }

    [Fact]
    public void Repetition_needs_real_handoffs_and_admissible_seams_not_coordinate_coincidence()
    {
        var frame = Frame(); var corner = ConstructionExchangeMachine.Create(frame, 8);
        var source = corner.Execute(corner.Publish(2, 1)).Sections;
        Assert.Throws<ArgumentException>(() => ConstructionRangeStudy.RepeatAndJoin(frame, source,
            CentrelineAction.FromBits(frame, 0, frame.Word(0, 0))));
        var sections = E(source).Cast<Relation>().ToArray(); var last = frame.Read(sections[1]);
        var words = E(last.ControlWords).Cast<Relation>().ToArray();
        var detached = S(sections[0], frame.Line(new Placeholder(), last.End, words[0], words[1]));
        Assert.Throws<ArgumentException>(() => ConstructionRangeStudy.RepeatAndJoin(frame, detached,
            CentrelineAction.FromBits(frame, 6, frame.Word(0, 0))));
        Assert.Equal(2, E(source).Length);
    }

    private static ConstructionRangeStudy.RangeSummary Range(string family) => Study.Value.Ranges.Single(r => r.Family == family);
    private static ConstructionRangeStudy.Comparison Comparison(string first, string second, bool directed) =>
        Study.Value.Comparisons.Single(c => c.First == first && c.Second == second && c.Directed == directed);
    private static Curve.Frame Frame() => new(new QualitativeMotionFrame());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
