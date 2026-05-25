using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class AdditiveGroupTests
{
    [Fact]
    public void AdditiveAttachmentReadsSingleParticipantOnHolder()
    {
        var holder = new Carrier();
        var color = new Carrier();
        var attachment = Relation.AdditiveAttachment(holder, color);

        Assert.True(attachment.TryReadAdditiveParticipantsOn(holder, out var participants));

        Assert.Single(participants);
        Assert.Same(color, participants[0]);
    }

    [Fact]
    public void AdditiveGroupReadsParticipantsInConstructionOrder()
    {
        var holder = new Carrier();
        var stroke = new Carrier();
        var color = new Carrier();
        var opacity = new Carrier();
        var group = Relation.AdditiveGroup(holder, stroke, color, opacity);

        Assert.True(group.TryReadAdditiveParticipantsOn(holder, out var participants));

        Assert.Equal(3, participants.Length);
        Assert.Same(stroke, participants[0]);
        Assert.Same(color, participants[1]);
        Assert.Same(opacity, participants[2]);
    }

    [Fact]
    public void AdditiveGroupPreservesRelationParticipantsAsWholeStructure()
    {
        var holder = new Carrier();
        var line = new Carrier();
        var segment = Relation.Segment(line, 2, 5);
        var label = new Carrier();
        var group = Relation.AdditiveGroup(holder, segment, label);

        Assert.True(group.TryReadAdditiveParticipantsOn(holder, out var participants));

        Assert.Equal(2, participants.Length);
        Assert.Equal(segment, participants[0]);
        Assert.Same(label, participants[1]);
    }

    [Fact]
    public void AdditiveGroupReadsCombinedParticipantUnitShape()
    {
        var holder = new Carrier();
        var appleShape = new Carrier().UnitShape;
        var orangeShape = new Carrier().UnitShape;
        var firstApple = new Carrier(appleShape);
        var orange = new Carrier(orangeShape);
        var secondApple = new Carrier(appleShape);
        var group = Relation.AdditiveGroup(holder, firstApple, orange, secondApple);

        Assert.True(group.TryReadAdditiveUnitShapeOn(holder, out var unitShape));
        Assert.True(unitShape.TryReadAdditiveOperands(out var operands));

        Assert.Equal(2, operands.Length);
        Assert.True(operands[0].CompatibleWith(appleShape));
        Assert.True(operands[1].CompatibleWith(orangeShape));
    }

    [Fact]
    public void AdditiveGroupDoesNotRouteParticipantTensionToHolder()
    {
        var holder = new Carrier();
        var line = new Carrier();
        var segment = Relation.Segment(line, 0, 3);
        var group = Relation.AdditiveGroup(holder, segment);

        holder.Attach(group);
        line.Add(7);

        Assert.Equal(7UL, line.Value);
        Assert.Equal(0UL, holder.Value);
        Assert.Equal(0, holder.Tension);
    }

    [Fact]
    public void AdditiveUnitShapeReadFailsWhenParticipantHasNoReadableUnitShape()
    {
        var holder = new Carrier();
        var tick = new TickRef(4);
        var group = Relation.AdditiveGroup(holder, tick);

        Assert.False(group.TryReadAdditiveUnitShapeOn(holder, out _));
    }
}
