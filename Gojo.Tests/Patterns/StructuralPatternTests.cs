using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Patterns;

public sealed class StructuralPatternTests
{
    [Fact]
    public void BindingReplacesEveryMatchingPlaceholder()
    {
        var slot = new Placeholder();
        var carrier = new Carrier();
        var pattern = new Relation(slot, slot);

        var bound = StructuralPattern.Apply(pattern, StructuralPattern.Bind(slot, carrier));

        Assert.Equal(new Relation(carrier, carrier), bound);
    }

    [Fact]
    public void BindingCanReplaceAPlaceholderWithRelationStructure()
    {
        var sourceSlot = new Placeholder();
        var targetSlot = new Placeholder();
        var source = new Carrier();
        var target = new Carrier();
        var pattern = new Relation(sourceSlot, targetSlot);

        var bound = StructuralPattern.Apply(
            pattern,
            StructuralPattern.Bind(sourceSlot, Relation.Point(source, 10)),
            StructuralPattern.Bind(targetSlot, Relation.Point(target, 0)));

        Assert.Equal(
            new Relation(Relation.Point(source, 10), Relation.Point(target, 0)),
            bound);
    }

    [Fact]
    public void RepeatedPatternBuildsDecimalCarryChain()
    {
        var sourceSlot = new Placeholder();
        var targetSlot = new Placeholder();
        var ones = new Carrier();
        var tens = new Carrier();
        var hundreds = new Carrier();
        var carryPattern = new Relation(sourceSlot, targetSlot);

        var carries = StructuralPattern.Repeat(
            carryPattern,
            [
                StructuralPattern.Bind(sourceSlot, Relation.Point(ones, 10)),
                StructuralPattern.Bind(targetSlot, Relation.Point(tens, 0))
            ],
            [
                StructuralPattern.Bind(sourceSlot, Relation.Point(tens, 10)),
                StructuralPattern.Bind(targetSlot, Relation.Point(hundreds, 0))
            ]);

        foreach (var carry in carries.OfType<Relation>())
        {
            ones.Attach(carry);
        }

        ones.Add(375);

        Assert.Equal(5UL, ones.Value);
        Assert.Equal(7UL, tens.Value);
        Assert.Equal(3UL, hundreds.Value);
    }

    [Fact]
    public void RepeatedPatternCanPreserveSharedInputAsPowerLikeParticipation()
    {
        var slot = new Placeholder();
        var value = new Carrier();
        var copies = StructuralPattern.Repeat(
            slot,
            [StructuralPattern.Bind(slot, value)],
            [StructuralPattern.Bind(slot, value)],
            [StructuralPattern.Bind(slot, value)]);

        var chain = StructuralPattern.Chain(copies);

        Assert.Equal(new Relation(new Relation(value, value), value), chain);
    }

    [Fact]
    public void BindingPositivePlaceholderAlsoBindsDualFace()
    {
        var slot = new Placeholder();
        var carrier = new Carrier();
        var pattern = slot.Dual();

        var bound = StructuralPattern.Apply(pattern, StructuralPattern.Bind(slot, carrier));

        Assert.Same(carrier.Inverse(), bound);
    }

    [Fact]
    public void DescriptorBindingReplacesUnitShapeDescriptorSlotSurface()
    {
        var carrier = new Carrier();
        var slot = UnitShape.DescriptorSlot(0);
        var pattern = new Relation(slot, slot);

        var bound = StructuralPattern.Apply(pattern, StructuralPattern.BindDescriptor(0, carrier));

        Assert.Equal(new Relation(carrier, carrier), bound);
    }

    [Fact]
    public void DescriptorBindingCanGroundBareUnitShapeAlgebraSurface()
    {
        var descriptor = new Carrier();
        var replacement = new Carrier();
        var bare = UnitShape.DescribedAtom(descriptor).Bare();

        var bound = StructuralPattern.Apply(
            bare.AlgebraSurface,
            StructuralPattern.BindDescriptor(0, replacement));

        Assert.Same(replacement, bound);
    }

    [Fact]
    public void SlotReadRecognizesPlaceholdersAndDescriptorSlots()
    {
        var placeholder = new Placeholder();
        var descriptorSlot = UnitShape.DescriptorSlot(2);

        Assert.True(StructuralPattern.TryReadSlot(placeholder, out var readPlaceholder));
        Assert.Same(placeholder, readPlaceholder);
        Assert.True(StructuralPattern.TryReadSlot(descriptorSlot, out var readDescriptorSlot));
        Assert.Equal(UnitShape.DescriptorSlot(2), readDescriptorSlot);
    }

    [Fact]
    public void SlotDiscoveryReadsDistinctOpenSurfacesInTraversalOrder()
    {
        var first = new Placeholder();
        var second = new Placeholder();
        var descriptor = UnitShape.DescriptorSlot(0);
        var pattern = new Relation(new Relation(first, descriptor), new Relation(first, second));

        var slots = StructuralPattern.ReadSlots(pattern);

        Assert.Equal([first, descriptor, second], slots);
    }

