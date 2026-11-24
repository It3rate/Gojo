using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Material tension (<c>T+</c> pressure and <c>T−</c> draw) as
/// conserved unary fields settled by <see cref="Flows.Move"/> and zero-copy
/// <see cref="Unary.Room"/> views, never netted automatically.</summary>
public sealed class TensionMaterialTests
{
    private const int Cap = 3;

    private static (Store Site, Field Value, Field Plus, Field Minus) Site(int positions = 1)
    {
        var store = new Store(3 * Cap, positions);
        return (
            store,
            new Field(Shape.Unary(Cap), 0, 1, 2),
            new Field(Shape.Unary(Cap), 3, 4, 5),
            new Field(Shape.Unary(Cap), 6, 7, 8));
    }

    /// <summary>Units arriving at a full site do not disappear: they wait in
    /// <c>T+</c> and are absorbed by <see cref="Tension.Absorb"/> as downstream
    /// flow frees room. <c>value + T+ + sink</c> is conserved at every step.</summary>
    [Fact]
    public void OverflowWaitsInTPlusAndAbsorbsWhenRoomFrees()
    {
        var (site, value, plus, _) = Site(2);
        value.Write(site, 0, 3);
        var absorb = Tension.Absorb(site, plus, value);
        absorb.Add(0, 0);
        var drain = new Wiring(Flows.Move(Cap, Cap), new Binding(site, [.. value.Planes]), new Binding(site, [.. value.Planes]));
        drain.Add(0, 1);
        var engine = new Engine().Add(drain).Add(absorb);

        var first = Unary.Offer(site, value, plus, Plane.Of(2, 0));
        var second = Unary.Offer(site, value, plus, Plane.Of(2, 0));

        Assert.True(first.Absorbed.None && first.Held[0] && first.Blocked.None);
        Assert.True(second.Absorbed.None && second.Held[0] && second.Blocked.None);
        Assert.Equal((3, 2, 0), (value.Read(site, 0), plus.Read(site, 0), value.Read(site, 1)));

        for (var step = 0; step < 3; step++)
        {
            engine.Step();
            Assert.Equal(5, value.Read(site, 0) + plus.Read(site, 0) + value.Read(site, 1));
        }

        Assert.Equal((2, 0, 3), (value.Read(site, 0), plus.Read(site, 0), value.Read(site, 1)));
    }

    /// <summary>A draw against an empty site stays in <c>T−</c> and is paid
    /// through <see cref="Tension.Draw"/> (a move into <see cref="Unary.Room"/>)
    /// once supply arrives. <c>value − T−</c> is conserved throughout.</summary>
    [Fact]
    public void UnmetDrawWaitsInTMinusAndIsPaidWhenSupplyArrives()
    {
        var (site, value, _, minus) = Site();
        var pay = Tension.Draw(site, minus, value);
        pay.Add(0, 0);
        var engine = new Engine().Add(pay);

        var d1 = Unary.Draw(site, value, minus, Plane.Ones(1));
        var d2 = Unary.Draw(site, value, minus, Plane.Ones(1));

        Assert.True(d1.Absorbed.None && d1.Held[0]);
        Assert.True(d2.Absorbed.None && d2.Held[0]);
        Assert.Equal((0, 2), (value.Read(site, 0), minus.Read(site, 0)));

        engine.Settle(8);
        Assert.Equal((0, 2), (value.Read(site, 0), minus.Read(site, 0)));

        value.Write(site, 0, 3);
        var netBefore = value.Read(site, 0) - minus.Read(site, 0);
        Assert.Equal(1, netBefore);

        engine.Settle(8);

        Assert.Equal((1, 0), (value.Read(site, 0), minus.Read(site, 0)));
        Assert.Equal(netBefore, value.Read(site, 0) - minus.Read(site, 0));
    }

    /// <summary><c>T+</c> and <c>T−</c> are kept apart and never netted
    /// automatically: both witnesses remain visible until an explicit
    /// <see cref="Tension.Cancel"/> wiring declares they are the same quantity.</summary>
    [Fact]
    public void TPlusAndTMinusStaySeparateUntilCancelIsExplicitlyWired()
    {
        var (site, value, plus, minus) = Site();
        plus.Write(site, 0, 2);
        minus.Write(site, 0, 3);

        // No cancel wiring: stepping an engine holding the site changes neither face.
        var unnetted = new Engine().Hold(site);
        unnetted.Run(4);
        Assert.Equal((2, 3), (plus.Read(site, 0), minus.Read(site, 0)));

        var cancel = Tension.Cancel(site, plus, minus);
        cancel.Add(0, 0);
        unnetted.Add(cancel);
        var diffBefore = plus.Read(site, 0) - minus.Read(site, 0);

        unnetted.Settle(8);

        Assert.Equal((0, 1), (plus.Read(site, 0), minus.Read(site, 0)));
        Assert.Equal(diffBefore, plus.Read(site, 0) - minus.Read(site, 0));
        Assert.Equal(0, value.Read(site, 0));
    }

