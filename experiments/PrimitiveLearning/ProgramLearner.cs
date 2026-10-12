using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace PrimitiveLearning;

/// <summary>
/// A bounded condition/response grammar experiment. Native rules, edit parents,
/// case coverage, empirical utility, complexity cost and retained binding own
/// the learned composition. No constructor knows the world's correct response.
/// </summary>
public sealed class ProgramLearner
{
    // candidate = G(id point, native rules, ledger, complexity point,
    //   edit witness, G(four case-count carriers)).
    // Root = G(four public axes, score/count/candidate/complexity axes, roles,
    //   candidates, retained selector, trial selector, probe phase, edit count,
    //   latest decision, latest feedback, recent receipts, retention, history boundary).
    public Carrier ModeAxis { get; }
    public Carrier DirectionAxis { get; }
    public Carrier ActionAxis { get; }
    public Carrier UtilityAxis { get; }
    private readonly Carrier scoreAxis, countAxis, candidateAxis, complexityAxis, modeRole, directionRole,
        retained, selector, phase, editCount, forgotten;
    private readonly Relation[] candidates;
    private readonly List<Relation> receipts = [];
    private Relation retention;
    public Relation Root { get; private set; }
    public Relation LastDecision { get; private set; }
    public Relation LastFeedback { get; private set; }
    public const int ProbesPerCase = 4;
    public const int ProbePeriod = 64;
    public int CandidateCount => candidates.Length;
    public long AcceptedEdits => checked((long)editCount.Value);
    public long SelectedComplexity => ((CarrierLocation)((Relation)LearningGraph.Parts(candidates[(int)retained.Value], 6)[3]).Left).Ticks;

