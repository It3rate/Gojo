using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning.Tests;

public sealed class OrdinalPredictorTests
{
    [Fact]
    public void ForecastRequiresRepeatedEvidenceAndKeepsMinorityAlternatives()
    {
        var direction = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(direction, cue);
        Assert.False(OrdinalPredictor.TryReadPrediction(predictor.Predict(P(cue, 1)), out _));
        for (var i = 0; i < 3; i++) { predictor.Observe(predictor.Predict(P(cue, 1)), P(direction, 1)); Assert.False(OrdinalPredictor.TryReadPrediction(predictor.Predict(P(cue, 1)), out _)); }
        predictor.Observe(predictor.Predict(P(cue, 1)), P(direction, 1));
        Assert.Equal(1, Value(predictor.Predict(P(cue, 1))));
        predictor.Observe(predictor.Predict(P(cue, 1)), P(direction, -1));
        var forecast = predictor.Predict(P(cue, 1)); Assert.Equal(1, Value(forecast));
        Assert.Equal(new long[] { -1, 1 }, Alternatives(forecast));
        Assert.True(ReadWitness.TryRead(forecast, out _, out var nativeContract, out _, out _, out _));
        Assert.True(((Relation)nativeContract).TryReadOrderedGroup(out var parts)); Assert.IsType<Relation>(parts[1]);
    }

    [Fact]
    public void BalancedNoisyContextAbstainsWhilePredictableNoisyContextCanForecast()
    {
        var direction = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(direction, cue);
        for (var i = 0; i < 80; i++) predictor.Observe(predictor.Predict(P(cue, 0)), P(direction, i % 2 == 0 ? -1 : 1));
        Assert.False(OrdinalPredictor.TryReadPrediction(predictor.Predict(P(cue, 0)), out _));
        Assert.Equal(new long[] { -1, 1 }, Alternatives(predictor.Predict(P(cue, 0))));
        for (var i = 0; i < 80; i++) predictor.Observe(predictor.Predict(P(cue, 1)), P(direction, i % 20 == 0 ? -1 : 1));
        Assert.Equal(1, Value(predictor.Predict(P(cue, 1))));
        Assert.Equal(new long[] { -1, 1 }, Alternatives(predictor.Predict(P(cue, 1))));
    }

    [Fact]
    public void DistinctConcreteContextPortsPreserveCueAndDirectionMeaning()
    {
        var direction = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(direction, cue, direction);
        for (var repetition = 0; repetition < 6; repetition++)
            foreach (var context in new[] { 0, 1 }) foreach (var prior in new[] { -1, 1 })
                predictor.Observe(predictor.Predict(P(cue, context), P(direction, prior)), P(direction, context == 1 ? prior : -prior));
        Assert.Equal(1, Value(predictor.Predict(P(cue, 1), P(direction, 1))));
        Assert.Equal(-1, Value(predictor.Predict(P(cue, 0), P(direction, 1))));
        Assert.False(OrdinalPredictor.TryReadPrediction(predictor.Predict(P(direction, 1), P(cue, 1)), out _));
        Assert.Equal(4, predictor.RowCount);
    }

    [Fact]
    public void TwoEventContextResolvesASequenceThatPreviousOnlyCannot()
    {
        var axis = new Carrier(); var previous = new OrdinalPredictor(axis, 1); var context = new OrdinalPredictor(axis, 2);
        var sequence = new[] { -1, -1, 1, 1 };
        for (var i = 0; i < 80; i++) foreach (var model in new[] { previous, context }) model.Observe(model.Predict(), P(axis, sequence[i % 4]));
        var settledPrevious = 0; var correctContext = 0;
        for (var i = 80; i < 112; i++)
        {
            var a = previous.Predict(); var b = context.Predict(); var actual = sequence[i % 4];
            if (OrdinalPredictor.TryReadPrediction(a, out _)) settledPrevious++;
            if (OrdinalPredictor.TryReadPrediction(b, out var result) && result.Ticks == actual) correctContext++;
            previous.Observe(a, P(axis, actual), false); context.Observe(b, P(axis, actual), false);
        }
        Assert.Equal(0, settledPrevious); Assert.Equal(32, correctContext);
    }

