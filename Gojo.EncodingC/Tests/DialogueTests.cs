using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A short dialogue as mechanism: asking, answering, clarifying,
/// declining and grounding, with obligations as material tension and meaning
/// as narrowing. What a car or a person is does not matter; only the structure
/// of the exchange does.
/// <para><b>Speakers are local structures</b> joined only by ports. Each
/// utterance has two parts. Its <b>token</b> is an obligation unit, moved and
/// conserved like any material. Its <b>content</b> is a value the listener
/// reads through the speaker's port, narrowing an open value of its own.</para>
/// <para><b>A question's life is one ledger:</b> asked, owed (a debt at the
/// answerer), answered or declined (in the answerer's outbox), heard (pressure
/// at the asker), grounded (acknowledged). The token is conserved through all
/// of it.</para>
/// <para><b>Scope, not data, decides what is resolved.</b> The answerer
/// repairs only when the value the question demands, the answer, is open.
/// Ambiguity elsewhere, such as which car "this" is, is tracked but not
/// repaired unless it reaches the answer.</para>
/// <para><b>Repair is ranked by structure:</b> answer when the answer is
/// settled; otherwise ask which one, while an input to the answer is open;
/// otherwise decline. The three gates exclude each other, so the cheaper move
/// is always the one taken.</para>
/// <para><b>No turn controller.</b> Whoever owes something speaks; an
/// obligation selects the next speaker.</para></summary>
public sealed class DialogueTests
{
    private static readonly Plane Here = Plane.Of(1, 0);
    private static readonly Shape Car = Shape.Binary(3);
    private const int John = 0, Mary = 1, Ann = 2, No = 0, Yes = 1;

    /// <summary>A small store helper: unary counts and codes added as planes.</summary>
    private sealed class Planes(Store store)
    {
        public Store Store => store;

        public Field Count(int capacity) => new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);

        public Field Code(Shape shape) => new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

        public int Bit() => store.AddPlane();

        public Binding On(int plane) => new(store, plane);

