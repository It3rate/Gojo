using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Contention resolution: turning <see cref="Evidence.Contested"/> and
/// starvation (<c>T−</c>) into explicit turn-taking choices settled by ordinary
/// laws inside the machine.</summary>
public sealed class TensionContentionTests
{
    private const int Cap = 4;

    /// <summary>Two consumers start with both gates open and race for units
    /// from one source under <see cref="Engine.StepTogether"/>. On the first
    /// step both are contested and nothing moves; <see cref="TensionLaws.TurnStep"/>
    /// reads <see cref="Evidence.Contested"/> to break the tie and then
    /// alternates turns while the source holds units so both consumers receive
    /// two units with full conservation. The whole machine saves and restores
    /// via <see cref="Transport"/>.</summary>
    [Fact]
    public void ContestedTakersResolveByTurnTakingLawReadingContestedEvidence()
    {
        // Store layout: planes 0..3 unary count. Positions: 0 source, 1 consumer A, 2 consumer B.
        var store = new Store(Cap, 3);
        var count = new Field(Shape.Unary(Cap), 0, 1, 2, 3);
        count.Write(store, 0, 4);

        // Gates: pos 0 = gate A, pos 1 = gate B. Both start open (1, 1) -> guaranteed contest!
        var gates = new Store(1, 2);
        gates.Write(0, 0, true);
        gates.Write(0, 1, true);

        var takeA = new Wiring(Flows.GatedMove(Cap, Cap), new Binding(store, [.. count.Planes]), new Binding(gates, 0), new Binding(store, [.. count.Planes]));
        takeA.Add(0, 0, 1);
        takeA.Record();

        var takeB = new Wiring(Flows.GatedMove(Cap, Cap), new Binding(store, [.. count.Planes]), new Binding(gates, 0), new Binding(store, [.. count.Planes]));
        takeB.Add(0, 1, 2);
        var evB = takeB.Record();

        // TurnStep reads evB.Contested, source non-empty (plane 0), gate A, gate B.
        var turn = new Wiring(
            TensionLaws.TurnStep,
            new Binding(takeB.Rows, evB.Contested),
            new Binding(store, 0),
            new Binding(gates, 0),
            new Binding(gates, 0));
        turn.Add(0, 0, 0, 1);

        var engine = new Engine().Add(takeA).Add(takeB).Add(turn);
        var saved = Transport.Save(engine, store, gates);
        var (restored, copies) = Transport.Load(saved);

        // Step 1 (StepTogether): both gates open -> contested, nothing moved, evidence.Contested set!
        var first = engine.StepTogether();
        Assert.True(first.Steps[0].Contested![0] && first.Steps[1].Contested![0]);
        Assert.Equal((4, 0, 0), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        // Step 2: TurnStep reads Contested and clears gate B (contest resolves!).
        engine.StepTogether();
        Assert.True(gates.Read(0, 0));
        Assert.False(gates.Read(0, 1));

        // Let the machine run until quiet: alternating turns deliver 2 units to each consumer.
        Assert.NotNull(engine.Settle(32, together: true));
        Assert.Equal((0, 2, 2), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        // Restored machine runs identically from bits alone.
        Assert.NotNull(restored.Settle(32, together: true));
        Assert.Equal((0, 2, 2), (count.Read(copies[0], 0), count.Read(copies[0], 1), count.Read(copies[0], 2)));
    }

    /// <summary>The deficit face of contention (starvation): consumer A holds
    /// the turn bit while already full (<c>Changed = 0</c>), and consumer B
    /// waits with unmet draw (<c>T− &gt; 0</c>). <see cref="TensionLaws.YieldTurn"/>
    /// flips the turn bit to B, delivery resumes through <see cref="Flipped"/>
    /// of the turn bit, and B's draw is paid to zero.</summary>
    [Fact]
    public void IdleHolderYieldsTurnToWaitingConsumerWithUnmetTMinus()
    {
        const int SmallCap = 2;
        // Store: 0..1 value, 2..3 T- (unmet draw). Pos 0 source, pos 1 consumer A (full), pos 2 consumer B (starving).
        var site = new Store(4, 3);
        var value = new Field(Shape.Unary(SmallCap), 0, 1);
        var minus = new Field(Shape.Unary(SmallCap), 2, 3);
        value.Write(site, 0, 2);
        value.Write(site, 1, 2); // A is full
        minus.Write(site, 2, 2); // B has unmet draw of 2

        // Turn bit: 1 = A's turn, 0 = B's turn (B reads Flipped(turn)).
        var turn = new Store(1, 1);
        turn.Write(0, 0, true);

        var takeA = new Wiring(Flows.GatedMove(SmallCap, SmallCap), new Binding(site, 0, 1), new Binding(turn, 0), new Binding(site, 0, 1));
        takeA.Add(0, 0, 1);
        var evA = takeA.Record();

        var takeB = new Wiring(Flows.GatedMove(SmallCap, SmallCap), new Binding(site, 0, 1), new Binding(new Flipped(turn, 0), 0), new Binding(site, 0, 1));
        takeB.Add(0, 0, 2);

        var payB = Tension.Draw(site, minus, value);
        payB.Add(2, 2);

        // If A did not move (Changed = 0) and B has T- > 0 (bit 0 of minus is plane 2), yield turn to B.
        var yieldTurn = new Wiring(TensionLaws.YieldTurn, new Binding(takeA.Rows, evA.Changed), new Binding(site, 2), new Binding(turn, 0));
        yieldTurn.Add(0, 2, 0);

        var engine = new Engine().Add(takeA).Add(takeB).Add(payB).Add(yieldTurn);
        engine.Settle(16);

        Assert.False(turn.Read(0, 0)); // turn yielded to B
        Assert.Equal((0, 2, 0), (value.Read(site, 0), value.Read(site, 1), value.Read(site, 2)));
        Assert.Equal(0, minus.Read(site, 2)); // B's draw of 2 was completely paid
    }
}
