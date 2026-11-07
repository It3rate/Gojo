using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Study = Gojo.Experiments.QualitativeFrieze.Composition.WeaveJunctionStudy;

namespace QualitativeExecution.Tests;

public sealed class WeaveJunctionStudyTests
{
    [Fact]
    public void The_same_projected_X_has_two_components_or_one_shared_four_arm_junction()
    {
        var pair = Study.Cases(); var crossing = pair[0]; var joined = pair[1];
        Assert.Equal(2, crossing.MaterialComponents); Assert.Equal(1, joined.MaterialComponents);
        Assert.Equal(4, crossing.Ends); Assert.Equal(4, joined.Ends);
        Assert.Equal(1, crossing.ProjectedCrossings); Assert.Equal(0, joined.ProjectedCrossings);
        Assert.Equal(0, crossing.BranchSites); Assert.Equal(1, joined.BranchSites);
        Assert.Equal(2, crossing.Segments.Length); Assert.Equal(4, joined.Segments.Length);
        var center = Assert.Single(joined.Sites, site => site.Degree == 4);
        Assert.Equal(new Study.Point(0, 0), center.At);
        Assert.All(joined.Segments, s => Assert.True(s.FromSite == center.Id || s.ToSite == center.Id));
        Assert.DoesNotContain(crossing.Sites, site => site.At == center.At);
        // Each junction half-arm lies on exactly one known through-section.
        foreach (var arm in joined.Segments)
            Assert.Single(crossing.Segments, full => Contains(full, arm.From) && Contains(full, arm.To));
        Assert.Equal(crossing.Segments.Sum(LengthSquared), 2 * joined.Segments.Sum(LengthSquared));
    }

    [Fact]
    public void Replacing_the_shared_center_with_equal_but_independent_sites_removes_the_junction()
    {
        var fixture = Study.Build(true);
        var body = Assert.IsType<Relation>(fixture.Surface.Right);
        var scope = Assert.IsType<Relation>(body.Left);
        Assert.True(StructuralConstraint.TryReadScope(scope, out var parts, out _, out _));
        var altered = parts.Cast<Relation>().Select(section =>
        {
            var read = fixture.Frame.Read(section); var endpoints = Assert.IsType<Relation>(section.Left);
            return (IElement)new Relation(new Relation(read.Controls[0] == default ? new Placeholder() : endpoints.Left,
                read.Controls[^1] == default ? new Placeholder() : endpoints.Right), section.Right);
        }).ToArray();
        var detached = fixture with { Surface = new Relation(fixture.Surface.Left,
            new Relation(StructuralConstraint.Closed(altered), body.Right)) };
        var result = Study.Read(detached);
        Assert.Equal(4, result.MaterialComponents); Assert.Equal(8, result.Ends); Assert.Equal(0, result.BranchSites);
    }

    private static bool Contains(Study.Segment s, Study.Point p)
    {
        long dx = s.To.X - s.From.X, dy = s.To.Y - s.From.Y;
        return dx * (p.Y - s.From.Y) - dy * (p.X - s.From.X) == 0 &&
            p.X >= Math.Min(s.From.X, s.To.X) && p.X <= Math.Max(s.From.X, s.To.X) &&
            p.Y >= Math.Min(s.From.Y, s.To.Y) && p.Y <= Math.Max(s.From.Y, s.To.Y);
    }
    private static int LengthSquared(Study.Segment s) => (s.To.X - s.From.X) * (s.To.X - s.From.X) + (s.To.Y - s.From.Y) * (s.To.Y - s.From.Y);
}