        public Binding Not(int plane) => new(new Flipped(store, plane), plane);
    }

    /// <summary>The one who asks: it knows which car it means, wants to know the
    /// answer, and keeps one ledger per answerer.</summary>
    private sealed record Asker(Engine Engine, Planes P, Field Referent, Port ReferentOut, Supports Value, Field Tally,
        int[] AskSlots, Field Slot, Port[] AskOut, Field[] Heard, Field[] DontKnow, Field Grounded, Field[] OwesClarify, Port[] ClarifyOutPort);

    /// <summary>One who answers: what "this" may be from where it stands, the
    /// answer, what it knows (a relation from car to answer), and its ledger.</summary>
    private sealed record Answerer(Engine Engine, Planes P, Supports Referent, Field ReferentTally, Supports Value, Field Tally,
        Field Said, Port SaidOut, Field Owed, Field Slot, Port OutboxPort, Field DeclinedSlot, Port DeclinedPort,
        Field ClarifySlot, Port ClarifyOutPort, Field HeardClarify, Field ClarifyGrounded)
    {
        public const int Outbox = 1, Declined = 2, Clarify = 3;
    }

    private sealed record Dialogue(Asker Asker, Answerer[] Answerers)
    {
        public Engine[] Engines => [Asker.Engine, .. Answerers.Select(a => a.Engine)];

        /// <summary>Steps both sides until a round changes nothing, and returns
        /// how many rounds that took.</summary>
        public int Settle(bool together = false)
        {
            for (var round = 1; round <= 64; round++)
                if (Engines.Select(e => together ? e.StepTogether() : e.Step()).ToArray().All(step => step.Steps.All(s => s.Quiet))) return round;
            throw new InvalidOperationException("The dialogue did not come to rest.");
        }

        /// <summary>Every obligation of answerer i's question: still to ask, owed,
        /// in the outbox, declined, heard, told "I don't know", grounded.</summary>
        public int Tokens(int i)
        {
            var (a, b) = (Asker, Answerers[i]);
            return Read(a.P, a.Slot, a.AskSlots[i]) + Read(b.P, b.Owed) + Read(b.P, b.Slot, Answerer.Outbox) + Read(b.P, b.Slot, Answerer.Declined)
                + Read(a.P, a.Heard[i]) + Read(a.P, a.DontKnow[i]);
        }

        public int Read(Planes p, Field f, int at = 0) => f.Read(p.Store, at)!.Value;
    }

    /// <summary>A dialogue about one car the asker points at. The question is a
    /// relation from car to answer (owner, or whether it is red), with values
    /// of <paramref name="answers"/> kinds. Each answerer knows the answer for
    /// some cars (null where it does not know) and sees <paramref name="visible"/>
    /// cars as what "this" could be.
    /// <para>Each speaker's own state is position 0 of its store. Each utterance
    /// it sends is a position of its own, a slot holding one token, which one
    /// listener takes through a port: an utterance is a thing, so it has its
    /// own identity, and two listeners never take from one position.</para></summary>
    private static Dialogue Ask(int meant, int answers, int[] visible, params int?[][] knowledge)
    {
        var answer = Shape.Binary(answers);
        var n = knowledge.Length;
        var asker = new Store(0, 1 + 2 * n);
        var a = new Planes(asker);
        var referent = a.Code(Car);
        referent.Write(asker, 0, meant);
        var value = Supports.On(asker, a.Code(answer));
        var tally = a.Count(answers);
        var (grounded, slot) = (a.Count(n), a.Count(1));
        var owesClarify = knowledge.Select(_ => a.Count(1)).ToArray();
        var heard = knowledge.Select(_ => a.Count(1)).ToArray();
        var dontKnow = knowledge.Select(_ => a.Count(1)).ToArray();
        int AskSlot(int i) => 1 + i;
        int ClarifySlot(int i) => 1 + n + i;
        for (var i = 0; i < n; i++) slot.Write(asker, AskSlot(i), 1);
        var referentOut = asker.Publish([0], [.. referent.Planes]);
        var askOut = Enumerable.Range(0, n).Select(i => asker.Publish([AskSlot(i)], [.. slot.Planes])).ToArray();
        var clarifyOutPort = Enumerable.Range(0, n).Select(i => asker.Publish([ClarifySlot(i)], [.. slot.Planes])).ToArray();
        var askerEngine = new Engine();
        var one = new Field(Shape.Unary(1), 0);

        var answerers = knowledge.Select((knows, i) =>
        {
            const int Outbox = 1, Declined = 2, Clarify = 3;
            var store = new Store(0, 4);
            var b = new Planes(store);
            var where = Supports.On(store, b.Code(Car));
            where.Restrict(0, visible.Aggregate(0UL, (m, c) => m | 1UL << c));
            var says = Supports.On(store, b.Code(answer));
            var (refTally, ansTally, said) = (b.Count(3), b.Count(answers), b.Code(answer));
            var (owed, bSlot, heardClarify, clarifyGrounded) = (b.Count(1), b.Count(1), b.Count(1), b.Count(1));
            var (asked, clarify, decline) = (b.Bit(), b.Bit(), b.Bit());
            var engine = new Engine()
                .Add(Tension.Route(one.On(askOut[i]), owed.On(store), Here))
                .Add(Tension.Route(one.On(clarifyOutPort[i]), heardClarify.On(store), Here))
                .Add(Wire(Settling.SameWhen(Car), b.On(heardClarify.Planes[0]), new Field(Car, 0, 1).On(referentOut), where.Variable));
            var facts = RelationLaw.FromPredicate([Car, answer], t => knows[t[0]] is not { } v || v == t[1]);
            engine.Add(Wire(facts, where.Variable, says.Variable))
                .Add(Tension.Wire(Counts.Tally(3), [.. where.Planes.Select(p => b.On(p)), refTally.On(store)], Here))
                .Add(Tension.Wire(Counts.Tally(answers), [.. says.Planes.Select(p => b.On(p)), ansTally.On(store)], Here))
                .Add(Wire(Settling.Enact(answer), new Binding(store, [.. says.Planes]), said.On(store)))
                // Ranked repair: ask which one while an input to the answer is open; decline when none is.
                .Add(Tension.Wire(Settling.Justify(4), [b.On(owed.Planes[0]), b.On(ansTally.Planes[1]), b.On(refTally.Planes[1]), b.Not(asked), b.On(clarify)], Here))
                .Add(Tension.Wire(Settling.Justify(3), [b.On(owed.Planes[0]), b.On(ansTally.Planes[1]), b.Not(refTally.Planes[1]), b.On(decline)], Here))
                .Add(Link(Counts.Accumulate(1), [(b.On(clarify), 0), (bSlot.On(store), Clarify)]))
                .Add(Tension.Wire(Settling.Latch(true), [b.On(clarify), b.On(asked)], Here))
                .Add(Link(Flows.When(Flows.Move(1, 1)), [(owed.On(store), 0), (bSlot.On(store), Declined), (b.On(decline), 0)]))
                .Add(Link(Flows.When(Flows.Move(1, 1), 2), [(owed.On(store), 0), (bSlot.On(store), Outbox), (b.On(ansTally.Planes[0]), 0), (b.Not(ansTally.Planes[1]), 0)]))
                .Add(Tension.Route(heardClarify.On(store), clarifyGrounded.On(store), Here, b.On(refTally.Planes[0]), b.Not(refTally.Planes[1])));
            return new Answerer(engine, b, where, refTally, says, ansTally, said, store.Publish([0], [.. said.Planes]),
                owed, bSlot, store.Publish([Outbox], [.. bSlot.Planes]), bSlot, store.Publish([Declined], [.. bSlot.Planes]),
                bSlot, store.Publish([Clarify], [.. bSlot.Planes]), heardClarify, clarifyGrounded);
        }).ToArray();

        for (var i = 0; i < n; i++)
        {
            var b = answerers[i];
            askerEngine
                .Add(Tension.Route(one.On(b.OutboxPort), heard[i].On(asker), Here))
                .Add(Tension.Route(one.On(b.DeclinedPort), dontKnow[i].On(asker), Here))
                .Add(Wire(Settling.SameWhen(answer), a.On(heard[i].Planes[0]), new Field(answer, [.. Enumerable.Range(0, answer.Bits)]).On(b.SaidOut), value.Variable))
                .Add(Tension.Route(one.On(b.ClarifyOutPort), owesClarify[i].On(asker), Here))
                .Add(Link(Flows.Move(1, 1), [(owesClarify[i].On(asker), 0), (slot.On(asker), ClarifySlot(i))]));
        }
        askerEngine
            .Add(Tension.Wire(Counts.Tally(answers), [.. value.Planes.Select(p => a.On(p)), tally.On(asker)], Here));
        foreach (var h in heard)
            askerEngine.Add(Tension.Route(h.On(asker), grounded.On(asker), Here, a.On(tally.Planes[0]), a.Not(tally.Planes[1])));
        foreach (var d in dontKnow) askerEngine.Add(Tension.Route(d.On(asker), grounded.On(asker), Here));

        return new Dialogue(new Asker(askerEngine, a, referent, referentOut, value, tally, [.. Enumerable.Range(0, n).Select(AskSlot)], slot, askOut,
            heard, dontKnow, grounded, owesClarify, clarifyOutPort), answerers);
    }

    private static Wiring Wire(Law law, params Binding[] roles)
    {
        var wiring = new Wiring(law, roles);
        wiring.Add([.. Enumerable.Repeat(0, roles.Length)]);
        return wiring;
    }

    /// <summary>One row of a law whose roles sit at different positions, such
    /// as a move from a speaker's own state into one of its outgoing slots.</summary>
    private static Wiring Link(TransitionLaw law, (Binding Role, int At)[] roles)
    {
        var wiring = new Wiring(law, [.. roles.Select(r => r.Role)]);
        wiring.Add([.. roles.Select(r => r.At)]);
        return wiring;
    }

    /// <summary>"Whose car is this?", with nothing ambiguous. Taken together,
    /// each move takes a step, so the one token is seen at every stage: asked,
    /// a debt at the answerer, in its outbox, pressure at the asker, and, once
    /// the asker is settled on John, grounded. It is conserved throughout.</summary>
    [Fact]
    public void AQuestionIsADebtAndItsAnswerIsPressureUntilAcknowledged()
    {
        var d = Ask(meant: 0, answers: 3, visible: [0], [John, Mary, Ann]);
        var (a, b) = (d.Asker, d.Answerers[0]);
        (int, int, int, int, int) Ledger() =>
            (d.Read(a.P, a.Slot, a.AskSlots[0]), d.Read(b.P, b.Owed), d.Read(b.P, b.Slot, Answerer.Outbox), d.Read(a.P, a.Heard[0]), d.Read(a.P, a.Grounded));

        var stages = new List<(int, int, int, int, int)> { Ledger() };
        for (var round = 0; round < 12; round++)
        {
            foreach (var engine in d.Engines) engine.StepTogether();
            Assert.Equal(1, d.Tokens(0) + d.Read(a.P, a.Grounded));
            if (Ledger() != stages[^1]) stages.Add(Ledger());
        }

        Assert.Equal([(1, 0, 0, 0, 0), (0, 1, 0, 0, 0), (0, 0, 1, 0, 0), (0, 0, 0, 1, 0), (0, 0, 0, 0, 1)], stages);
        Assert.Equal(John, new Question(a.Value, 0).Answer);
    }

    /// <summary>"Whose car is this?" when "this" could be either of two cars that
    /// are both John's. The referent stays ambiguous, and the ambiguity is
    /// tracked, but it does not reach the answer, so nothing is asked about it:
    /// the answer is John and the question is grounded.</summary>
    [Fact]
    public void AmbiguityThatDoesNotReachTheAnswerIsKeptButNotRepaired()
    {
        var d = Ask(meant: 2, answers: 3, visible: [0, 2], [John, Mary, John]);
        var b = d.Answerers[0];
        d.Settle();

        Assert.Equal(2, d.Read(b.P, b.ReferentTally));
        Assert.Equal(0, d.Read(b.P, b.Slot, Answerer.Clarify) + d.Read(d.Asker.P, d.Asker.OwesClarify[0]) + d.Read(b.P, b.HeardClarify) + d.Read(b.P, b.ClarifyGrounded));
        Assert.Equal((John, 1), (new Question(d.Asker.Value, 0).Answer, d.Read(d.Asker.P, d.Asker.Grounded)));
    }

    /// <summary>The same ambiguity, two questions. Car 0 is John's and car 1
    /// Mary's, and both are red; "this" could be either, and the asker means car
    /// 1. "Whose car is this?" depends on which, so the answerer asks which one:
    /// an inserted question, a debt at the asker, answered with the car it means.
    /// The answerer narrows, answers Mary, and both questions are grounded. "Is
    /// this car red?" does not depend on which, so it is answered yes at once
    /// and the referent stays ambiguous. Scope decided, not data.</summary>
    [Fact]
    public void TheSameAmbiguityIsRepairedForOneQuestionAndNotForAnother()
    {
        var whose = Ask(meant: 1, answers: 3, visible: [0, 1], [John, Mary, null]);
        whose.Settle();
        var (a, b) = (whose.Asker, whose.Answerers[0]);
        Assert.Equal(Mary, new Question(a.Value, 0).Answer);
        Assert.Equal((1, 1), (whose.Read(b.P, b.ClarifyGrounded), whose.Read(a.P, a.Grounded)));
        Assert.Equal(1, whose.Read(b.P, b.ReferentTally));

        var red = Ask(meant: 1, answers: 2, visible: [0, 1], [Yes, Yes, null]);
        red.Settle();
        var (ra, rb) = (red.Asker, red.Answerers[0]);
        Assert.Equal(Yes, new Question(ra.Value, 0).Answer);
        Assert.Equal((0, 1), (red.Read(rb.P, rb.ClarifyGrounded), red.Read(ra.P, ra.Grounded)));
        Assert.Equal(2, red.Read(rb.P, rb.ReferentTally));
    }

    /// <summary>The answerer sees which car is meant but does not know its owner.
    /// Nothing it could ask would settle the answer, so the next cheapest move is
    /// to decline. "I don't know" discharges the obligation and is grounded, and
    /// the asker's question stays open, its deficit visible: three possible
    /// owners, no debt left.</summary>
    [Fact]
    public void AnAnswererWhoCannotKnowDeclinesAndTheQuestionStaysOpen()
    {
        var d = Ask(meant: 2, answers: 3, visible: [2], [John, Mary, null]);
        d.Settle();
        var (a, b) = (d.Asker, d.Answerers[0]);

        Assert.Equal((1, 0, 0), (d.Read(a.P, a.Grounded), d.Read(b.P, b.Owed), d.Read(b.P, b.Slot, Answerer.Clarify)));
        Assert.Equal(3, d.Read(a.P, a.Tally));
        Assert.Equal(0, d.Tokens(0));
    }

    /// <summary>Two answerers who disagree: one says John, the other Mary. The
    /// asker's value has no possibility left, an information excess, and since
    /// acknowledging waits on a settled answer, neither answer is grounded: both
    /// stay as pressure, visible, and the contradiction is the result.</summary>
    [Fact]
    public void ConflictingAnswersLeaveAContradictionAndNeitherIsGrounded()
    {
        var d = Ask(meant: 0, answers: 3, visible: [0], [John, null, null], [Mary, null, null]);
        d.Settle();
        var a = d.Asker;

        Assert.Equal(0, d.Read(a.P, a.Tally));
        Assert.Equal((1, 1, 0), (d.Read(a.P, a.Heard[0]), d.Read(a.P, a.Heard[1]), d.Read(a.P, a.Grounded)));
        Assert.Equal(0, new Question(a.Value, 0).Remaining);
    }

    /// <summary>Small and fast: the dialogue with a clarification saves to a few
    /// hundred bytes and settles in a handful of rounds, and a saved and loaded
    /// copy runs the same exchange.</summary>
    [Fact]
    public void TheDialogueIsSmallAndSavesWithItsLedger()
    {
        var d = Ask(meant: 1, answers: 3, visible: [0, 1], [John, Mary, null]);
        var saved = Transport.SaveLocals(d.Engines);
        var rounds = d.Settle();

        Assert.InRange(saved.Length, 1, 1024);
        Assert.InRange(rounds, 1, 16);
        var copies = Transport.LoadLocals(saved);
        for (var round = 0; round < rounds; round++)
            foreach (var (engine, _) in copies) engine.Step();
        Assert.Equal(Transport.SaveLocals(d.Engines), Transport.SaveLocals([.. copies.Select(c => c.Engine)]));
    }
}
