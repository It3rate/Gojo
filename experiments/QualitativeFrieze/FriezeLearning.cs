using Gojo.Core;
using Gojo.Patterns;
using Gojo.Equations;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded feedback search over native phase-to-motion bindings.
/// This learns a program in an authored grammar, not the grammar or its laws.
/// Numeric utility is an optional observation of unchanged qualitative sources.</summary>
public static class FriezeLearning
{
    // edit = (parent-output-law -> child-output-law) -> Closed(phase -> (old-word -> new-word))
    // goal = (structural-rule-surface -> Closed(requested-empty-scopes)) ->
    //        ((measured-read -> secondary-read) -> (coverage-surface -> utility-witness))
    // evaluation = (fixture -> trace) -> (goal -> (hard-count-point -> cost-point))
    // selection = (parent-evaluation -> proposed-evaluation) -> chosen-evaluation
    // history = Closed((edit -> proposal-evaluation) -> selection)
    // search = (initial-output-law -> (repeated-word-bag -> exchange-template)) ->
    //          ((phase-capacity -> archive-capacity) -> proposal-budget)
    // archive-admission = (search -> (archive-before -> proposal-evaluation)) ->
    //                     ((archive-after -> dropped) -> adjacent-rank-evidence)
    // Metric points/counts are derived reads, not mutations of physical sources.
    public sealed record Evaluation(FriezeFixture Fixture, Relation Trace, Relation StructuralRead,
        Relation? Calibration, Relation? MeasuredRead, FriezeCoverage.Evaluation? Coverage,
        Relation Surface, long HardFailures, long CostTicks, string[] Symmetries,
        int Generation, Evaluation? Parent);
    public sealed record RunResult(string Name, bool AllowCrossings, Evaluation Initial, Evaluation Best,
        IReadOnlyList<Evaluation> Candidates, Relation History, int StructuralRejected,
        int MeasuredQueries, int CoverageQueries, Relation SearchSurface, Relation ArchiveHistory);

    public static IReadOnlyList<RunResult> Run(int trials = 240, int seed = 431)
    {
        if (trials is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(trials));
        return [Search("Orthogonal / crossings forbidden", 0, false, trials, seed),
            Search("Meander / crossings forbidden", 1, false, trials, seed + 1),
            Search("Diagonal / crossings allowed", 2, true, trials, seed + 2)];
    }

