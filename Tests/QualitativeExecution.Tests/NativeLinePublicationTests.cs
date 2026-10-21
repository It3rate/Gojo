using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class NativeLinePublicationTests
{
    private static readonly Lazy<IReadOnlyList<NativeLatticeSources.SourceCase>> Cases = new(NativeLatticeSources.Cases);

    [Fact]
    public void Publication_preserves_actual_incidence_and_reach_aliases_with_fresh_site_and_stroke_identities()
    {
        var original = Case("N02"); var publication = new NativeLinePublication(original); var published = publication.Published;
        Assert.Same(original, publication.Original); Assert.Same(original.Graph.Frame, published.Graph.Frame);
        Assert.Equal(E(original.Graph.Sites).Length, E(published.Graph.Sites).Length);
        Assert.Equal(E(original.Graph.Strokes).Length, E(published.Graph.Strokes).Length);
        foreach (var site in E(original.Graph.Sites))
        {
            Assert.True(publication.TryMapSite(site, out var replacement)); Assert.NotSame(site, replacement);
            Assert.Contains(E(published.Graph.Sites), actual => ReferenceEquals(actual, replacement));
        }
        foreach (var stroke in E(original.Graph.Strokes).Cast<Relation>())
        {
            Assert.True(publication.TryMapStroke(stroke, out var replacement)); Assert.NotSame(stroke, replacement);
            Assert.True(QualitativeStrokeGraph.TryReadStroke(stroke, out var oldCause, out var from, out var to, out var word));
            Assert.True(QualitativeStrokeGraph.TryReadStroke(replacement, out var cause, out var a, out var b, out var publicWord));
            Assert.IsType<Placeholder>(cause); Assert.NotSame(oldCause, cause);
            Assert.True(publication.TryMapSite(from, out var aa)); Assert.Same(aa, a);
            Assert.True(publication.TryMapSite(to, out var bb)); Assert.Same(bb, b);
            Assert.Equal(E(word).Length, E(publicWord).Length);
            foreach (var pair in E(word).Cast<Relation>().Zip(E(publicWord).Cast<Relation>()))
            {
                Assert.Same(pair.First.Left, pair.Second.Left);
                Assert.True(publication.TryMapReach(pair.First.Right, out var reach)); Assert.Same(reach, pair.Second.Right);
            }
            Assert.Contains(E(published.StraightPassages), actual => ReferenceEquals(actual, replacement));
        }
        Assert.Single(E(publication.ReachMap)); Assert.Empty(E(publication.Unresolved));
        Assert.Empty(E(published.Provenance));
    }

    [Fact]
    public void Withheld_endpoint_binding_cannot_be_recovered_from_published_stroke_ancestry()
    {
        var original = Case("N05"); var publication = new NativeLinePublication(original); var published = publication.Published;
        var hidden = E(original.Bindings).Cast<Relation>().Single(row => !Closed((Relation)row.Right));
        Assert.True(publication.TryMapSite(hidden.Left, out var site));
        var offered = Assert.Single(E(published.Bindings).Cast<Relation>(), row => ReferenceEquals(row.Left, site));
        Assert.False(Closed((Relation)offered.Right));
        var publicRoot = new Relation(published.Graph.Surface, new Relation(published.Bindings,
            new Relation(published.StraightPassages, published.Provenance)));
        var reachable = OrnamentReconstruction.Participants(publicRoot);
        Assert.DoesNotContain(hidden.Left, reachable); Assert.DoesNotContain(hidden, reachable);
        foreach (var stroke in E(original.Graph.Strokes)) Assert.DoesNotContain(stroke, reachable);
        var sameSiteBindings = reachable.OfType<Relation>().Where(row => ReferenceEquals(row.Left, site) &&
            row.Right is Relation word && StructuralConstraint.TryReadScope(word, out var terms, out _, out _) &&
            terms.All(term => term is Relation move && published.Graph.Frame.Direction(move, out _, out _, out _))).ToArray();
        Assert.Contains(offered, sameSiteBindings);
        Assert.DoesNotContain(sameSiteBindings, row => Closed((Relation)row.Right));
        // The separate audit still holds the correspondence needed to explain
        // the abstraction; that audit is not part of this publication.
        Assert.Contains(original.Graph.Surface, OrnamentReconstruction.Participants(publication.Surface));
        Assert.DoesNotContain(publication.SiteMap, reachable);
    }

    [Fact]
    public void Opaque_reaches_do_not_export_nested_private_structure_or_destroy_shared_extent_identity()
    {
        var original = Case("N02"); var secret = new Placeholder();
        var privateExtent = new Relation(secret, original.Bindings);
        var source = Reaches(original, _ => privateExtent);
        var publication = new NativeLinePublication(source);
        Assert.True(publication.TryMapReach(privateExtent, out var extent)); Assert.IsType<Placeholder>(extent);
        var reachable = OrnamentReconstruction.Participants(new Relation(publication.Published.Graph.Surface, publication.Published.Bindings));
        Assert.DoesNotContain(privateExtent, reachable); Assert.DoesNotContain(secret, reachable); Assert.DoesNotContain(original.Bindings, reachable);
        Assert.Single(E(publication.ReachMap));
        var allReaches = E(publication.Published.Graph.Strokes).Cast<Relation>().SelectMany(stroke => {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word); return E(word).Cast<Relation>().Select(move => move.Right); }).ToArray();
        Assert.All(allReaches, reach => Assert.Same(extent, reach));
    }

    [Fact]
    public void Independent_reach_atoms_and_placeholder_duals_remain_distinct_under_publication()
    {
        var original = Case("N02"); var extent = new Placeholder();
        var source = Reaches(original, move => original.Graph.Frame.Direction(move, out var axis, out _, out _) &&
            ReferenceEquals(axis, original.Graph.Frame.Horizontal) ? extent : extent.Dual());
        var publication = new NativeLinePublication(source);
        Assert.True(publication.TryMapReach(extent, out var direct)); Assert.True(publication.TryMapReach(extent.Dual(), out var inverse));
        Assert.Equal(2, E(publication.ReachMap).Length); Assert.NotSame(direct, inverse);
        Assert.Equal(Assert.IsType<Placeholder>(direct).Dual(), Assert.IsType<Placeholder>(inverse));
        Assert.False(((Placeholder)direct).IsInverse); Assert.True(((Placeholder)inverse).IsInverse);
    }

    [Fact]
    public void Open_and_malformed_participation_stays_visible_without_exporting_the_original_bad_rows()
    {
        var original = Case("N02"); var privateMarker = new Placeholder();
        var malformed = new Relation(privateMarker, original.Provenance);
        var source = original with { Graph = new(original.Graph.Frame, original.Graph.Sites,
            StructuralConstraint.Open(E(original.Graph.Strokes).Append(malformed).ToArray())),
            StraightPassages = StructuralConstraint.Open(E(original.StraightPassages)) };
        var publication = new NativeLinePublication(source); var published = publication.Published;
        Assert.False(Closed(published.Graph.Strokes)); Assert.False(Closed(published.StraightPassages));
        Assert.Equal(7, E(published.Graph.Strokes).Length); Assert.NotEmpty(E(publication.Unresolved));
        Assert.True(publication.TryMapStroke(malformed, out var opaqueRow));
        Assert.False(QualitativeStrokeGraph.TryReadStroke(opaqueRow, out _, out _, out _, out _));
        var reachable = OrnamentReconstruction.Participants(new Relation(published.Graph.Surface, new Relation(published.Bindings, published.StraightPassages)));
        Assert.DoesNotContain(malformed, reachable); Assert.DoesNotContain(privateMarker, reachable);
        Assert.DoesNotContain(original.Provenance, reachable);
    }

    [Fact]
    public void Public_native_reads_keep_the_same_selected_square_and_rectangle_results()
    {
        foreach (var id in new[] { "N02", "N03", "N04", "N05", "N07", "N09" })
        {
            var original = Case(id); var published = new NativeLinePublication(original).Published;
            var before = new NativeLineSupplier(original); var after = new NativeLineSupplier(published);
            Assert.Equal(before.Candidates.Count, after.Candidates.Count);
            for (var i = 0; i < before.Candidates.Count; i++)
                for (var goal = 0; goal < before.Candidates[i].Frame.Goals.Count; goal++)
                    Assert.Equal(before.Candidates[i].Read(before.Candidates[i].Frame.Goals[goal].Predicate).Answer,
                        after.Candidates[i].Read(after.Candidates[i].Frame.Goals[goal].Predicate).Answer);
        }
    }

    private static NativeLatticeSources.SourceCase Reaches(NativeLatticeSources.SourceCase source, Func<Relation, IElement> extent)
    {
        Relation Word(Relation word) => S(E(word).Cast<Relation>().Select(move => (IElement)new Relation(move.Left, extent(move))).ToArray());
        var strokes = E(source.Graph.Strokes).Cast<Relation>().Select(stroke => {
            QualitativeStrokeGraph.TryReadStroke(stroke, out var identity, out var from, out var to, out var word);
            return (IElement)QualitativeStrokeGraph.Stroke(identity, from, to, Word(word)); }).ToArray();
        return source with { Graph = new(source.Graph.Frame, source.Graph.Sites, S(strokes)), StraightPassages = S(strokes),
            Bindings = S(E(source.Bindings).Cast<Relation>().Select(binding => (IElement)new Relation(binding.Left, Word((Relation)binding.Right))).ToArray()) };
    }
    private static NativeLatticeSources.SourceCase Case(string id) => Cases.Value.Single(item => item.Id == id);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var head, out var tail) && head.Equals(tail);
}
