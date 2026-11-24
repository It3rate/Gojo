using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Scenario #4: A short dialogue — asking, answering, clarifying and
/// grounding in <see cref="EncodingC"/>.
/// <para>Exercises material tension as conversational obligations (<c>T−</c> =
/// unanswered question debt, <c>T+</c> = unacknowledged answer pressure,
/// <c>Value</c> = grounded common ground), information tension in both faces
/// (open deficit and contradiction excess) scoped strictly by which role is
/// demanded rather than collapsing all underlying data, <see cref="Closure"/>
/// choosing the narrowest clarifying question for the question scope, the
/// relation form of the open-value gap (<see cref="TensionLaws.ValueGap"/>),
/// quiet two-party turn-taking (<see cref="Flows.Turn"/>), self-running
/// firmness repair with in-machine <see cref="Settling.Forget"/>, and
/// lexicographic repair-cost ranking (<see cref="TensionLaws.RankCombine"/>).</para></summary>
public sealed class TensionDialogueTests
{
    // ScopeStore positions: 0..5 belong to the questioned car's discourse graph;
    // 6..7 are an unrelated distractor outside the question's closure cone.
    private const int PosCar = 0;
    private const int PosOwner = 1;
    private const int PosColor = 2;
    private const int PosKind = 3;
    private const int PosLot = 4;
    private const int PosIsAlice = 5;
    private const int PosBike = 6;
    private const int PosBikeOwner = 7;
    private const int ScopePositions = 8;

    // Domain codes (structural labels only; no semantic quantification).
    private const int Car0 = 0, Car1 = 1, Car2 = 2, Car3 = 3;
    private const int Alice = 0, Bob = 1, Carol = 2, Dan = 3;
    private const int Red = 0, Blue = 1;
    private const int Sedan = 0, Van = 1;
    private const int North = 0, South = 1;
    private const int No = 0, Yes = 1;

    // Car table (4 tiny cars that separate scope from data):
    //   Car 0: Red,  Sedan, North, Alice
    //   Car 1: Red,  Van,   North, Bob
    //   Car 2: Blue, Sedan, South, Alice
    //   Car 3: Blue, Van,   South, Carol
    private static readonly Shape Quad = Shape.Binary(4);
    private static readonly RelationLaw CarToOwner = RelationLaw.FromPredicate([Quad, Quad], t =>
        t[1] == t[0] switch { Car0 => Alice, Car1 => Bob, Car2 => Alice, _ => Carol });
    private static readonly RelationLaw CarToColor = RelationLaw.FromPredicate([Quad, Shape.Bit], t =>
        t[1] == (t[0] is Car0 or Car1 ? Red : Blue));
    private static readonly RelationLaw CarToKind = RelationLaw.FromPredicate([Quad, Shape.Bit], t =>
        t[1] == (t[0] is Car0 or Car2 ? Sedan : Van));
    private static readonly RelationLaw CarToLot = RelationLaw.FromPredicate([Quad, Shape.Bit], t =>
        t[1] == (t[0] is Car0 or Car1 ? North : South));
    private static readonly RelationLaw OwnerToIsAlice = RelationLaw.FromPredicate([Quad, Shape.Bit], t =>
        t[1] == (t[0] == Alice ? Yes : No));

    private static ulong Mask(params int[] values) => values.Aggregate(0UL, (m, v) => m | (1UL << v));

    /// <summary>Two-speaker dialogue world: <see cref="Asker"/> and
    /// <see cref="Answerer"/> communicate strictly through ports and sockets.
    /// All discourse roles sit as positions on one compact <see cref="ScopeStore"/>
    /// with a single <c>Demand</c> plane so the exact same machine answers any
    /// question scope without rewiring.</summary>
    internal sealed class DialogueWorld
    {
        public required Engine Asker { get; init; }
        public required Store AskerStore { get; init; }
        public required Engine Answerer { get; init; }
        public required Store ScopeStore { get; init; }
        public required Store FloorStore { get; init; }

        public required Supports QuadSupports { get; init; }
        public required Supports BitSupports { get; init; }
        public required Field AskQueue { get; init; }
        public required Field MainGrounded { get; init; }
        public required Field SpokenAnswer { get; init; }
        public required Wiring ClarifyFactWiring { get; init; }
        public required IReadOnlyList<Wiring> KnowledgeWirings { get; init; }

        // ScopeStore plane indices:
        // 0..1 = field code bits (disjoint from support planes as required by Supports)
        // 2..5 = support planes (4 values for Quad roles; 2 values on 2..3 with 4..5 = 0 for Bit roles)
        public const int SupportPlane0 = 2;
        public const int DemandPlane = 6;
        public const int Seen1Plane = 7;
        public const int Seen2Plane = 8;
        public const int DeficitPlane = 9;
        public const int ExcessPlane = 10;
        public const int SettledPlane = 11;

        // AskerStore plane indices (positions = 1)
        public const int AskQueuePlane0 = 0; // cap 2: planes 0..1
        public const int MainGroundedPlane0 = 2; // cap 2: planes 2..3
        public const int AckGatePlane = 4;
        public const int ClarifyReplyPlane = 5;
        public const int ClarifySpokenPlane = 6;

        // FloorStore has 4 positions so each taken port owns a disjoint position (EncodingC single-taker rule):
        //   pos 0 = MainDebt channel, pos 1 = MainPressure channel, pos 2 = Clarify channel, pos 3 = Turn & control gates.
        public const int FloorMainDebtPos = 0;
        public const int FloorMainPressurePos = 1;
        public const int FloorClarifyPos = 2;
        public const int FloorControlPos = 3;

        // FloorStore plane indices
        public const int MainDebtPlane = 0;        // T- : unanswered main question (at FloorMainDebtPos)
        public const int MainPressurePlane = 1;    // T+ : unacknowledged main answer (at FloorMainPressurePos)
        public const int ClarifyBudgetPlane = 2;   // conserved supply for asking 1 clarification (at FloorClarifyPos)
        public const int ClarifyDebtPlane = 3;     // T- : unanswered clarifying sub-question (at FloorClarifyPos)
        public const int ClarifyPressurePlane = 4; // T+ : unacknowledged clarification reply (at FloorClarifyPos)
        public const int ClarifyGroundedPlane = 5; // Value : grounded clarification (at FloorClarifyPos)
        public const int TurnPlane = 6;            // 0 = Asker's floor, 1 = Answerer's floor (at FloorControlPos)
        public const int NeedAskerPlane = 7;
        public const int NeedAnswererPlane = 8;
        public const int CanAckPlane = 9;
        public const int CanAnswerMainPlane = 10;
        public const int CanAskClarifyPlane = 11;
        public const int SpokenAnswerPlane0 = 12;  // 2-bit enacted answer: planes 12..13 (at FloorControlPos)
        public const int NeedAskerOrAskPlane = 14;
        public const int NeedAnswererOrClarifyPlane = 15;
        public const int WantClarifyPlane = 16;
        public const int AskerTurnPlane = 17;      // ~TurnPlane (1 = Asker's floor), published over plain Socket

        public int DemandedPosition { get; }

        public DialogueWorld(int demandedPosition) => DemandedPosition = demandedPosition;

