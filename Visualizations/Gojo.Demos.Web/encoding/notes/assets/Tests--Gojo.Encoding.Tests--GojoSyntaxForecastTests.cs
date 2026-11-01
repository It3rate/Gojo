using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxForecastTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FitForecastObserveAndRevisePreservesProspectiveFailure(int restores)
    {
        var f = new Fixture(restores);
        var a = f.Record(0, 3, 100);
        var b = f.Record(1, 7, 101);
        var fit = f.Fit(a, b);
        var first = f.Issue(f.Candidate(fit), 2, 102);
        Number(F(F(first)[2])[4], f.Quantity, 11);
        Assert.Equal(2, F(F(f.State)[2]).Length); // No target sample exists here.
        Assert.Empty(F(F(f.State)[4]));
        var c = f.Record(2, 11, 103);
        var success = f.Assess(first, c, 104);
        Number(F(success)[2], f.Quantity, 0);

        var second = f.Issue(f.Candidate(f.Fit(b, c)), 3, 105);
        Number(F(F(second)[2])[4], f.Quantity, 15);
        var d = f.Record(3, 14, 106);
        Number(F(f.Assess(second, d, 107))[2], f.Quantity, -1);
        var revised = f.Issue(f.Candidate(f.Fit(c, d)), 4, 108);
        Number(F(F(revised)[2])[4], f.Quantity, 17);
        Number(F(F(second)[2])[4], f.Quantity, 15);
        Assert.IsType<Placeholder>(F(F(revised)[2])[5]);
        Assert.Equal(0UL, f.Quantity.Value);
        Assert.Equal(0, f.Quantity.Tension); // Assessment is a read, not a reward or delta.
    }

    [Theory]
    [InlineData(0, 3, 1, 7, 2, 11, 1)]
    [InlineData(10, 3, 14, 7, 20, 13, 1)]
    [InlineData(0, 0, 3, 1, 4, 4, 3)]
    [InlineData(0, -3, 1, -7, 2, -11, 1)]
    [InlineData(0, 5, 4, 5, 9, 5, 1)]
    public void ContinuationUsesSourceTimeSignedValuesAndExactRationals(
        long t0, long x0, long t1, long x1, long target, long numerator, long denominator)
    {
        var f = new Fixture();
        var a = f.Record(t0, x0, 800);
        var b = f.Record(t1, x1, 803);
        var issue = f.Issue(f.Candidate(f.Fit(a, b)), target, 999);
        Number(F(F(issue)[2])[4], f.Quantity, numerator, denominator);
        var observed = f.Record(target, ProjectionResidue.Create(f.Quantity, numerator, denominator), 1000);
        var assessment = f.Assess(issue, observed, 1200);
        Number(F(assessment)[2], f.Quantity, 0);
        if (x0 == x1)
        {
            var remainder = R(Assert.Single(F(F(issue)[3])));
            Number(remainder.Left, f.SourceClock, target);
            Assert.Same(F(issue)[3], F(assessment)[3]);
        }
    }

    [Fact]
    public void TwoInversionsHaveSameExposedPredictionWithoutForcingInternalAgreement()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0);
        var b = f.Record(1, 7, 1);
        var direct = f.Issue(f.Candidate(f.Fit(a, b)), 2, 2);
        var invertedFit = f.Fit(a, b, 1);
        var inverted = f.Issue(f.Candidate(invertedFit, 2), 2, 3);
        var latent = Assert.IsType<CarrierLocation>(R(R(F(invertedFit)[2]).Right).Left).Carrier;
        Number(F(F(inverted)[4])[1], latent, -11);
        Number(F(F(direct)[2])[4], f.Quantity, 11);
        Number(F(F(inverted)[2])[4], f.Quantity, 11);
        Assert.NotSame(F(F(direct)[2])[3], F(F(inverted)[2])[3]);
        var c = f.Record(2, 11, 4);
        Number(F(f.Assess(direct, c, 5))[2], f.Quantity, 0);
        Number(F(f.Assess(inverted, c, 6))[2], f.Quantity, 0);
        Assert.Same(c, F(F(F(F(f.State)[4])[0])[1])[2]);
        // Two models tested on the same receipt do not make two independent observations.
        Assert.Equal(3, F(F(f.State)[2]).Length);
    }

    [Fact]
    public void WrongReadoutIsAConcreteFailedPredictionRatherThanAnIllegalInternalSign()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var fit = f.Fit(a, b, 1);
        var latent = Assert.IsType<CarrierLocation>(R(R(F(fit)[2]).Right).Left).Carrier;
        var wrongReadout = new Relation(Relation.Segment(latent, 0, 1), Relation.Segment(f.Quantity, 0, 1));
        var issue = f.Issue(G(fit, wrongReadout, new Placeholder()), 2, 2);
        Number(F(F(issue)[2])[4], f.Quantity, -11);
        var c = f.Record(2, 11, 3);
        Number(F(f.Assess(issue, c, 4))[2], f.Quantity, 22);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AlreadyObservedOrEarlierTargetsCannotBeIssuedAsFutureTests(long target)
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var candidate = f.Candidate(f.Fit(a, b));
        f.Record(2, 11, 2);
        var state = f.State;
        var request = f.IssueRequest(candidate, target, 3);
        Assert.False(LocalCalibration.TryIssueForecast(request, out var unresolved, out var retained));
        Assert.Same(state, retained);
        Assert.IsType<Placeholder>(F(unresolved)[2]);
        Assert.Same(request, F(unresolved)[3]);
    }

    [Fact]
    public void FittingAfterOutcomeCannotBeSmuggledIntoAnEarlierLocalState()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var earlier = f.State;
        f.Record(2, 11, 2);
        var fit = f.Fit(a, b); // Fits old samples, but its declared availability includes the outcome.
        var request = Replace(f.IssueRequest(f.Candidate(fit), 2, 3), 0, earlier);
        Assert.False(LocalCalibration.TryIssueForecast(request, out _, out var retained));
        Assert.Same(earlier, retained);
    }

    [Fact]
    public void UnavailableLookalikeReceiptsAndReversedFitCannotSupplyTrainingEvidence()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var earlier = f.State;
        var b = f.Record(1, 7, 1);
        var request = f.FitRequest(a, b);
        Assert.False(LocalCalibration.TryFitTemporalFrame(Replace(request, 0, earlier), out _));
        Assert.False(LocalCalibration.TryFitTemporalFrame(Replace(request, 1, G(F(a))), out _));
        Assert.False(LocalCalibration.TryFitTemporalFrame(f.FitRequest(b, a), out _));
        Assert.False(LocalCalibration.TryFitTemporalFrame(f.FitRequest(a, a), out _));
    }

    [Fact]
    public void PendingSampleIsRetainedButCannotTrainOrAssessAndPreventsBackdatedForecast()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var candidate = f.Candidate(f.Fit(a, b));
        var issue = f.Issue(candidate, 2, 2);
        var unknown = new Placeholder();
        var pending = f.Record(2, unknown, 3);
        Assert.Same(unknown, F(F(F(pending)[2])[0])[2]);
        Assert.False(LocalCalibration.TryFitTemporalFrame(f.FitRequest(b, pending), out _));
        Assert.False(LocalCalibration.TryAssessForecast(f.AssessRequest(issue, pending, 4), out _, out var retained));
        Assert.Same(f.State, retained);
        Assert.False(LocalCalibration.TryIssueForecast(f.IssueRequest(candidate, 2, 4), out _, out _));
        // Revising the same source event requires a separate correction contract.
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(2, 11, 4), out _, out _));
    }

    [Fact]
    public void TargetCorrespondenceIsSpecificEvenWhenLaterValuesCoincide()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var issue = f.Issue(f.Candidate(f.Fit(a, b)), 2, 2);
        var later = f.Record(3, 11, 4); // Correct number, wrong target event.
        Assert.False(LocalCalibration.TryAssessForecast(f.AssessRequest(issue, later, 5), out _, out var retained));
        Assert.Same(f.State, retained);
        Assert.Empty(F(F(retained)[4]));
    }

    [Fact]
    public void DelayedAssessmentOfEarlierTargetSurvivesLaterSamplesAndConsumesIssueOnce()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var issue = f.Issue(f.Candidate(f.Fit(a, b)), 2, 2);
        var c = f.Record(2, 11, 50);
        f.Record(3, 15, 51);
        var old = f.State;
        Number(F(f.Assess(issue, c, 100))[2], f.Quantity, 0);
        Assert.False(LocalCalibration.TryAssessForecast(f.AssessRequest(issue, c, 101), out _, out var retained));
        Assert.Same(f.State, retained);
        Assert.Empty(F(F(old)[4]));
        Assert.Single(F(F(f.State)[4]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void EqualSampleWithWrongPortClockSubjectOrContextIsRefused(int field)
    {
        var f = new Fixture();
        var request = f.RecordRequest(0, 3, 10);
        IElement foreign = field == 1 ? new CarrierLocation(new Carrier(), 0) : new Placeholder();
        request = Replace(request, 1, Replace(F(request)[1], field, foreign));
        Assert.False(LocalCalibration.TryRecordTemporalSample(request, out _, out var retained));
        Assert.Same(f.State, retained);
    }

    [Fact]
    public void BasisIdentityAndLocalEventOrderingAreNotInferredFromEqualCoordinates()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 10);
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(1, 7, 10), out _, out _));
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(1, 7, 9), out _, out _));
        Assert.False(LocalCalibration.TryRecordTemporalSample(Replace(f.RecordRequest(1, 7, 11), 2,
            new CarrierLocation(new Carrier(f.LocalClock.UnitShape), 11)), out _, out _));
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(1,
            new CarrierLocation(new Carrier(f.Quantity.UnitShape), 7), 11), out _, out _));
        var b = f.Record(1, 7, 11);
        var request = f.IssueRequest(f.Candidate(f.Fit(a, b)), 2, 12);
        Assert.False(LocalCalibration.TryIssueForecast(Replace(request, 2,
            new CarrierLocation(f.LocalClock, 2)), out _, out _));
        Assert.False(LocalCalibration.TryIssueForecast(Replace(request, 3,
            new CarrierLocation(f.LocalClock, 11)), out _, out _));
    }

    [Fact]
    public void OriginalSampleAndModelRetargetingCannotRewriteCapturedForecast()
    {
        var f = new Fixture();
        var raw = new CarrierLocation(f.Quantity, 3);
        var a = f.Record(0, raw, 0); var b = f.Record(1, 7, 1);
        raw.Tick.Set(300);
        var fit = f.Fit(a, b);
        var readout = new Relation(Relation.Segment(f.Quantity, 0, 1), Relation.Segment(f.Quantity, 0, 1));
        var target = new CarrierLocation(f.SourceClock, 2);
        var request = Replace(f.IssueRequest(G(fit, readout, new Placeholder()), 2, 2), 2, target);
        Assert.True(LocalCalibration.TryIssueForecast(request, out var issue, out var state));
        f.State = state;
        Assert.IsType<CarrierLocation>(R(R(F(fit)[2]).Right).Right).Tick.Set(700);
        Assert.IsType<CarrierLocation>(R(R(readout.Right).Right).Left).Tick.Set(200);
        target.Tick.Set(2000);
        var c = f.Record(2, 11, 3);
        Number(F(f.Assess(issue, c, 4))[2], f.Quantity, 0);
        Number(F(F(issue)[2])[4], f.Quantity, 11);
        Assert.Equal(2, Assert.IsType<CarrierLocation>(F(F(issue)[2])[2]).Ticks);
    }

    [Fact]
    public void ChangedFitBeforeIssueAndUnrepresentableForecastStayUnresolved()
    {
        var f = new Fixture();
        var a = f.Record(0, 0, 0); var b = f.Record(1, long.MaxValue, 1);
        var fit = f.Fit(a, b);
        var request = f.IssueRequest(f.Candidate(fit), 2, 2);
        Assert.False(LocalCalibration.TryIssueForecast(request, out var unresolved, out var retained));
        Assert.Same(f.State, retained);
        Assert.Same(request, F(unresolved)[3]);
        Assert.IsType<CarrierLocation>(R(R(F(fit)[2]).Right).Right).Tick.Set(7);
        Assert.False(LocalCalibration.TryIssueForecast(request, out _, out _));
    }

    [Fact]
    public void SelectedReadoutRequiresNondegenerateCompatibleFrames()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var fit = f.Fit(a, b);
        foreach (var readout in new[]
        {
            new Relation(Relation.Segment(f.Quantity, 0, 1), Relation.Segment(f.Quantity, 11, 11)),
            new Relation(Relation.Segment(f.Quantity, 0, 0), Relation.Segment(f.Quantity, 0, 1)),
            new Relation(Relation.Segment(new Carrier(), 0, 1), Relation.Segment(f.Quantity, 0, 1))
        })
            Assert.False(LocalCalibration.TryIssueForecast(f.IssueRequest(G(fit, readout, new Placeholder()), 2, 2), out _, out _));
    }

    [Fact]
    public void CompetingContinuationsRetainTheirHistoryAndWaitForTheSameLaterObservation()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var original = f.Candidate(f.Fit(a, b));
        var c = f.Record(2, 11, 2); var d = f.Record(3, 14, 3);
        var revised = f.Candidate(f.Fit(c, d));
        var olderIssue = f.Issue(original, 4, 4);
        var newerIssue = f.Issue(revised, 4, 5);
        Number(F(F(olderIssue)[2])[4], f.Quantity, 19);
        Number(F(F(newerIssue)[2])[4], f.Quantity, 17);
        Assert.Empty(F(F(f.State)[4]));
        var outcome = f.Record(4, 17, 6);
        var first = f.Assess(olderIssue, outcome, 7);
        var second = f.Assess(newerIssue, outcome, 8);
        Number(F(first)[2], f.Quantity, -2);
        Number(F(second)[2], f.Quantity, 0);
        Assert.Same(outcome, F(F(first)[1])[2]);
        Assert.Same(outcome, F(F(second)[1])[2]);
        Assert.Equal(2, F(F(f.State)[3]).Length); // No automatic winner or pruning.
    }

    [Fact]
    public void LookalikeIssueCannotClaimAnAssessmentAndSourceEventsCannotRepeat()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var issue = f.Issue(f.Candidate(f.Fit(a, b)), 2, 2);
        var outcome = f.Record(2, 11, 3);
        Assert.False(LocalCalibration.TryAssessForecast(f.AssessRequest(G(F(issue)), outcome, 4), out _, out _));
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(2, 11, 4), out _, out _));
        Assert.False(LocalCalibration.TryRecordTemporalSample(f.RecordRequest(1, 7, 4), out _, out _));
        Assert.False(LocalCalibration.TryAssessForecast(f.AssessRequest(issue, outcome, 3), out _, out _));
        Number(F(f.Assess(issue, outcome, 4))[2], f.Quantity, 0);
    }

    [Fact]
    public void PendingForecastRestoresAndAcceptsFutureEvidenceWithAliasesAndAssessmentHistory()
    {
        var f = new Fixture();
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        var issue = f.Issue(f.Candidate(f.Fit(a, b)), 2, 2);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode([.. f.Roots, f.State, issue]));
        var restored = new Fixture(roots[..5]) { State = R(roots[5]) };
        var restoredIssue = R(roots[6]);
        Assert.Same(restoredIssue, F(F(restored.State)[3])[0]);
        Assert.NotSame(f.Quantity, restored.Quantity);
        var c = restored.Record(2, 11, 3);
        Number(F(restored.Assess(restoredIssue, c, 4))[2], restored.Quantity, 0);
        var second = GojoNumericFormat.Decode(GojoNumericFormat.Encode([restored.State, restoredIssue, c]));
        var again = R(second[0]);
        var clock = Assert.IsType<Carrier>(F(F(again)[0])[3]);
        Assert.False(LocalCalibration.TryAssessForecast(G(again, second[1], second[2],
            new CarrierLocation(clock, 5)), out _, out var retained));
        Assert.Same(again, retained);
        Assert.Single(F(F(again)[4]));
    }

    [Fact]
    public void RenamingSyntaxDoesNotChangeTheTemporalRead()
    {
        var text = Fixture.Text();
        var names = new Dictionary<string, string>();
        text = Regex.Replace(text, @"[$?][A-Za-z_][A-Za-z_0-9]*", m =>
            names.TryGetValue(m.Value, out var name) ? name : names[m.Value] = m.Value[0] + "n" + names.Count);
        var f = new Fixture(GojoSyntax.Parse(text).Roots.ToArray());
        var a = f.Record(0, 3, 0); var b = f.Record(1, 7, 1);
        Number(F(F(f.Issue(f.Candidate(f.Fit(a, b)), 2, 2))[2])[4], f.Quantity, 11);
    }

    [Fact]
    public void EveryDemoStageMatchesExecutedSyntaxState()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "prospective-calibration.html"));
        var json = Regex.Match(html, "<script id=\"trace\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(json.Success);
        using var document = JsonDocument.Parse(json.Groups[1].Value);
        var rows = document.RootElement.EnumerateArray().ToArray();
        var f = new Fixture(); var index = 0;
        void Check()
        {
            var row = rows[index++];
            var samples = F(F(f.State)[2]); var issued = F(F(f.State)[3]); var assessed = F(F(f.State)[4]);
            var actualSamples = samples.Select(receipt => F(F(F(receipt)[2])[0])).Select(o =>
                (Assert.IsType<CarrierLocation>(o[1]).Ticks, Assert.IsType<CarrierLocation>(o[2]).Ticks)).ToArray();
            var expectedSamples = row.GetProperty("samples").EnumerateArray().Select(p => (p[0].GetInt64(), p[1].GetInt64())).ToArray();
            Assert.Equal(expectedSamples, actualSamples);
            Assert.Equal(row.GetProperty("issued").GetInt32(), issued.Length);
            Assert.Equal(row.GetProperty("assessed").GetInt32(), assessed.Length);
            if (issued.Length == 0)
            {
                Assert.Equal(JsonValueKind.Null, row.GetProperty("target").ValueKind);
                Assert.Equal(JsonValueKind.Null, row.GetProperty("prediction").ValueKind);
            }
            else
            {
                var forecast = F(F(issued[^1])[2]);
                Assert.Equal(row.GetProperty("target").GetInt64(), Assert.IsType<CarrierLocation>(forecast[2]).Ticks);
                Number(forecast[4], f.Quantity, row.GetProperty("prediction").GetInt64());
            }
            var matching = assessed.LastOrDefault(a => ReferenceEquals(F(a)[0], issued.LastOrDefault()));
            if (matching is null) Assert.Equal(JsonValueKind.Null, row.GetProperty("mismatch").ValueKind);
            else Number(F(matching)[2], f.Quantity, row.GetProperty("mismatch").GetInt64());
        }
        var a = f.Record(0, 3, 100); Check();
        var b = f.Record(1, 7, 101); var fit = f.Fit(a, b); Check();
        var first = f.Issue(f.Candidate(fit), 2, 102); Check();
        var c = f.Record(2, 11, 103); Check();
        f.Assess(first, c, 104); Check();
        var second = f.Issue(f.Candidate(f.Fit(b, c)), 3, 105); Check();
        var d = f.Record(3, 14, 106); Check();
        f.Assess(second, d, 107); Check();
        f.Issue(f.Candidate(f.Fit(c, d)), 4, 108); Check();
        Assert.Equal(rows.Length, index);
    }

    private sealed class Fixture
    {
        public IElement[] Roots { get; }
        public Relation State { get; set; }
        public IElement[] Stream => F(F(State)[0]);
        public Carrier SourceClock => Assert.IsType<Carrier>(F(Stream[0])[1]);
        public Carrier LocalClock => Assert.IsType<Carrier>(Stream[3]);
        public Carrier Quantity => Assert.IsType<CarrierLocation>(R(R(F(Stream[0])[0]).Left).Left).Carrier;
        public Fixture(int restores = 0) : this(GojoSyntax.Parse(Text()).Roots.ToArray())
        {
            for (var i = 0; i < restores; i++) Roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Roots));
            State = R(Roots[0]);
        }
        public Fixture(IElement[] roots) { Roots = roots; State = R(roots[0]); }
        public static string Text() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", "prospective-calibration.gojo"));
        public Relation Bind(int pattern, params (int Slot, IElement Value)[] bindings) =>
            R(StructuralPattern.Refine(F(Roots[2])[pattern], bindings.Select(b =>
                new Relation(F(Roots[3])[b.Slot], b.Value)).ToArray()));
        public Relation RecordRequest(long time, long value, long local) =>
            RecordRequest(time, new CarrierLocation(Quantity, value), local);
        public Relation RecordRequest(long time, IElement value, long local) => Bind(1,
            (0, State), (3, Bind(0, (1, new CarrierLocation(SourceClock, time)), (2, value))),
            (4, new CarrierLocation(LocalClock, local)));
        public Relation Record(long time, long value, long local) => Record(time, new CarrierLocation(Quantity, value), local);
        public Relation Record(long time, IElement value, long local)
        {
            Assert.True(LocalCalibration.TryRecordTemporalSample(RecordRequest(time, value, local), out var receipt, out var state));
            State = state; return receipt;
        }
        public Relation FitRequest(IElement a, IElement b, int encoding = 0) =>
            Bind(2, (0, State), (5, a), (6, b), (7, F(Roots[1])[encoding]));
        public Relation Fit(IElement a, IElement b, int encoding = 0)
        {
            Assert.True(LocalCalibration.TryFitTemporalFrame(FitRequest(a, b, encoding), out var fit)); return fit;
        }
        public Relation Candidate(IElement fit, int readout = 0) => Bind(3, (8, fit), (9, F(Roots[1])[readout]));
        public Relation IssueRequest(IElement candidate, long target, long local) =>
            Bind(4, (0, State), (10, candidate), (11, new CarrierLocation(SourceClock, target)),
                (4, new CarrierLocation(LocalClock, local)));
        public Relation Issue(IElement candidate, long target, long local)
        {
            Assert.True(LocalCalibration.TryIssueForecast(IssueRequest(candidate, target, local), out var issue, out var state));
            State = state; return issue;
        }
        public Relation AssessRequest(IElement issue, IElement receipt, long local) =>
            Bind(5, (0, State), (12, issue), (13, receipt), (4, new CarrierLocation(LocalClock, local)));
        public Relation Assess(IElement issue, IElement receipt, long local)
        {
            Assert.True(LocalCalibration.TryAssessForecast(AssessRequest(issue, receipt, local), out var assessment, out var state));
            State = state; return assessment;
        }
    }

    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var fields)); return fields; }
    private static Relation Replace(IElement e, int index, IElement replacement)
    { var fields = F(e); fields[index] = replacement; return G(fields); }
    private static void Number(IElement e, Carrier basis, long n, long d = 1)
    {
        Assert.True(ProjectionResidue.TryRead(e, out var actual, out var numerator, out var denominator));
        Assert.Same(basis, actual); Assert.Equal((BigInteger)n * denominator, (BigInteger)numerator * d);
    }
}