    private static RunResult Search(string name, int grammar, bool allowCrossings, int trials, int seed)
    {
        var frame = new QualitativeMotionFrame(); var horizontal = new Placeholder(); var vertical = new Placeholder();
        Relation Word(params IElement[] directions) => S(directions.Select(direction =>
            (IElement)QualitativeMotionFrame.Move(direction,
                ReferenceEquals(direction, frame.Right) || ReferenceEquals(direction, frame.Left) ? horizontal : vertical)).ToArray());
        Relation[] alphabet = [Word(frame.Right), Word(frame.Left), Word(frame.Up), Word(frame.Down),
            Word(frame.Right, frame.Up), Word(frame.Left, frame.Up), Word(frame.Right, frame.Down), Word(frame.Left, frame.Down)];
        int[] bag = grammar switch
        {
            0 => [0, 0, 0, 0, 2, 2, 3, 3],
            1 => [0, 0, 0, 0, 1, 2, 2, 3, 3],
            _ => [0, 0, 0, 0, 4, 5, 6, 7]
        };
        var initialFixture = FriezeFixture.Sequence(frame, S(bag.Select(index => (IElement)alphabet[index]).ToArray()), name);
        if (!FiniteLaw.TryRead(initialFixture.OutputLaw, out var phaseDomain, out var ports, out var words, out var rows))
            throw new InvalidOperationException("Generated program must expose its output law.");
        var slots = S(Enumerable.Range(0, 12).Select(_ => (IElement)new Placeholder()).ToArray());
        var archiveSlots = S(Enumerable.Range(0, 12).Select(_ => (IElement)new Placeholder()).ToArray());
        var proposalBasis = new Carrier();
        // The source word bag retains repeated participation. Permutations may
        // rebind these words but cannot introduce a word or change multiplicity.
        var wordBag = S(bag.Select(index => (IElement)alphabet[index]).ToArray());
        var phaseA = new Placeholder(); var phaseB = new Placeholder(); var wordA = new Placeholder(); var wordB = new Placeholder();
        var exchangeTemplate = new Relation(new Relation(new Relation(phaseA, wordA), new Relation(phaseB, wordB)),
            new Relation(new Relation(phaseA, wordB), new Relation(phaseB, wordA)));
        var searchSurface = new Relation(new Relation(initialFixture.OutputLaw, new Relation(wordBag, exchangeTemplate)),
            new Relation(new Relation(slots, archiveSlots), Relation.Segment(proposalBasis, 0, trials)));
        var capacity = new Carrier(); var costBasis = new Carrier();
        var evaluations = new List<Evaluation>(); var history = new List<IElement>(); var archives = new List<IElement>();
        var population = new List<(int[] Program, Evaluation Read)>();
        var seen = new HashSet<string>(); var random = new Random(seed);
        var nativeRejected = 0; var measuredQueries = 0; var coverageQueries = 0;
        var initial = Evaluate(initialFixture, null, 0); evaluations.Add(initial);
        population.Add((bag, initial)); seen.Add(Key(bag)); var best = initial;
        for (var attempt = 0; evaluations.Count < trials && attempt < trials * 32; attempt++)
        {
            var parent = population[random.Next(population.Count)];
            var candidate = (int[])parent.Program.Clone();
            if (attempt % 5 == 0) random.Shuffle(candidate);
            else
            {
                var a = random.Next(candidate.Length); var b = random.Next(candidate.Length);
                if (attempt % 3 == 0) Array.Reverse(candidate, Math.Min(a, b), Math.Abs(a - b) + 1);
                else (candidate[a], candidate[b]) = (candidate[b], candidate[a]);
            }
            if (!seen.Add(Key(candidate))) continue;
            var changes = new List<IElement>();
            var outputs = new Relation[rows.Length];
            for (var i = 0; i < rows.Length; i++)
            {
                var pair = (Relation)rows[i].Left;
                outputs[i] = FiniteLaw.Row(pair.Left, pair.Right, S(alphabet[candidate[i]]));
                if (candidate[i] != parent.Program[i])
                    changes.Add(new Relation(pair.Left, new Relation(alphabet[parent.Program[i]], alphabet[candidate[i]])));
            }
            var fixture = initialFixture.WithOutputs(FiniteLaw.Create(phaseDomain, ports, words, outputs));
            var edit = new Relation(new Relation(parent.Read.Fixture.OutputLaw, fixture.OutputLaw), S(changes.ToArray()));
            var evaluation = Evaluate(fixture, parent.Read, evaluations.Count); evaluations.Add(evaluation);
            var chosen = Compare(evaluation, parent.Read) < 0 ? evaluation : parent.Read;
            history.Add(new Relation(new Relation(edit, evaluation.Surface),
                new Relation(new Relation(parent.Read.Surface, evaluation.Surface), chosen.Surface)));
            var beforeArchive = S(population.Select(item => (IElement)item.Read.Surface).ToArray());
            population.Add((candidate, evaluation));
            population.Sort((a, b) => Compare(a.Read, b.Read));
            var ordering = S(population.Zip(population.Skip(1), (a, b) => (IElement)new Relation(
                new Relation(a.Read.Surface, b.Read.Surface),
                new Relation(new Relation(Relation.Point(capacity, a.Read.HardFailures), Relation.Point(capacity, b.Read.HardFailures)),
                    new Relation(Relation.Point(costBasis, a.Read.CostTicks), Relation.Point(costBasis, b.Read.CostTicks))))).ToArray());
            var dropped = population.Count > 12 ? S(population[^1].Read.Surface) : S();
            if (population.Count > 12) population.RemoveAt(population.Count - 1);
            var afterArchive = S(population.Select(item => (IElement)item.Read.Surface).ToArray());
            archives.Add(new Relation(new Relation(searchSurface, new Relation(beforeArchive, evaluation.Surface)),
                new Relation(new Relation(afterArchive, dropped), ordering)));
            if (Compare(evaluation, best) < 0) best = evaluation;
        }
        return new(name, allowCrossings, initial, best, evaluations, S(history.ToArray()), nativeRejected,
            measuredQueries, coverageQueries, searchSurface, S(archives.ToArray()));

        Evaluation Evaluate(FriezeFixture fixture, Evaluation? parent, int generation)
        {
            var trace = fixture.Run(bag.Length); var reader = new FriezeStructuralRules(fixture, slots);
            var structural = reader.Evaluate(trace);
            var hard = 0L; var cost = 1_000_000L;
            Relation? calibration = null, measured = null, secondary = null;
            FriezeCoverage.Evaluation? coverage = null;
            Relation utility = StructuralConstraint.Open(); var claims = new List<IElement>();
            var names = Array.Empty<string>();
            if (!reader.TryReadResult(structural, out _, out var violations, out _) ||
                !reader.TryReadPeriod(structural, out _, out _)) hard++;
            else hard += E(violations).Length;
            if (hard != 0) { nativeRejected++; return Finish(); }
            calibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 4, 4));
            measuredQueries++;
            measured = FriezeMeasuredRules.Evaluate(fixture, trace, calibration);
            if (!ReadGeometry(measured, out var summary, out var strokes)) { hard++; return Finish(); }
            names = summary.VerifiedSymmetryNames;
            if (hard != 0) return Finish();
            var secondCalibration = FriezeRealization.CreateCalibration(fixture, FriezeRealization.DefaultLengths(fixture, 6, 4));
            measuredQueries++;
            secondary = FriezeMeasuredRules.Evaluate(fixture, trace, secondCalibration);
            if (!ReadGeometry(secondary, out _, out _)) { hard++; return Finish(); }
            if (hard != 0) return Finish();
            coverageQueries++;
            coverage = FriezeCoverage.Evaluate(fixture, measured, strokes, summary.PeriodX, minimumY: -8, maximumY: 8);
            hard += coverage.Projection.Unresolved;
            if (coverage.Projection.Cells == 0) hard++;
            if (hard != 0) return Finish();
            utility = Utility(coverage, measured, summary, costBasis, out cost);
            return Finish();