        public int MainDebt => FloorStore.Read(MainDebtPlane, FloorMainDebtPos) ? 1 : 0;
        public int MainPressure => FloorStore.Read(MainPressurePlane, FloorMainPressurePos) ? 1 : 0;
        public int MainAskCount => AskQueue.Read(AskerStore, 0)!.Value;
        public int MainGroundedCount => MainGrounded.Read(AskerStore, 0)!.Value;
        public int ClarifyBudget => FloorStore.Read(ClarifyBudgetPlane, FloorClarifyPos) ? 1 : 0;
        public int ClarifyDebt => FloorStore.Read(ClarifyDebtPlane, FloorClarifyPos) ? 1 : 0;
        public int ClarifyPressure => FloorStore.Read(ClarifyPressurePlane, FloorClarifyPos) ? 1 : 0;
        public int ClarifyGrounded => FloorStore.Read(ClarifyGroundedPlane, FloorClarifyPos) ? 1 : 0;
        public int Turn => FloorStore.Read(TurnPlane, FloorControlPos) ? 1 : 0;
        public bool ScopeDeficit => ScopeStore.Read(DeficitPlane, DemandedPosition);
        public bool ScopeExcess => ScopeStore.Read(ExcessPlane, DemandedPosition);
        public bool ScopeSettled => ScopeStore.Read(SettledPlane, DemandedPosition);

        /// <summary>Total conserved main-exchange tokens across Asker and Floor.</summary>
        public int MainTotal => MainAskCount + MainDebt + MainPressure + MainGroundedCount;

        /// <summary>Total conserved clarification-exchange tokens across Answerer and Floor.</summary>
        public int ClarifyTotal => ClarifyBudget + ClarifyDebt + ClarifyPressure + ClarifyGrounded;

        /// <summary>Steps both speaker engines once; returns true when both are quiet.</summary>
        public bool StepOnce()
        {
            var a = Answerer.Step();
            var b = Asker.Step();
            return a.Steps.All(s => !s.Changed.Any) && b.Steps.All(s => !s.Changed.Any);
        }

        /// <summary>Runs both speakers until both are simultaneously quiet.</summary>
        public int SettleWorld(int limit = 48)
        {
            for (var step = 1; step <= limit; step++)
                if (StepOnce()) return step;
            throw new InvalidOperationException("Dialogue did not come to rest.");
        }
    }