    [Fact]
    public void SlotBindingReadKeepsFirstBindingAndIgnoresClosedRelations()
    {
        var slot = new Placeholder();
        var first = new Carrier();
        var second = new Carrier();
        var closed = new Relation(new Carrier(), new Carrier());

        var bindings = StructuralPattern.ReadSlotBindings(
            StructuralPattern.Bind(slot, first),
            StructuralPattern.Bind(slot, second),
            closed);

        Assert.Equal([StructuralPattern.Bind(slot, first)], bindings);
    }

    [Fact]
    public void PartialApplicationLeavesUnboundSlotsInspectable()
    {
        var boundSlot = new Placeholder();
        var openSlot = new Placeholder();
        var descriptorSlot = UnitShape.DescriptorSlot(0);
        var boundValue = new Carrier();
        var pattern = new Relation(new Relation(boundSlot, openSlot), descriptorSlot);

        var complete = StructuralPattern.TryApplyComplete(
            pattern,
            out var applied,
            out var unbound,
            StructuralPattern.Bind(boundSlot, boundValue));

        Assert.False(complete);
        Assert.Equal(new Relation(new Relation(boundValue, openSlot), descriptorSlot), applied);
        Assert.Equal([openSlot, descriptorSlot], unbound);
    }

    [Fact]
    public void CompleteFrameReportsNoUnboundSlots()
    {
        var firstSlot = new Placeholder();
        var secondSlot = UnitShape.DescriptorSlot(0);
        var firstValue = new Carrier();
        var secondValue = new Carrier();
        var pattern = new Relation(firstSlot, secondSlot);
        var bindings = new[]
        {
            StructuralPattern.Bind(firstSlot, firstValue),
            StructuralPattern.BindDescriptor(0, secondValue)
        };

        Assert.True(StructuralPattern.IsComplete(pattern, bindings));
        Assert.True(StructuralPattern.TryApplyComplete(pattern, bindings, out var applied, out var unbound));
        Assert.Equal(new Relation(firstValue, secondValue), applied);
        Assert.Empty(unbound);
    }

    [Fact]
    public void AdvanceMovesCurrentBindingThroughSlotToSlotTransition()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Carrier();
        var second = new Carrier();
        var current = new[]
        {
            StructuralPattern.Bind(a, first),
            StructuralPattern.Bind(b, second)
        };

        var next = StructuralPattern.Advance(
            current,
            StructuralPattern.Bind(a, b),
            StructuralPattern.Bind(b, a));

        Assert.Equal(
            [
                StructuralPattern.Bind(a, second),
                StructuralPattern.Bind(b, first)
            ],
            next);
    }

    [Fact]
    public void AdvanceCanBindNextSlotToCurrentStructuralExpression()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Carrier();
        var second = new Carrier();
        var current = new[]
        {
            StructuralPattern.Bind(a, first),
            StructuralPattern.Bind(b, second)
        };
        var pairExpression = new Relation(a, b);

        var next = StructuralPattern.Advance(
            current,
            StructuralPattern.Bind(a, b),
            StructuralPattern.Bind(b, pairExpression));

        Assert.Equal(
            [
                StructuralPattern.Bind(a, second),
                StructuralPattern.Bind(b, new Relation(first, second))
            ],
            next);
    }

    [Fact]
    public void AdvanceCanReadDescriptorSlotFromCurrentFrame()
    {
        var a = new Placeholder();
        var descriptorValue = new Carrier();
        var current = new[]
        {
            StructuralPattern.BindDescriptor(0, descriptorValue)
        };

        var next = StructuralPattern.Advance(
            current,
            StructuralPattern.Bind(a, UnitShape.DescriptorSlot(0)));

        Assert.Equal([StructuralPattern.Bind(a, descriptorValue)], next);
    }

    [Fact]
    public void AdvanceCanWriteDescriptorSlotInNextFrame()
    {
        var a = new Placeholder();
        var value = new Carrier();
        var current = new[]
        {
            StructuralPattern.Bind(a, value)
        };

        var next = StructuralPattern.Advance(
            current,
            StructuralPattern.BindDescriptor(0, a));

        Assert.Equal([StructuralPattern.BindDescriptor(0, value)], next);
    }

    [Fact]
    public void RepeatedAdvanceCanGrowFibonacciLikeStateStructure()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Carrier();
        var second = new Carrier();
        var transitions = new[]
        {
            StructuralPattern.Bind(a, b),
            StructuralPattern.Bind(b, new Relation(a, b))
        };
        var frame = new[]
        {
            StructuralPattern.Bind(a, first),
            StructuralPattern.Bind(b, second)
        };

        frame = StructuralPattern.Advance(frame, transitions);
        frame = StructuralPattern.Advance(frame, transitions);

        Assert.Equal(
            [
                StructuralPattern.Bind(a, new Relation(first, second)),
                StructuralPattern.Bind(b, new Relation(second, new Relation(first, second)))
            ],
            frame);
    }
}