    /// <summary>Absorb, draw and cancel are ordinary <see cref="Flows.Move"/>
    /// wirings over the site and its room views, so they save and restore
    /// through <see cref="Transport"/> and isolate inside a <see cref="Fork"/>.</summary>
    [Fact]
    public void MaterialTensionSavesRestoresAndForksCleanly()
    {
        var (site, value, plus, minus) = Site();
        value.Write(site, 0, 1);
        plus.Write(site, 0, 2);
        minus.Write(site, 0, 1);

        var absorb = Tension.Absorb(site, plus, value);
        absorb.Add(0, 0);
        var draw = Tension.Draw(site, minus, value);
        draw.Add(0, 0);
        var engine = new Engine().Add(absorb).Add(draw);

        var saved = Transport.Save(engine, site);
        var (restored, copies) = Transport.Load(saved);

        // Fork tries settling without mutating the parent site.
        var fork = engine.Fork();
        fork.Engine.Settle(8);
        Assert.Equal((2, 0, 0), (value.Read(fork.Of(site), 0), plus.Read(fork.Of(site), 0), minus.Read(fork.Of(site), 0)));
        Assert.Equal((1, 2, 1), (value.Read(site, 0), plus.Read(site, 0), minus.Read(site, 0)));

        // Parent and restored machine reach the exact same settled state.
        engine.Settle(8);
        restored.Settle(8);
        Assert.Equal((2, 0, 0), (value.Read(site, 0), plus.Read(site, 0), minus.Read(site, 0)));
        Assert.Equal((2, 0, 0), (value.Read(copies[0], 0), plus.Read(copies[0], 0), minus.Read(copies[0], 0)));
    }

    /// <summary>Reorienting a site swaps which of <c>T+</c> and <c>T−</c> reads
    /// as positive (credit-positive vs debt-positive) without changing bits or
    /// turning a full bucket into an empty one; room is a separate anti-fill
    /// read from capacity.</summary>
    [Fact]
    public void ReorientationSwapsTensionFacesDistinctFromRoomRead()
    {
        var (site, value, plus, minus) = Site();
        value.Write(site, 0, 1);
        plus.Write(site, 0, 2);
        minus.Write(site, 0, 0);

        // Credit orientation: positive = T+ (2), opposed = T- (0).
        // Debt orientation: positive = T- (0), opposed = T+ (2) ("negative debt of 2").
        var (creditPos, creditNeg) = (plus.Read(site, 0), minus.Read(site, 0));
        var (debtPos, debtNeg) = (minus.Read(site, 0), plus.Read(site, 0));
        Assert.Equal((2, 0), (creditPos, creditNeg));
        Assert.Equal((0, 2), (debtPos, debtNeg));

        // Room (anti-fill) is capacity - value = 3 - 1 = 2, independent of T+/T-.
        var roomBinding = Unary.Room(site, value);
        var roomField = new Field(Shape.Unary(Cap), [.. roomBinding.Planes]);
        Assert.Equal(2, roomField.Read(roomBinding.Surface, 0));
        Assert.Equal(1, value.Read(site, 0));
    }

    /// <summary>Broken unary patterns (such as <c>010</c>) stay broken and are
    /// reported in <see cref="TensionEntry.Broken"/> rather than repaired.</summary>
    [Fact]
    public void BrokenCountsAreReportedAndNeverRepairedByOfferOrDraw()
    {
        var (site, value, plus, minus) = Site();
        site.Write(1, 0, true); // 010 in value

        var offered = Unary.Offer(site, value, plus, Plane.Ones(1));
        var drawn = Unary.Draw(site, value, minus, Plane.Ones(1));

        Assert.True(offered.Broken[0] && offered.Absorbed.None && offered.Held.None);
        Assert.True(drawn.Broken[0] && drawn.Absorbed.None && drawn.Held.None);
        Assert.Equal(0b010UL, value.Pattern(site, 0));
        Assert.Equal((0, 0), (plus.Read(site, 0), minus.Read(site, 0)));
    }
}