    /// <summary>Builds the two-speaker dialogue world for any demanded
    /// question scope (<paramref name="demandedPosition"/>) and any subset of
    /// candidate cars (<paramref name="carMask"/>).</summary>
    internal static DialogueWorld BuildDialogue(
        ulong carMask,
        int demandedPosition = PosOwner,
        int questionsToAsk = 1,
        bool askerAcknowledges = true,
        int clarifyTargetPosition = PosKind,
        int clarifyFactValue = Sedan,
        int clarifyBudget = 1)
    {
        // ---- Asker store & engine ----
        var askerStore = new Store(7, 1);
        var askQueue = new Field(Shape.Unary(2), DialogueWorld.AskQueuePlane0, DialogueWorld.AskQueuePlane0 + 1);
        var mainGrounded = new Field(Shape.Unary(2), DialogueWorld.MainGroundedPlane0, DialogueWorld.MainGroundedPlane0 + 1);
        askQueue.Write(askerStore, 0, questionsToAsk);
        askerStore.Write(DialogueWorld.AckGatePlane, 0, askerAcknowledges);
        askerStore.Write(DialogueWorld.ClarifyReplyPlane, 0, clarifyFactValue != 0);

        // ---- Answerer ScopeStore (all 8 discourse roles on one compact store) ----
        // Planes 0..1: field bits; 2..5: support planes (Quad uses 2..5; Bit uses 2..3 with 4..5 = 0)
        // Plane 6: Demand, 7: Seen1, 8: Seen2, 9: Deficit, 10: Excess, 11: Settled
        var scopeStore = new Store(12, ScopePositions);
        var quadSupports = new Supports(scopeStore, new Field(Quad, 0, 1), 2, 3, 4, 5);
        var bitSupports = new Supports(scopeStore, Field.Bit(0), 2, 3);

        // Initialize all roles open within their domain width (2 or 4 values).
        quadSupports.Forget(PosCar);
        quadSupports.Forget(PosOwner);
        quadSupports.Forget(PosBike);
        quadSupports.Forget(PosBikeOwner);
        bitSupports.Forget(PosColor);
        bitSupports.Forget(PosKind);
        bitSupports.Forget(PosLot);
        bitSupports.Forget(PosIsAlice);

        // Narrow Car referent to the contextually visible candidates.
        quadSupports.Restrict(PosCar, carMask);

        // Mark ONLY the demanded question scope! All other roles stay demand = 0.
        scopeStore.Write(DialogueWorld.DemandPlane, demandedPosition, true);

        // ---- FloorStore (4 disjoint channel positions + quiet turn-taking) ----
        const int pDebt = DialogueWorld.FloorMainDebtPos;
        const int pPres = DialogueWorld.FloorMainPressurePos;
        const int pClar = DialogueWorld.FloorClarifyPos;
        const int pCtrl = DialogueWorld.FloorControlPos;
        var floorStore = new Store(18, 4);
        floorStore.Write(DialogueWorld.ClarifyBudgetPlane, pClar, clarifyBudget > 0);
        floorStore.Write(DialogueWorld.AskerTurnPlane, pCtrl, true); // Turn starts 0 (Asker's floor)
        var spokenAnswer = new Field(Quad, DialogueWorld.SpokenAnswerPlane0, DialogueWorld.SpokenAnswerPlane0 + 1);

        // ---- Ports & sockets between Asker and Answerer ----
        // Asker publishes read-only lanes to Answerer: [0]=AskQueue0, [1]=AskQueue1, [2]=AckGate, [3]=ClarifyReply, [4]=ClarifySpoken
        var askerPort = askerStore.Publish([0], [0, 1, DialogueWorld.AckGatePlane, DialogueWorld.ClarifyReplyPlane, DialogueWorld.ClarifySpokenPlane]);
        var askerSock = Socket.For(askerPort);
        askerSock.Plug(askerPort);

        // Answerer publishes one dedicated port per channel on disjoint positions (single-taker rule):
        var mainDebtPort = floorStore.Publish([pDebt], [DialogueWorld.MainDebtPlane]);
        var mainPressurePort = floorStore.Publish([pPres], [DialogueWorld.MainPressurePlane]);
        var clarifyPort = floorStore.Publish([pClar], [DialogueWorld.ClarifyDebtPlane, DialogueWorld.ClarifyPressurePlane]);
        var turnPort = floorStore.Publish([pCtrl], [DialogueWorld.AskerTurnPlane]);

        var mainDebtSock = Socket.For(mainDebtPort); mainDebtSock.Plug(mainDebtPort);
        var mainPressureSock = Socket.For(mainPressurePort); mainPressureSock.Plug(mainPressurePort);
        var clarifySock = Socket.For(clarifyPort); clarifySock.Plug(clarifyPort);
        var turnSock = Socket.For(turnPort); turnSock.Plug(turnPort);

        // ---- Answerer engine: knowledge relations + scope observer + floor ----
        var answerer = new Engine().Hold(scopeStore, floorStore);

        var rOwner = new Wiring(CarToOwner, quadSupports.Variable, quadSupports.Variable);
        rOwner.Add(PosCar, PosOwner);
        var rColor = new Wiring(CarToColor, quadSupports.Variable, bitSupports.Variable);
        rColor.Add(PosCar, PosColor);
        var rKind = new Wiring(CarToKind, quadSupports.Variable, bitSupports.Variable);
        rKind.Add(PosCar, PosKind);
        var rLot = new Wiring(CarToLot, quadSupports.Variable, bitSupports.Variable);
        rLot.Add(PosCar, PosLot);
        var rIsAlice = new Wiring(OwnerToIsAlice, quadSupports.Variable, bitSupports.Variable);
        rIsAlice.Add(PosOwner, PosIsAlice);
        // Unrelated distractor in the same store (outside the questioned car's closure cone).
        var rBike = new Wiring(CarToOwner, quadSupports.Variable, quadSupports.Variable);
        rBike.Add(PosBike, PosBikeOwner);

        // Dormant clarification observation row: reads Asker's private ClarifyReply port
        // into the chosen clarifying role ONLY after Asker speaks (ClarifySpoken = 1).
        var clarifyFact = new Wiring(Settling.Same(Shape.Bit), new Binding(askerSock, 3), bitSupports.Variable);
        clarifyFact.AddSpare(0, clarifyTargetPosition);

        var recruitClarifyFact = new Wiring(Settling.Latch(true), new Binding(askerSock, 4), new Binding(clarifyFact.Rows.Life, 0));
        recruitClarifyFact.Add(0, 0);

        answerer.Add(rOwner).Add(rColor).Add(rKind).Add(rLot).Add(rIsAlice).Add(rBike)
            .Add(recruitClarifyFact).Add(clarifyFact);

        // Single shared 4-plane combinational support observer across ALL 8 scope positions!
        // Because bit-roles have planes 2..3 = 0, the exact same 4 wirings cover both 4-value and 2-value scopes.
        Tension.ObserveSupports(
            answerer,
            quadSupports,
            scopeStore,
            DialogueWorld.DemandPlane,
            scopeStore,
            DialogueWorld.Seen1Plane,
            DialogueWorld.Seen2Plane,
            DialogueWorld.DeficitPlane,
            DialogueWorld.ExcessPlane);

        // Settled bit on ScopeStore: settled = seen1 & ~seen2 (exact singleton!)
        var settledWiring = new Wiring(
            TensionLaws.And,
            new Binding(scopeStore, DialogueWorld.Seen1Plane),
            new Binding(new Flipped(scopeStore, DialogueWorld.Seen2Plane), DialogueWorld.Seen2Plane),
            new Binding(scopeStore, DialogueWorld.SettledPlane));
        for (var p = 0; p < ScopePositions; p++) settledWiring.Add(p, p, p);
        answerer.Add(settledWiring);

        // Answerer decision gates (reading the demanded position's Settled / Deficit planes)
        var canAck = new Wiring(
            TensionLaws.And,
            new Binding(floorStore, DialogueWorld.MainPressurePlane),
            new Binding(askerSock, 2),
            new Binding(floorStore, DialogueWorld.CanAckPlane));
        canAck.Add(pPres, 0, pCtrl);

        var canAnswerMain = new Wiring(
            TensionLaws.And,
            new Binding(floorStore, DialogueWorld.MainDebtPlane),
            new Binding(scopeStore, DialogueWorld.SettledPlane),
            new Binding(floorStore, DialogueWorld.CanAnswerMainPlane));
        canAnswerMain.Add(pDebt, demandedPosition, pCtrl);

        var wantClarify = new Wiring(
            TensionLaws.And,
            new Binding(floorStore, DialogueWorld.MainDebtPlane),
            new Binding(scopeStore, DialogueWorld.DeficitPlane),
            new Binding(floorStore, DialogueWorld.WantClarifyPlane));
        wantClarify.Add(pDebt, demandedPosition, pCtrl);

        var canAskClarify = new Wiring(
            TensionLaws.And,
            new Binding(floorStore, DialogueWorld.WantClarifyPlane),
            new Binding(floorStore, DialogueWorld.ClarifyBudgetPlane),
            new Binding(floorStore, DialogueWorld.CanAskClarifyPlane));
        canAskClarify.Add(pCtrl, pClar, pCtrl);

        // NeedAsker = (AskQueue > 0) | ClarifyDebt | CanAck
        var needAsker1 = new Wiring(
            TensionLaws.Or,
            new Binding(askerSock, 0),
            new Binding(floorStore, DialogueWorld.ClarifyDebtPlane),
            new Binding(floorStore, DialogueWorld.NeedAskerOrAskPlane));
        needAsker1.Add(0, pClar, pCtrl);

        var needAsker = new Wiring(
            TensionLaws.Or,
            new Binding(floorStore, DialogueWorld.NeedAskerOrAskPlane),
            new Binding(floorStore, DialogueWorld.CanAckPlane),
            new Binding(floorStore, DialogueWorld.NeedAskerPlane));
        needAsker.Add(pCtrl, pCtrl, pCtrl);

        // NeedAnswerer = ClarifyPressure | CanAnswerMain | CanAskClarify
        var needAns1 = new Wiring(
            TensionLaws.Or,
            new Binding(floorStore, DialogueWorld.ClarifyPressurePlane),
            new Binding(floorStore, DialogueWorld.CanAnswerMainPlane),
            new Binding(floorStore, DialogueWorld.NeedAnswererOrClarifyPlane));
        needAns1.Add(pClar, pCtrl, pCtrl);

        var needAnswerer = new Wiring(
            TensionLaws.Or,
            new Binding(floorStore, DialogueWorld.NeedAnswererOrClarifyPlane),
            new Binding(floorStore, DialogueWorld.CanAskClarifyPlane),
            new Binding(floorStore, DialogueWorld.NeedAnswererPlane));
        needAnswerer.Add(pCtrl, pCtrl, pCtrl);

        // Quiet two-party turn-taking law (8 bytes RAM, stops when both needs are 0!)
        var turnWiring = new Wiring(
            Flows.Turn,
            new Binding(floorStore, DialogueWorld.NeedAskerPlane),
            new Binding(floorStore, DialogueWorld.NeedAnswererPlane),
            new Binding(floorStore, DialogueWorld.TurnPlane));
        turnWiring.Add(pCtrl, pCtrl, pCtrl);

        // Expose ~Turn as a plain store plane (AskerTurnPlane) so Asker binds a plain Socket
        // (Transport serializes Flipped(Store) directly without changing the wire format).
        var askerTurnMirror = new Wiring(
            Settling.Justify(1),
            new Binding(new Flipped(floorStore, DialogueWorld.TurnPlane), DialogueWorld.TurnPlane),
            new Binding(floorStore, DialogueWorld.AskerTurnPlane));
        askerTurnMirror.Add(pCtrl, pCtrl);

        // Answerer acts ONLY when Turn == 1:
        // 1. Ground clarification reply (ClarifyPressure -> ClarifyGrounded)
        var groundClarify = new Wiring(
            Flows.When(Flows.Move(1, 1), 1),
            new Binding(floorStore, DialogueWorld.ClarifyPressurePlane),
            new Binding(floorStore, DialogueWorld.ClarifyGroundedPlane),
            new Binding(floorStore, DialogueWorld.TurnPlane));
        groundClarify.Add(pClar, pClar, pCtrl);

        // 2. Ask clarifying sub-question (ClarifyBudget -> ClarifyDebt) when ambiguous
        var askClarify = new Wiring(
            Flows.When(Flows.Move(1, 1), 2),
            new Binding(floorStore, DialogueWorld.ClarifyBudgetPlane),
            new Binding(floorStore, DialogueWorld.ClarifyDebtPlane),
            new Binding(floorStore, DialogueWorld.TurnPlane),
            new Binding(floorStore, DialogueWorld.WantClarifyPlane));
        askClarify.Add(pClar, pClar, pCtrl, pCtrl);

        // 3. Enact settled answer from the demanded scope into SpokenAnswer
        var enactAnswer = new Wiring(
            Settling.Enact(Quad),
            new Binding(scopeStore, [.. quadSupports.Planes]),
            spokenAnswer.On(floorStore));
        enactAnswer.Add(demandedPosition, pCtrl);

        // 4. Answer main question: move obligation debt T- -> answer pressure T+
        var answerMain = new Wiring(
            Flows.When(Flows.Move(1, 1), 2),
            new Binding(floorStore, DialogueWorld.MainDebtPlane),
            new Binding(floorStore, DialogueWorld.MainPressurePlane),
            new Binding(floorStore, DialogueWorld.TurnPlane),
            new Binding(scopeStore, DialogueWorld.SettledPlane));
        answerMain.Add(pDebt, pPres, pCtrl, demandedPosition);

        answerer
            .Add(canAck).Add(canAnswerMain).Add(wantClarify).Add(canAskClarify)
            .Add(needAsker1).Add(needAsker).Add(needAns1).Add(needAnswerer)
            .Add(turnWiring).Add(askerTurnMirror)
            .Add(groundClarify).Add(askClarify).Add(enactAnswer).Add(answerMain);

        // ---- Asker engine: acts ONLY when AskerTurn == 1 (plain turnSock lane 0) ----
        var asker = new Engine().Hold(askerStore);

        // 1. Post main question: AskQueue -> MainDebt (conserved!)
        var postQuestion = new Wiring(
            Flows.When(Flows.Move(2, 1), 1),
            askQueue.On(askerStore),
            new Binding(mainDebtSock, 0),
            new Binding(turnSock, 0));
        postQuestion.Add(0, 0, 0);

        // 2. Answer clarifying sub-question: ClarifyDebt -> ClarifyPressure AND latch ClarifySpoken = 1
        var replyClarify = new Wiring(
            Flows.When(Flows.Move(1, 1), 1),
            new Binding(clarifySock, 0),
            new Binding(clarifySock, 1),
            new Binding(turnSock, 0));
        replyClarify.Add(0, 0, 0);

        // Latch ClarifySpoken = 1 once ClarifyPressure = 1 (so Asker's private reply becomes observable!)
        var speakClarify = new Wiring(
            Settling.Latch(true),
            new Binding(clarifySock, 1),
            new Binding(askerStore, DialogueWorld.ClarifySpokenPlane));
        speakClarify.Add(0, 0);

        // 3. Ground main answer: MainPressure -> Asker.MainGrounded when AskerTurn == 1 & AckGate == 1
        var groundMain = new Wiring(
            Flows.When(Flows.Move(1, 2), 2),
            new Binding(mainPressureSock, 0),
            mainGrounded.On(askerStore),
            new Binding(turnSock, 0),
            new Binding(askerStore, DialogueWorld.AckGatePlane));
        groundMain.Add(0, 0, 0, 0);

        asker.Add(postQuestion).Add(replyClarify).Add(speakClarify).Add(groundMain);

        return new DialogueWorld(demandedPosition)
        {
            Asker = asker,
            AskerStore = askerStore,
            Answerer = answerer,
            ScopeStore = scopeStore,
            FloorStore = floorStore,
            QuadSupports = quadSupports,
            BitSupports = bitSupports,
            AskQueue = askQueue,
            MainGrounded = mainGrounded,
            SpokenAnswer = spokenAnswer,
            ClarifyFactWiring = clarifyFact,
            KnowledgeWirings = [rOwner, rColor, rKind, rLot, rIsAlice, rBike],
        };
    }