    public ProgramLearner(Carrier modeAxis, Carrier directionAxis, Carrier actionAxis, Carrier utilityAxis)
    {
        ModeAxis = modeAxis; DirectionAxis = directionAxis; ActionAxis = actionAxis; UtilityAxis = utilityAxis;
        scoreAxis = new(); countAxis = new(); candidateAxis = new(); complexityAxis = new(); modeRole = new(); directionRole = new();
        retained = new(candidateAxis.UnitShape); selector = new(candidateAxis.UnitShape); phase = new(countAxis.UnitShape);
        editCount = new(countAxis.UnitShape); forgotten = new(countAxis.UnitShape);
        phase.Attach(new Relation(Relation.Point(phase, ProbePeriod), Relation.Point(phase, 0)));
        // Enumerating response assignments is host grammar construction, never
        // evaluation of the hidden target. Constants and one-port branches are
        // constructed before their small two-port edits. There are 16 possible
        // binary-response assignments on these two declared binary ports.
        int[] assignments = [0, 15, 3, 12, 5, 10, 1, 2, 4, 8, 7, 11, 13, 14, 6, 9];
        var built = new List<Relation>();
        for (var index = 0; index < assignments.Length; index++)
        {
            var assignment = assignments[index]; var responses = Enumerable.Range(0, 4)
                .Select(site => (assignment & (1 << site)) == 0 ? -1 : 1).ToArray();
            var usesMode = responses[0] != responses[2] || responses[1] != responses[3];
            var usesDirection = responses[0] != responses[1] || responses[2] != responses[3];
            var rules = new List<Relation>();
            for (var mode = 0; mode < (usesMode ? 2 : 1); mode++)
            for (var direction = 0; direction < (usesDirection ? 2 : 1); direction++)
            {
                var conditions = new List<Relation>();
                if (usesMode) conditions.Add(SupportDecisionSurface.Condition(new Carrier(), modeRole, Relation.Segment(ModeAxis, mode, mode)));
                if (usesDirection) conditions.Add(SupportDecisionSurface.Condition(new Carrier(), directionRole,
                    Relation.Segment(DirectionAxis, direction == 0 ? -1 : 1, direction == 0 ? -1 : 1)));
                rules.Add(SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(ActionAxis, responses[mode * 2 + direction]), conditions.ToArray()));
            }
            var policy = SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
            var complexity = Relation.Point(complexityAxis, rules.Count * ((usesMode ? 1 : 0) + (usesDirection ? 1 : 0)));
            var parentIndex = -1;
            if (index > 0)
            {
                var nearest = int.MaxValue;
                for (var previous = 0; previous < index; previous++)
                {
                    var distance = System.Numerics.BitOperations.PopCount((uint)(assignment ^ assignments[previous]));
                    if (distance < nearest) { parentIndex = previous; nearest = distance; }
                }
            }
            IElement parent = parentIndex < 0 ? new Placeholder() : built[parentIndex];
            IElement oldPolicy = parentIndex < 0 ? new Placeholder() : LearningGraph.Parts(built[parentIndex], 6)[1];
            var edit = ReadWitness.Create(new Carrier(), parent, new Relation(oldPolicy, policy), policy,
                parentIndex < 0 ? new Placeholder() : new Relation(oldPolicy, policy),
                LearningGraph.G(parent, Relation.Point(candidateAxis, index), complexity,
                    Relation.Segment(ActionAxis, -1, 1), LearningGraph.G(modeRole, directionRole)));
            built.Add(LearningGraph.G(Relation.Point(candidateAxis, index), policy,
                LearningGraph.Ledger(countAxis, UtilityAxis, UtilityAxis), complexity, edit,
                LearningGraph.G(Enumerable.Range(0, 4).Select(_ => (IElement)new Carrier(countAxis.UnitShape)).ToArray())));
        }
        candidates = built.ToArray();
        retention = LearningGraph.Open(LearningGraph.G(candidates), Relation.Point(candidateAxis, 0));
        LastDecision = LastFeedback = LearningGraph.Open(LearningGraph.G(ModeAxis, DirectionAxis), retention);
        Root = BuildRoot();
    }

    private ProgramLearner(Relation root)
    {
        var fields = LearningGraph.Parts(root, 20);
        ModeAxis = (Carrier)fields[0]; DirectionAxis = (Carrier)fields[1]; ActionAxis = (Carrier)fields[2]; UtilityAxis = (Carrier)fields[3];
        scoreAxis = (Carrier)fields[4]; countAxis = (Carrier)fields[5]; candidateAxis = (Carrier)fields[6]; complexityAxis = (Carrier)fields[7];
        modeRole = (Carrier)fields[8]; directionRole = (Carrier)fields[9]; candidates = LearningGraph.Parts((Relation)fields[10], 16).Cast<Relation>().ToArray();
        retained = (Carrier)fields[11]; selector = (Carrier)fields[12]; phase = (Carrier)fields[13]; editCount = (Carrier)fields[14];
        LastDecision = (Relation)fields[15]; LastFeedback = (Relation)fields[16]; receipts.AddRange(LearningGraph.Parts((Relation)fields[17]).Cast<Relation>());
        retention = (Relation)fields[18]; forgotten = (Carrier)LearningGraph.Parts((Relation)fields[19], 2)[1]; Root = root;
    }

    public static ProgramLearner FromRoot(Relation root) => new(root);

    public Relation BindUtility(Relation physicalUtility) => LearningGraph.BindFeedback(LastDecision, physicalUtility, UtilityAxis);

    /// <summary>Reads the selected native program. During training, uncovered
    /// public input cases receive bounded candidate probes before exploitation.</summary>
    public Relation Act(Relation mode, Relation direction, bool learning)
    {
        if (!LearningGraph.TryValue(mode, ModeAxis, out var modeValue) || modeValue.Ticks is < 0 or > 1 ||
            !LearningGraph.TryValue(direction, DirectionAxis, out var directionValue) || directionValue.Ticks is not (-1 or 1))
            return PublishOpen(LearningGraph.G(mode, direction));
        var caseIndex = (int)modeValue.Ticks * 2 + (directionValue.Ticks == -1 ? 0 : 1);
        var trial = (int)retained.Value; var probeEvidence = new List<IElement>();
        if (learning)
        {
            var found = false;
            for (var index = 0; index < candidates.Length; index++)
            {
                var count = CaseCount(candidates[index], caseIndex);
                var claim = OperationClaim.AtMost(new Carrier(), count.ValueSlot, new CarrierLocation(count, ProbesPerCase - 1), new Carrier());
                probeEvidence.Add(claim);
                if (LearningGraph.Pass(claim)) { trial = index; found = true; break; }
            }
            if (!found && phase.Value == 0)
            {
                trial = 0;
                for (var index = 1; index < candidates.Length; index++)
                {
                    var proposed = CaseCount(candidates[index], caseIndex); var incumbent = CaseCount(candidates[trial], caseIndex);
                    // Counts have compatible units but independent identities.
                    // The explicit same-basis read makes their comparison local.
                    var claim = OperationClaim.AtMost(new Carrier(), new CarrierLocation(countAxis, checked((long)proposed.Value)),
                        new CarrierLocation(countAxis, checked((long)incumbent.Value - 1)), new Carrier());
                    probeEvidence.Add(claim); if (LearningGraph.Pass(claim)) trial = index;
                }
            }
        }
        selector.Set(trial);
        var choice = ScopedChoice.Create(selector.ValueSlot, Relation.Segment(selector, 0, candidates.Length), candidateAxis,
            candidates.Select((candidate, index) => ScopedChoice.Offer(Relation.Segment(selector, index, index + 1), candidate,
                Equation.Affine(candidateAxis, 0, 1, candidateAxis, 0, 1))).ToArray());
        var selectorRead = new Relation(new CarrierLocation(selector, trial), LearningGraph.G(probeEvidence.ToArray()));
        if (!ScopedChoice.TrySelect(choice, selectorRead, out var selected, out _, out var binding) || selected is not Relation candidateRead)
            return PublishOpen(LearningGraph.G(mode, direction));
        var policy = (Relation)LearningGraph.Parts(candidateRead, 6)[1];
        var decision = SupportDecisionSurface.Select(new Carrier(), policy,
            SupportDecisionSurface.Binding(new Carrier(), modeRole, LearningGraph.Publication(ModeAxis, modeValue.Ticks, mode)),
            SupportDecisionSurface.Binding(new Carrier(), directionRole, LearningGraph.Publication(DirectionAxis, directionValue.Ticks, direction)));
        if (!SupportDecisionSurface.TryReadSelection(decision, out _, out var response, out _, out var earlier) || response is Placeholder)
            return PublishOpen(LearningGraph.G(mode, direction));
        LastDecision = ReadWitness.Create(new Carrier(), LearningGraph.G(mode, direction), LearningGraph.G(choice, policy), response, earlier,
            LearningGraph.G(candidateRead, binding, decision, mode, direction, Relation.Point(countAxis, caseIndex),
                Relation.Point(countAxis, learning ? 1 : 0), LearningGraph.G(probeEvidence.ToArray())));
        Root = BuildRoot(); return LastDecision;
    }

    /// <summary>Credits only the actual tried program using a later independent
    /// physical outcome. Complexity is a separate declared selection cost.</summary>
    public Relation Feedback(Relation utility, bool learn = true)
    {
        if (!learn || !LearningGraph.TryValue(utility, UtilityAxis, out var value) || !LearningGraph.IsBoundTo(utility, LastDecision, UtilityAxis) ||
            !ReadWitness.TryRead(LastDecision, out _, out _, out _, out _, out var provenance) || provenance is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length != 8 || fields[0] is not Relation candidate ||
            !LearningGraph.TryPoint(fields[5], out var inputCase) || inputCase.Ticks is < 0 or > 3 ||
            !LearningGraph.TryPoint(fields[6], out var training) || training.Ticks != 1 ||
            LearningGraph.ReceiptSeen(receipts, LastDecision, utility) || !LearningGraph.CanUpdate(Ledger(candidate), value.Ticks, 0))
        { LastFeedback = LearningGraph.Open(utility, LastDecision); Root = BuildRoot(); return LastFeedback; }
        var ledger = Ledger(candidate); var before = LearningGraph.CaptureLedger(ledger);
        LearningGraph.Update(ledger, value.Ticks, 0); CaseCount(candidate, (int)inputCase.Ticks).Add(1); phase.Add(1);
        RetainBest();
        LastFeedback = ReadWitness.Create(new Carrier(), utility, LastDecision, LearningGraph.CaptureLedger(ledger), LearningGraph.G(),
            LearningGraph.G(LastDecision, utility, candidate, before, ledger, retention));
        receipts.Add(LastFeedback);
        if (receipts.Count > LearningGraph.RecentReceiptLimit) { receipts.RemoveAt(0); forgotten.Add(1); }
        Root = BuildRoot(); return LastFeedback;
    }

    public long ObservationCount(int candidate) => checked((long)LearningGraph.Count(Ledger(candidates[candidate])).Value);

    private void RetainBest()
    {
        var winner = (int)retained.Value; var scores = new List<IElement>(); var claims = new List<IElement>();
        var currentScore = LearningGraph.Score(Ledger(candidates[winner]), scoreAxis, Complexity(candidates[winner]));
        foreach (var (candidate, index) in candidates.Select((candidate, index) => (candidate, index)))
        {
            var covered = true;
            for (var inputCase = 0; inputCase < 4; inputCase++)
            {
                var count = CaseCount(candidate, inputCase); var coverage = OperationClaim.AtLeast(new Carrier(), count.ValueSlot,
                    new CarrierLocation(count, ProbesPerCase), new Carrier()); claims.Add(coverage); covered &= LearningGraph.Pass(coverage);
            }
            var score = LearningGraph.Score(Ledger(candidate), scoreAxis, Complexity(candidate)); scores.Add(score);
            if (!covered || !LearningGraph.TryValue(score, scoreAxis, out var proposed)) continue;
            if (!LearningGraph.TryValue(currentScore, scoreAxis, out var current)) { winner = index; currentScore = score; continue; }
            var improvement = OperationClaim.AtLeast(new Carrier(), proposed, new CarrierLocation(scoreAxis, checked(current.Ticks + 1)), new Carrier());
            claims.Add(improvement); if (LearningGraph.Pass(improvement)) { winner = index; currentScore = score; }
        }
        var before = new CarrierLocation(retained, checked((long)retained.Value));
        if (winner != (int)retained.Value) { retained.Set(winner); editCount.Add(1); }
        retention = ReadWitness.Create(new Carrier(), LearningGraph.G(candidates), LearningGraph.G(scores.ToArray()), candidates[winner],
            LearningGraph.G(claims.ToArray()), LearningGraph.G(before, new CarrierLocation(retained, winner), currentScore,
                LearningGraph.Parts(candidates[winner], 6)[4], Relation.Point(complexityAxis, 1), Relation.Point(scoreAxis, LearningGraph.ScoreScale)));
    }

    private Relation PublishOpen(IElement supplied) { LastDecision = LearningGraph.Open(supplied, LearningGraph.G(candidates)); Root = BuildRoot(); return LastDecision; }
    private Relation BuildRoot() => LearningGraph.G(ModeAxis, DirectionAxis, ActionAxis, UtilityAxis, scoreAxis, countAxis, candidateAxis, complexityAxis,
        modeRole, directionRole, LearningGraph.G(candidates), retained, selector, phase, editCount, LastDecision, LastFeedback,
        LearningGraph.G(receipts.ToArray()), retention, LearningGraph.G(Relation.Point(countAxis, LearningGraph.RecentReceiptLimit), forgotten));
    private static Relation Ledger(Relation candidate) => (Relation)LearningGraph.Parts(candidate, 6)[2];
    private static Carrier CaseCount(Relation candidate, int inputCase) => (Carrier)LearningGraph.Parts((Relation)LearningGraph.Parts(candidate, 6)[5], 4)[inputCase];
    private static long Complexity(Relation candidate) => ((CarrierLocation)((Relation)LearningGraph.Parts(candidate, 6)[3]).Left).Ticks;
}
