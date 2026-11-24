namespace Gojo.EncodingC;

internal enum DialogueTurn { Asker, Respondent, Grounded }

internal readonly record struct DialogueReading(
    bool Debt, bool Pressure, int? Grounded, int? Offered, bool Ambiguous,
    bool Contradiction, int? Clarification, DialogueTurn Turn);

/// <summary>A one-question exchange. The content owner publishes possible
/// answers and eligible repairs. The exchange owns the obligation, the offered
/// answer, and the receipt of a matching acknowledgment.</summary>
internal sealed class DialogueExchange
{
    private const int AskPulse = 0, AckPulse = 1;
    private static readonly Field AckCode = new(Shape.Binary(3), 2, 3);

    private const int Started = 0, Debt = 1, Pressure = 2, Offer = 3, Receipt = 4;
    private const int SeenOne = 5, SeenTwo = 6, Ambiguous = 7, Contradiction = 8, Permission = 9;
    // Code zero denotes no clarification. The next two codes identify the probes.
    private static readonly Field Clarification = new(Shape.Binary(3), 10, 11);
    private const int RespondentTurn = 12;

    private static readonly TransitionLaw AcceptAsk = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        state => [0, state[1] | state[0], state[2] | (state[0] & (1 - state[1]))],
        new Accounting(true, new NeverIncreases(0), new NeverDecreases(1), new NeverDecreases(2)));

    // Possibilities, acknowledgment pulse, acknowledgment code, previous offer, permission.
    private static readonly TransitionLaw CheckAcknowledgment = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, AckCode.Shape, Shape.Bit, Shape.Bit],
        state => [state[0], state[1], state[2], state[3], state[4],
            state[2] == 1 && state[0] != state[1] && state[3] == state[4] && state[4] == state[1] ? 1 : 0],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new Unchanged(3), new Unchanged(4)));

    private static readonly TransitionLaw ConsumeAcknowledgment = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit], _ => [0, 0],
        new Accounting(true, new NeverIncreases(0), new NeverIncreases(1)));

    // Only the current singleton can enter the offer slot. Revising an already
    // occupied slot keeps its pressure. Acknowledgment runs before replacement.
    private static readonly TransitionLaw OfferAnswer = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        state => state[2] == 1 && state[0] != state[1]
            ? [state[0], state[1], state[2], 1, state[1]]
            : state,
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new NeverDecreases(3)));

    private static readonly TransitionLaw ChooseRepair = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Clarification.Shape],
        state => [state[0], state[1], state[2], state[0] == 0 ? 0 : state[1] == 1 ? 1 : state[2] == 1 ? 2 : 0],
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));

    private static readonly TransitionLaw GiveTurn = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Clarification.Shape, Shape.Bit],
        state =>
        {
            var canAcknowledge = state[1] == 1 && state[2] == 1 && state[3] == 0;
            state[5] = state[0] == 1 && !canAcknowledge && state[4] == 0 ? 1 : 0;
            return state;
        },
        new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2), new Unchanged(3), new Unchanged(4)));

    private readonly Store events;
    private readonly Store state;

    private DialogueExchange(Engine engine, Store events, Store state)
    {
        Engine = engine;
        this.events = events;
        this.state = state;
    }

    internal Engine Engine { get; }

    internal static DialogueExchange Create(Store possible, Store repairs)
    {
        var possibilities = OpenInput(possible);
        var eligibleRepairs = OpenInput(repairs);
        var events = new Store(4, 1);
        var state = new Store(13, 1);
        var engine = new Engine();

        Binding Event(int plane) => new(events, plane);
        Binding State(int plane) => new(state, plane);
        var first = new Binding(possibilities, 0);
        var second = new Binding(possibilities, 1);
        var clarification = new Binding(state, [.. Clarification.Planes]);

        void Wire(TransitionLaw law, params Binding[] bindings)
        {
            var wiring = new Wiring(law, bindings);
            wiring.Add(new int[bindings.Length]);
            engine.Add(wiring);
        }

        Wire(AcceptAsk, Event(AskPulse), State(Started), State(Debt));
        Wire(InformationTension.SupportPair, first, second, State(SeenOne), State(SeenTwo));
        Wire(CheckAcknowledgment, first, second, Event(AckPulse), new Binding(events, [.. AckCode.Planes]), State(Offer), State(Permission));
        Wire(Flows.When(Flows.Fulfill(1, 1, 1)), State(Pressure), State(Debt), State(Receipt), State(Permission));
        Wire(ConsumeAcknowledgment, Event(AckPulse), State(Permission));
        Wire(OfferAnswer, first, second, State(Debt), State(Pressure), State(Offer));
        Wire(InformationTension.DemandedStatus, State(Debt), State(SeenOne), State(SeenTwo), State(Ambiguous), State(Contradiction));
        Wire(ChooseRepair, State(Ambiguous), new Binding(eligibleRepairs, 0), new Binding(eligibleRepairs, 1), clarification);
        Wire(GiveTurn, State(Debt), State(Pressure), State(SeenOne), State(SeenTwo), clarification, State(RespondentTurn));
        return new DialogueExchange(engine, events, state);
    }

    internal static DialogueExchange Restore(Engine engine)
    {
        var accept = engine.Wirings[0];
        return new DialogueExchange(engine, (Store)accept.Bindings[0].Surface, (Store)accept.Bindings[1].Surface);
    }

    internal void Ask() => events.Write(AskPulse, 0, true);

    internal void Acknowledge(int answer)
    {
        AckCode.Write(events, 0, (uint)answer < 2 ? answer : 2);
        events.Write(AckPulse, 0, true);
    }

    internal DialogueReading Read()
    {
        var debt = state.Read(Debt, 0);
        var pressure = state.Read(Pressure, 0);
        var offeredAnswer = state.Read(Offer, 0) ? 1 : 0;
        var grounded = state.Read(Receipt, 0);
        var repair = Clarification.Read(state, 0) is int code && code > 0 ? code - 1 : (int?)null;
        var turn = state.Read(RespondentTurn, 0) ? DialogueTurn.Respondent : DialogueTurn.Asker;
        return new DialogueReading(debt, pressure, grounded ? offeredAnswer : null, pressure ? offeredAnswer : null,
            state.Read(Ambiguous, 0), state.Read(Contradiction, 0), repair, grounded ? DialogueTurn.Grounded : turn);
    }

    private static Socket OpenInput(Store store)
    {
        var port = store.Publish([0], [0, 1]);
        var socket = Socket.For(port);
        socket.Plug(port);
        return socket;
    }
}
