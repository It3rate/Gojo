using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ConstructionAtlasTests
{
    [Fact]
    public void CatalogueContainsFourteenPatternsAndThirtyOneDistinctConstructions()
    {
        Assert.Equal(14, ConstructionAtlas.Patterns.Count);
        Assert.Equal(31, ConstructionAtlas.All.Count);
        Assert.All(ConstructionAtlas.All.GroupBy(e => e.Pattern), variants =>
        {
            Assert.InRange(variants.Count(), 2, 3);
            Assert.Equal(variants.Count(), variants.Select(e => e.Method).Distinct().Count());
        });
        Assert.All(ConstructionAtlas.All, e => Assert.False(string.IsNullOrWhiteSpace(e.Description)));
    }

    [Fact]
    public void EveryConstructionUsesActualLinksAndEverySelectedGroupIsContinuous()
    {
        foreach (var example in ConstructionAtlas.All)
        {
            var surface = example.Surface;
            var table = surface.Table;
            var template = table.Template;
            var before = FamilyCodec.Encode(table);
            Assert.Equal(template.SiteCount, example.Points.Length);
            Assert.Same(template, surface.Uses.Template);
            Assert.All(example.Points, p => Assert.True(double.IsFinite(p.X) && double.IsFinite(p.Y)));
            var used = new HashSet<int>();
            for (var group = 0; group < surface.Uses.GroupCount; group++)
            {
                Assert.True(surface.Uses.IsContinuous(table, surface.Entry, group), example.Pattern + ": " + example.Method);
                foreach (var occurrence in surface.Uses.ReadGroup(table, surface.Entry, group))
                {
                    used.Add(occurrence.Link);
                    Assert.Same(table, occurrence.Span.Start.Owner);
                    Assert.Equal(surface.Entry, occurrence.Span.Value.Entry);
                    Assert.Equal(1UL, table.ReadRaw(surface.Entry, occurrence.Span.Value.Field));
                }
            }
            Assert.Equal(Enumerable.Range(0, template.LinkCount), used.Order());
            Assert.Equal(before, FamilyCodec.Encode(table));
        }
    }

    [Fact]
    public void AlternativeMethodsShareTheIllustrativeOutlineWithoutSharingPhysicalOwners()
    {
        foreach (var variants in ConstructionAtlas.All.GroupBy(e => e.Pattern))
        {
            var first = variants.First();
            var outline = Outline(first);
            foreach (var other in variants.Skip(1))
            {
                Assert.NotSame(first.Surface.Table, other.Surface.Table);
                Assert.True(outline.SetEquals(Outline(other)), variants.Key + ": " + other.Method);
            }
        }
    }

    [Fact]
    public void SharedReachAndIndependentReachAreEncodedByFieldReferences()
    {
        foreach (var square in ConstructionAtlas.All.Where(e => e.Pattern == "Square"))
        {
            Assert.Equal(1, square.Surface.Table.Template.FieldCount);
            Assert.All(square.Surface.Table.ReadLinks(square.Surface.Entry), link => Assert.Equal(0, link.Value.Field));
        }
        foreach (var rectangle in ConstructionAtlas.All.Where(e => e.Pattern == "Rectangle"))
        {
            var links = rectangle.Surface.Table.ReadLinks(rectangle.Surface.Entry);
            Assert.Equal(links[0].Value, links[2].Value);
            Assert.Equal(links[1].Value, links[3].Value);
            Assert.NotEqual(links[0].Value, links[1].Value);
        }
        foreach (var pattern in new[] { "Triangle", "Pentagon", "Hexagon" })
        {
            var radial = Find(pattern, 0).Surface;
            var separate = Find(pattern, 1).Surface;
            Assert.Equal(1, radial.Table.Template.FieldCount);
            Assert.Equal(separate.Table.Template.LinkCount, separate.Table.Template.FieldCount);
            Assert.NotNull(radial.Basis);
            Assert.Null(separate.Basis);
        }
        Assert.Equal(9, ConstructionAtlas.All.Count(e => e.Surface.Basis != null));
    }

    [Fact]
    public void AuthoredGeometricClaimsHaveCompleteSourceCorrespondenceAndLeaveUnprovedEqualityOpen()
    {
        foreach (var example in ConstructionAtlas.All.Where(e => e.Surface.Basis != null))
            Assert.True(example.Surface.Basis!.Corresponds(example.Surface), example.Pattern + ": " + example.Method);
        foreach (var example in ConstructionAtlas.All.Where(e => e.Pattern == "Square"))
            Assert.Equal(((byte)2, (byte)2), example.Surface.Basis!.Read(example.Surface));
        foreach (var example in ConstructionAtlas.All.Where(e => e.Pattern == "Rectangle"))
            Assert.Equal(((byte)2, (byte)3), example.Surface.Basis!.Read(example.Surface));
        foreach (var example in ConstructionAtlas.All.Where(e => e.Pattern is "Triangle" or "Pentagon" or "Hexagon"))
            if (example.Surface.Basis is { } basis)
            {
                Assert.True(basis.RadialSource!.Read().RegularPolygon);
                Assert.Equal(((byte)3, (byte)3), basis.Read(example.Surface));
            }
    }

    [Fact]
    public void BranchAndMergeReverseTheSameYUsesWithoutChangingDirectionBits()
    {
        var branch = Find("Y", 0).Surface;
        var merge = Find("Y", 1).Surface;
        Assert.Equal(FamilyCodec.Encode(branch.Table), FamilyCodec.Encode(merge.Table));
        for (var group = 0; group < 2; group++)
        {
            var expected = branch.Uses.ReadGroup(branch.Table, branch.Entry, group, reverse: true);
            var actual = merge.Uses.ReadGroup(merge.Table, merge.Entry, group);
            Assert.Equal(expected.Select(e => (e.Link, e.Reversed, e.Span.Start.Local, e.Span.End.Local)),
                actual.Select(e => (e.Link, e.Reversed, e.Span.Start.Local, e.Span.End.Local)));
        }
        Assert.Equal(0, merge.Uses.ReadGroup(merge.Table, merge.Entry, 0)[^1].Link);
        Assert.Equal(0, merge.Uses.ReadGroup(merge.Table, merge.Entry, 1)[^1].Link);
        Assert.All(Enumerable.Range(0, merge.Table.Template.FieldCount), field =>
            Assert.Equal((byte)1, merge.Table.ReadCoarse(merge.Entry, field)));
    }

    [Fact]
    public void HouseDistinguishesSharedSeamFromMatchingEndpointsAndFromAnOuterWalk()
    {
        var shared = Find("House", 0).Surface;
        var duplicate = Find("House", 1).Surface;
        var outer = Find("House", 2).Surface;
        Assert.Equal(6, shared.Table.Template.LinkCount);
        Assert.Equal(7, duplicate.Table.Template.LinkCount);
        var roof = shared.Uses.ReadGroup(shared.Table, shared.Entry, 0)[0];
        var room = shared.Uses.ReadGroup(shared.Table, shared.Entry, 1)[0];
        Assert.Equal(roof.Link, room.Link);
        Assert.Equal(roof.Span.Start, room.Span.End);
        Assert.Equal(roof.Span.Value, room.Span.Value);
        var otherRoof = duplicate.Uses.ReadGroup(duplicate.Table, duplicate.Entry, 0)[0];
        var otherRoom = duplicate.Uses.ReadGroup(duplicate.Table, duplicate.Entry, 1)[0];
        Assert.Equal(otherRoof.Span.Start, otherRoom.Span.End);
        Assert.Equal(otherRoof.Span.End, otherRoom.Span.Start);
        Assert.NotEqual(otherRoof.Link, otherRoom.Link);
        Assert.NotEqual(otherRoof.Span.Value, otherRoom.Span.Value);
        Assert.Equal(FamilyCodec.Encode(shared.Table), FamilyCodec.Encode(outer.Table));
        Assert.Equal(2, ClosedGroups(shared));
        Assert.Equal(2, ClosedGroups(duplicate));
        Assert.Equal(1, ClosedGroups(outer));
    }

    [Fact]
    public void TheSameAAndChurchGraphsSupportDifferentClosedSubwalks()
    {
        var legs = Find("A", 0).Surface;
        var loop = Find("A", 1).Surface;
        Assert.Equal(FamilyCodec.Encode(legs.Table), FamilyCodec.Encode(loop.Table));
        Assert.Equal(0, ClosedGroups(legs));
        Assert.Equal(1, ClosedGroups(loop));
        var faces = Find("Church", 0).Surface;
        var outer = Find("Church", 1).Surface;
        Assert.Equal(FamilyCodec.Encode(faces.Table), FamilyCodec.Encode(outer.Table));
        Assert.Equal(4, ClosedGroups(faces));
        Assert.Equal(1, ClosedGroups(outer));
        Assert.Equal(12, faces.Table.Template.LinkCount);
    }

    [Fact]
    public void CoincidentProjectedCrossingKeepsItsTwoComponentsAndSeparateInteriorSites()
    {
        var joined = Find("Cross", 0);
        var separate = Find("Cross", 1);
        Assert.Equal(1, Components(joined.Surface));
        Assert.Equal(2, Components(separate.Surface));
        var s = separate.Surface;
        var across = s.Uses.ReadGroup(s.Table, s.Entry, 0);
        var up = s.Uses.ReadGroup(s.Table, s.Entry, 1);
        Assert.NotEqual(across[0].Span.End, up[0].Span.End);
        Assert.Equal(separate.Points[across[0].Span.End.Local], separate.Points[up[0].Span.End.Local]);
        Assert.True(Outline(joined).SetEquals(Outline(separate)));
    }

    private static ConstructionExample Find(string pattern, int method) =>
        ConstructionAtlas.All.Where(e => e.Pattern == pattern).ElementAt(method);
    private static int ClosedGroups(ConstructionSurface surface) => Enumerable.Range(0, surface.Uses.GroupCount)
        .Count(group => surface.Uses.IsClosed(surface.Table, surface.Entry, group));

    private static HashSet<(double, double, double, double)> Outline(ConstructionExample example)
    {
        var result = new HashSet<(double, double, double, double)>();
        foreach (var span in example.Surface.Table.ReadLinks(example.Surface.Entry))
        {
            var a = example.Points[span.Start.Local]; var b = example.Points[span.End.Local];
            if (a.X > b.X || a.X == b.X && a.Y > b.Y) (a, b) = (b, a);
            result.Add((a.X, a.Y, b.X, b.Y));
        }
        return result;
    }

    private static int Components(ConstructionSurface surface)
    {
        var edges = surface.Table.ReadLinks(surface.Entry);
        var remaining = edges.SelectMany(e => new[] { e.Start.Local, e.End.Local }).ToHashSet();
        var count = 0;
        while (remaining.Count != 0)
        {
            count++;
            var pending = new Stack<int>(); pending.Push(remaining.First());
            while (pending.TryPop(out var site))
            {
                if (!remaining.Remove(site)) continue;
                foreach (var edge in edges)
                {
                    if (edge.Start.Local == site) pending.Push(edge.End.Local);
                    if (edge.End.Local == site) pending.Push(edge.Start.Local);
                }
            }
        }
        return count;
    }
}
