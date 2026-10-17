using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicParameterSurfaceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void NormalizedValueDoesNotReadAnUnrelatedOrMissingParameterFrame(int malformed)
    {
        var parameter = new Carrier();
        var other = new Carrier(parameter.UnitShape);
        IElement domain = malformed switch
        {
            0 => new Relation(new Placeholder(), new Placeholder()),
            1 => Relation.Segment(parameter, 0, 0),
            2 => Relation.Segment(parameter, 100, 0),
            3 => Relation.Segment(parameter, 10, 100),
            4 => new Relation(Relation.Point(parameter, 0), Relation.Point(other, 100)),
            _ => Relation.Segment(parameter, 0, 100)
        };
        var surface = Relation.OrderedGroup(new Carrier(),
            Relation.Segment(new Carrier(), 10, 210), domain,
            Relation.Point(malformed == 5 ? other : parameter, 40),
            Relation.Point(malformed == 6 ? other : parameter, 47),
            new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder());

        Assert.False(GraphicParameterSurface.TryReadNormalizedValue(surface,
            out _, out _, out _, out _, out _, out _, out _, out _));
        Assert.True(surface.TryReadOrderedGroup(out var retained));
        Assert.Same(domain, retained[1]);
    }

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
    public void NormalizedValueCanBuildResolutionEvidenceFromRefinedTicks()
    {
        var basis = Relation.Segment(new Carrier(), 0, 100);
        var coarseResult = Relation.Point(new Carrier(), 40);
        var refinedResult = Relation.Point(new Carrier(), 47);

        var value = GraphicParameterSurface.NormalizedValue(
            new Carrier(),
            basis,
            refinedTicks: 47,
            scaleTicks: 100,
            levelScales: [1, 8, 40],
            coarseResult,
            refinedResult,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(GraphicParameterSurface.TryReadNormalizedValue(
            value,
            out var readBasis,
            out _,
            out var coarse,
            out var refined,
            out var readCoarseResult,
            out var readRefinedResult,
            out var readResolution,
            out _));
        Assert.Same(basis, readBasis);
        Assert.Equal(40, coarse.Ticks);
        Assert.Equal(47, refined.Ticks);
        Assert.Same(coarseResult, readCoarseResult);
        Assert.Same(refinedResult, readRefinedResult);
        var resolution = Assert.IsType<Relation>(readResolution);
        var holder = Assert.IsType<Carrier>(resolution.Right);
        Assert.True(ResolutionLadder.TryReadLevels(resolution, holder, out var levels));
        Assert.Equal(47, ResolutionLadder.ReadComposite(levels, [1, 8, 40]));
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

    [Fact]
    public void TemplateInstanceCanCreateBindingFrameFromSlotBindings()
    {
        var center = new Placeholder();
        var radius = new Placeholder();
        var template = GraphicParameterSurface.Frame(new Carrier(), center, radius);
        var concreteCenter = Relation.Point(new Carrier(), 12);
        var concreteRadius = Relation.Segment(new Carrier(), 0, 5);
        var result = new Relation(concreteCenter, concreteRadius);

        var instance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            template,
            result,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Bind(center, concreteCenter),
            FocusSurface.Bind(radius, concreteRadius));

        Assert.True(GraphicParameterSurface.TryReadTemplateInstance(
            instance,
            out _,
            out var readBindings,
            out var readResult,
            out _));
        Assert.True(FocusSurface.TryReadBindingFrame((Relation)readBindings, out var bindings));
        Assert.Equal(2, bindings.Length);
        Assert.Same(result, readResult);
    }

    [Fact]
    public void TemplateInstanceRequiresBoundTemplateSlot()
    {
        var slot = new Placeholder();
        var template = GraphicParameterSurface.Frame(new Carrier(), slot);
        var unrelated = new Placeholder();
        var result = Relation.Point(new Carrier(), 1);
        var instance = GraphicParameterSurface.TemplateInstance(
            new Carrier(),
            template,
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(unrelated, result)),
            result,
            Relation.Point(Carrier.Unknown, 0));

        Assert.False(GraphicParameterSurface.TryReadTemplateInstance(
            instance,
            out _,
            out _,
            out _,
            out _));
    }
}
