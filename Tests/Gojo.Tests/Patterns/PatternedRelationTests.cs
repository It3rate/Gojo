using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Patterns;

public sealed class PatternedRelationTests
{
    [Fact]
    public void PlaceholderPortPatternProjectsThroughBoundSurfaces()
    {
        var sourceSurface = new Placeholder();
        var targetSurface = new Placeholder();
        var source = new Carrier();
        var target = new Carrier();
        var portPattern = new Relation(sourceSurface, targetSurface);
        source.Set(27);

        Assert.True(PatternedRelation.TryProject(
            portPattern,
            source.ValueSlot,
            target,
            out var projected,
            StructuralPattern.Bind(sourceSurface, Relation.Segment(source, 0, 10)),
            StructuralPattern.Bind(targetSurface, Relation.Point(target, 1))));

        Assert.Equal(
            new Relation(new CarrierLocation(target, 2), new CarrierLocation(source, 7)),
            projected);
    }

    [Fact]
    public void DescriptorSlotPortPatternProjectsThroughBoundSurfaces()
    {
        var source = new Carrier();
        var target = new Carrier();
        var portPattern = new Relation(UnitShape.DescriptorSlot(0), UnitShape.DescriptorSlot(1));
        source.Set(27);

        Assert.True(PatternedRelation.TryProject(
            portPattern,
            source.ValueSlot,
            target,
            out var projected,
            StructuralPattern.BindDescriptor(0, Relation.Segment(source, 0, 10)),
            StructuralPattern.BindDescriptor(1, Relation.Point(target, 1))));

        Assert.Equal(
            new Relation(new CarrierLocation(target, 2), new CarrierLocation(source, 7)),
            projected);
    }

    [Fact]
    public void PatternedRelationReportsUnboundSlotsWithoutRejectingPartialRelation()
    {
        var firstSlot = new Placeholder();
        var secondSlot = UnitShape.DescriptorSlot(0);
        var source = new Carrier();
        var relationPattern = new Relation(firstSlot, secondSlot);

        var complete = PatternedRelation.TryInstantiateComplete(
            relationPattern,
            out var relation,
            out var unbound,
            StructuralPattern.Bind(firstSlot, source));

        Assert.False(complete);
        Assert.Equal(new Relation(source, secondSlot), relation);
        Assert.Equal([secondSlot], unbound);
    }

    [Fact]
    public void PlaceholderOperationPatternReadsBoundAddition()
    {
        var xPort = new Placeholder();
        var yPort = new Placeholder();
        var output = new Placeholder();
        var xInput = new Placeholder();
        var yInput = new Placeholder();
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var operationPattern = new Relation(new Relation(xPort, yPort), output);
        var inputPattern = new Relation(xInput, yInput);
        x.Set(3);
        y.Set(4);

        Assert.True(PatternedRelation.TryReadOperationFrom(
            operationPattern,
            inputPattern,
            result,
            out var read,
            StructuralPattern.Bind(xPort, new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(yPort, new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(output, Relation.Point(result, 1)),
            StructuralPattern.Bind(xInput, x.ValueSlot),
            StructuralPattern.Bind(yInput, y.ValueSlot)));

        Assert.Equal(
            new Relation(
                new CarrierLocation(result, 7),
                new Relation(new CarrierLocation(x, 0), new CarrierLocation(y, 0))),
            read);
    }

    [Fact]
    public void PlaceholderOperationPatternCrystallizesProductUnitShape()
    {
        var widthPort = new Placeholder();
        var heightPort = new Placeholder();
        var output = new Placeholder();
        var lengthShape = new Carrier().UnitShape;
        var width = new Carrier(lengthShape);
        var height = new Carrier(lengthShape);
        var areaBasis = new Carrier();
        var operationPattern = new Relation(new Relation(widthPort, heightPort), output);

        Assert.True(PatternedRelation.TryCrystallize(
            operationPattern,
            out var crystal,
            StructuralPattern.Bind(widthPort, new Relation(Relation.Point(width, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(heightPort, new Relation(Relation.Point(height, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(output, Relation.Segment(areaBasis, 0, 1))));

        Assert.True(crystal.UnitShape.CompatibleWith(UnitShape.Product(lengthShape, lengthShape)));
    }

    [Fact]
    public void RepeatedInputFramesCanFeedOnePlaceholderOperationPattern()
    {
        var xPort = new Placeholder();
        var yPort = new Placeholder();
        var output = new Placeholder();
        var xInput = new Placeholder();
        var yInput = new Placeholder();
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var operationPattern = new Relation(new Relation(xPort, yPort), output);
        var inputPattern = new Relation(xInput, yInput);
        var fixedBindings = new[]
        {
            StructuralPattern.Bind(xPort, new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(yPort, new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1))),
            StructuralPattern.Bind(output, Relation.Point(result, 1))
        };
        var frames = new[]
        {
            new[]
            {
                StructuralPattern.Bind(xInput, new CarrierLocation(x, 1)),
                StructuralPattern.Bind(yInput, new CarrierLocation(y, 2))
            },
            new[]
            {
                StructuralPattern.Bind(xInput, new CarrierLocation(x, 3)),
                StructuralPattern.Bind(yInput, new CarrierLocation(y, 5))
            }
        };

        var reads = frames
            .Select(frame =>
            {
                Assert.True(PatternedRelation.TryReadOperationFrom(
                    operationPattern,
                    inputPattern,
                    result,
                    [.. fixedBindings, .. frame],
                    out var read));
                return read;
            })
            .ToArray();

        Assert.Equal(new CarrierLocation(result, 3), Assert.IsType<CarrierLocation>(reads[0].Left));
        Assert.Equal(new CarrierLocation(result, 8), Assert.IsType<CarrierLocation>(reads[1].Left));
    }
}
