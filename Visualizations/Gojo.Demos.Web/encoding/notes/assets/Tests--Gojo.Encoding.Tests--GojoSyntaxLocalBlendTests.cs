using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Executes the authored experiment with existing local kernel reads. The test
/// driver chooses when to observe and perturb; it is not a learning engine.
/// </summary>
public sealed class GojoSyntaxLocalBlendTests
{
    [Fact]
    public void PartialComponentsAndPartialFiltersRemainConnectedWithoutInventingZero()
    {
        var f = new Fixture();
        Assert.False(StructuralPattern.TryApplyComplete(
            f.InputPattern, f.PartialBindings, out var partial, out var open));
        var input = Assert.IsType<Relation>(partial);
        Assert.Same(f.Light.Tick, Assert.IsType<CarrierLocation>(input.Left).Tick);
        Assert.Same(f.InputPattern.Right, Assert.Single(open));
        Assert.Same(open[0], input.Right);
        Assert.Same(open[0], f.Expectation.Left);

        Assert.False(StructuralPattern.IsComplete(f.Mixture));
        Assert.Same(f.EndpointSlot, Assert.Single(StructuralPattern.ReadSlots(f.Mixture)));
        Assert.True(StructuralPattern.TryApplyComplete(f.Mixture, f.InitialBindings,
            out var initialModel, out var unbound));
        Assert.Empty(unbound);
        Assert.Equal(8, Predict(Assert.IsType<Relation>(initialModel), f.Input(), f.Output).Ticks);
        Assert.False(f.Mixture.TryReadOperationFrom(f.Input(), f.Output, out _));
        Assert.False(f.Model(5).TryReadOperationFrom(partial, f.Output, out _));

        var zero = new CarrierLocation(f.Heat.Carrier, 0);
        var zeroInput = StructuralPattern.Apply(f.InputPattern,
            f.PartialBindings[0], new Relation(f.InputPattern.Right, zero));
        Assert.Equal(3, Predict(f.Model(5), zeroInput, f.Output).Ticks);
        Assert.Equal(8, Predict(f.Model(5), f.Input(), f.Output).Ticks);
        Assert.Same(f.Heat.Tick, Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(f.Input()).Right).Tick);
        Assert.Same(f.Light.Tick, Assert.IsType<CarrierLocation>(
            Assert.IsType<Relation>(f.Input()).Left).Tick);
    }

    [Fact]
    public void ExposedInputsBlendWithoutDrainingOrReadingHiddenAmbientParticipation()
    {
        var f = new Fixture();
        Assert.True(AccessCapabilitySurface.TryRead(f.Source, out var ambient,
            out var exposed, out var capabilities, out var blocked, out var residue, out _));
        Assert.Equal(3, Group(ambient).Length);
        Assert.Equal(2, Group(exposed).Length);
        Assert.Empty(Group(capabilities));
        Assert.Empty(Group(blocked));
        Assert.IsType<Placeholder>(residue);

        var model = f.Model(5);
        var filters = Assert.IsType<Relation>(model.Left);
        Assert.Equal(3, Project(Assert.IsType<Relation>(filters.Left), f.Light, f.Output).Target.Ticks);
        Assert.Equal(5, Project(Assert.IsType<Relation>(filters.Right), f.Heat, f.Output).Target.Ticks);
        Assert.Equal(8, Predict(model, f.Input(), f.Output).Ticks);
        Assert.Equal(10, Predict(f.Alternative, f.Input(), f.Output).Ticks);

        var hidden = Assert.IsType<CarrierLocation>(Group(ambient)[2]);
        hidden.Tick.Move(1000);
        Assert.Equal(8, Predict(model, f.Input(), f.Output).Ticks);
        Assert.Equal(0UL, f.Light.Carrier.Value);
        Assert.Equal(0UL, f.Heat.Carrier.Value);
        Assert.Equal(0UL, f.Output.Value);
        Assert.Equal(0, f.Error.Tension);
    }

    [Fact]
    public void ClaimMismatchDoesNotAssignFaultOrMutateAnyParticipant()
    {
        var f = new Fixture();
        var model = f.Model(5);
        var predicted = Predict(model, f.Input(), f.Output);
        var claim = OperationClaim.Equality(new Carrier(), predicted, f.Expected, f.Error);
        Assert.Equal(2, Residual(claim).Ticks);
        Assert.True(OperationClaim.TryRead(claim, out var statement, out _, out _, out _, out _, out _));
        Assert.Equal(predicted, statement.Left);
        Assert.Same(f.Expected, statement.Right);

        var reversed = OperationClaim.Equality(new Carrier(), f.Expected, predicted, f.Error);
        Assert.Equal(-2, Residual(reversed).Ticks);
        Assert.Equal(8, Predict(model, f.Input(), f.Output).Ticks);
        Assert.Equal(10, Point(f.Expected).Ticks);
        Assert.Equal(0UL, f.Gain.Value);
        Assert.Equal(0, f.Error.Tension);

        // A different carrier is not silently equated by coordinate or unit compatibility.
        var foreign = Relation.Point(new Carrier(f.Output.UnitShape), 10);
        var unresolved = OperationClaim.Equality(new Carrier(), predicted, foreign, f.Error);
        Assert.True(OperationClaim.TryRead(unresolved, out _, out var unresolvedResidual,
            out _, out _, out _, out var unresolvedEvidence));
        Assert.IsType<Relation>(unresolvedResidual);
        Assert.IsType<Relation>(unresolvedEvidence);
    }

    [Fact]
    public void FeedbackChangesOneExposedParameterAndKeepsStallAndAlternativeEvidence()
    {
        var f = new Fixture();
        Assert.True(RepeatSurface.TryRead(f.Cycle, out var focus, out var controller,
            out var action, out var feedback, out _, out var initialHistory));
        Assert.IsType<Placeholder>(controller);
        Assert.True(FocusSurface.TryRead(Assert.IsType<Relation>(focus), out _, out var goal,
            out _, out var complement, out _, out _, out _, out _));
        Assert.Same(f.Goal, goal);
        Assert.Same(f.Alternative, Group(complement)[1]);
        Assert.True(TransformSurface.TryRead(Assert.IsType<Relation>(action),
            out _, out var response, out _, out _, out _));
        Assert.Same(f.HalfResponse, response);
        var feedbackParts = Group(feedback);
        Assert.Equal(3, feedbackParts.Length);
        Assert.Same(f.Gain, Assert.IsType<Relation>(feedbackParts[0]).Right);
        Assert.True(TransformSurface.TryRead(Assert.IsType<Relation>(feedbackParts[1]),
            out var gainInput, out var endpointRead, out var endpointOutput, out _, out _));
        Assert.Same(f.Gain, Assert.Single(Group(gainInput)));
        Assert.Same(f.EndpointRead, endpointRead);
        var rebind = Assert.IsType<Relation>(feedbackParts[2]);
        Assert.Same(Assert.Single(Group(endpointOutput)), rebind.Left);
        Assert.Same(f.EndpointSlot, rebind.Right);
        Assert.True(TimeStepSequence.TryRead(Assert.IsType<Relation>(initialHistory),
            out var origin, out _, out var initialSteps));
        Assert.Empty(initialSteps);

        // An explicit actuator seed, through signed-tension settlement.
        f.Gain.Add(checked((ulong)Point(f.InitialGain).Ticks));
        var initial = Observe(f);
        Assert.Equal(8, Result(initial).Ticks);
        var first = Respond(f, initial, f.HalfResponse);
        Assert.Equal(6UL, f.Gain.Value);
        var second = Observe(f);
        Assert.Equal(9, Result(second).Ticks);

        var stalled = Respond(f, second, f.HalfResponse);
        Assert.Equal(6UL, f.Gain.Value);
        Assert.True(ReadWitness.TryRead(stalled, out _, out _, out _, out var routeRemainder, out _));
        Assert.Equal(1, Assert.IsType<CarrierLocation>(routeRemainder).Ticks);

        // An explicit new experiment uses the other authored route. There is no
        // automatic promotion, confidence field, or forced winner selection.
        var revised = Respond(f, second, f.FullResponse);
        var final = Observe(f);
        Assert.Equal(7UL, f.Gain.Value);
        Assert.Equal(0, f.Gain.Tension);
        Assert.Equal(10, Result(final).Ticks);
        Assert.Equal(0, ClaimResidual(final).Ticks);
        Assert.Equal(10, Predict(f.Alternative, f.Input(), f.Output).Ticks);
        Assert.Equal(0UL, f.Output.Value);

        var timeline = TimeStepSequence.Create(new Carrier(), origin.Carrier, initial,
            TimeStepSequence.Step(new Carrier(), origin.Carrier, 0, 1, initial, first, second, ClaimResidual(second)),
            TimeStepSequence.Step(new Carrier(), origin.Carrier, 1, 2, second, stalled, second, routeRemainder),
            TimeStepSequence.Step(new Carrier(), origin.Carrier, 2, 3, second, revised, final, ClaimResidual(final)));
        var retained = Relation.OrderedGroup(new Carrier(), f.Alternative, timeline);
        Assert.Same(f.Alternative, Group(retained)[0]);
        Assert.True(TimeStepSequence.TryRead(timeline, out _, out var retainedInitial, out var steps));
        Assert.Equal(3, steps.Length);
        Assert.Same(initial, retainedInitial);
        Assert.Equal(8, Result(initial).Ticks);
        Assert.Equal(2, ClaimResidual(initial).Ticks);
    }

    [Theory]
    [InlineData(6, 5, 10)]
    [InlineData(6, 7, 10)]
    [InlineData(8, 7, 12)]
    public void EqualOutputsDoNotCollapseDistinctMixtures(long light, long endpoint, long alternative)
    {
        // The two models are independently readable; output equality at one
        // sample does not establish equivalence across the input frame.
        var f = new Fixture();
        f.Light.Tick.Set(light);
        var model = f.Model(endpoint);
        Assert.NotEqual(model, f.Alternative);
        Assert.Equal(light / 2 + endpoint, Predict(model, f.Input(), f.Output).Ticks);
        Assert.Equal(alternative, Predict(f.Alternative, f.Input(), f.Output).Ticks);
    }

    [Fact]
    public void CapturedPredictionStaysStableAndSourceCorrespondenceExposesLaterMismatch()
    {
        var f = new Fixture();
        f.Gain.Add(5);
        var old = Observe(f);
        var oldInput = Assert.IsType<Relation>(Selected(old));
        Assert.Equal(6, Assert.IsType<CarrierLocation>(oldInput.Left).Ticks);
        Assert.NotSame(f.Light.Tick, Assert.IsType<CarrierLocation>(oldInput.Left).Tick);
        f.Light.Tick.Move(2);
        Assert.Equal(6, Assert.IsType<CarrierLocation>(oldInput.Left).Ticks);
        Assert.Equal(8, Result(old).Ticks);
        Assert.Equal(9, Result(Observe(f)).Ticks);

        Assert.True(ReadWitness.TryRead(Witness(old), out _, out _, out _, out _, out var provenance));
        var correspondence = Assert.IsType<Relation>(Group(provenance)[1]);
        Assert.Same(f.Source, correspondence.Left);
        Assert.Same(oldInput, correspondence.Right);
        var staleRead = OperationClaim.Equality(new Carrier(), oldInput.Left, f.Light, f.Error);
        Assert.Equal(2, Residual(staleRead).Ticks);
        // This is a local dependency check, not a general graph invalidation service.
    }

    [Fact]
    public void CoarseReadPreservesFineRemainderWithoutInventingDetail()
    {
        var f = new Fixture();
        var coarse = Point(Assert.IsType<Relation>(f.CoarseRead.Right).Right).Carrier;
        var eight = Project(f.CoarseRead, Predict(f.Model(5), f.Input(), f.Output), coarse);
        var ten = Project(f.CoarseRead, Predict(f.Model(7), f.Input(), f.Output), coarse);
        Assert.Equal(2, eight.Target.Ticks);
        Assert.Equal(0, eight.Residue.Ticks);
        Assert.Equal(2, ten.Target.Ticks);
        Assert.Equal(2, ten.Residue.Ticks);
        Assert.Equal(10, ten.Target.Ticks * 4 + ten.Residue.Ticks);
        Assert.Equal(0UL, coarse.Value);
        Assert.Equal(0, f.Output.Tension);
    }

    [Fact]
    public void InputProjectionResidueAndClaimMismatchRemainSeparateInAnObservation()
    {
        var f = new Fixture();
        f.Light.Tick.Set(7);
        f.Gain.Add(5);
        var observation = Observe(f);
        Assert.Equal(8, Result(observation).Ticks);
        Assert.Equal(2, ClaimResidual(observation).Ticks);
        Assert.True(ReadWitness.TryRead(Witness(observation), out _, out _, out _, out var remainder, out _));
        var projectedResidue = Assert.IsType<Relation>(Group(remainder)[0]);
        Assert.Equal(new CarrierLocation(f.Light.Carrier, 1), projectedResidue.Left);
        Assert.Equal(new CarrierLocation(f.Heat.Carrier, 0), projectedResidue.Right);
        Assert.Equal(0, f.Light.Carrier.Tension);
        Assert.Equal(0, f.Error.Tension);
    }

    [Fact]
    public void NegativeFeedbackDrawsFromTheChosenGainWithoutRewritingTheTarget()
    {
        var f = new Fixture();
        Point(f.Expected).Tick.Set(6);
        f.Gain.Add(5);
        var initial = Observe(f);
        Assert.Equal(-2, ClaimResidual(initial).Ticks);
        var response = Respond(f, initial, f.HalfResponse);
        Assert.Equal(4UL, f.Gain.Value);
        Assert.Equal(0, f.Gain.Tension);
        Assert.Equal(7, Result(Observe(f)).Ticks);
        Assert.Equal(6, Point(f.Expected).Ticks);
        Assert.True(ReadWitness.TryRead(response, out _, out _, out var application, out _, out _));
        Assert.Equal(-1, Assert.IsType<CarrierLocation>(Group(application)[1]).Ticks);
        Assert.Equal(8, Result(initial).Ticks);
        Assert.Equal(0UL, f.Output.Value);
    }

    private static Relation Observe(Fixture f)
    {
        // Deliberate value capture with source correspondence. This fixture has
        // two distinct source ticks; this is not an arbitrary graph snapshotter.
        var input = new Relation(new CarrierLocation(f.Light.Carrier, f.Light.Ticks),
            new CarrierLocation(f.Heat.Carrier, f.Heat.Ticks));
        var endpoint = Project(f.EndpointRead, f.Gain.ValueSlot, f.Output).Target;
        var binding = new Relation(f.EndpointSlot, Relation.Point(endpoint.Carrier, endpoint.Ticks));
        var model = Assert.IsType<Relation>(StructuralPattern.Apply(f.Mixture, binding));
        Assert.True(model.TryReadOperationFrom(input, f.Output, out var prediction));
        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out var projectionResidue));
        var expected = new CarrierLocation(f.Output, Point(f.Expected).Ticks);
        var claim = OperationClaim.Equality(new Carrier(), predicted, expected, f.Error);
        var witness = ReadWitness.Create(new Carrier(), input, model, predicted,
            Relation.OrderedGroup(new Carrier(), projectionResidue, Residual(claim)),
            Relation.OrderedGroup(new Carrier(), claim, new Relation(f.Source, input),
                f.Goal, f.EndpointRead, new CarrierLocation(f.Gain, checked((long)f.Gain.Value)), prediction,
                new Relation(f.Expected, expected)));
        var refinement = RefinementRead.Create(new Carrier(), f.EndpointSlot, binding, witness,
            Residual(claim), f.Cycle);
        Assert.True(RefinementRead.TryReadWitness(refinement, out var read));
        Assert.Same(witness, read);
        return refinement;
    }

    private static Relation Respond(Fixture f, Relation observation, Relation route)
    {
        var mismatch = ClaimResidual(observation);
        var (delta, remainder) = Project(route, mismatch, f.Gain);
        var before = new CarrierLocation(f.Gain, checked((long)f.Gain.Value));
        // This response introduces a new perturbation justified by a local read.
        // It is not a draining transfer of sensory input or of claim residue.
        if (delta.Ticks > 0) f.Gain.Add(checked((ulong)delta.Ticks));
        if (delta.Ticks < 0) f.Gain.Remove(checked((ulong)-delta.Ticks));
        var after = new CarrierLocation(f.Gain, checked((long)f.Gain.Value));
        return ReadWitness.Create(new Carrier(), mismatch, route,
            Relation.OrderedGroup(new Carrier(), before, delta, after, f.Gain.TensionSlot),
            remainder, observation);
    }

    private static CarrierLocation Predict(Relation model, IElement input, Carrier output)
    {
        Assert.True(model.TryReadOperationFrom(input, output, out var prediction));
        Assert.True(Equation.TryReadPrediction(prediction, out var result, out _));
        return result;
    }

    private static (CarrierLocation Target, CarrierLocation Residue) Project(Relation port, CarrierLocation input, Carrier output)
    {
        Assert.True(port.TryProject(input, output, out var read));
        Assert.True(read.TryGetCarrierLocations(out var target, out var residue));
        Assert.Same(output, target.Carrier);
        return (target, residue);
    }

    private static CarrierLocation Residual(Relation claim)
    {
        Assert.True(OperationClaim.TryRead(claim, out _, out var residual, out _, out _, out _, out _));
        return Assert.IsType<CarrierLocation>(residual);
    }

    private static CarrierLocation ClaimResidual(Relation witness)
    {
        Assert.True(ReadWitness.TryRead(Witness(witness), out _, out _, out _, out var residue, out _));
        return Assert.IsType<CarrierLocation>(Group(residue)[1]);
    }

    private static CarrierLocation Result(Relation witness)
    {
        Assert.True(ReadWitness.TryRead(Witness(witness), out _, out _, out var result, out _, out _));
        return Assert.IsType<CarrierLocation>(result);
    }

    private static IElement Selected(Relation witness)
    {
        Assert.True(ReadWitness.TryRead(Witness(witness), out var selected, out _, out _, out _, out _));
        return selected;
    }

    private static Relation Witness(Relation refinement)
    {
        Assert.True(RefinementRead.TryReadWitness(refinement, out var witness));
        return witness;
    }

    private static IElement[] Group(IElement element)
    {
        var relation = Assert.IsType<Relation>(element);
        Assert.True(relation.TryReadOrderedElementsOn(Assert.IsType<Carrier>(relation.Right), out var parts));
        return parts;
    }

    private static CarrierLocation Point(IElement element) =>
        Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(element).Left);

    private sealed class Fixture
    {
        private readonly IElement[] components;
        private readonly IElement[] models;
        private readonly IElement[] control;
        public Fixture()
        {
            var document = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "Syntax", "local-blend.gojo")));
            Assert.Equal(6, document.Roots.Count);
            Source = Assert.IsType<Relation>(document.Roots[0]);
            Assert.True(AccessCapabilitySurface.TryRead(Source, out _, out var ports,
                out _, out _, out _, out _));
            var exposed = Group(ports);
            Light = Assert.IsType<CarrierLocation>(exposed[0]);
            Heat = Assert.IsType<CarrierLocation>(exposed[1]);
            components = Group(document.Roots[1]);
            models = Group(document.Roots[2]);
            control = Group(document.Roots[3]);
            Cycle = Assert.IsType<Relation>(document.Roots[4]);
            CoarseRead = Assert.IsType<Relation>(document.Roots[5]);
        }
        public Relation Source { get; }
        public CarrierLocation Light { get; }
        public CarrierLocation Heat { get; }
        public Relation InputPattern => Assert.IsType<Relation>(components[0]);
        public Relation[] PartialBindings => Group(components[1]).Cast<Relation>().ToArray();
        public Relation Expectation => Assert.IsType<Relation>(components[3]);
        public Relation Mixture => Assert.IsType<Relation>(models[0]);
        public Relation[] InitialBindings => Group(models[2]).Cast<Relation>().ToArray();
        public Placeholder EndpointSlot => Assert.IsType<Placeholder>(models[1]);
        public Relation Alternative => Assert.IsType<Relation>(models[3]);
        public Relation Goal => Assert.IsType<Relation>(control[0]);
        public Relation Expected => Assert.IsType<Relation>(control[1]);
        public Carrier Output => Point(Expected).Carrier;
        public Carrier Error => Assert.IsType<Carrier>(control[2]);
        public Carrier Gain => Assert.IsType<Carrier>(control[3]);
        public Relation InitialGain => Assert.IsType<Relation>(control[4]);
        public Relation EndpointRead => Assert.IsType<Relation>(control[5]);
        public Relation HalfResponse => Assert.IsType<Relation>(control[6]);
        public Relation FullResponse => Assert.IsType<Relation>(control[7]);
        public Relation Cycle { get; }
        public Relation CoarseRead { get; }
        public IElement Input() => StructuralPattern.Apply(InputPattern, Group(components[2]).Cast<Relation>().ToArray());
        public Relation Model(long endpoint) => Assert.IsType<Relation>(StructuralPattern.Apply(Mixture,
            new Relation(EndpointSlot, Relation.Point(Output, endpoint))));
    }
}
