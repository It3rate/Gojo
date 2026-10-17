using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class AttributeLadderTests
{
    [Fact]
    public void RgbComponentsAttachToColorCarrierAsOrderedAdditiveLadder()
    {
        var color = new Carrier();
        var red = new Carrier();
        var green = new Carrier();
        var blue = new Carrier();

        var rgb = color.AttachAdditive(red, green, blue);

        Assert.True(rgb.TryReadAdditiveParticipantsOn(color, out var participants));
        Assert.Equal(3, participants.Length);
        Assert.Same(red, participants[0]);
        Assert.Same(green, participants[1]);
        Assert.Same(blue, participants[2]);
    }

    [Fact]
    public void AttributeLadderDoesNotChangeCarrierTypeOrValue()
    {
        var color = new Carrier();
        var colorShape = color.UnitShape;
        var red = new Carrier();
        var green = new Carrier();
        var blue = new Carrier();
        red.Set(128);
        green.Set(64);
        blue.Set(32);

        color.AttachAdditive(red, green, blue);

        Assert.True(color.UnitShape.CompatibleWith(colorShape));
        Assert.Equal(0UL, color.Value);
        Assert.Equal(128UL, red.Value);
        Assert.Equal(64UL, green.Value);
        Assert.Equal(32UL, blue.Value);
    }

    [Fact]
    public void SameAttributeCarrierCanHaveAlternateComponentLadders()
    {
        var color = new Carrier();
        var red = new Carrier();
        var green = new Carrier();
        var blue = new Carrier();
        var hue = new Carrier();
        var saturation = new Carrier();
        var lightness = new Carrier();

        var rgb = color.AttachAdditive(red, green, blue);
        var hsl = color.AttachAdditive(hue, saturation, lightness);

        Assert.True(rgb.TryReadAdditiveParticipantsOn(color, out var rgbParticipants));
        Assert.True(hsl.TryReadAdditiveParticipantsOn(color, out var hslParticipants));
        Assert.Equal([red, green, blue], rgbParticipants);
        Assert.Equal([hue, saturation, lightness], hslParticipants);
    }

    [Fact]
    public void AttributeLadderExposesComponentUnitParticipation()
    {
        var color = new Carrier();
        var red = new Carrier();
        var green = new Carrier();
        var blue = new Carrier();

        var rgb = color.AttachAdditive(red, green, blue);

        Assert.True(rgb.TryReadAdditiveUnitShapeOn(color, out var unitShape));
        Assert.True(unitShape.TryReadAdditiveOperands(out var operands));
        Assert.Equal(3, operands.Length);
        Assert.True(operands[0].CompatibleWith(red.UnitShape));
        Assert.True(operands[1].CompatibleWith(green.UnitShape));
        Assert.True(operands[2].CompatibleWith(blue.UnitShape));
    }

    [Fact]
    public void FillCanCarryColorWhoseComponentsRemainInspectable()
    {
        var boundaryHolder = new Carrier();
        var attributeHolder = new Carrier();
        var boundary = Relation.AdditiveGroup(
            boundaryHolder,
            Relation.Segment(new Carrier(), 0, 4));
        var fill = boundary.CreateFillBasis();
        var color = new Carrier();
        var red = new Carrier();
        var green = new Carrier();
        var blue = new Carrier();
        var rgb = color.AttachAdditive(red, green, blue);
        var attributedFill = Relation.AdditiveGroup(attributeHolder, fill, color);

        Assert.True(attributedFill.TryReadAdditiveParticipantsOn(attributeHolder, out var attributes));
        Assert.True(rgb.TryReadAdditiveParticipantsOn(color, out var components));
        Assert.Equal(2, attributes.Length);
        Assert.Same(fill, attributes[0]);
        Assert.Same(color, attributes[1]);
        Assert.Equal([red, green, blue], components);
    }
}