            bool ReadGeometry(Relation read, out FriezeMeasuredRules.Summary summary, out Relation strokes)
            {
                summary = FriezeMeasuredRules.Describe(read); strokes = S();
                if (!FriezeMeasuredRules.TryReadResult(read, out strokes, out var overlaps, out var crossings, out _, out _, out var pending)) return false;
                claims.Add(new Relation(overlaps, S()));
                hard += summary.OverlapCount + E(pending).Length;
                if (!allowCrossings) { claims.Add(new Relation(crossings, S())); hard += summary.CrossingCount; }
                // A nondegenerate target is explicit, so a straight baseline cannot win filling.
                var heightGoal = new Relation(Relation.Point(capacity, 0), Relation.Point(capacity, summary.Height));
                claims.Add(new Relation(read, heightGoal));
                if (summary.Height <= 0) hard++;
                return true;
            }
            Evaluation Finish()
            {
                var goal = new Relation(new Relation(reader.Surface, S(claims.ToArray())),
                    new Relation(new Relation(measured ?? StructuralConstraint.Open(), secondary ?? StructuralConstraint.Open()),
                        new Relation(coverage?.Surface ?? StructuralConstraint.Open(), utility)));
                var surface = new Relation(new Relation(fixture.Surface, trace),
                    new Relation(new Relation(structural, goal),
                        new Relation(Relation.Point(capacity, hard), Relation.Point(costBasis, cost))));
                return new(fixture, trace, structural, calibration, measured, coverage, surface, hard, cost, names, generation, parent);
            }
        }
    }

    private static Relation Utility(FriezeCoverage.Evaluation coverage, Relation geometry,
        FriezeMeasuredRules.Summary summary, Carrier output, out long cost)
    {
        var missing = new Carrier(); var multiple = new Carrier(); var spread = new Carrier(); var symmetry = new Carrier();
        var q = coverage.Projection;
        var equation = Equation.Sum(output,
            Equation.WeightedPort(missing, 0, q.Cells, 0, 4096),
            Equation.WeightedPort(multiple, 0, q.Cells, 0, 4096),
            Equation.WeightedPort(spread, 0, q.MaximumClearance + 1, 0, 1024),
            Equation.WeightedPort(symmetry, 0, 1, 0, -32));
        var input = new Relation(new Relation(new Relation(new CarrierLocation(missing, q.Empty),
                new CarrierLocation(multiple, q.Multiple)), new CarrierLocation(spread, q.ClearanceSpread)),
            new CarrierLocation(symmetry, summary.VerifiedSymmetryNames.Length));
        var prediction = Equation.Predict(equation, input, output);
        if (!Equation.TryReadPrediction(prediction, out var result, out _))
            throw new InvalidOperationException("Observation utility must have an explicit Gojo equation read.");
        cost = result.Ticks;
        return new(new Relation(coverage.Surface, geometry), new Relation(equation, new Relation(input, prediction)));
    }

    private static int Compare(Evaluation a, Evaluation b)
    {
        var hard = a.HardFailures.CompareTo(b.HardFailures);
        return hard != 0 ? hard : a.CostTicks.CompareTo(b.CostTicks);
    }
    private static string Key(int[] program) => string.Join(',', program);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