    /// <summary>Structurally picks the narrowest clarifying question for a
    /// demanded <paramref name="question"/> using ONLY <see cref="Question.Cone"/>
    /// and isolated <see cref="Workspace"/> narrowing. Never looks at domain
    /// names and resolves strictly by the question's demanded scope, not by
    /// collapsing the underlying referent to a singleton.</summary>
    private static (int Position, int WorstScopeRemaining, int QuestionWidth) ChooseNarrowestClarification(
        DialogueWorld world,
        Question question)
    {
        // Step 1: Grow closure cone from the questioned scope (reach 1 covers Car and its attributes,
        // and excludes unrelated distractors like Bike / BikeOwner).
        var cone = question.Cone(world.Answerer, reach: 1);
        var inCone = cone.Of(world.ScopeStore);

        // Collect all open candidate roles in the cone (excluding the questioned scope itself,
        // unless nothing narrower is available).
        var candidates = new List<(int Pos, int WorstScope, int Width)>();
        foreach (var pos in inCone.Indexes())
        {
            if (pos == question.Position) continue;
            var isQuad = pos is PosCar or PosOwner or PosBike or PosBikeOwner;
            var supp = isQuad ? world.QuadSupports : world.BitSupports;
            var mask = supp.Values(pos);
            var width = System.Numerics.BitOperations.PopCount(mask);
            if (width <= 1) continue; // already settled; asking it adds nothing

            var worstRemaining = 0;
            for (var v = 0; v < (isQuad ? 4 : 2); v++)
            {
                if (((mask >> v) & 1UL) == 0) continue;

                // Isolated Workspace trial: suppose candidate role `pos` has value `v`,
                // narrow ONLY the knowledge relations in the cone, and measure remaining
                // answers in the QUESTION SCOPE (not in `PosCar`!).
                var ws = new Workspace(world.ScopeStore).SelectAll();
                var wsQuad = world.QuadSupports.In(ws);
                var wsBit = world.BitSupports.In(ws);
                (isQuad ? wsQuad : wsBit).Restrict(pos, 1UL << v);

                var wsWirings = new List<Wiring>();
                foreach (var kw in world.KnowledgeWirings)
                {
                    if (cone.Rows(kw).None) continue;
                    var bindings = kw.Bindings.Select(b =>
                        b.Planes.Count == 4 ? wsQuad.Variable : wsBit.Variable).ToArray();
                    var w = new Wiring(kw.Law, bindings);
                    foreach (var row in cone.Rows(kw).Indexes()) w.Add(kw.Row(row));
                    wsWirings.Add(w);
                }

                for (var iter = 0; iter < 8; iter++)
                {
                    var changed = false;
                    foreach (var w in wsWirings) changed |= ws.Apply(w).Step.Changed.Any;
                    if (!changed) break;
                }

                var qIsQuad = question.Position is PosCar or PosOwner or PosBike or PosBikeOwner;
                var qMask = (qIsQuad ? wsQuad : wsBit).Values(question.Position);
                var remaining = System.Numerics.BitOperations.PopCount(qMask);
                if (remaining > worstRemaining) worstRemaining = remaining;
            }

            candidates.Add((pos, worstRemaining, width));
        }

        // Strict lexicographic preference:
        // Tier 1: smallest worst-case remaining answers in the QUESTION SCOPE (1 = settles scope)
        // Tier 2: smallest clarifying question width (narrowest question wins: 2-way beats 3-way!)
        return candidates
            .OrderBy(c => c.WorstScope)
            .ThenBy(c => c.Width)
            .First();
    }

