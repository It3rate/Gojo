using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class DialogueProtocolTests
{
    [Fact]
    public void OnlyAnAcknowledgmentOfTheOfferedAnswerGroundsAnAskedQuestion()
    {
        var exchange = DialogueExchange.Create(Bits(0b10), Bits(0));

        var idle = Settle(exchange);
        Assert.False(idle.Debt);
        Assert.False(idle.Pressure);
        Assert.Null(idle.Offered);
        Assert.Null(idle.Grounded);
        Assert.Equal(DialogueTurn.Asker, idle.Turn);

        exchange.Ask();
        AssertAwaitingAcknowledgment(Settle(exchange), 1);

        exchange.Acknowledge(0);
        AssertAwaitingAcknowledgment(Settle(exchange), 1);

        exchange.Acknowledge(1);
        var grounded = Settle(exchange);
        AssertGrounded(grounded, 1);
        Assert.Equal(1, exchange.Engine.Settle(16));
        Assert.Equal(grounded, exchange.Read());
    }

    [Fact]
    public void AcknowledgmentBeforeTheFirstOfferCannotGroundIt()
    {
        var exchange = DialogueExchange.Create(Bits(0b01), Bits(0));
        exchange.Ask();
        exchange.Acknowledge(0);

        AssertAwaitingAcknowledgment(Settle(exchange), 0);

        exchange.Acknowledge(0);
        AssertGrounded(Settle(exchange), 0);
    }

    [Theory]
    [InlineData(0b00, null)]
    [InlineData(0b01, 0)]
    [InlineData(0b10, 1)]
    [InlineData(0b11, 0)]
    public void AmbiguityRequestsTheCheapestViableClarification(int viable, int? clarification)
    {
        var possible = Bits(0b11);
        var exchange = DialogueExchange.Create(possible, Bits(viable));
        var idle = Settle(exchange);
        Assert.False(idle.Ambiguous);
        Assert.Null(idle.Clarification);

        exchange.Ask();
        var unresolved = Settle(exchange);
        Assert.True(unresolved.Debt);
        Assert.True(unresolved.Ambiguous);
        Assert.False(unresolved.Contradiction);
        Assert.False(unresolved.Pressure);
        Assert.Null(unresolved.Grounded);
        Assert.Null(unresolved.Offered);
        Assert.Equal(clarification, unresolved.Clarification);
        Assert.Equal(clarification.HasValue ? DialogueTurn.Asker : DialogueTurn.Respondent, unresolved.Turn);

        SetBits(possible, 0b01);
        AssertAwaitingAcknowledgment(Settle(exchange), 0);
    }

    [Fact]
    public void AStaleAcknowledgmentCannotGroundAReplacementAnswerOrBeReplayed()
    {
        var possible = Bits(0b01);
        var exchange = DialogueExchange.Create(possible, Bits(0));
        exchange.Ask();
        AssertAwaitingAcknowledgment(Settle(exchange), 0);

        exchange.Acknowledge(0);
        SetBits(possible, 0b10);
        AssertAwaitingAcknowledgment(Settle(exchange), 1);

        SetBits(possible, 0b01);
        AssertAwaitingAcknowledgment(Settle(exchange), 0);

        exchange.Acknowledge(0);
        AssertGrounded(Settle(exchange), 0);
    }

    [Fact]
    public void ContradictionRetainsDebtAndPressureAndRejectsAcknowledgment()
    {
        var possible = Bits(0b10);
        var exchange = DialogueExchange.Create(possible, Bits(0b11));
        exchange.Ask();
        AssertAwaitingAcknowledgment(Settle(exchange), 1);

        SetBits(possible, 0);
        AssertBlocked(Settle(exchange));
        exchange.Acknowledge(1);
        AssertBlocked(Settle(exchange));

        SetBits(possible, 0b10);
        AssertAwaitingAcknowledgment(Settle(exchange), 1);

        static void AssertBlocked(DialogueReading reading)
        {
            Assert.True(reading.Debt);
            Assert.True(reading.Pressure);
            Assert.True(reading.Contradiction);
            Assert.False(reading.Ambiguous);
            Assert.Equal(1, reading.Offered);
            Assert.Null(reading.Grounded);
            Assert.Null(reading.Clarification);
            Assert.Equal(DialogueTurn.Respondent, reading.Turn);
        }
    }

    private static void AssertAwaitingAcknowledgment(DialogueReading reading, int answer)
    {
        Assert.True(reading.Debt);
        Assert.True(reading.Pressure);
        Assert.False(reading.Ambiguous);
        Assert.False(reading.Contradiction);
        Assert.Equal(answer, reading.Offered);
        Assert.Null(reading.Grounded);
        Assert.Null(reading.Clarification);
        Assert.Equal(DialogueTurn.Asker, reading.Turn);
    }

    private static void AssertGrounded(DialogueReading reading, int answer)
    {
        Assert.False(reading.Debt);
        Assert.False(reading.Pressure);
        Assert.Equal(answer, reading.Grounded);
        Assert.Null(reading.Offered);
        Assert.Null(reading.Clarification);
        Assert.Equal(DialogueTurn.Grounded, reading.Turn);
    }

    private static DialogueReading Settle(DialogueExchange exchange)
    {
        Assert.NotNull(exchange.Engine.Settle(16));
        return exchange.Read();
    }

    private static Store Bits(int mask)
    {
        var store = new Store(2, 1);
        SetBits(store, mask);
        return store;
    }

    private static void SetBits(Store store, int mask)
    {
        store.Write(0, 0, (mask & 0b01) != 0);
        store.Write(1, 0, (mask & 0b10) != 0);
    }
}
