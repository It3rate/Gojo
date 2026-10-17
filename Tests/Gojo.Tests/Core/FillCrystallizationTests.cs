using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class FillCrystallizationTests
{
    [Fact]
    public void FillMeasureReadsCurrentBoundaryWithoutCrystallizing()
    {
        var line = new Carrier();
        var start = new TickRef(2);
        var end = new TickRef(5);
        var segment = new Relation(
            new Relation(new CarrierLocation(line, start), line),
            new Relation(new CarrierLocation(line, end), line));
        var reader = new Carrier(line.UnitShape);

        Assert.True(segment.TryReadFillMeasureOn(reader, out var measure));
        Assert.Equal(3, measure.Ticks);
        Assert.True(measure.Carrier.SameIdentityAs(reader));
        Assert.Equal(0UL, reader.Value);

        end.Set(9);

        Assert.True(segment.TryReadFillMeasureOn(reader, out var reread));
        Assert.Equal(7, reread.Ticks);
        Assert.True(reread.Carrier.SameIdentityAs(reader));
        Assert.Equal(0UL, reader.Value);
    }

    [Fact]
    public void SegmentCrystallizesAsFilledIntervalCarrier()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 2, 7);

        Assert.True(segment.TryCrystallizeFill(out var fill));

        Assert.Equal(5UL, fill.Value);
        Assert.False(fill.IsInverse);
        Assert.False(fill.SameIdentityAs(line));
        Assert.True(fill.UnitShape.CompatibleWith(line.UnitShape));
    }

    [Fact]
    public void CrystallizedFillKeepsIndependentMeasureWhenBoundaryMoves()
    {
        var line = new Carrier();
        var start = new TickRef(2);
        var end = new TickRef(5);
        var segment = new Relation(
            new Relation(new CarrierLocation(line, start), line),
            new Relation(new CarrierLocation(line, end), line));
        var reader = new Carrier(line.UnitShape);

        Assert.True(segment.TryCrystallizeFill(out var fill));
        end.Set(9);
        Assert.True(segment.TryReadFillMeasureOn(reader, out var reread));

        Assert.Equal(3UL, fill.Value);
        Assert.Equal(7, reread.Ticks);
        Assert.False(fill.SameIdentityAs(reader));
    }

    [Fact]
    public void ReversedSegmentCrystallizesAsInverseFilledIntervalCarrier()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 7, 2);

        Assert.True(segment.TryCrystallizeFill(out var fill));

        Assert.Equal(5UL, fill.Value);
        Assert.True(fill.IsInverse);
        Assert.False(fill.SameIdentityAs(line));
        Assert.True(fill.UnitShape.CompatibleWith(line.UnitShape));
    }

    [Fact]
    public void ReversedFillMeasureReadsOnInverseReaderFace()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 7, 2);
        var reader = new Carrier(line.UnitShape);

        Assert.True(segment.TryReadFillMeasureOn(reader, out var measure));

        Assert.Equal(5, measure.Ticks);
        Assert.True(measure.Carrier.SameIdentityAs(reader.Inverse()));
    }

    [Fact]
    public void FilledSegmentCarrierCanFeedAreaOperationThroughUnitBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var horizontal = new Carrier(lengthShape);
        var vertical = new Carrier(lengthShape);
        var areaBasis = new Carrier();
        var width = Relation.Segment(horizontal, 0, 3);
        var height = Relation.Segment(vertical, 0, 2);
        var widthPort = new Relation(Relation.Point(horizontal, 1), Relation.Point(Carrier.Unknown, 1));
        var heightPort = new Relation(Relation.Point(vertical, 1), Relation.Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(widthPort, heightPort), Relation.Segment(areaBasis, 0, 1));

        Assert.True(width.TryCrystallizeFill(out var widthFill));
        Assert.True(height.TryCrystallizeFill(out var heightFill));
        Assert.True(operation.TryCrystallize(
            new Relation(widthFill.ValueSlot, heightFill.ValueSlot),
            out var area,
            out var read));

        Assert.Equal(6UL, area.Value);
        Assert.True(area.UnitShape.CompatibleWith(UnitShape.Product(lengthShape, lengthShape)));
        Assert.Equal(2, area.UnitShape.ProductParticipationOf(lengthShape));
        Assert.Equal(
            new Relation(
                new CarrierLocation(area, 6),
                new Relation(new CarrierLocation(widthFill, 0), new CarrierLocation(heightFill, 0))),
            read);
    }

    [Fact]
    public void AreaResultCarrierCanFeedLaterOperationThroughProductUnitBasis()
    {
        var lengthShape = new Carrier().UnitShape;
        var horizontal = new Carrier(lengthShape);
        var vertical = new Carrier(lengthShape);
        var depth = new Carrier(lengthShape);
        var areaBasis = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var volumeBasis = new Carrier();
        var width = Relation.Segment(horizontal, 0, 3);
        var height = Relation.Segment(vertical, 0, 2);
        var areaOperation = new Relation(
            new Relation(
                new Relation(Relation.Point(horizontal, 1), Relation.Point(Carrier.Unknown, 1)),
                new Relation(Relation.Point(vertical, 1), Relation.Point(Carrier.Unknown, 1))),
            Relation.Segment(areaBasis, 0, 1));
        var volumeOperation = new Relation(
            new Relation(
                new Relation(Relation.Point(areaBasis, 1), Relation.Point(Carrier.Unknown, 1)),
                new Relation(Relation.Point(depth, 1), Relation.Point(Carrier.Unknown, 1))),
            Relation.Segment(volumeBasis, 0, 1));
        depth.Set(5);

        Assert.True(width.TryCrystallizeFill(out var widthFill));
        Assert.True(height.TryCrystallizeFill(out var heightFill));
        Assert.True(areaOperation.TryCrystallize(
            new Relation(widthFill.ValueSlot, heightFill.ValueSlot),
            out var area,
            out _));
        Assert.True(volumeOperation.TryCrystallize(
            new Relation(area.ValueSlot, depth.ValueSlot),
            out var volume,
            out _));

        Assert.Equal(30UL, volume.Value);
        Assert.Equal(3, volume.UnitShape.ProductParticipationOf(lengthShape));
    }

    [Fact]
    public void SameUnitCarrierDoesNotFeedPositionedProjectionFrame()
    {
        var lengthShape = new Carrier().UnitShape;
        var positioned = new Carrier(lengthShape);
        var fill = new Carrier(lengthShape);
        var reader = new Carrier();
        var port = new Relation(Relation.Segment(positioned, 5, 6), Relation.Point(Carrier.Unknown, 1));
        fill.Set(6);

        Assert.False(port.TryProject(fill.ValueSlot, reader, out _));
    }

    [Fact]
    public void FillMeasureRequiresCompatibleReaderUnit()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 2, 5);
        var incompatible = new Carrier();

        Assert.False(segment.TryReadFillMeasureOn(incompatible, out _));
    }

    [Fact]
    public void ZeroLengthFrameDoesNotHardenAsFill()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 4, 4);

        Assert.False(segment.TryCrystallizeFill(out _));
    }

    [Fact]
    public void PointFrameDoesNotHardenAsFill()
    {
        var line = new Carrier();
        var point = Relation.Point(line, 4);

        Assert.False(point.TryCrystallizeFill(out _));
    }

    [Fact]
    public void UnboundFrameDoesNotHardenAsFill()
    {
        var unbound = Relation.Segment(Carrier.Unknown, 0, 8);

        Assert.False(unbound.TryCrystallizeFill(out _));
    }
}