    /// <summary>1. Clear single-referent Wh-question ("Whose car is this?"):
    /// obligation arrives as debt (<c>T−</c>), Answerer takes the floor via
    /// <see cref="Flows.Turn"/>, enacts the settled owner and converts debt
    /// into answer pressure (<c>T+</c>). While Asker withholds acknowledgement,
    /// <c>T+</c> waits visibly without disappearing; once acknowledged, it
    /// absorbs into Grounded and both engines settle quietly with strict
    /// conservation and exact <see cref="Transport.SaveLocals"/> replay.</summary>
    [Fact]
    public void ClearQuestionTurnsDebtIntoAnswerPressureThenGroundedWithQuietTurnTaking()
    {
        var world = BuildDialogue(carMask: Mask(Car1), demandedPosition: PosOwner, questionsToAsk: 1, askerAcknowledges: false);
        Assert.Equal(1, world.MainTotal);

        // Settle with AckGate = 0: Asker asks -> Answerer answers Bob -> answer pressure T+ waits!
        world.SettleWorld();
        Assert.Equal(0, world.MainAskCount);
        Assert.Equal(0, world.MainDebt);
        Assert.Equal(1, world.MainPressure);
        Assert.Equal(0, world.MainGroundedCount);
        Assert.Equal(1, world.MainTotal);
        Assert.Equal(Bob, world.SpokenAnswer.Read(world.FloorStore, DialogueWorld.FloorControlPos));
        Assert.Equal(0, world.ClarifyGrounded); // no clarification was needed
        Assert.False(world.ScopeDeficit);
        Assert.False(world.ScopeExcess);

        // Save mid-dialogue (while T+ = 1 waits for acknowledgement), restore, and ground!
        var saved = Transport.SaveLocals(world.Answerer, world.Asker);
        var restored = Transport.LoadLocals(saved);
        var rFloor = restored[0].Stores.Single(s => s.PlaneCount == 18 && s.Count == 4);
        var rAsker = restored[1].Stores.Single(s => s.PlaneCount == 7 && s.Count == 1);
        Assert.True(rFloor.Read(DialogueWorld.MainPressurePlane, DialogueWorld.FloorMainPressurePos));

        // Open AckGate on both live and restored Asker -> answer pressure grounds cleanly!
        world.AskerStore.Write(DialogueWorld.AckGatePlane, 0, true);
        world.SettleWorld();
        Assert.Equal((0, 0, 0, 1), (world.MainAskCount, world.MainDebt, world.MainPressure, world.MainGroundedCount));
        Assert.Equal(1, world.MainTotal);

        rAsker.Write(DialogueWorld.AckGatePlane, 0, true);
        for (var i = 0; i < 16; i++)
        {
            var s0 = restored[0].Engine.Step();
            var s1 = restored[1].Engine.Step();
            if (s0.Steps.All(x => !x.Changed.Any) && s1.Steps.All(x => !x.Changed.Any)) break;
        }
        Assert.False(rFloor.Read(DialogueWorld.MainPressurePlane, DialogueWorld.FloorMainPressurePos));
        Assert.Equal(1, world.MainGrounded.Read(rAsker, 0));
    }

    /// <summary>2. Resolution follows the QUESTION SCOPE, not the underlying
    /// data: the exact same ambiguous referent <c>{Car 0, Car 2}</c> settles
    /// immediately with zero tension when asked "Whose car?" (<c>Owner = Alice</c>)
    /// or "Sedan or van?" (<c>Kind = Sedan</c>), yet raises information deficit
    /// when asked "What color?" or "Which car?". Likewise, on <c>{Car 1, Car 3}</c>,
    /// the polar question "Is this Alice's car?" settles to <c>No</c> even though
    /// both the car and the owner stay ambiguous.</summary>
    [Fact]
    public void SameAmbiguousDataResolvesOrStaysOpenStrictlyByQuestionScope()
    {
        // Car 0 (Red, Sedan, North, Alice) and Car 2 (Blue, Sedan, South, Alice)
        var twoAliceCars = Mask(Car0, Car2);

        // Scope = Owner ("Whose car?"): SETTLED (Alice) while Car, Color, Lot remain ambiguous!
        var whose = BuildDialogue(twoAliceCars, demandedPosition: PosOwner, clarifyBudget: 0);
        whose.SettleWorld();
        Assert.True(whose.ScopeSettled);
        Assert.False(whose.ScopeDeficit);
        Assert.Equal(Alice, whose.SpokenAnswer.Read(whose.FloorStore, DialogueWorld.FloorControlPos));
        Assert.Equal(1, whose.MainGroundedCount);
        Assert.Equal(twoAliceCars, whose.QuadSupports.Values(PosCar));      // Car STILL ambiguous!
        Assert.Equal(Mask(Red, Blue), whose.BitSupports.Values(PosColor));  // Color STILL ambiguous!
        Assert.Equal(Mask(North, South), whose.BitSupports.Values(PosLot)); // Lot STILL ambiguous!

        // Scope = Kind ("Sedan or van?"): SETTLED (Sedan) on the same ambiguous cars!
        var whatKind = BuildDialogue(twoAliceCars, demandedPosition: PosKind, clarifyBudget: 0);
        whatKind.SettleWorld();
        Assert.True(whatKind.ScopeSettled);
        Assert.False(whatKind.ScopeDeficit);
        Assert.Equal(Sedan, whatKind.SpokenAnswer.Read(whatKind.FloorStore, DialogueWorld.FloorControlPos));
        Assert.Equal(1, whatKind.MainGroundedCount);

        // Scope = Color ("What color?"): AMBIGUOUS under demand -> deficit = 1, debt stays unpaid!
        var whatColor = BuildDialogue(twoAliceCars, demandedPosition: PosColor, clarifyBudget: 0);
        whatColor.SettleWorld();
        Assert.False(whatColor.ScopeSettled);
        Assert.True(whatColor.ScopeDeficit);
        Assert.Equal(1, whatColor.MainDebt);
        Assert.Equal(0, whatColor.MainGroundedCount);

        // Scope = Car ("Which exact car?"): AMBIGUOUS under demand -> deficit = 1, debt stays unpaid!
        var whichCar = BuildDialogue(twoAliceCars, demandedPosition: PosCar, clarifyBudget: 0);
        whichCar.SettleWorld();
        Assert.False(whichCar.ScopeSettled);
        Assert.True(whichCar.ScopeDeficit);
        Assert.Equal(1, whichCar.MainDebt);

        // Polar vs. Wh-question on Car 1 (Bob) and Car 3 (Carol):
        var bobOrCarol = Mask(Car1, Car3);

        // Wh-question ("Whose car?"): ambiguous between Bob and Carol -> deficit = 1!
        var whOwner = BuildDialogue(bobOrCarol, demandedPosition: PosOwner, clarifyBudget: 0);
        whOwner.SettleWorld();
        Assert.True(whOwner.ScopeDeficit);
        Assert.Equal(Mask(Bob, Carol), whOwner.QuadSupports.Values(PosOwner));
        Assert.Equal(1, whOwner.MainDebt);

        // Polar question ("Is this Alice's car?"): SETTLED (No) even though Car AND Owner stay ambiguous!
        var polarAlice = BuildDialogue(bobOrCarol, demandedPosition: PosIsAlice, clarifyBudget: 0);
        polarAlice.SettleWorld();
        Assert.True(polarAlice.ScopeSettled);
        Assert.False(polarAlice.ScopeDeficit);
        Assert.Equal(No, polarAlice.SpokenAnswer.Read(polarAlice.FloorStore, DialogueWorld.FloorControlPos));
        Assert.Equal(1, polarAlice.MainGroundedCount);
        Assert.Equal(bobOrCarol, polarAlice.QuadSupports.Values(PosCar));         // Car still open!
        Assert.Equal(Mask(Bob, Carol), polarAlice.QuadSupports.Values(PosOwner)); // Owner still open!
    }

