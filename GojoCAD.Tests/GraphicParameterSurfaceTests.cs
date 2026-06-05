using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicParameterSurfaceTests
{
    [Fact]
    public void NormalizedValueCarriesCoarseRefinedResultsAndResolutionLadder()
    {
        var basis = Relation.Segment(new Carrier(), 0, 1);
        var coarseResult = Relation.Point(new Carrier(), 40);
        var refinedResult = Relation.Point(new Carrier(), 47);
        var resolution = GraphicParameterSurface.ResolutionValue(
            new Carrier(),
            [1, 8, 40],
            [7, 0, 1],
            out var levels,
            out var composite);

        var value = GraphicParameterSurface.NormalizedValue(
            new Carrier(),
            basis,
            new Carrier(),
            scaleTicks: 100,
            coarseTicks: 40,
            refinedTicks: composite,
            coarseResult,
            refinedResult,
            resolution,
            Relation.Point(Carrier.Unknown, 0));

        Assert.Equal(47, composite);
        Assert.Equal(3, levels.Length);
        Assert.True(GraphicParameterSurface.TryReadNormalizedValue(
            value,
            out var readBasis,
            out var span,
            out var coarse,
            out var refined,
            out var readCoarseResult,
            out var readRefinedResult,
            out var readResolution,
            out _));
        Assert.Same(basis, readBasis);
        Assert.Equal(Relation.Segment(coarse.Carrier, 0, 100), span);
        Assert.True(coarse.Carrier.SameIdentityAs(refined.Carrier));
        Assert.Equal(40, coarse.Ticks);
        Assert.Equal(47, refined.Ticks);
        Assert.Same(coarseResult, readCoarseResult);
        Assert.Same(refinedResult, readRefinedResult);
        Assert.Same(resolution, readResolution);
        var holder = Assert.IsType<Carrier>(resolution.Right);
        Assert.True(ResolutionLadder.TryReadLevels(resolution, holder, out var readLevels));
        Assert.Equal(levels, readLevels);
    }

    [Fact]
    public void TemplateInstanceCarriesTemplateBindingsAndResult()
    {
        var center = new Placeholder();
        var radius = new Placeholder();
        var template = GraphicParameterSurface.Frame(new Carrier(), center, radius);
        var concreteCenter = Relation.Point(new Carrier(), 12);
        var concreteRadius = Relation.Segment(new Carrier(), 0, 5);
        var result = new Relation(concreteCenter, concreteRadius);
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(center, concreteCenter),
            FocusSurface.Bind(radius, concreteRadius));

        var instance = GraphicParameterSurface.TemplateInstance(
            new Carrier(),
            template,
            bindings,
            result,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(GraphicParameterSurface.TryReadTemplateInstance(
            instance,
            out var readTemplate,
            out var readBindings,
            out var readResult,
            out _));
        Assert.Same(template, readTemplate);
        Assert.Same(bindings, readBindings);
        Assert.Same(result, readResult);
    }
}
