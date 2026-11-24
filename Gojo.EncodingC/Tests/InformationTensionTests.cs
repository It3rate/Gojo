using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class InformationTensionTests
{
    [Fact]
    public void AJustifiedPossibilityReopensWhenItsReasonsReturn()
    {
        var reasons = new Store(2, 1);
        reasons.Write(0, 0, true);
        var choice = Supports.On(new Store(1, 1), Field.Bit(0));
        var justify = new Wiring(Settling.Justify(2),
            new Binding(reasons, 0), new Binding(reasons, 1), new Binding(choice.Surface, choice.Planes[0]));
        justify.Add(0, 0, 0);
        var engine = new Engine().Add(justify);

        Assert.Equal(2, engine.Settle(2));
        Assert.Equal(0b10UL, choice.Values(0));
        Assert.True(reasons.Read(0, 0));
        Assert.False(reasons.Read(1, 0));

        reasons.Write(1, 0, true);

        Assert.Equal(2, engine.Settle(2));
        Assert.Equal(0b11UL, choice.Values(0));
        Assert.True(reasons.Read(0, 0));
        Assert.True(reasons.Read(1, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(int.MaxValue)]
    public void JustificationRejectsReasonCountsOutsideTheTableLimit(int reasons) =>
        Assert.Throws<ArgumentOutOfRangeException>(nameof(reasons), () => Settling.Justify(reasons));

    [Theory]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, false, false, true)]
    [InlineData(true, true, false, false, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, false, false, false, false)]
    public void InformationTensionMeetsAnExplicitDemand(bool demand, bool possibleA, bool possibleB, bool open, bool contradiction)
    {
        var store = new Store(7, 1);
        store.Write(0, 0, possibleA);
        store.Write(1, 0, possibleB);
        store.Write(2, 0, demand);
        var summarize = new Wiring(InformationTension.SupportPair,
            new Binding(store, 0), new Binding(store, 1), new Binding(store, 3), new Binding(store, 4));
        summarize.Add(0, 0, 0, 0);
        var status = new Wiring(InformationTension.DemandedStatus,
            new Binding(store, 2), new Binding(store, 3), new Binding(store, 4), new Binding(store, 5), new Binding(store, 6));
        status.Add(0, 0, 0, 0, 0);
        var engine = new Engine().Add(summarize).Add(status);

        Assert.NotNull(engine.Settle(2));
        Assert.Equal((open, contradiction), (store.Read(5, 0), store.Read(6, 0)));
        Assert.Equal((possibleA, possibleB, demand), (store.Read(0, 0), store.Read(1, 0), store.Read(2, 0)));
    }
}
