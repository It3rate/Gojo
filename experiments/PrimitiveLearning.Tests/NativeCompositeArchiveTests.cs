using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace PrimitiveLearning.Tests;

public sealed class NativeCompositeArchiveTests
{
    [Fact]
    public void FourLearnedModelsRetainStateAliasesAndFrozenBehaviorAfterNativeTransport()
    {
        var trained = Train();
        var original = G(Ports(trained.World), trained.Adaptive.Root, trained.Predictor.Root,
            trained.Query.Root, trained.Program.Root);
        var decoded = GojoNumericFormat.Decode(GojoNumericFormat.Encode(original));
        var restored = Assert.IsType<Relation>(Assert.Single(decoded));
        Assert.Equal(NativeInspection.Signature(original), NativeInspection.Signature(restored));
        Assert.Equal(NativeInspection.Count(original), NativeInspection.Count(restored));
        var fields = Parts(restored, 5);
        var axes = Parts(Assert.IsType<Relation>(fields[0]), 10).Cast<Carrier>().ToArray();
        var adaptive = AdaptiveOrdinal.FromRoot(Assert.IsType<Relation>(fields[1]));
        var predictor = OrdinalPredictor.FromRoot(Assert.IsType<Relation>(fields[2]));
        var query = QueryLearner.FromRoot(Assert.IsType<Relation>(fields[3]));
        var program = ProgramLearner.FromRoot(Assert.IsType<Relation>(fields[4]));

        Assert.NotSame(trained.World.LoadSource, axes[0]);
        Assert.False(trained.World.LoadSource.SameIdentityAs(axes[0]));
        Assert.Equal(trained.Adaptive.LandmarkCount, adaptive.LandmarkCount);
        Assert.Equal(trained.Adaptive.Updates, adaptive.Updates);
        Assert.Equal(trained.Predictor.RowCount, predictor.RowCount);
        Assert.Equal(trained.Predictor.Updates, predictor.Updates);
        Assert.Equal(trained.Predictor.Mismatches, predictor.Mismatches);
        Assert.Equal(trained.Program.AcceptedEdits, program.AcceptedEdits);
        Assert.Equal(trained.Program.SelectedComplexity, program.SelectedComplexity);
        Assert.True(program.AcceptedEdits > 0);
        AssertCounts(trained.Query, query, trained.Program, program);

        Assert.Same(axes[4], query.ContextAxis);
        Assert.Same(axes[9], query.BenefitAxis);
        Assert.Same(axes[8], query.CostAxis);
        Assert.Same(axes[3], program.ModeAxis);
        Assert.Same(axes[5], program.DirectionAxis);
        Assert.Same(axes[6], program.ActionAxis);
        Assert.Same(axes[7], program.UtilityAxis);
        Assert.True(AdaptiveOrdinal.TryReadModel(adaptive.Root, out var adaptivePolicy, out var restoredLandmarks, out _));
        var policy = Parts(adaptivePolicy, 8);
        Assert.Same(axes[0], policy[0]);
        Assert.Same(axes[1], FrameAxis(policy[1]));
        Assert.Same(axes[2], FrameAxis(policy[2]));
        Assert.Same(program.ModeAxis, FrameAxis(policy[3]));
        Assert.True(OrdinalPredictor.TryReadModel(predictor.Root, out var predictorPolicy, out _, out _));
        var predictorPorts = Parts(Assert.IsType<Relation>(Parts(predictorPolicy, 7)[1]), 2);
        Assert.Same(query.ContextAxis, FrameAxis(Assert.IsType<Relation>(predictorPorts[0]).Right));
        Assert.Same(program.DirectionAxis, FrameAxis(Assert.IsType<Relation>(predictorPorts[1]).Right));

        // Restore the external instrument through explicit archive-root
        // correspondences. Private readings remain in this test instrument;
        // neither native model contains a recovered metric coordinate.
        Assert.True(AdaptiveOrdinal.TryReadModel(trained.Adaptive.Root, out _, out var originalLandmarks, out _));
        var originalRows = Parts(originalLandmarks); var restoredRows = Parts(restoredLandmarks);
        Assert.Equal(originalRows.Length, restoredRows.Length);
        var readings = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < originalRows.Length; index++)
        {
            Assert.True(AdaptiveOrdinal.TryReadLandmark(Assert.IsType<Relation>(originalRows[index]), out var oldSample, out _, out _));
            Assert.True(AdaptiveOrdinal.TryReadLandmark(Assert.IsType<Relation>(restoredRows[index]), out var newSample, out _, out _));
            readings.Add(newSample, trained.PrivateReadings[oldSample]);
            Assert.True(SamplingSurface.TryReadSample(newSample, out _, out var magnitude));
            Assert.IsType<Placeholder>(magnitude);
        }
        Relation Compare(Relation current, Relation anchor) => SamplingSurface.Comparison(new Carrier(), current, anchor,
            axes[2], readings[current].CompareTo(readings[anchor]));
        foreach (var (load, offset) in new[] { (0, 0), (4, 7), (7, 7), (8, 7), (12, 3), (15, 15) })
        {
            var episode = new RelayEpisode(1000 + load, load, load < 8, 0, 1, 1, false, false, FineOffset: offset);
            var oldSample = trained.World.Acquire(episode);
            var newSample = SamplingSurface.Sample(new Carrier(), axes[0], axes[1], true);
            readings.Add(newSample, load * 16 + offset);
            var a = trained.Adaptive.Predict(oldSample, trained.World.Compare);
            var b = adaptive.Predict(newSample, Compare);
            Assert.Equal(AdaptiveValue(a), AdaptiveValue(b));
            Assert.Equal(Alternatives(a), Alternatives(b));
        }
        foreach (var context in new[] { 0, 1 })
        foreach (var previous in new[] { -1, 1 })
        {
            var a = trained.Predictor.Predict(NativePublication.Point(trained.World.ContextAxis, context),
                NativePublication.Point(trained.World.DirectionAxis, previous));
            var b = predictor.Predict(NativePublication.Point(axes[4], context), NativePublication.Point(axes[5], previous));
            Assert.Equal(PredictedValue(a), PredictedValue(b));
            Assert.NotNull(PredictedValue(b));
        }
        foreach (var context in new[] { 0, 1 })
        {
            var a = trained.Query.Decide(NativePublication.Point(trained.World.ContextAxis, context), learn: false);
            var b = query.Decide(NativePublication.Point(axes[4], context), learn: false);
            Assert.Equal(NativePublication.Read(a), NativePublication.Read(b));
            Assert.Equal((long)context, NativePublication.Read(b));
        }
        foreach (var fits in new[] { 0, 1 })
        foreach (var direction in new[] { -1, 1 })
        {
            var a = trained.Program.Act(NativePublication.Point(trained.World.FitAxis, fits),
                NativePublication.Point(trained.World.DirectionAxis, direction), learning: false);
            var b = program.Act(NativePublication.Point(axes[3], fits), NativePublication.Point(axes[5], direction), learning: false);
            Assert.Equal(NativePublication.Read(a), NativePublication.Read(b));
            Assert.Equal((long)(fits == 1 ? direction : -direction), NativePublication.Read(b));
        }
        AssertCounts(trained.Query, query, trained.Program, program);
        Assert.Equal(trained.Adaptive.Updates, adaptive.Updates);
        Assert.Equal(trained.Predictor.Updates, predictor.Updates);