    [Fact]
    public void FrozenFeedbackPreservesCountsAndOldForecastButRetainsMismatch()
    {
        var axis = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(axis, cue);
        for (var i = 0; i < 5; i++) predictor.Observe(predictor.Predict(P(cue, 1)), P(axis, 1));
        var before = Counts(predictor); var old = predictor.Predict(P(cue, 1));
        var feedback = predictor.Observe(old, P(axis, -1), false);
        Assert.Equal(before, Counts(predictor)); Assert.Equal(5, predictor.Updates); Assert.Equal(1, predictor.Mismatches); Assert.Equal(1, Value(old));
        Assert.True(ReadWitness.TryRead(feedback, out _, out _, out _, out var residue, out _)); ((Relation)residue).TryReadOrderedGroup(out var fields);
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)fields[0], out var violation)); Assert.Equal(2, violation.Ticks);
    }

    [Fact]
    public void ForeignAndUnavailablePortsCannotBeCreditedAsExactEvidence()
    {
        var axis = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(axis, cue);
        var open = ReadWitness.Create(new Carrier(), cue, cue, new Placeholder(), new Placeholder(), cue);
        predictor.Observe(predictor.Predict(open), P(axis, 1));
        predictor.Observe(predictor.Predict(P(cue, 0)), P(new Carrier(), 1));
        var capacity = FiniteReadSurface.Capacity(new Carrier(), cue, 0, 4, 2);
        var coarse = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(cue, 1), new Placeholder());
        Assert.False(OrdinalPredictor.TryReadPrediction(predictor.Predict(coarse), out _));
        predictor.Observe(predictor.Predict(coarse), P(axis, 1));
        Assert.Equal(0, predictor.Updates); Assert.Equal(0, predictor.RowCount);
    }

    [Fact]
    public void RepeatedTrainingClipsByDeclaredAgingAndDoesNotRetainTheFullHistory()
    {
        var axis = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(axis, cue);
        for (var i = 0; i < 300; i++) predictor.Observe(predictor.Predict(P(cue, 1)), P(axis, 1));
        var early = NativeInspection.Count(predictor.Root);
        for (var i = 0; i < 300; i++) predictor.Observe(predictor.Predict(P(cue, 1)), P(axis, 1));
        var late = NativeInspection.Count(predictor.Root);
        Assert.All(Counts(predictor), count => Assert.InRange(count, 0, 255)); Assert.Equal(1, Value(predictor.Predict(P(cue, 1))));
        Assert.InRange(late.Relations, 1, early.Relations + 20); Assert.InRange(late.Carriers, 1, early.Carriers + 20);
    }

    [Fact]
    public void RestoredNativeRowsRediscoverTheirOwnAxesAndKeepForecasts()
    {
        var axis = new Carrier(); var cue = new Carrier(); var predictor = new OrdinalPredictor(axis, cue, axis);
        for (var i = 0; i < 8; i++) predictor.Observe(predictor.Predict(P(cue, 1), P(axis, -1)), P(axis, 1));
        var restored = OrdinalPredictor.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(predictor.Root)))));
        Assert.True(OrdinalPredictor.TryReadModel(restored.Root, out var policy, out _, out _)); policy.TryReadOrderedGroup(out var contract);
        var output = Axis((Relation)contract[0]); ((Relation)contract[1]).TryReadOrderedGroup(out var ports);
        var restoredCue = Axis((Relation)((Relation)ports[0]).Right);
        var restoredDirection = Axis((Relation)((Relation)ports[1]).Right);
        Assert.True(output.SameIdentityAs(restoredDirection)); Assert.False(output.SameIdentityAs(restoredCue));
        Assert.Equal(1, Value(restored.Predict(P(restoredCue, 1), P(restoredDirection, -1))));
        Assert.Equal(Counts(predictor), Counts(restored));
    }

    private static Relation P(Carrier axis, long value) => ReadWitness.Create(new Carrier(), new Placeholder(), Relation.Segment(axis, -16, 16), new CarrierLocation(axis, value), new Placeholder(), new Placeholder());
    private static Carrier Axis(Relation frame) => ((CarrierLocation)((Relation)frame.Left).Left).Carrier;
    private static long Value(Relation forecast) { Assert.True(OrdinalPredictor.TryReadPrediction(forecast, out var point)); return point.Ticks; }
    private static long[] Alternatives(Relation forecast) { Assert.True(OrdinalPredictor.TryReadAlternatives(forecast, out var points)); return points.Select(p => p.Ticks).ToArray(); }
    private static long[] Counts(OrdinalPredictor predictor)
    {
        OrdinalPredictor.TryReadModel(predictor.Root, out _, out var group, out _); group.TryReadOrderedGroup(out var rows);
        return rows.SelectMany(row => { OrdinalPredictor.TryReadRow((Relation)row, out _, out var counts); return counts.Select(c => (long)c.Value); }).ToArray();
    }
}
