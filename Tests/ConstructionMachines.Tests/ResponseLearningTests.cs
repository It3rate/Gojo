using Gojo.Core;
using Gojo.Encoding;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class ResponseLearningTests
{
    private static readonly Lazy<LocalResponseData.Sets> Data = new(() => LocalResponseData.Generate(true));
    private static readonly Lazy<ResponseTraining.Fit> Table = new(() => ResponseTraining.Table(Data.Value));
    private static readonly Lazy<ResponseTraining.Fit> Linear = new(() => ResponseTraining.Linear(Data.Value));

    [Fact]
    public void OneReaderExecutesConstantCellsAffineLayersAndComposedPositiveLayers()
    {
        var patch = Data.Value.Test[0]; var constant = Constant(24);
        var prediction = constant.Predict(Context(constant, patch));
        Assert.Equal(3, LocalResponseProgram.Choice(constant.Select(prediction)));
        Assert.Equal(6, F(F(prediction)[2]).Length);
        var neural = ResponseTraining.Network([
            [new[] { .1, .2, .1, 0, 0, 0, 0 }, new[] { .2, 0, .1, 0, 0, 0, 0 }],
            Enumerable.Range(0, 6).Select(i => new[] { i * .05, .2, -.1 }).ToArray()], G(), 1);
        var read = neural.Predict(Context(neural, patch));
        Assert.Equal(8, F(F(read)[4]).Length == 3 ? F(F(F(read)[4])[2]).Length : 0);
        Assert.True(LocalResponseProgram.Choice(neural.Select(read)) >= 0);
        Assert.Equal(6, F(F(F(constant.Cells[0])[1])[0]).Length);
        Assert.Equal(2, F(F(neural.Cells[0])[1]).Length);
    }

    [Fact]
    public void ContextIsInvariantToReflectionAndRequestOrder()
    {
        var a = LocalResponseData.Create(0, "test", [3, 7, 8, -80]);
        var b = LocalResponseData.Create(1, "test", [80, -8, -3, -7]);
        Assert.Equal(a.X, b.X);
        Assert.Equal(ResponseLearningExperiment.Predict(Table.Value.Program, a), ResponseLearningExperiment.Predict(Table.Value.Program, b));
        Assert.Equal(a.Rewards, b.Rewards); Assert.Equal(a.Moves.Select(x => -x), b.Moves);
    }

    [Fact]
    public void DirectionalContextDistinguishesNetOpposingTheMedian()
    {
        var against = LocalResponseData.Create(0, "test", [3, 7, 8, -80]);
        var with = LocalResponseData.Create(1, "test", [-3, 7, 8, 80]);
        Assert.True(against.X[1] < .5); Assert.True(with.X[1] > .5);
    }

    [Fact]
    public void CounterfactualLabelsUseActualBoundedReceiverMovement()
    {
        var patch = LocalResponseData.Create(0, "test", [3, 7, 8, -80]);
        for (var i = 0; i < LocalResponseProgram.Caps.Length; i++)
        {
            var cap = LocalResponseProgram.Caps[i]; var sum = patch.Requests.Sum(x => Math.Clamp(x, -cap, cap));
            var move = (long)Math.Clamp(sum / 4d, -8, 8);
            var relief = (patch.Requests.Sum(x => Math.Abs(x)) - patch.Requests.Sum(x => Math.Abs(x - move))) / (double)patch.Requests.Sum(x => Math.Abs(x));
            Assert.Equal(move, patch.Moves[i]); Assert.Equal(Math.Truncate(relief * LocalResponseProgram.OutputScale) / LocalResponseProgram.OutputScale, patch.Rewards[i], 12);
            var branch = F(F(patch.Surface)[4])[i]; var reward = F(F(branch)[2])[2];
            Assert.True(ProjectionResidue.TryRead(F(reward)[3], out _, out _, out _));
            var remainder = CoupledSettlement.Number(F(reward)[3]);
            var before = patch.Requests.Sum(x => Math.Abs(x)); var gain = before - patch.Requests.Sum(x => Math.Abs(x - move));
            Assert.Equal(gain, patch.Rewards[i] * before + remainder, 10);
        }
        Assert.True(patch.Rewards.Max() > patch.Rewards.Min());
    }

    [Fact]
    public void WholeProblemsAndTheirReflectionsDoNotCrossSplits()
    {
        var data = Data.Value; var all = data.Train.Concat(data.Validation).Concat(data.Test).Concat(data.Stress).ToArray();
        Assert.Equal(all.Length, all.Select(p => LocalResponseData.Key(p.Requests)).Distinct().Count());
        Assert.Equal(all.Length, all.Select(p => p.Id).Distinct().Count());
        Assert.All(data.Train, p => Assert.Equal("training", p.Split)); Assert.All(data.Test, p => Assert.Equal("test", p.Split));
    }

    [Fact]
    public void ContextCellsAreDisjointAndHaveRetainedTrainingSupport()
    {
        var program = Table.Value.Program;
        Assert.Equal(16, program.Cells.Length);
        Assert.Equal(Data.Value.Train.Length, program.Cells.Sum(c => (long)((CarrierLocation)F(c)[2]).Ticks));
        foreach (var patch in Data.Value.Test.Take(20))
            Assert.IsType<Relation>(F(program.Predict(Context(program, patch)))[2]);
    }

    [Fact]
    public void RegressionAndNeuralFittingReducePredictionErrorFromZero()
    {
        var data = Data.Value; var zero = data.Train.Average(p => p.Rewards.Select(x => x * x).Average());
        Assert.True(Linear.Value.TrainingMse < zero);
        var neural = ResponseTraining.Neural(data, 29, 80);
        Assert.True(neural.TrainingMse < zero); Assert.InRange(neural.BestEpoch, 10, 80);
        Assert.Equal(110, neural.Parameters); Assert.Equal(42, Linear.Value.Parameters);
    }

    [Fact]
    public void TestLabelsDoNotParticipateInFitting()
    {
        var data = Data.Value; var altered = data with { Test = data.Test.Select(p => p with { Rewards = p.Rewards.Select(x => -x).ToArray() }).ToArray() };
        var linear = ResponseTraining.Linear(altered); var table = ResponseTraining.Table(altered);
        Assert.Equal(ResponseLearningExperiment.Predict(Linear.Value.Program, data.Validation[0]), ResponseLearningExperiment.Predict(linear.Program, data.Validation[0]));
        Assert.Equal(ResponseLearningExperiment.Predict(Table.Value.Program, data.Validation[0]), ResponseLearningExperiment.Predict(table.Program, data.Validation[0]));
    }

    [Fact]
    public void PredictionQueriesDoNotChangeTheProgramOrSource()
    {
        var program = Linear.Value.Program; var context = Context(program, Data.Value.Test[0]);
        var packet = G(program.Surface, context); var before = GojoNumericFormat.Encode(packet);
        for (var i = 0; i < 4; i++) program.Select(program.Predict(context));
        Assert.Equal(before, GojoNumericFormat.Encode(packet));
    }

    [Fact]
    public void EqualValuesInForeignInputBasesRemainUnresolved()
    {
        var program = Linear.Value.Program; var context = Context(program, Data.Value.Test[0]); var values = F(F(context)[2]);
        values[0] = new CarrierLocation(new Carrier(), ((CarrierLocation)values[0]).Ticks);
        var foreign = ReadWitness.Create(new Carrier(), F(context)[0], F(context)[1], G(values), G(), F(context)[4]);
        var read = program.Predict(foreign); Assert.IsType<Placeholder>(F(read)[2]); Assert.Equal(-1, LocalResponseProgram.Choice(program.Select(read)));
    }

    [Fact]
    public void OverlappingDomainsPreserveCompetingCellsInsteadOfSelectingFirst()
    {
        var program = Constant(12); var f = F(program.Surface); var cell = program.Cells[0];
        var ambiguous = new LocalResponseProgram(G(f[0], f[1], G(cell, cell), f[3], f[4]));
        Assert.IsType<Placeholder>(F(ambiguous.Predict(Context(ambiguous, Data.Value.Test[0])))[2]);
    }

    [Fact]
    public void HiddenClippingAndFiniteRemaindersStayInPredictionWitnesses()
    {
        var weights = new[] { new[] { new[] { 20d, 0d, 0d, 0d, 0d, 0d, 0d } }, Enumerable.Range(0, 6).Select(_ => new[] { 0d, .01 }).ToArray() };
        var program = ResponseTraining.Network(weights, G(), 1); var read = program.Predict(Context(program, Data.Value.Test[0]));
        var nodes = F(F(F(read)[4])[2]); var residue = F(F(nodes[0])[3]);
        Assert.True(CoupledSettlement.Number(residue[0]) > 0); Assert.IsType<Relation>(F(read)[2]);
    }

    [Fact]
    public void SavedProgramsKeepTheirWithinProgramAliasesAndPredictions()
    {
        var program = Linear.Value.Program;
        var copy = new LocalResponseProgram((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(program.Surface))[0]);
        Assert.False(ReferenceEquals(program.Inputs[0], copy.Inputs[0]));
        Assert.Equal(ResponseLearningExperiment.Predict(program, Data.Value.Test[2]), ResponseLearningExperiment.Predict(copy, Data.Value.Test[2]));
        var node = F(F(F(copy.Cells[0])[1])[0])[0]; var map = (Relation)F(F(node)[1])[0];
        TimeStepSequence.TryReadSpan((Relation)map.Left, out var from, out _); Assert.Same(copy.Inputs[0], from.Carrier);
    }

    [Fact]
    public void LearnedTwelveTickProgramReproducesTheFixedLaw()
    {
        var fixedWorld = EnsembleRecipes.Create(method: 4).Model;
        var learned = EnsembleRecipes.Create(method: 4, learner: Constant(12)).Model;
        for (var i = 0; i < 8; i++)
        {
            fixedWorld.Step(); learned.Step(); Assert.Equal(Coordinates(fixedWorld), Coordinates(learned));
            Assert.Equal(CoupledSettlement.Number(EnsembleSettlement.Cost(fixedWorld.LastRead)), CoupledSettlement.Number(EnsembleSettlement.Cost(learned.LastRead)));
        }
    }

    [Fact]
    public void LearnedWorldRestoresAndContinuesAcrossStructuralReview()
    {
        var world = EnsembleRecipes.Create(method: 4, learner: Linear.Value.Program).Model; world.Step(); world.Step();
        var restored = EnsembleSettlement.Restore((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(world.Surface))[0]);
        var passages = F(F(restored.Policy)[6]); var program = F(F(passages[0])[3])[0];
        Assert.All(passages, passage => Assert.Same(program, F(F(passage)[3])[0]));
        for (var i = 0; i < 8; i++) { world.Step(); restored.Step(); Assert.Equal(Coordinates(world), Coordinates(restored)); }
        Assert.Equal(0, CoupledSettlement.OpenPaths(restored.Joint));
    }

    [Fact]
    public void UnresolvedModelFallsBackWithItsUncertaintyRetained()
    {
        var program = Constant(24); var cell = F(program.Cells[0]); var domains = F(cell[0]);
        domains[0] = Relation.Segment(program.Inputs[0], 0, 0);
        var pf = F(program.Surface); var limited = new LocalResponseProgram(G(pf[0], pf[1], G(G(G(domains), cell[1], cell[2], cell[3])), pf[3], pf[4]));
        var world = EnsembleRecipes.Create(method: 4, learner: limited).Model;
        var step = world.Step(); var reads = LearnedReads(step);
        Assert.NotEmpty(reads); Assert.Contains(reads, r => F(r)[2] is Placeholder);
        Assert.Equal(0, CoupledSettlement.OpenPaths(world.Joint));
    }

    [Fact]
    public void TheLearnedContextContainsNoPrivateReceiverActuators()
    {
        var world = EnsembleRecipes.Create(method: 4, learner: Linear.Value.Program).Model; var step = world.Step();
        foreach (var read in LearnedReads(step)) foreach (var machine in world.Machines) foreach (var route in machine.Responses)
        {
            TimeStepSequence.TryReadSpan((Relation)((Relation)F(route)[1]).Right, out var actuator, out _);
            Assert.DoesNotContain(actuator.Carrier, read.Carriers());
        }
    }

    [Fact]
    public void LearningDoesNotTurnUnknownPermissionsIntoResolvedEvidence()
    {
        var world = EnsembleRecipes.Create(method: 4, condition: "unknown", learner: Linear.Value.Program).Model;
        for (var i = 0; i < 6; i++) Assert.False(world.Step().Rewritten);
        Assert.Equal(1, EnsembleSettlement.Unknown(world.LastRead));
    }

    [Fact]
    public void ACountQualifiedReadReusesTheFunctionAndKeepsOutOfCoverageQueriesOpen()
    {
        var program = Constant(24); var covered = ResponseCoverageExperiment.RestrictCount(program, 3, 8, G());
        Assert.Same(F(program.Cells[0])[1], F(covered.Cells[0])[1]);
        var pair = LocalResponseData.Create(0, "probe", [-20, 100]);
        Assert.IsType<Relation>(F(program.Predict(Context(program, pair)))[2]);
        Assert.IsType<Placeholder>(F(covered.Predict(Context(covered, pair)))[2]);
        Assert.IsType<Relation>(F(covered.Predict(Context(covered, Data.Value.Test[0])))[2]);
    }

    [Fact]
    public void RecordedPortDecisionsComeFromTheirNativeQueries()
    {
        var scene = EnsembleRecipes.Create(method: 4, learner: Linear.Value.Program); var step = scene.Model.Step();
        var frame = EnsembleExperiment.ObserveFrame(scene, 1, 1, "Local responses", step.AfterMotion, step);
        var reads = LearnedReads(step).Distinct().ToArray(); Assert.Equal(reads.Length, frame.decisions!.Length);
        Assert.All(frame.decisions, decision => { Assert.Equal(6, decision.features.Length); Assert.Equal(6, decision.scores!.Length); Assert.NotNull(decision.cap); });
        Assert.Equal(CoupledSettlement.Number(EnsembleSettlement.Cost(step.AfterMotion)), frame.cost);
    }

    [Fact]
    public void TheFiniteContextScopeRefusesAnUnrepresentableScaledRequest()
    {
        var patch = Data.Value.Test[0]; var requests = F(F(patch.Surface)[1]).Cast<Relation>().ToArray(); var fields = F(requests[0]);
        ProjectionResidue.TryRead(fields[3], out var axis, out _, out _);
        requests[0] = G(fields[0], fields[1], fields[2], new CarrierLocation(axis, long.MaxValue - 128), fields[4]);
        var read = RequestContext.Read(requests, axis, Linear.Value.Program.Inputs);
        Assert.IsType<Placeholder>(F(read)[2]); Assert.NotEmpty(F(F(read)[3]));
    }

    private static LocalResponseProgram Constant(long cap)
    {
        var rows = Enumerable.Range(0, 6).Select(i => new[] { LocalResponseProgram.Caps[i] == cap ? .25 : 0, 0d, 0d, 0d, 0d, 0d, 0d }).ToArray();
        return ResponseTraining.Network([rows], G(), 1);
    }
    private static Relation Context(LocalResponseProgram p, LocalResponseData.Patch patch) => RequestContext.Rebind((Relation)F(patch.Surface)[2], p.Inputs);
    private static double[] Coordinates(EnsembleSettlement m) => m.Machines.SelectMany(c => c.Points.SelectMany(Program.XY)).ToArray();
    private static Relation[] LearnedReads(EnsembleSettlement.Transition step) => F(F(step.Negotiation)[4]).Select(entry => F(F(entry)[0])[4])
        .Where(p => F(p).Length == 3).Select(p => (Relation)F(p)[2]).Where(p => F(p).Length == 5).ToArray();
}