    /// <summary>3. Closure + Workspace selects the narrowest clarifying question
    /// for the demanded scope (ignoring unrelated distractors outside the cone),
    /// chooses different clarifications for different scopes on the exact same
    /// 3-car world, and then runs the full two-speaker clarification sub-dialogue
    /// autonomously to grounding—leaving <c>Car</c> ambiguous once <c>Owner</c>
    /// settles.</summary>
    [Fact]
    public void ClosureChoosesNarrowestClarifyingQuestionForTheDemandedScopeAndRunsTwoSpeakerClarification()
    {
        // Car 0 (Red, Sedan, North, Alice), Car 1 (Red, Van, North, Bob), Car 2 (Blue, Sedan, South, Alice)
        var threeCars = Mask(Car0, Car1, Car2);

        // A. Scope = Owner ("Whose car?"):
        //    Color fails (Red leaves {Alice, Bob}); Lot fails (North leaves {Alice, Bob});
        //    Car settles Owner but has width 3; Kind settles Owner in both branches with width 2!
        var ownerProbe = BuildDialogue(threeCars, demandedPosition: PosOwner, questionsToAsk: 0);
        ownerProbe.Answerer.Settle(12);
        var ownerQ = new Question(ownerProbe.QuadSupports, PosOwner);
        var ownerCone = ownerQ.Cone(ownerProbe.Answerer, reach: 1);
        Assert.True(ownerCone.Of(ownerProbe.ScopeStore)[PosCar]);
        Assert.True(ownerCone.Of(ownerProbe.ScopeStore)[PosKind]);
        Assert.False(ownerCone.Of(ownerProbe.ScopeStore)[PosBike]);       // unrelated distractor excluded!
        Assert.False(ownerCone.Of(ownerProbe.ScopeStore)[PosBikeOwner]);

        var pickForOwner = ChooseNarrowestClarification(ownerProbe, ownerQ);
        Assert.Equal((PosKind, 1, 2), pickForOwner);

        // B. Scope = Color ("What color?") on the EXACT SAME 3 cars:
        //    Now Kind fails (Sedan leaves {Red, Blue}) and Owner fails (Alice leaves {Red, Blue}),
        //    while Lot (North -> Red, South -> Blue) settles Color with width 2!
        var colorProbe = BuildDialogue(threeCars, demandedPosition: PosColor, questionsToAsk: 0);
        colorProbe.Answerer.Settle(12);
        var pickForColor = ChooseNarrowestClarification(colorProbe, new Question(colorProbe.BitSupports, PosColor));
        Assert.Equal((PosLot, 1, 2), pickForColor);

        // C. Now run the full two-speaker self-running clarification dialogue for "Whose car?"
        //    wired to the chosen clarifying role (PosKind) while Asker privately knows Kind = Sedan!
        var world = BuildDialogue(
            threeCars,
            demandedPosition: PosOwner,
            questionsToAsk: 1,
            askerAcknowledges: true,
            clarifyTargetPosition: pickForOwner.Position,
            clarifyFactValue: Sedan,
            clarifyBudget: 1);

        Assert.False(world.ClarifyFactWiring.Live[0]); // private fact is NOT visible before clarification!
        var steps = world.SettleWorld(48);
        Assert.True(steps > 1);

        // Both main question AND clarification sub-dialogue settled and grounded!
        Assert.True(world.ClarifyFactWiring.Live[0]);
        Assert.Equal((0, 0, 0, 1), (world.MainAskCount, world.MainDebt, world.MainPressure, world.MainGroundedCount));
        Assert.Equal((0, 0, 0, 1), (world.ClarifyBudget, world.ClarifyDebt, world.ClarifyPressure, world.ClarifyGrounded));
        Assert.Equal(1, world.MainTotal);
        Assert.Equal(1, world.ClarifyTotal);
        Assert.Equal(Alice, world.SpokenAnswer.Read(world.FloorStore, DialogueWorld.FloorControlPos));
        Assert.False(world.ScopeDeficit);
        Assert.False(world.ScopeExcess);

        // Crucially: Owner is settled (Alice), while Car STILL stays ambiguous {Car 0, Car 2}!
        Assert.Equal(Mask(Alice), world.QuadSupports.Values(PosOwner));
        Assert.Equal(Mask(Car0, Car2), world.QuadSupports.Values(PosCar));
        Assert.Equal(Mask(Red, Blue), world.BitSupports.Values(PosColor));
    }

    /// <summary>4. The relation form of the open-value gap
    /// (<see cref="TensionLaws.ValueGap"/>): compares two open categorical
    /// values without reducing them to counts, settles disagreement on disjoint
    /// open sets, keeps overlapping open sets as an open gap raising demanded
    /// deficit, narrows backward under goal assertion (<c>gap = 0</c> or
    /// <c>gap = 1</c>), and gates Asker's grounding so a mismatched spoken
    /// answer stays held as <c>T+</c> pressure.</summary>
    [Fact]
    public void OpenValueGapComparesOpenSupportsForwardAndBackwardAndBlocksMismatchedGrounding()
    {
        // Four test positions: 0=equal singletons, 1=different singletons, 2=disjoint open, 3=overlapping open
        var store = new Store(0, 4);
        var left = Supports.On(store, new Field(Quad, store.AddPlane(), store.AddPlane()));
        var right = Supports.On(store, new Field(Quad, store.AddPlane(), store.AddPlane()));
        var gap = Supports.On(store, Field.Bit(store.AddPlane()));
        var demand = store.AddPlane();
        var seen1 = store.AddPlane();
        var seen2 = store.AddPlane();
        var deficit = store.AddPlane();
        var excess = store.AddPlane();

        left.Restrict(0, Mask(Alice)); right.Restrict(0, Mask(Alice));
        left.Restrict(1, Mask(Alice)); right.Restrict(1, Mask(Bob));
        left.Restrict(2, Mask(Alice, Bob)); right.Restrict(2, Mask(Carol, Dan)); // disjoint open!
        left.Restrict(3, Mask(Alice, Bob)); right.Restrict(3, Mask(Alice));      // overlapping open!
        for (var p = 0; p < 4; p++) store.Write(demand, p, true);

        var gapLaw = TensionLaws.ValueGap(Quad);
        Assert.Equal(32, gapLaw.BitCount); // 4 * 4 * 2 = 32 bits (4 bytes!)

        var compare = new Wiring(gapLaw, left.Variable, right.Variable, gap.Variable);
        for (var p = 0; p < 4; p++) compare.Add(p, p, p);
        var engine = new Engine().Add(compare);
        Tension.ObserveSupports(engine, gap, store, demand, store, seen1, seen2, deficit, excess);
        Assert.NotNull(engine.Settle(16));

        Assert.Equal(Mask(0), gap.Values(0));    // equal -> no gap (0)
        Assert.Equal(Mask(1), gap.Values(1));    // different -> definite gap (1)
        Assert.Equal(Mask(1), gap.Values(2));    // disjoint open -> definite gap (1) while both stay open!
        Assert.Equal(Mask(0, 1), gap.Values(3)); // overlapping open -> open gap {0, 1}!
        Assert.Equal(Mask(Alice, Bob), left.Values(2));
        Assert.Equal(Mask(Carol, Dan), right.Values(2));
        Assert.False(store.Read(deficit, 2)); // gap settled to {1}
        Assert.True(store.Read(deficit, 3));  // open gap under demand raises info deficit!

        // Backward goal narrowing in a Workspace:
        // (a) Assert gap = 0 ("they must agree") on pos 3 -> narrows left {Alice, Bob} to {Alice}!
        var agreeWs = new Workspace(store).SelectAll();
        var agreeGap = gap.In(agreeWs);
        var agreeLeft = left.In(agreeWs);
        agreeGap.Restrict(3, Mask(0));
        var wsCompare = new Wiring(gapLaw, agreeLeft.Variable, right.In(agreeWs).Variable, agreeGap.Variable);
        wsCompare.Add(3, 3, 3);
        agreeWs.Apply(wsCompare);
        Assert.Equal(Mask(Alice), agreeLeft.Values(3));

        // (b) Assert gap = 1 ("NOT Alice!") on pos 3 -> rules out Alice, leaving {Bob}!
        var notWs = new Workspace(store).SelectAll();
        var notGap = gap.In(notWs);
        var notLeft = left.In(notWs);
        notGap.Restrict(3, Mask(1));
        var wsNot = new Wiring(gapLaw, notLeft.Variable, right.In(notWs).Variable, notGap.Variable);
        wsNot.Add(3, 3, 3);
        notWs.Apply(wsNot);
        Assert.Equal(Mask(Bob), notLeft.Values(3));

        // (c) Grounding gate driven by ValueGap: Asker expects Alice, Answerer speaks Bob ->
        //     gap settles to {1} (disagrees) -> acceptance gate (gap's 0-support plane) drops to 0 ->
        //     answer pressure T+ waits on the floor instead of grounding!
        var floor = new Store(0, 1);
        var grounded = new Field(Shape.Unary(1), floor.AddPlane());
        var answerTension = new Tension(floor, grounded, capacity: 1);
        answerTension.Receive(Plane.Of(1, 0)); // T+ = 1 answer waiting

        var expected = Supports.On(floor, new Field(Quad, floor.AddPlane(), floor.AddPlane()));
        var spoken = Supports.On(floor, new Field(Quad, floor.AddPlane(), floor.AddPlane()));
        var groundGap = Supports.On(floor, Field.Bit(floor.AddPlane()));
        expected.Restrict(0, Mask(Alice));
        spoken.Restrict(0, Mask(Bob));

        var checkGround = new Wiring(gapLaw, expected.Variable, spoken.Variable, groundGap.Variable);
        checkGround.Add(0, 0, 0);
        // Absorb T+ into Grounded ONLY when groundGap's 0-possible plane is 1 AND 1-possible plane is 0 (settled agreement!)
        var agreeBit = floor.AddPlane();
        var isAgreed = new Wiring(
            TensionLaws.And,
            new Binding(groundGap.Surface, groundGap.Planes[0]),
            new Binding(new Flipped(groundGap.Surface, groundGap.Planes[1]), groundGap.Planes[1]),
            new Binding(floor, agreeBit));
        isAgreed.Add(0, 0, 0);
        var absorb = answerTension.Absorb(Plane.Of(1, 0), new Binding(floor, agreeBit));

        var gEngine = new Engine().Add(checkGround).Add(isAgreed).Add(absorb);
        Assert.NotNull(gEngine.Settle(16));
        Assert.Equal(Mask(1), groundGap.Values(0)); // mismatch!
        Assert.Equal(1, answerTension.Plus.Read(floor, 0)); // pressure stays visibly held!
        Assert.Equal(0, grounded.Read(floor, 0));           // NOT silently grounded!

        // Now correct spoken claim to Alice and reopen groundGap -> agreement settles and T+ grounds!
        spoken.Forget(0);
        spoken.Restrict(0, Mask(Alice));
        groundGap.Forget(0);
        Assert.NotNull(gEngine.Settle(16));
        Assert.Equal(Mask(0), groundGap.Values(0));
        Assert.Equal(0, answerTension.Plus.Read(floor, 0));
        Assert.Equal(1, grounded.Read(floor, 0));
    }

