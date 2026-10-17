using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class FillBasisTests
{
    [Fact]
    public void BoundaryGroupCreatesFillBasisWithoutMeasuringArea()
    {
        var holder = new Carrier();
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var bottom = Relation.Segment(horizontal, 0, 3);
        var side = Relation.Segment(vertical, 0, 2);
        var boundary = Relation.AdditiveGroup(holder, bottom, side);

        var fill = boundary.CreateFillBasis();

        Assert.Equal(1UL, fill.Value);
        Assert.False(fill.IsInverse);
        Assert.True(fill.UnitShape.TryReadOrthogonalSource(out var source));
        var sourceGroup = Assert.IsType<Relation>(source);
        Assert.True(sourceGroup.TryReadAdditiveParticipantsOn(holder, out var participants));
        Assert.Equal(2, participants.Length);
        Assert.Equal(bottom, participants[0]);
        Assert.Equal(side, participants[1]);
    }

    [Fact]
    public void FillBasisUsesOrthogonalSourceShapeInsteadOfBoundaryUnit()
    {
        var line = new Carrier();
        var segment = Relation.Segment(line, 2, 5);
        var measureReader = new Carrier(line.UnitShape);

        var fill = segment.CreateFillBasis();

        Assert.Equal(1UL, fill.Value);
        Assert.True(segment.TryReadFillMeasureOn(measureReader, out var measure));
        Assert.Equal(3, measure.Ticks);
        Assert.False(fill.UnitShape.CompatibleWith(line.UnitShape));
        Assert.True(fill.UnitShape.CompatibleWith(UnitShape.OrthogonalOf(segment)));
    }

    [Fact]
    public void InverseFillBasisMarksOpposedParticipation()
    {
        var holder = new Carrier();
        var boundary = Relation.AdditiveGroup(holder, new Carrier());

        var fill = boundary.CreateInverseFillBasis();

        Assert.Equal(1UL, fill.Value);
        Assert.True(fill.IsInverse);
        Assert.True(fill.UnitShape.TryReadOrthogonalSource(out var source));
        Assert.Equal(boundary, source);
    }

    [Fact]
    public void FillBasisCanCarryAdditiveAttributesWithoutAreaMeasure()
    {
        var boundaryHolder = new Carrier();
        var attributeHolder = new Carrier();
        var segment = Relation.Segment(new Carrier(), 0, 4);
        var boundary = Relation.AdditiveGroup(boundaryHolder, segment);
        var fill = boundary.CreateFillBasis();
        var color = new Carrier();
        var attributedFill = Relation.AdditiveGroup(attributeHolder, fill, color);

        Assert.True(attributedFill.TryReadAdditiveParticipantsOn(attributeHolder, out var participants));
        Assert.True(attributedFill.TryReadAdditiveUnitShapeOn(attributeHolder, out var unitShape));
        Assert.True(unitShape.TryReadAdditiveOperands(out var operands));
        Assert.Equal(2, participants.Length);
        Assert.Same(fill, participants[0]);
        Assert.Same(color, participants[1]);
        Assert.True(operands[0].CompatibleWith(fill.UnitShape));
        Assert.True(operands[1].CompatibleWith(color.UnitShape));
        Assert.Equal(1UL, fill.Value);
    }
}
