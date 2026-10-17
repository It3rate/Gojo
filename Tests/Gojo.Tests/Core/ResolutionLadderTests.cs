using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class ResolutionLadderTests
{
    [Fact]
    public void LadderCreatesInspectableRootedResolutionStructure()
    {
        var root = new Carrier();
        var structure = ResolutionLadder.AttachCarry(
            root,
            levelCount: 4,
            stepTicks: 10,
            out var levels,
            out var steps);

        Assert.True(ResolutionLadder.TryReadLevels(structure, root, out var readLevels));
        Assert.True(ResolutionLadder.TryReadSteps(structure, root, out var readSteps));

        Assert.Equal(4, readLevels.Length);
        Assert.Equal(3, readSteps.Length);
        Assert.All(readLevels, level => Assert.True(level.UnitShape.CompatibleWith(root.UnitShape)));
        Assert.Equal(levels, readLevels);
        Assert.Equal(steps, readSteps);
    }

    [Fact]
    public void DecimalLadderCarriesThroughExplicitRelations()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 4, stepTicks: 10, out var levels, out _);

        ResolutionLadder.AddToLevel(levels, levelIndex: 0, ticks: 396);

        Assert.Equal(6UL, levels[0].Value);
        Assert.Equal(9UL, levels[1].Value);
        Assert.Equal(3UL, levels[2].Value);
        Assert.Equal(0UL, levels[3].Value);
        Assert.Equal(396, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
    }

    [Fact]
    public void LadderCanSetCompositeDigitsFromHighestResolution()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 3, stepTicks: 10, out var levels, out _);

        ResolutionLadder.SetDigitsFrom(levels, highestLevelIndex: 2, 3, 9, 6);

        Assert.Equal(6UL, levels[0].Value);
        Assert.Equal(9UL, levels[1].Value);
        Assert.Equal(3UL, levels[2].Value);
        Assert.Equal(396, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
    }

    [Fact]
    public void PartialDigitsRepresentLowerResolutionValueWithoutInventingDetail()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 3, stepTicks: 10, out var levels, out _);

        ResolutionLadder.SetDigitsFrom(levels, highestLevelIndex: 2, 3, 9);

        Assert.Equal(0UL, levels[0].Value);
        Assert.Equal(9UL, levels[1].Value);
        Assert.Equal(3UL, levels[2].Value);
        Assert.Equal(390, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
        Assert.Equal(90, ResolutionLadder.ReadLevelOnRoot(root, levels, radix: 10, levelIndex: 1).Ticks);
    }

    [Fact]
    public void OneLevelCanActAsPresenceOrLowResolutionFill()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 3, stepTicks: 10, out var levels, out _);

        ResolutionLadder.SetLevel(levels, levelIndex: 0, ticks: 1);

        Assert.Equal(1, ResolutionLadder.ReadCompositeFromRadix(levels, highestLevelIndex: 0, radix: 10));
        Assert.Equal(1UL, levels[0].Value);
        Assert.Equal(0UL, levels[1].Value);
        Assert.Equal(0UL, levels[2].Value);
    }

    [Fact]
    public void CompositePrefixProjectsThroughWholeFrameAsPercentage()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 2, stepTicks: 10, out var levels, out _);
        var percent = new Carrier();
        ResolutionLadder.SetDigitsFrom(levels, highestLevelIndex: 1, 4, 5);

        var read = ResolutionLadder.ProjectCompositeTo(
            root,
            levels,
            highestLevelIndex: 1,
            radix: 10,
            target: percent,
            targetStart: 0,
            targetEnd: 100);

        Assert.True(read.TryGetCarrierLocations(out var projected, out var residue));
        Assert.Equal(percent, projected.Carrier);
        Assert.Equal(45, projected.Ticks);
        Assert.Equal(root, residue.Carrier);
        Assert.Equal(0, residue.Ticks);
    }

    [Fact]
    public void CoarseLevelCanProjectPercentageInCoarseSteps()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 2, stepTicks: 10, out var levels, out _);
        var percent = new Carrier();
        ResolutionLadder.SetDigitsFrom(levels, highestLevelIndex: 1, 4);

        var read = ResolutionLadder.ProjectCompositeTo(
            root,
            levels,
            highestLevelIndex: 1,
            radix: 10,
            target: percent,
            targetStart: 0,
            targetEnd: 100);

        Assert.True(read.TryGetCarrierLocations(out var projected, out _));
        Assert.Equal(40, projected.Ticks);
        Assert.Equal(40, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
        Assert.Equal(0UL, levels[0].Value);
        Assert.Equal(4UL, levels[1].Value);
    }

    [Fact]
    public void BinaryLadderUsesSameStructureWithDifferentRadix()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 4, stepTicks: 2, out var levels, out _);

        ResolutionLadder.AddToLevel(levels, levelIndex: 0, ticks: 13);

        Assert.Equal(1UL, levels[0].Value);
        Assert.Equal(0UL, levels[1].Value);
        Assert.Equal(1UL, levels[2].Value);
        Assert.Equal(1UL, levels[3].Value);
        Assert.Equal(13, ResolutionLadder.ReadCompositeFromRadix(levels, 2));
    }

    [Fact]
    public void LadderCanCreateArbitraryStructuralRatioSteps()
    {
        var root = new Carrier();
        var structure = ResolutionLadder.AttachRatios(
            root,
            [(2, 0), (10, 1), (-1, 0)],
            out var levels,
            out var steps);

        Assert.True(ResolutionLadder.TryReadLevels(structure, root, out var readLevels));
        Assert.True(ResolutionLadder.TryReadSteps(structure, root, out var readSteps));
        Assert.Equal(levels, readLevels);
        Assert.Equal(steps, readSteps);
        Assert.Equal(-1, ((Relation)steps[2].Left).LeftLocation.Ticks);
        Assert.Equal(1, ((Relation)steps[1].Right).LeftLocation.Ticks);
    }

    [Fact]
    public void ZeroLevelAttachReturnsInspectableRootRoute()
    {
        var root = new Carrier();
        var structure = ResolutionLadder.AttachCarry(
            root,
            levelCount: 0,
            stepTicks: 10,
            out var levels,
            out var steps);

        Assert.Empty(levels);
        Assert.Empty(steps);
        Assert.True(ResolutionLadder.TryReadLevels(structure, root, out var readLevels));
        Assert.True(ResolutionLadder.TryReadSteps(structure, root, out var readSteps));
        Assert.Empty(readLevels);
        Assert.Empty(readSteps);
        Assert.Equal(0, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
    }

    [Fact]
    public void StepInputsRepeatLastAvailableValueAndIgnoreExtraValues()
    {
        var root = new Carrier();
        var levels = ResolutionLadder.CreateLevels(root, 4);

        var repeated = ResolutionLadder.CreateCarrySteps(levels, [2, 10]);
        var truncated = ResolutionLadder.CreateCarrySteps(levels, [2, 10, 100, 1000, 10000]);

        Assert.Equal(3, repeated.Length);
        Assert.Equal(3, truncated.Length);
        Assert.Equal((0, 2, 0), ReadCarryShape(repeated[0]));
        Assert.Equal((0, 10, 0), ReadCarryShape(repeated[1]));
        Assert.Equal((0, 10, 0), ReadCarryShape(repeated[2]));
        Assert.Equal((0, 100, 0), ReadCarryShape(truncated[2]));
    }

    [Fact]
    public void ExtraDigitsAndIndexesAreForgivingReads()
    {
        var root = new Carrier();
        ResolutionLadder.AttachCarry(root, levelCount: 3, stepTicks: 10, out var levels, out _);

        ResolutionLadder.SetDigitsFrom(levels, highestLevelIndex: 1, 4, 5, 6, 7);

        Assert.Equal(5UL, levels[0].Value);
        Assert.Equal(4UL, levels[1].Value);
        Assert.Equal(0UL, levels[2].Value);
        Assert.Equal(45, ResolutionLadder.ReadCompositeFromRadix(levels, 10));
        Assert.Equal(0, ResolutionLadder.ReadLevelOnRoot(root, levels, radix: 10, levelIndex: 9).Ticks);
    }

    [Fact]
    public void RepeatedRadixAllowsZeroAndNegativeSteps()
    {
        Assert.Empty(ResolutionLadder.RepeatedRadixScales(0, 10));
        Assert.Equal([1, 0, 0], ResolutionLadder.RepeatedRadixScales(3, 0));
        Assert.Equal([1, -2, 4, -8], ResolutionLadder.RepeatedRadixScales(4, -2));
        Assert.Equal(0, ResolutionLadder.SpanFromRadix(0, 10));
        Assert.Equal(-8, ResolutionLadder.SpanFromRadix(3, -2));
    }

    [Fact]
    public void CompositeReadCanUseExplicitNonRadixScales()
    {
        var root = new Carrier();
        var levels = ResolutionLadder.CreateLevels(root, 3);
        var structure = ResolutionLadder.Attach(root, levels, []);
        ResolutionLadder.SetDigitsLowToHigh(levels, 4, 3, 2);

        Assert.True(ResolutionLadder.TryReadLevels(structure, root, out var readLevels));
        Assert.Equal(34, ResolutionLadder.ReadComposite(readLevels, [1, 10, 0]));
    }

    private static (long SourceStart, long SourceEnd, long ReceiverTicks) ReadCarryShape(Relation step)
    {
        var source = (Relation)step.Left;
        var sourceStart = ((Relation)source.Left).LeftLocation.Ticks;
        var sourceEnd = ((Relation)source.Right).LeftLocation.Ticks;
        var receiver = ((Relation)step.Right).LeftLocation.Ticks;
        return (sourceStart, sourceEnd, receiver);
    }
}