    /// <summary>5. Conflicting answers raise information excess (contradiction);
    /// self-running firmness (<see cref="TensionLaws.YieldByFirmness"/> +
    /// in-machine <see cref="Flows.When"/> over <see cref="Settling.Forget"/>)
    /// retires the weaker claim and reopens the owner with zero host calls,
    /// equal firmness stays visibly contradicted, and in-machine
    /// <see cref="TensionLaws.RankCombine"/> picks the cheapest valid repair
    /// lexicographically.</summary>
    [Fact]
    public void ConflictingAnswersRaiseContradictionResolveByFirmnessInMachineAndRankRepairsByCost()
    {
        // ---- Part A: Self-running contradiction repair by firmness (zero host reopen!) ----
        var store = new Store(0, 1);
        var owner = Supports.On(store, new Field(Quad, store.AddPlane(), store.AddPlane()));
        var demand = store.AddPlane();
        var seen1 = store.AddPlane();
        var seen2 = store.AddPlane();
        var deficit = store.AddPlane();
        var excess = store.AddPlane();
        store.Write(demand, 0, true);

        // Two conflicting answers: claim 0 says Alice (firmness 1), claim 1 says Bob (firmness 3).
        // Plane 2 on `claims` holds each claim's active flag (lives on `claims` even after the wiring row retires).
        var claims = new Store(3, 2);
        var claimVal = new Field(Quad, 0, 1);
        claimVal.Write(claims, 0, Alice);
        claimVal.Write(claims, 1, Bob);
        claims.Write(2, 0, true);
        claims.Write(2, 1, true);

        var firmness = new Store(3, 2);
        var firmField = new Field(Shape.Unary(3), 0, 1, 2);
        firmField.Write(firmness, 0, 1); // weak casual guess
        firmField.Write(firmness, 1, 3); // firm registration check

        var assertWeak = new Wiring(Settling.Same(Quad), claimVal.On(claims), owner.Variable);
        assertWeak.Add(0, 0);
        var assertFirm = new Wiring(Settling.Same(Quad), claimVal.On(claims), owner.Variable);
        assertFirm.Add(1, 0);

        // YieldByFirmness clears the weaker claim's active flag when excess = 1;
        // Settling.Latch(false) retires its wiring row permanently; and reopenOwner
        // fires Flows.When(Settling.Forget(4), 2) ONCE while excess = 1 && ~active = 1!
        var yieldWeak = new Wiring(
            TensionLaws.YieldByFirmness(3),
            new Binding(store, excess),
            firmField.On(firmness),
            firmField.On(firmness),
            new Binding(claims, 2));
        yieldWeak.Add(0, 0, 1, 0);

        var yieldFirm = new Wiring(
            TensionLaws.YieldByFirmness(3),
            new Binding(store, excess),
            firmField.On(firmness),
            firmField.On(firmness),
            new Binding(claims, 2));
        yieldFirm.Add(0, 1, 0, 1);

        var retireWeakRow = new Wiring(
            Settling.Latch(false),
            new Binding(new Flipped(claims, 2), 2),
            new Binding(assertWeak.Rows.Life, 0));
        retireWeakRow.Add(0, 0);

        var retireFirmRow = new Wiring(
            Settling.Latch(false),
            new Binding(new Flipped(claims, 2), 2),
            new Binding(assertFirm.Rows.Life, 0));
        retireFirmRow.Add(1, 0);

        var reopenOwner = new Wiring(
            Flows.When(Settling.Forget(4), 2),
            new Binding(store, [.. owner.Planes]),
            new Binding(store, excess),
            new Binding(new Flipped(claims, 2), 2));
        reopenOwner.Add(0, 0, 0);

        var repairEngine = new Engine().Add(assertWeak).Add(assertFirm);
        Tension.ObserveSupports(repairEngine, owner, store, demand, store, seen1, seen2, deficit, excess);
        repairEngine.Add(yieldWeak).Add(yieldFirm).Add(retireWeakRow).Add(retireFirmRow).Add(reopenOwner);

        // Step 1: both claims narrow -> contradiction (owner empty, excess = 1) -> weak active clears & row retires!
        // Next step: reopenOwner forgets owner -> surviving firm claim settles Bob -> excess = 0 -> quiet!
        var steps = repairEngine.Settle(16);
        Assert.NotNull(steps);
        Assert.False(assertWeak.Live[0]);
        Assert.True(assertFirm.Live[0]);
        Assert.Equal(Mask(Bob), owner.Values(0));
        Assert.False(store.Read(excess, 0));
        Assert.False(store.Read(deficit, 0));

        // ---- Part B: Tied firmness (2 vs 2) stays visibly contradicted! ----
        var tiedStore = new Store(0, 1);
        var tiedOwner = Supports.On(tiedStore, new Field(Quad, tiedStore.AddPlane(), tiedStore.AddPlane()));
        var tDem = tiedStore.AddPlane();
        var tS1 = tiedStore.AddPlane();
        var tS2 = tiedStore.AddPlane();
        var tDef = tiedStore.AddPlane();
        var tExc = tiedStore.AddPlane();
        tiedStore.Write(tDem, 0, true);

        var tiedClaims = new Store(2, 2);
        claimVal.Write(tiedClaims, 0, Alice);
        claimVal.Write(tiedClaims, 1, Bob);

        var tiedFirm = new Store(3, 2);
        firmField.Write(tiedFirm, 0, 2);
        firmField.Write(tiedFirm, 1, 2);
        var cA = new Wiring(Settling.Same(Quad), claimVal.On(tiedClaims), tiedOwner.Variable);
        cA.Add(0, 0);
        var cB = new Wiring(Settling.Same(Quad), claimVal.On(tiedClaims), tiedOwner.Variable);
        cB.Add(1, 0);
        var yA = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStore, tExc), firmField.On(tiedFirm), firmField.On(tiedFirm), new Binding(cA.Rows.Life, 0));
        yA.Add(0, 0, 1, 0);
        var yB = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStore, tExc), firmField.On(tiedFirm), firmField.On(tiedFirm), new Binding(cB.Rows.Life, 0));
        yB.Add(0, 1, 0, 0);

        var tiedEngine = new Engine().Add(cA).Add(cB);
        Tension.ObserveSupports(tiedEngine, tiedOwner, tiedStore, tDem, tiedStore, tS1, tS2, tDef, tExc);
        tiedEngine.Add(yA).Add(yB);
        Assert.NotNull(tiedEngine.Settle(16));
        Assert.True(cA.Live[0] && cB.Live[0]);
        Assert.True(tiedStore.Read(tExc, 0)); // genuine tie remains visibly contradicted!

        // ---- Part C: In-machine lexicographic repair-cost ranking (Tier 1: InfoTension, Tier 2: RepairCost) ----
        // Compare pairs of candidate repairs (3 positions = 3 pairwise comparisons):
        //   pos 0: Repair A (info=0, cost=1) vs Repair B (info=0, cost=3) -> A wins on Tier 2 (cheaper repair!)
        //   pos 1: Repair B (info=0, cost=3) vs Repair C (info=1, cost=1) -> B wins on Tier 1 (hard tier beats cost!)
        //   pos 2: Repair A (info=0, cost=1) vs Repair C (info=1, cost=1) -> A wins on Tier 1!
        var rankStore = new Store(0, 3);
        Field AddUnary(int cap) => new(Shape.Unary(cap), [.. Enumerable.Range(0, cap).Select(_ => rankStore.AddPlane())]);
        var t1Left = AddUnary(1);
        var t1Right = AddUnary(1);
        var t2Left = AddUnary(3);
        var t2Right = AddUnary(3);
        int t1Drop = rankStore.AddPlane(), t1Rise = rankStore.AddPlane();
        int t2Drop = rankStore.AddPlane(), t2Rise = rankStore.AddPlane();
        int leftBetter = rankStore.AddPlane(), rightBetter = rankStore.AddPlane();

        void SetPair(int pos, int iL, int cL, int iR, int cR)
        {
            t1Left.Write(rankStore, pos, iL);
            t1Right.Write(rankStore, pos, iR);
            t2Left.Write(rankStore, pos, cL);
            t2Right.Write(rankStore, pos, cR);
        }
        SetPair(0, iL: 0, cL: 1, iR: 0, cR: 3); // A vs B
        SetPair(1, iL: 0, cL: 3, iR: 1, cR: 1); // B vs C
        SetPair(2, iL: 0, cL: 1, iR: 1, cR: 1); // A vs C

        var rankEngine = new Engine();
        // ObserveMismatch(have=left, want=right): needMore (left < right) = leftBetter; haveExtra (left > right) = rightBetter.
        Tension.ObserveMismatch(rankEngine, rankStore, t1Left, rankStore, t1Right, rankStore, t1Drop, t1Rise);
        Tension.ObserveMismatch(rankEngine, rankStore, t2Left, rankStore, t2Right, rankStore, t2Drop, t2Rise);
        var combine = new Wiring(
            TensionLaws.RankCombine,
            new Binding(rankStore, t1Drop),
            new Binding(rankStore, t1Rise),
            new Binding(rankStore, t2Drop),
            new Binding(rankStore, t2Rise),
            new Binding(rankStore, leftBetter),
            new Binding(rankStore, rightBetter));
        for (var p = 0; p < 3; p++) combine.Add(p, p, p, p, p, p);
        rankEngine.Add(combine);
        Assert.NotNull(rankEngine.Settle(8));

        // In every pair: left strictly wins (leftBetter = 1, rightBetter = 0): A > B > C!
        for (var p = 0; p < 3; p++)
        {
            Assert.True(rankStore.Read(leftBetter, p));
            Assert.False(rankStore.Read(rightBetter, p));
        }
    }

    /// <summary>6. Quiet turn-taking (<see cref="Flows.Turn"/>) alternates
    /// fairly when both speakers have work, hands the floor to whoever has work
    /// alone, stops quietly when both are clear, and completes a back-to-back
    /// two-question exchange with strict conservation. Also checks table-limit
    /// guards on <see cref="Flows.When"/>, <see cref="Settling.Justify"/>, and
    /// <see cref="Settling.Latch"/>.</summary>
    [Fact]
    public void QuietTurnTakingAlternatesFairlyUnderSimultaneousWorkAndStopsWhenClear()
    {
        // Unit check on Flows.Turn (8 bytes in RAM!)
        Assert.Equal(64, Flows.Turn.BitCount);
        var turnStore = new Store(3, 1); // 0=needA, 1=needB, 2=turn
        var turnW = new Wiring(Flows.Turn, new Binding(turnStore, 0), new Binding(turnStore, 1), new Binding(turnStore, 2));
        turnW.Add(0, 0, 0);
        var turnEngine = new Engine().Add(turnW);

        // Both need floor -> alternates 0 -> 1 -> 0
        turnStore.Write(0, 0, true);
        turnStore.Write(1, 0, true);
        Assert.True(turnEngine.Step().Steps[0].Changed.Any);
        Assert.True(turnStore.Read(2, 0));
        Assert.True(turnEngine.Step().Steps[0].Changed.Any);
        Assert.False(turnStore.Read(2, 0));

        // Only B needs floor -> sets turn = 1 and settles quietly
        turnStore.Write(0, 0, false);
        Assert.NotNull(turnEngine.Settle(4));
        Assert.True(turnStore.Read(2, 0));

        // Neither needs floor -> holds turn = 1 quietly in 1 check!
        turnStore.Write(1, 0, false);
        Assert.Equal(1, turnEngine.Settle(4));
        Assert.True(turnStore.Read(2, 0));

        // Two back-to-back questions in the dialogue world: Asker queues 2 questions,
        // both speakers interleave posting, answering and grounding via Flows.Turn, and
        // the entire exchange settles with strict conservation (MainTotal == 2).
        var world = BuildDialogue(carMask: Mask(Car0, Car2), demandedPosition: PosOwner, questionsToAsk: 2, askerAcknowledges: true);
        Assert.Equal(2, world.MainTotal);
        world.SettleWorld(48);
        Assert.Equal((0, 0, 0, 2), (world.MainAskCount, world.MainDebt, world.MainPressure, world.MainGroundedCount));
        Assert.Equal(2, world.MainTotal);

        // Verify compact wire size of the full two-speaker dialogue world (< 1 KB!)
        var saved = Transport.SaveLocals(world.Answerer, world.Asker);
        Assert.InRange(saved.Length, 100, 1024);

        // Boundary guards on Flows.When, Settling.Justify, and Settling.Latch
        Assert.Throws<ArgumentOutOfRangeException>(() => Flows.When(TensionLaws.TrackGap(5), gates: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Settling.Justify(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Settling.Justify(11));
        Assert.Equal(16, Settling.Latch(true).BitCount);
        Assert.Equal(16, Settling.Latch(false).BitCount);
    }
}
