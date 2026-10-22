using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Small supplied-premise calculations. The diagrams explain the
/// fixtures; they do not supply likelihoods or recognition evidence.</summary>
public static class EvidenceCalculationExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private sealed record Check(string Law, bool Passed, string Detail);
    private sealed record CandidateRow(string Candidate, string Prior, string Likelihood, string Posterior, string Status);
    private sealed record Step(string Name, string Icon, string Explanation, IReadOnlyList<CandidateRow> Candidates);
    private sealed record Case(string Id, string Name, string Question, string Premises, string Finding,
        IReadOnlyList<Step> Steps, IReadOnlyList<Check> Checks, Relation Native);

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "evidence-calculation");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --evidence-calculation [--output DIRECTORY]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "diagrams"));
        var watch = Stopwatch.StartNew(); var cases = Cases();
        var failures = cases.SelectMany(item => item.Checks.Where(check => !check.Passed).Select(check => item.Id + ": " + check.Law)).ToArray();
        if (failures.Length != 0) throw new InvalidOperationException("Math fixture failed: " + string.Join("; ", failures));
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("evidence-calculation-v1", S(cases.Select(item => (IElement)item.Native).ToArray())));
        using (var file = File.Create(Path.Combine(output, "receipts.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var summary = new { Schema = "evidence-calculation-v1", Cases = cases.Count,
            LawChecks = cases.Sum(item => item.Checks.Count), PassedChecks = cases.Sum(item => item.Checks.Count(check => check.Passed)),
            Seconds = watch.Elapsed.TotalSeconds,
            Scope = "Supplied statistical premises and native exact calculation receipts. No pattern recognition, learned likelihoods, measured geometry or automatic structural-prior mutation.",
            Interpretation = "These are declared candidate-selection partitions. Two valid descriptions need not be mutually exclusive outside that selected question. A posterior is conditional on the supplied scope, priors, likelihoods and dependency premises." };
        Write(output, cases, summary);
        Console.WriteLine($"Evidence calculation: {cases.Count} supplied-premise cases, {summary.PassedChecks}/{summary.LawChecks} law checks; {watch.Elapsed.TotalSeconds:0.000}s. {Path.GetFullPath(output)}");
        return 0;
    }

    private static IReadOnlyList<Case> Cases()
    {
        var cases = new List<Case>();
        var a = new Placeholder(); var b = new Placeholder();
        var frame = new StatisticalEvidence.Frame(new Relation(a, b), a, b);
        var prior = frame.Prior(new Relation(a, frame.Weight(1, 2)), new Relation(b, frame.Weight(1, 2)));
        var names = new[] { "Parallel-line recipe", "Equal-partition circular recipe" };
        StatisticalEvidence.Question Question() => new(new Placeholder(), new Placeholder(), new Relation(a, b));
        Relation Bind(int an, int ad, int bn, int bd) => S(new Relation(a, frame.Weight(an, ad)), new Relation(b, frame.Weight(bn, bd)));
        StatisticalEvidence.Transition Apply(Relation state, Relation observation, Relation likelihoods, bool independent = false)
        {
            var events = S(StatisticalEvidence.Read(state).Events);
            return StatisticalEvidence.Update(state, StatisticalEvidence.Offer(observation, likelihoods,
                independent ? S() : events, independent ? events : S(), new Relation(state, likelihoods)));
        }
        List<CandidateRow> Rows(Relation before, Relation after, string la, string lb, string status = "Conditional selection weight") => [
            new(names[0], Probability(before, a), la, Probability(after, a), status),
            new(names[1], Probability(before, b), lb, Probability(after, b), status)
        ];
        Case Example(string id, string name, string question, string premises, string finding,
            IReadOnlyList<Step> steps, IReadOnlyList<Check> checks, Relation state, params IElement[] receipts) =>
            new(id, name, question, premises, finding, steps, checks, new Relation(S(receipts), state));
        Check Assert(string law, bool passed, string detail) => new(law, passed, detail);
        bool Equal(Relation state, IElement model, int numerator, int denominator) => Matches(state, model, numerator, denominator);

        var orientationSteps = new List<Step>(); var orientationChecks = new List<Check>(); var orientationReceipts = new List<IElement>();
        foreach (var (opposed, affirm, label, expected) in new[] {
            (false, true, "Ask keep · answer affirm", 99), (false, false, "Ask keep · answer oppose", 1),
            (true, true, "Ask discard · answer affirm", 1), (true, false, "Ask discard · answer oppose", 99) })
        {
            var question = Question(); var observation = question.Observe(new Placeholder(), opposed, affirm);
            var result = Apply(prior, observation, Bind(99, 100, 1, 100));
            orientationReceipts.Add(result.Surface);
            orientationSteps.Add(new(label, "orientation", "The likelihood port describes the positive claim. The oriented answer selects that likelihood or its complement; reversing both question and answer preserves the contribution.",
                Rows(prior, result.State, opposed == affirm ? "1/100" : "99/100", opposed == affirm ? "99/100" : "1/100")));
            orientationChecks.Add(Assert(label, result.Accepted && Equal(result.State, a, expected, 100), "The response is interpreted relative to its actual question frame."));
        }
        cases.Add(Example("E01", "Orientation is separate from the answer", "What does affirming or opposing an inverted question contribute?",
            "Equal candidate priors. Positive-claim likelihoods are 99/100 and 1/100. Four independent fixture worlds use four oriented readings.",
            "Keep + affirm and discard + oppose produce the same weight. Inverting only the question or only the answer complements the evidence.",
            orientationSteps, orientationChecks, prior, orientationReceipts.ToArray()));

        var square = prior; var squareReceipts = new List<IElement>(); var squareAccepted = 0;
        for (var i = 0; i < 100; i++)
        {
            var result = Apply(square, Question().Observe(new Placeholder()), Bind(1, 1, 1, 1));
            squareReceipts.Add(result.Surface); square = result.State; if (result.Accepted) squareAccepted++;
        }
        cases.Add(Example("E02", "One hundred square checks do not identify the recipe", "Which construction made this square?",
            "Every supplied square check has likelihood 1 under both recipes, conditional on all preceding reports. No independence between the checks is asserted.",
            "The reports fit both constructions equally. They leave recipe-selection odds tied; they do not establish a unique cause.",
            [new("Initial candidate partition", "circle-square", "This is an explicitly selected comparison between two hypotheses. They could both remain valid descriptions outside this selection question.", Rows(prior, prior, "—", "—")),
             new("After 100 compatible reports", "square", "Right angles, equal sides and parallelism can all be compatible with both mechanisms. Only differences in their report predictions can distinguish them.", Rows(prior, square, "1 for each report", "1 for each report"))],
            [Assert("No discrimination from equal likelihoods", Equal(square, a, 1, 2) && Equal(square, b, 1, 2), "Both exact normalized weights remain 1/2."),
             Assert("All declared reports remain inspectable", squareAccepted == 100 && StatisticalEvidence.Read(square).Events.Length == 100, "The event history contains 100 accepted reports; the count does not become posterior precision.")], square, squareReceipts.ToArray()));

        var rectangle = Apply(square, Question().Observe(new Placeholder()), Bind(99, 100, 1, 100));
        cases.Add(Example("E03", "A single discriminating report can dominate", "Does a rectangle continuation favour one square construction?",
            "The continuation is explicitly modeled as 99/100 likely under the parallel-line recipe and 1/100 under the fixed equal-partition circular recipe. These numbers are test inputs, not measurements or learned estimates.",
            "The 100 preceding shared checks cancel from relative preference. The new report produces 99:1 odds rather than a 101-versus-100 test score.",
            [new("Shared square evidence", "square", "Both candidates begin this step with the same conditional weight.", Rows(prior, square, "Shared reports", "Shared reports")),
             new("Noisy rectangle report", "rectangle", "The report has a nonzero error channel, so neither recipe is logically excluded.", Rows(square, rectangle.State, "99/100", "1/100"))],
            [Assert("Likelihood-ratio update", rectangle.Accepted && Equal(rectangle.State, a, 99, 100), "Equal prior odds multiplied by 99:1 report odds yield 99/100 and 1/100."),
             Assert("Low-weight alternative is retained", StatisticalEvidence.Read(rectangle.State).Rows.Count == 2 && Equal(rectangle.State, b, 1, 100), "No automatic pruning follows from a small posterior weight.")], rectangle.State, rectangle.Surface));

        var hard = Apply(square, Question().Observe(new Placeholder()), Bind(1, 1, 0, 1));
        cases.Add(Example("E04", "An exact incompatibility is not a small score penalty", "Can a fixed true circle with four equal partitions yield a non-square rectangle?",
            "Within the supplied strict construction promises, the report likelihoods are 1 and 0. A reliable proof is being tested, not inferred from this illustration.",
            "The exact zero excludes this particular recipe under these retained premises. Its native candidate and reason remain available for revision or another question.",
            [new("Compatible square", "circle-square", "Both candidates pass the initial shape checks.", Rows(prior, square, "1", "1")),
             new("Exact rectangle witness", "rectangle", "A zero is a strong premise. If the observation or geometric contract is uncertain, the likelihood should not silently be zero.", Rows(square, hard.State, "1", "0", "Candidate retained, including zero weight"))],
            [Assert("Necessary-promise conflict", hard.Accepted && Equal(hard.State, a, 1, 1) && Equal(hard.State, b, 0, 1), "The observed contract yields exact conditional weights 1 and 0."),
             Assert("Exclusion does not erase structure", StatisticalEvidence.Read(hard.State).Rows.Count == 2, "The zero-weight candidate is still present in the native partition.")], hard.State, hard.Surface));

        var partition = Apply(square, Question().Observe(new Placeholder()), Bind(1, 100, 99, 100));
        cases.Add(Example("E05", "The permitted change determines the informative evidence", "What if the observed continuation changes the circular partition count?",
            "This fixture now permits the radial construction to alter its partition count. A pentagon report has supplied likelihoods 1/100 and 99/100. This differs from a fixed four-partition promise.",
            "The same initial square now favours the radial mechanism. Preference depends on the actual exposed possibilities and constraints.",
            [new("Same square starting point", "square", "Visual agreement alone does not decide which construction context is most useful.", Rows(prior, square, "1", "1")),
             new("Allowed partition-count change", "partitions", "The likelihood port records the changed modeling premise. The drawing is explanatory only.", Rows(square, partition.State, "1/100", "99/100"))],
            [Assert("Context-sensitive Bayes factor", partition.Accepted && Equal(partition.State, b, 99, 100), "Swapping the supplied predictions swaps the resulting preference.")], partition.State, partition.Surface));

        var duplicateQuestion = Question(); var duplicateObservation = duplicateQuestion.Observe(new Placeholder());
        var first = Apply(prior, duplicateObservation, Bind(9, 10, 1, 10));
        var again = Apply(first.State, duplicateObservation, Bind(9, 10, 1, 10));
        cases.Add(Example("E06", "Another route is not another observation", "Does importing the same observation twice increase support?",
            "Both imports preserve the same event identity. The second import is a repeated route to the first event.",
            "The event is acknowledged as a duplicate and does not multiply its likelihood again.",
            [new("First import", "square", "A declared discriminating observation changes the odds once.", Rows(prior, first.State, "9/10", "1/10")),
             new("Same event imported again", "duplicate", "The evidence ledger identifies the previously accepted event before requesting new dependency coverage.", Rows(first.State, again.State, "Already accounted", "Already accounted", "Duplicate; no new update"))],
            [Assert("Shared event is idempotent", first.Accepted && again.Duplicate && !again.Accepted && Equal(again.State, a, 9, 10), "The second import retains 9/10 rather than producing 81/82."),
             Assert("No duplicate history participation", StatisticalEvidence.Read(again.State).Events.Length == 1, "One event remains one event across repeated imports.")], again.State, first.Surface, again.Surface));

        var unknownLikelihood = S(new Relation(a, new Placeholder()), new Relation(b, frame.Weight(1, 2)));
        var unknown = Apply(prior, Question().Observe(new Placeholder()), unknownLikelihood);
        cases.Add(Example("E07", "Unknown is not a half, zero or negative answer", "Can the update settle while one likelihood is unavailable?",
            "One candidate likelihood is an open slot. The other is 1/2. No default value is supplied for the open slot.",
            "The update remains open. The last accepted state stays available; it is not presented as a newly calculated posterior.",
            [new("Open report model", "open", "The missing report likelihood is preserved rather than invented.", [
                new(names[0], "1/2", "Open", "Unresolved", "Last accepted weight remains 1/2"),
                new(names[1], "1/2", "1/2", "Unresolved", "Last accepted weight remains 1/2")])],
            [Assert("Missing likelihood blocks this calculation", unknown.Open && !unknown.Accepted, "There is no complete normalized posterior for this offer."),
             Assert("No fabricated evidence", Equal(unknown.State, a, 1, 2) && StatisticalEvidence.Read(unknown.State).Events.Length == 0, "No report is accepted and no open slot becomes a numeric default.")], unknown.State, unknown.Surface));

        var impossible = Apply(prior, Question().Observe(new Placeholder()), Bind(0, 1, 0, 1));
        cases.Add(Example("E08", "When every modeled candidate fails, keep the scope failure", "What if the report is impossible under all current candidates?",
            "Both supplied likelihoods are exactly zero. The candidate scope is finite and explicitly selected; it need not contain the true explanation.",
            "There is no normalized posterior. The contradiction concerns the proposed modeled scope and its premises; it does not justify selecting a winner or manufacturing a new explanation.",
            [new("All-zero report likelihood", "contradiction", "The earlier accepted state is retained alongside the failed update receipt.", [
                new(names[0], "1/2", "0", "No posterior", "Modeled scope contradicted"), new(names[1], "1/2", "0", "No posterior", "Modeled scope contradicted")])],
            [Assert("Zero normalizer is visible", impossible.Contradicted && !impossible.Accepted, "An all-zero product cannot be normalized."),
             Assert("Failed proposal does not overwrite prior", Equal(impossible.State, a, 1, 2) && Equal(impossible.State, b, 1, 2), "The accepted state is retained with its separate failure witness.")], impossible.State, impossible.Surface));

        var finerPrior = frame.Prior(new Relation(a, frame.Weight(50, 100)), new Relation(b, frame.Weight(50, 100)));
        cases.Add(Example("E09", "An unreduced display denominator is not evidence strength", "Does 50/100 establish a more certain balance than 1/2?",
            "These are two probability expressions with equivalent value and different retained partition expressions. Neither contains empirical trial history.",
            "Both reads give 1/2. Evidence strength needs its own justified acquisition/support structure; it cannot be inferred from a denominator alone.",
            [new("Coarse expression", "balance", "The ratio states a balance within the selected candidate partition.", Rows(prior, prior, "No observation", "No observation")),
             new("Finer expression", "resolution", "The input expression is 50/100 for each candidate. Exact normalization reduces the displayed probability without claiming more acquired evidence.", Rows(finerPrior, finerPrior, "No observation", "No observation"))],
            [Assert("Ratio equivalence", Equal(prior, a, 1, 2) && Equal(finerPrior, a, 1, 2), "Equal dimensionless weights produce equal normalized beliefs."),
             Assert("Resolution does not manufacture trials", StatisticalEvidence.Read(finerPrior).Events.Length == 0, "No acquisition history is inferred from 100 displayed parts.")], finerPrior, prior, finerPrior));

        var x = Question().Observe(new Placeholder()); var y = Question().Observe(new Placeholder());
        var xy1 = Apply(prior, x, Bind(9, 10, 1, 10)); var xy2 = Apply(xy1.State, y, Bind(1, 5, 4, 5), true);
        var yx1 = Apply(prior, y, Bind(1, 5, 4, 5)); var yx2 = Apply(yx1.State, x, Bind(9, 10, 1, 10), true);
        cases.Add(Example("E10", "Declared conditional independence permits reordering", "Can independent reports be accounted in either order?",
            "The two report channels are explicitly asserted conditionally independent given each model. Their likelihoods are (9/10,1/10) and (1/5,4/5). Independence is an authored premise.",
            "Both orders produce 9/13 versus 4/13. The numerical agreement does not prove that the independence premise is appropriate in a real application.",
            [new("Report X then report Y", "orientation", "The second offer names the first event in its independent-event surface.", Rows(prior, xy2.State, "(9/10) × (1/5)", "(1/10) × (4/5)")),
             new("Report Y then report X", "orientation", "The reversed calculation records the same declared conditional-independence relationship.", Rows(prior, yx2.State, "(1/5) × (9/10)", "(4/5) × (1/10)"))],
            [Assert("Order invariance under stated premises", xy2.Accepted && yx2.Accepted && Equal(xy2.State, a, 9, 13) && Equal(yx2.State, a, 9, 13), "Exact multiplication yields equal posterior ratios in both declared orders.")], xy2.State, xy1.Surface, xy2.Surface, yx1.Surface, yx2.Surface));

        var missingDependency = StatisticalEvidence.Update(xy1.State, StatisticalEvidence.Offer(y, Bind(1, 5, 4, 5), S(), S(), new Placeholder()));
        cases.Add(Example("E11", "Unstated dependencies remain open", "May we multiply another likelihood without accounting for previous events?",
            "A previous report is already accepted. The next offer neither conditions on that event nor names it in its conditional-independence surface.",
            "The new calculation remains unresolved. Distinct event identities do not by themselves justify independent evidence.",
            [new("Unaccounted prior report", "open", "The previous accepted state remains available while the missing relationship is exposed.", Rows(xy1.State, missingDependency.State, "Dependency open", "Dependency open", "Update unresolved; previous state retained"))],
            [Assert("No implicit independence", missingDependency.Open && !missingDependency.Accepted && Equal(missingDependency.State, a, 9, 10), "The unqualified report does not multiply the existing odds.")], missingDependency.State, xy1.Surface, missingDependency.Surface));

        var afterZero = Apply(hard.State, Question().Observe(new Placeholder()), Bind(1, 100, 99, 100));
        cases.Add(Example("E12", "Retaining a zero-weight candidate is not automatic resurrection", "Can later soft evidence revive a candidate assigned an exact zero?",
            "The earlier exact exclusion and its construction promises remain unchanged. The later report has finite nonzero likelihood under both recipes.",
            "The zero remains zero within this model. Revising the proof, observation model or structural prior requires an explicit new context; it is not accomplished by silently adding a small number.",
            [new("Exact exclusion retained", "rectangle", "The strict circular recipe remains present, with zero weight and its reason.", Rows(square, hard.State, "1", "0")),
             new("Later soft preference for circular recipe", "pentagon", "A finite likelihood ratio cannot undo an exact zero under unchanged premises.", Rows(hard.State, afterZero.State, "1/100", "99/100"))],
            [Assert("Exact zero is absorbing under unchanged model", afterZero.Accepted && Equal(afterZero.State, b, 0, 1), "A structural revision must explicitly change the earlier model or premise."),
             Assert("Zero candidate is still inspectable", StatisticalEvidence.Read(afterZero.State).Rows.Count == 2, "Retention and positive posterior participation are different reads.")], afterZero.State, hard.Surface, afterZero.Surface));

        var thirds = frame.Prior(new Relation(a, frame.Weight(1, 3)), new Relation(b, frame.Weight(2, 3)));
        var coarse = StatisticalEvidence.Quantize(thirds, a, 2); var fine = StatisticalEvidence.Quantize(thirds, a, 100);
        cases.Add(Example("E13", "Selected output resolution retains the same underlying belief", "What does a coarse read of a one-third probability promise?",
            "The exact state is 1/3 versus 2/3. A separate requested read exposes two partitions or 100 partitions. These partitions describe output resolution, not acquired evidence.",
            "The two-part read encloses 1/3 between 0 and 1/2. The 100-part read encloses it between 33/100 and 34/100. Neither changes the state or creates a new report.",
            [new("Two-part output read", "balance", "Displayed bounds retain what this resolution cannot distinguish.", [new(names[0], "1/3", "No new report", $"[{coarse.Lower}, {coarse.Upper}]", "Resolution enclosure")] ),
             new("100-part output read", "resolution", "A finer read narrows presentation bounds while retaining the exact source promise.", [new(names[0], "1/3", "No new report", $"[{fine.Lower}, {fine.Upper}]", "Resolution enclosure")] )],
            [Assert("Coarse enclosure", coarse.Complete && Fraction(coarse.Lower, 0, 1) && Fraction(coarse.Upper, 1, 2), "The coarse read retains a conservative bound rather than rounding to false certainty."),
             Assert("Fine enclosure", fine.Complete && Fraction(fine.Lower, 33, 100) && Fraction(fine.Upper, 17, 50), "The fine upper bound 34/100 is exactly 17/50."),
             Assert("Observation resolution is not mutation", Equal(thirds, a, 1, 3) && StatisticalEvidence.Read(thirds).Events.Length == 0, "Requesting refinement adds no acquisition event and changes no underlying probability.")], thirds, coarse.Surface, fine.Surface));

        var supportFrame = new EvidenceSupport.Frame();
        var looseEvent = new Placeholder(); var looseAcquisition = supportFrame.Acquisition(looseEvent);
        var loose = EvidenceSupport.Create(supportFrame, 1, 1, looseAcquisition);
        var events = Enumerable.Range(0, 99).Select(_ => (IElement)new Placeholder()).ToArray();
        var acquisition = supportFrame.Acquisition(events); var supported = EvidenceSupport.Create(supportFrame, 1, 1, acquisition);
        for (var i = 0; i < 98; i++) supported = supported.Observe(events[i], false, i < 49, acquisition);
        var looseRead = EvidenceSupport.Read(loose); var supportedRead = EvidenceSupport.Read(supported);
        var nextLoose = loose.Observe(looseEvent, false, true, looseAcquisition);
        var nextSupported = supported.Observe(events[98], false, true, acquisition);
        var nextLooseRead = EvidenceSupport.Read(nextLoose); var nextSupportedRead = EvidenceSupport.Read(nextSupported);
        var supportText = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("empirical-support-case-v1", supported.Surface));
        var supportTransport = GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(supportText), out var supportDecoded);
        var restoredSupport = supportTransport && supportDecoded.Root is Relation supportRoot ? EvidenceSupport.Read(supportRoot) : null;
        cases.Add(Example("E14", "Balance and acquired support are separate reads", "How can two half-and-half beliefs respond differently to new evidence?",
            "A separately selected empirical Beta/Bernoulli law has prior support 1:1. One channel has no observations. The other has 49 positive and 49 opposed events under an explicit common-rate, conditionally independent acquisition contract. Those premises are supplied rather than inferred.",
            "Both conditional mean estimates are 1/2, but their support and rate dispersion differ. The next binary outcome remains equally uncertain in both channels. One new positive event yields 2/3 in the lightly supported channel and 51/101 in the historical channel. This is a distinct empirical law, not a reinterpretation of a probability denominator.",
            [new("Same mean, different acquisition histories", "balance", "Rate dispersion belongs to the selected model, not a guaranteed physical error bar or proof of source reliability. One next-outcome variance remains 1/4 in both cases.", [
                new("No acquired reports", "Beta support 1:1", "0 events", looseRead.Mean.Reduced, $"Variance {looseRead.Variance.Reduced}"),
                new("Balanced acquired history", "Beta support 1:1", "49 positive + 49 opposed", supportedRead.Mean.Reduced, $"Variance {supportedRead.Variance.Reduced}")]),
             new("One further positive report", "resolution", "The same one-event contribution has less influence on a rate estimate backed by the explicitly selected acquisition history.", [
                new("Lightly supported channel", looseRead.Mean.Reduced, "One positive event", nextLooseRead.Mean.Reduced, $"Support {nextLooseRead.A}:{nextLooseRead.B}"),
                new("Historical channel", supportedRead.Mean.Reduced, "One positive event", nextSupportedRead.Mean.Reduced, $"Support {nextSupportedRead.A}:{nextSupportedRead.B}")])],
            [Assert("Equal balance does not imply equal support", looseRead.Mean.Reduced == "1/2" && supportedRead.Mean.Reduced == "1/2" && looseRead.Concentration == 2 && supportedRead.Concentration == 100,
                "The native acquisition history distinguishes no reports from 98 reports despite equal means."),
             Assert("Both selected empirical histories are complete", looseRead.Complete && supportedRead.Complete && nextLooseRead.Complete && nextSupportedRead.Complete,
                "The displayed empirical calculations account for every submitted report under their declared acquisition contracts."),
             Assert("Dispersion follows the selected empirical law", looseRead.Variance.Reduced == "1/12" && supportedRead.Variance.Reduced == "1/404", "These are exact conditional Beta variances, not confidence levels or hard bounds."),
             Assert("Rate uncertainty and outcome uncertainty differ", looseRead.PredictiveVariance.Reduced == "1/4" && supportedRead.PredictiveVariance.Reduced == "1/4", "Additional balanced history narrows the rate estimate without making a single binary outcome predictable."),
             Assert("New event influence depends on acquired support", nextLooseRead.Mean.Reduced == "2/3" && nextSupportedRead.Mean.Reduced == "51/101", "The two rate estimates respond differently without changing output resolution."),
             Assert("Empirical support survives native transport", restoredSupport is not null && restoredSupport.A == 50 && restoredSupport.B == 50 && restoredSupport.AcceptedEvents == 98,
                "The reconstructed native state preserves the common-rate contract and all 98 distinct events.")],
            prior, loose.Surface, supported.Surface, looseRead.Surface, supportedRead.Surface, nextLoose.Surface, nextSupported.Surface));

        var revised = StatisticalEvidence.RevisePrior(rectangle.State, Bind(1, 100, 99, 100), new Relation(prior, rectangle.Surface));
        cases.Add(Example("E15", "Prior revision is explicit and preserves the earlier calculation", "What changes when the prior selection weights are deliberately revised?",
            "The same 101 accepted report factors are retained. An explicit revision replaces equal prior weights with 1/100 for the parallel-line recipe and 99/100 for the circular recipe. This fixture changes prior weights; it does not automatically mutate a geometric recipe.",
            "The revised prior exactly balances the rectangle report's 99:1 likelihood ratio, giving a 1/2 posterior. The previous 99/100 result remains valid under its earlier premise and is retained in the revision receipt.",
            [new("Original declared prior", "rectangle", "Equal prior weights and the discriminating rectangle report yield the earlier preference.", Rows(square, rectangle.State, "99/100", "1/100")),
             new("Explicitly revised prior", "balance", "The retained likelihood factors are reevaluated against the new prior. No additional observation is counted.", [
                new(names[0], "1/100", "99/100", Probability(revised.State, a), "New prior context"),
                new(names[1], "99/100", "1/100", Probability(revised.State, b), "New prior context")])],
            [Assert("Revised prior is accounted explicitly", revised.Accepted && Equal(revised.State, a, 1, 2) && Equal(revised.State, b, 1, 2), "Prior odds 1:99 balance the retained 99:1 report factor."),
             Assert("Revision is not another acquisition", StatisticalEvidence.Read(revised.State).Events.Length == StatisticalEvidence.Read(rectangle.State).Events.Length, "All accepted report identities survive without adding an event."),
             Assert("Earlier premise and result remain available", Equal(rectangle.State, a, 99, 100), "The revision receipt retains the previous state instead of silently rewriting its history.")], revised.State, rectangle.Surface, revised.Surface));

        var poorlyPredicted = Apply(prior, Question().Observe(new Placeholder()), Bind(1, 100, 1, 100));
        var expectedReport = Apply(prior, Question().Observe(new Placeholder()), Bind(1, 1, 1, 1));
        var poorRead = StatisticalEvidence.Read(poorlyPredicted.State); var expectedRead = StatisticalEvidence.Read(expectedReport.State);
        cases.Add(Example("E16", "A balanced comparison can still fit the report poorly", "Does a half-and-half posterior mean the current candidate set explains the observation well?",
            "Two separate fixture worlds start from equal candidate priors. In one world, each candidate assigns the observed report probability 1/100. In the other, each assigns it probability 1. The report models and candidate scope are supplied explicitly.",
            "Both relative comparisons remain tied at 1/2, but their marginal report probabilities are 1/100 and 1. Relative preference alone cannot reveal that the whole modeled scope was surprised. A low marginal probability is a reason to inspect the model; it is not automatic proof that an unmodeled alternative is better.",
            [new("Equally poor report predictions", "square", $"The same illustrated report still has nonzero probability and the update is valid. Marginal report probability: {poorRead.EvidenceProbability}.",
                Rows(prior, poorlyPredicted.State, "1/100", "1/100", $"Scope report probability {poorRead.EvidenceProbability}")),
             new("Equally expected report predictions", "square", $"The candidates make the same relative predictions but a different joint prediction. Marginal report probability: {expectedRead.EvidenceProbability}.",
                Rows(prior, expectedReport.State, "1", "1", $"Scope report probability {expectedRead.EvidenceProbability}"))],
            [Assert("Equal likelihoods preserve relative weights", poorlyPredicted.Accepted && expectedReport.Accepted && Equal(poorlyPredicted.State, a, 1, 2) && Equal(expectedReport.State, a, 1, 2),
                "Normalization preserves the relative tie in both supplied worlds."),
             Assert("Marginal evidence preserves the scope prediction", poorRead.EvidenceProbability is { } low && Fraction(low, 1, 100) && expectedRead.EvidenceProbability is { } high && Fraction(high, 1, 1),
                "The normalized relative posterior is accompanied by the exact report probability under the selected prior and model scope.")],
            poorlyPredicted.State, poorlyPredicted.Surface, expectedReport.Surface, poorRead.EvidenceExpression, expectedRead.EvidenceExpression));

        return cases.Select(item =>
        {
            var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("evidence-calculation-case-v1", item.Native));
            var restored = GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var decoded) && decoded.Root is Relation;
            var actual = restored ? StatisticalEvidence.Read((Relation)((Relation)decoded.Root!).Right) : null;
            var expected = StatisticalEvidence.Read((Relation)item.Native.Right);
            var same = actual is not null && actual.Complete == expected.Complete && actual.Inconsistent == expected.Inconsistent &&
                actual.Events.Length == expected.Events.Length && actual.EvidenceProbability?.ToString() == expected.EvidenceProbability?.ToString() &&
                actual.Rows.Select(row => row.Probability?.ToString()).SequenceEqual(expected.Rows.Select(row => row.Probability?.ToString()));
            return item with { Checks = item.Checks.Append(Assert("Standalone native transport", same, "Frame, candidate aliases, likelihood products and retained event history survive a fresh compact-JSON reconstruction.")).ToArray() };
        }).ToArray();
    }

    private static bool Fraction(StatisticalEvidence.Ratio value, int numerator, int denominator) => value.Numerator * denominator == value.Denominator * numerator;
    private static bool Matches(Relation state, IElement model, int numerator, int denominator) =>
        StatisticalEvidence.Read(state).Rows.Single(row => ReferenceEquals(row.Model, model)).Probability is { } value && Fraction(value, numerator, denominator);
    private static string Probability(Relation state, IElement model) =>
        StatisticalEvidence.Read(state).Rows.Single(row => ReferenceEquals(row.Model, model)).Probability?.ToString() ?? "Open";

    private static void Write(string output, IReadOnlyList<Case> cases, object summary)
    {
        var rows = cases.Select(item => new { item.Id, item.Name, item.Question, item.Premises, item.Finding,
            Steps = item.Steps.Select((step, index) => new { step.Name, step.Explanation, step.Candidates,
                Image = $"diagrams/{item.Id.ToLowerInvariant()}-{index + 1}.svg" }).ToArray(), item.Checks }).ToArray();
        for (var at = 0; at < cases.Count; at++)
            for (var index = 0; index < cases[at].Steps.Count; index++)
                Save(Path.Combine(output, rows[at].Steps[index].Image), Icon(cases[at].Steps[index].Icon, cases[at].Steps[index].Name));
        var payload = JsonSerializer.Serialize(new { schema = "evidence-calculation-v1", summary, cases = rows }, Json);
        Save(Path.Combine(output, "gallery-data.json"), payload); Save(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        var lead = """
<header><p class="eyebrow">Gojo · evidence arithmetic fixtures</p><h1>What should change our belief?</h1>
<p>These small cases test calculation laws before larger recognition experiments. Every likelihood and independence premise is supplied explicitly. The drawings explain the questions; no answer is extracted from the drawings.</p>
<nav><a href="gallery.html">Inspect one case</a><a href="overview.html">All cases</a><a href="gallery-data.json">Calculation data</a><a href="receipts.gojo.json.gz">Native receipts</a></nav></header>
""";
        var metrics = $"<section class='metrics'><span><strong>{cases.Count}</strong> cases</span><span><strong>{cases.Sum(item => item.Checks.Count)}</strong> checked laws</span><span>Exact rational reads · explicit premises</span></section>";
        var script = """
<script>
globalThis.EVIDENCE_CALCULATION=PAYLOAD;
const data=globalThis.EVIDENCE_CALCULATION;
const esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
function card(row){return `<article class="case" data-case="${esc(row.id)}"><p class="eyebrow">${esc(row.id)}</p><h2>${esc(row.name)}</h2><p class="question">${esc(row.question)}</p><p class="premise"><strong>Supplied premises.</strong> ${esc(row.premises)}</p><div class="steps">${row.steps.map((s,i)=>`<section class="step"><h3>${i+1} · ${esc(s.name)}</h3><img src="${esc(s.image)}" alt="${esc(s.name)}"><p>${esc(s.explanation)}</p><div class="table-scroll"><table><thead><tr><th>Candidate</th><th>Prior / basis</th><th>Report input</th><th>Calculated read</th><th>Qualification</th></tr></thead><tbody>${s.candidates.map(c=>`<tr><th>${esc(c.candidate)}</th><td>${esc(c.prior)}</td><td>${esc(c.likelihood)}</td><td>${esc(c.posterior)}</td><td>${esc(c.status)}</td></tr>`).join('')}</tbody></table></div></section>`).join('')}</div><p class="finding">${esc(row.finding)}</p><details class="checks"><summary>${row.checks.filter(c=>c.passed).length}/${row.checks.length} calculation laws verified</summary><ul>${row.checks.map(c=>`<li><strong>${esc(c.law)}</strong> — ${esc(c.detail)}</li>`).join('')}</ul></details></article>`;}
const select=document.querySelector('#case');
if(select){for(const row of data.cases){const o=document.createElement('option');o.value=row.id;o.textContent=`${row.id} · ${row.name}`;select.append(o);}const show=()=>{const row=data.cases.find(x=>x.id===select.value)??data.cases[0];select.value=row.id;document.querySelector('#content').innerHTML=card(row);const u=new URL(location.href);u.searchParams.set('case',row.id);history.replaceState(null,'',u);};select.value=new URL(location.href).searchParams.get('case')??data.cases[0].id;select.addEventListener('change',show);show();}
else document.querySelector('#content').innerHTML=data.cases.map(card).join('');
</script>
""".Replace("PAYLOAD", payload, StringComparison.Ordinal);
        var footer = "<footer><p>Probabilities describe this selected comparison. An open likelihood remains unresolved; zero weight retains its candidate and reason. A failed modeled scope does not become certainty about an unmodeled explanation. Native receipts retain premises, updates and alternatives.</p></footer>";
        Save(Path.Combine(output, "gallery.html"), Page(lead + metrics + "<label class='chooser'>Case <select id='case'></select></label><main id='content'></main>" + footer + script));
        Save(Path.Combine(output, "overview.html"), Page(lead + metrics + "<main id='content' class='overview'></main>" + footer + script));
    }

    private static string Page(string body) => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · evidence calculation</title><style>
:root{color-scheme:light;--paper:#f5f2e8;--ink:#223f48;--muted:#59696b;--line:#c8cbc1;--accent:#9b652d}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.55 system-ui,sans-serif}header,main,footer,.metrics,.chooser{max-width:1500px;margin:auto;padding:24px 32px}header{padding-top:44px}h1{font:600 clamp(28px,4vw,46px)/1.15 Georgia,serif;margin:.2em 0 .5em}h2{font:600 28px/1.25 Georgia,serif;margin:.2em 0 .6em}h3{font-size:18px;margin:0 0 12px}p{margin:.6em 0}.eyebrow{text-transform:uppercase;font-size:12px;letter-spacing:.12em;color:var(--muted)}nav{display:flex;gap:12px 22px;flex-wrap:wrap;margin-top:22px}a{color:#305f79;text-underline-offset:3px}.metrics{border-top:1px solid var(--line);border-bottom:1px solid var(--line);display:flex;gap:20px;flex-wrap:wrap;padding-top:15px;padding-bottom:15px}.metrics strong{font-size:23px}.chooser{display:block}select{font:inherit;color:inherit;border:1px solid #91a3a1;background:#fffdf5;border-radius:5px;padding:9px 12px;max-width:100%;margin-left:12px}.case{border:1px solid var(--line);background:#fffdf7;border-radius:8px;padding:26px;margin:0 0 26px}.question{font-size:19px}.premise{padding:12px 16px;border-left:3px solid #9caeac;background:#eef0e9}.steps{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,420px),1fr));gap:20px;margin-top:24px}.step{min-width:0;border:1px solid #d9d9ce;border-radius:5px;padding:16px;background:#faf9f2}.step img{display:block;width:100%;height:180px;background:#f2f0e6;border-radius:4px}.step p{min-height:48px}.table-scroll{overflow:auto}table{border-collapse:collapse;width:100%;font-size:14px}th,td{text-align:left;border-bottom:1px solid #d3d7cd;padding:8px 7px;vertical-align:top}thead th{font-size:12px;font-weight:600;color:var(--muted)}tbody th{font-weight:500}.finding{border-top:1px solid var(--line);padding-top:15px;font-weight:550}.checks{font-size:14px;color:var(--muted)}summary{cursor:pointer;padding:6px 0}li{margin:9px 0}footer{color:var(--muted);font-size:14px}.overview .case{margin-bottom:36px}@media(max-width:650px){header,main,footer,.metrics,.chooser{padding-left:16px;padding-right:16px}.case{padding:16px}.step{padding:12px}h2{font-size:24px}select{display:block;margin:8px 0 0;width:100%}.steps{gap:12px}.step p{min-height:0}}
</style></head><body>BODY</body></html>
""".Replace("BODY", body, StringComparison.Ordinal);

    private static string Icon(string kind, string title)
    {
        var content = kind switch
        {
            "square" => "<path d='M130 30H270V170H130Z'/>",
            "rectangle" => "<path d='M85 50H315V150H85Z'/>",
            "pentagon" => "<path d='M200 20L285 82L253 175H147L115 82Z'/>",
            "circle-square" => "<circle cx='200' cy='100' r='80' stroke='#a6b4ad' stroke-dasharray='5 5'/><path d='M144 44H256V156H144Z'/>",
            "open" => "<path d='M130 30H270V170H130V110' stroke-dasharray='7 4'/><text x='200' y='110' text-anchor='middle' stroke='none' fill='#59696b' font-size='36'>?</text>",
            "duplicate" => "<path d='M100 45H200V145H100Z'/><path d='M200 70H300V170H200Z' stroke='#a6b4ad'/><path d='M145 18Q200 -6 255 18' stroke='#9b652d'/>",
            "partitions" => "<circle cx='200' cy='100' r='76' stroke='#a6b4ad' stroke-dasharray='4 4'/><path d='M200 24L272 76L245 161H155L128 76Z'/><path d='M200 100L200 24M200 100L272 76M200 100L245 161M200 100L155 161M200 100L128 76' stroke='#a6b4ad' stroke-width='2'/>",
            "balance" => "<path d='M200 40V170M135 170H265M100 65H300M125 65L90 120H160ZM275 65L240 120H310Z'/>",
            "contradiction" => "<path d='M85 55H190V155H85ZM225 35L300 145H150Z'/><path d='M100 20L310 180M310 20L100 180' stroke='#9b652d' stroke-width='3'/>",
            "resolution" => "<path d='M55 65H345V135H55Z'/><path d='M200 65V135' stroke='#9b652d'/><path d='M84 65V135M113 65V135M142 65V135M171 65V135M229 65V135M258 65V135M287 65V135M316 65V135' stroke='#a6b4ad' stroke-width='1.5'/>",
            "orientation" => "<path d='M90 80H310M90 120H310M290 60L310 80L290 100M110 100L90 120L110 140'/><text x='200' y='58' text-anchor='middle' stroke='none' fill='#59696b' font-size='18'>keep / discard</text>",
            _ => "<path d='M95 100H305M200 30V170'/><circle cx='200' cy='100' r='10' fill='#9b652d' stroke='none'/>"
        };
        return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 400 200' role='img'><title>{WebUtility.HtmlEncode(title)}</title><g fill='none' stroke='#305f69' stroke-width='4' stroke-linecap='round' stroke-linejoin='round'>{content}</g></svg>";
    }

    private static void Save(string path, string content) => JointReconstructionGallery.WriteText(path, content);
}