        // A resumed restored learner changes its own graph, not the archived
        // original. Shared axes remain shared within each independent world.
        var originalCounts = Enumerable.Range(0, 2).SelectMany(context => Enumerable.Range(0, 2)
            .Select(choice => trained.Query.ObservationCount(context, choice))).ToArray();
        var cue = NativePublication.Point(axes[4], 0);
        query.Decide(cue, learn: true);
        query.Observe(cue, query.BindBenefit(NativePublication.Point(axes[9], 4)),
            query.BindCost(NativePublication.Point(axes[8], 0)), learn: true);
        Assert.Equal(originalCounts, Enumerable.Range(0, 2).SelectMany(context => Enumerable.Range(0, 2)
            .Select(choice => trained.Query.ObservationCount(context, choice))).ToArray());
        Assert.Equal(originalCounts.Sum() + 1, Enumerable.Range(0, 2).Sum(context => Enumerable.Range(0, 2)
            .Sum(choice => query.ObservationCount(context, choice))));
    }

    private static Trained Train()
    {
        var world = new RelayWorld(new RelayCondition("archive fixture"));
        var adaptive = new AdaptiveOrdinal(world.LoadSource, world.PresenceAxis, world.OrderAxis, world.FitAxis);
        var predictor = new OrdinalPredictor(world.DirectionAxis, world.ContextAxis, world.DirectionAxis);
        var query = new QueryLearner(world.ContextAxis, world.QueryBenefitAxis, world.CostAxis);
        var program = new ProgramLearner(world.FitAxis, world.DirectionAxis, world.ActionAxis, world.UtilityAxis);
        var readings = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        for (var step = 0; step < 64; step++)
        {
            var load = step % 16;
            var episode = new RelayEpisode(step, load, load < 8, 0, 1, 1, false, false);
            var sample = world.Acquire(episode); readings.Add(sample, load * 16);
            adaptive.Observe(sample, world.ProbeFit(episode, sample), world.Compare);
            var context = step % 2; var previous = step / 2 % 2 == 0 ? -1 : 1;
            var prediction = predictor.Predict(NativePublication.Point(world.ContextAxis, context),
                NativePublication.Point(world.DirectionAxis, previous));
            predictor.Observe(prediction, NativePublication.Point(world.DirectionAxis, context == 0 ? previous : -previous));
        }
        for (var step = 0; step < 128; step++)
        {
            var context = step % 2; var cue = NativePublication.Point(world.ContextAxis, context);
            var choice = NativePublication.Read(query.Decide(cue));
            Assert.NotNull(choice);
            var physicalBenefit = NativePublication.Point(world.QueryBenefitAxis, context == 0 || choice == 1 ? 4 : 0);
            var physicalCost = NativePublication.Point(world.CostAxis, choice == 1 ? 1 : 0);
            query.Observe(cue, query.BindBenefit(physicalBenefit), query.BindCost(physicalCost));
        }
        for (var step = 0; step < 512; step++)
        {
            var fits = step / 2 % 2 == 1; var direction = step % 2 == 0 ? -1 : 1;
            var command = program.Act(NativePublication.Point(world.FitAxis, fits ? 1 : 0),
                NativePublication.Point(world.DirectionAxis, direction), learning: true);
            var episode = new RelayEpisode(step, fits ? 5 : 12, fits, 0, 1, direction, false, false);
            program.Feedback(program.BindUtility(world.Execute(episode, command)));
        }
        Assert.True(adaptive.Updates > 0); Assert.True(predictor.Updates > 0);
        return new(world, adaptive, predictor, query, program, readings);
    }

    private static void AssertCounts(QueryLearner originalQuery, QueryLearner restoredQuery,
        ProgramLearner originalProgram, ProgramLearner restoredProgram)
    {
        for (var context = 0; context < 2; context++)
        for (var choice = 0; choice < 2; choice++)
            Assert.Equal(originalQuery.ObservationCount(context, choice), restoredQuery.ObservationCount(context, choice));
        for (var candidate = 0; candidate < originalProgram.CandidateCount; candidate++)
            Assert.Equal(originalProgram.ObservationCount(candidate), restoredProgram.ObservationCount(candidate));
    }
    private static long? AdaptiveValue(Relation read) => AdaptiveOrdinal.TryReadPrediction(read, out var result) ? result.Ticks : null;
    private static long? PredictedValue(Relation read) => OrdinalPredictor.TryReadPrediction(read, out var result) ? result.Ticks : null;
    private static long[] Alternatives(Relation read) => AdaptiveOrdinal.TryReadAlternatives(read, out var points)
        ? points.Select(point => point.Ticks).ToArray() : [];
    private static Carrier FrameAxis(IElement frame) => Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(Assert.IsType<Relation>(frame).Left).Left).Carrier;
    private static IElement[] Parts(Relation group, int? count = null)
    {
        Assert.True(group.TryReadOrderedGroup(out var fields));
        if (count is not null) Assert.Equal(count.Value, fields.Length);
        return fields;
    }
    private static Relation Ports(RelayWorld world) => G(world.LoadSource, world.PresenceAxis, world.OrderAxis,
        world.FitAxis, world.ContextAxis, world.DirectionAxis, world.ActionAxis, world.UtilityAxis, world.CostAxis, world.QueryBenefitAxis);
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private sealed record Trained(RelayWorld World, AdaptiveOrdinal Adaptive, OrdinalPredictor Predictor,
        QueryLearner Query, ProgramLearner Program, Dictionary<Relation, int> PrivateReadings);
}
