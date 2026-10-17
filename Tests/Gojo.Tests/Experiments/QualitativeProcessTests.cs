using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Tests.Experiments;

/// <summary>
/// Selected local process laws are relation-shaped schemas. The shared matcher
/// knows neither water, signs, dominance nor activation. These experiments do
/// not establish a general qualitative physics engine or material settlement.
/// </summary>
public sealed class QualitativeProcessTests
{
    [Fact]
    public void DirectionHasTopologyWithoutMagnitudeAndIsSeparateFromTheCurrentValue()
    {
        var f = new ProcessWorld();
        var value = new Placeholder();
        var direction = new Placeholder();
        var property = new Relation(value, direction);
        var knowledge = new Relation(property, f.Directions);

        Assert.Same(f.Increasing.Left, f.Stable.Left);
        Assert.Same(f.Stable.Left, f.Stable.Right);
        Assert.Equal(f.Increasing.Reverse(), f.Decreasing);
        Assert.NotEqual(f.Increasing, f.Decreasing);
        Assert.Equal(3, Entries(f.Directions).Length);
        Assert.Empty(knowledge.Carriers());
        Assert.Empty(knowledge.CarrierLocations());

        var refined = Assert.IsType<Relation>(StructuralPattern.Refine(property,
            StructuralPattern.Bind(direction, f.Increasing)));
        Assert.Same(value, refined.Left);
        Assert.Same(f.Increasing, refined.Right);
        Assert.Same(direction, property.Right);
    }

    [Fact]
    public void ExistingSubstitutionDoesNotInferDirectionFromAnInfluenceOrDominance()
    {
        var f = new ProcessWorld();
        var result = new Placeholder();
        var source = new Relation(StructuralConstraint.Closed(f.Incoming, f.Outgoing),
            new Relation(f.Incoming, f.Outgoing));
        var experiment = new Relation(source, result);

        Assert.Same(experiment, StructuralPattern.Refine(experiment));
        Assert.Same(result, experiment.Right);
        Assert.Empty(StructuralPattern.Advance([], new Relation(f.Incoming, f.Outgoing)));
        Assert.Empty(experiment.Carriers());
    }

    [Fact]
    public void AClosedSingleInfluenceProjectsItsDirectionWithoutKnowingItsMagnitude()
    {
        var f = new ProcessWorld();
        var active = StructuralConstraint.Closed(f.Incoming);
        var schema = SingleInfluenceRule();
        var read = ApplyRule(schema.Rule, schema.Variables, active);

        AssertUnique(read, f.Increasing);
        Assert.Empty(read.Carriers());
        Assert.Empty(read.CarrierLocations());
        Assert.Equal([f.Incoming], Entries(active));
        Assert.Same(f.Increasing, f.Incoming.Right);
    }

    [Fact]
    public void AnOpenInfluenceScopeCannotJustifyTheSingleInfluenceConclusion()
    {
        var f = new ProcessWorld();
        var schema = SingleInfluenceRule();
        var active = StructuralConstraint.Open(f.Incoming);
        var read = ApplyRule(schema.Rule, schema.Variables, active);

        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
        Assert.Equal(3, Entries(f.Directions).Length);
        Assert.Empty(read.Carriers());
    }

    [Fact]
    public void DeactivationRevisesTheReadOnlyUnderAnExplicitEmptyInfluencePremise()
    {
        var f = new ProcessWorld();
        var single = SingleInfluenceRule();
        var earlier = ApplyRule(single.Rule, single.Variables, StructuralConstraint.Closed(f.Incoming));
        AssertUnique(earlier, f.Increasing);

        var seam = new Placeholder();
        var emptyRule = new Relation(new Relation(seam, seam), f.Stable);
        var empty = StructuralConstraint.Closed();
        var later = ApplyRule(emptyRule, StructuralConstraint.Closed(seam), empty);

        AssertUnique(later, f.Stable);
        AssertUnique(earlier, f.Increasing);
        Assert.False(StructuralConstraint.TryReadUnique(
            ApplyRule(single.Rule, single.Variables, empty), out _, out _));
        Assert.Same(f.Increasing, f.Incoming.Right);
    }

    [Fact]
    public void OppositeActiveInfluencesWithoutDominanceKeepAllThreeDirectionsPossible()
    {
        var f = new ProcessWorld();
        var active = StructuralConstraint.Closed(f.Incoming, f.Outgoing);
        var read = ReadDominance(active, StructuralConstraint.Open());
        var unresolved = new Relation(f.Directions, read);

        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
        Assert.Equal([f.Increasing, f.Decreasing, f.Stable], Entries(f.Directions));
        Assert.Same(f.Directions, unresolved.Left);
        Assert.Empty(unresolved.Carriers());
        Assert.Equal(f.Increasing.Reverse(), f.Decreasing);
    }

    [Fact]
    public void ReversingOnlyDominanceChangesTheConsequenceAgainstTheSameActiveScope()
    {
        var f = new ProcessWorld();
        var active = StructuralConstraint.Closed(f.Incoming, f.Outgoing);
        var incomingDominates = new Relation(f.Incoming, f.Outgoing);
        var outgoingDominates = new Relation(f.Outgoing, f.Incoming);

        var increasing = ReadDominance(active, StructuralConstraint.Closed(incomingDominates));
        var decreasing = ReadDominance(active, StructuralConstraint.Closed(outgoingDominates));
        var unknown = ReadDominance(active, StructuralConstraint.Open());

        AssertUnique(increasing, f.Increasing);
        AssertUnique(decreasing, f.Decreasing);
        Assert.False(StructuralConstraint.TryReadUnique(unknown, out _, out _));
        Assert.Equal([f.Incoming, f.Outgoing], Entries(active));
        Assert.Same(f.Incoming, outgoingDominates.Right);
        Assert.Same(f.Outgoing, outgoingDominates.Left);
        Assert.Empty(increasing.Carriers());
        Assert.Empty(decreasing.Carriers());
    }

    [Fact]
    public void AThirdActiveInfluenceOrAnOpenActiveTailDefeatsTheTwoInfluenceDominancePremise()
    {
        var f = new ProcessWorld();
        var third = new Relation(new Placeholder(), f.Increasing);
        var dominance = new Relation(f.Incoming, f.Outgoing);

        Assert.False(StructuralConstraint.TryReadUnique(ReadDominance(
            StructuralConstraint.Closed(f.Incoming, f.Outgoing, third),
            StructuralConstraint.Closed(dominance)), out _, out _));
        Assert.False(StructuralConstraint.TryReadUnique(ReadDominance(
            StructuralConstraint.Open(f.Incoming, f.Outgoing),
            StructuralConstraint.Closed(dominance)), out _, out _));
    }

    [Fact]
    public void DominanceOverAnUnlistedInfluenceCannotGovernTheClosedActivePair()
    {
        var f = new ProcessWorld();
        var foreign = new Relation(new Placeholder(), f.Increasing);
        var read = ReadDominance(StructuralConstraint.Closed(f.Incoming, f.Outgoing),
            StructuralConstraint.Closed(new Relation(foreign, f.Outgoing)));

        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
        Assert.Equal(3, Entries(f.Directions).Length);
    }

    [Fact]
    public void ALookalikeEffectOrSelfDominanceCannotAccountForTheOpposedActivePair()
    {
        var f = new ProcessWorld();
        var active = StructuralConstraint.Closed(f.Incoming, f.Outgoing);
        var clone = new Relation(f.Incoming.Left, f.Incoming.Right);
        Assert.Equal(f.Incoming, clone);
        Assert.NotSame(f.Incoming, clone);

        Assert.False(StructuralConstraint.TryReadUnique(ReadDominance(active,
            StructuralConstraint.Closed(new Relation(clone, f.Outgoing))), out _, out _));
        Assert.False(StructuralConstraint.TryReadUnique(ReadDominance(active,
            StructuralConstraint.Closed(new Relation(f.Incoming, f.Incoming))), out _, out _));
    }

    [Fact]
    public void AQualitativeLandmarkConditionActivatesAnEffectWithoutANumericBoundary()
    {
        var f = new ProcessWorld();
        var landmark = new Placeholder();
        var higher = new Placeholder();
        var lower = new Placeholder();
        var observedPosition = new Placeholder();
        var effect = new Placeholder();
        // The selected local rule exposes the effect only for landmark -> higher.
        // Its consequence is an influence, not a claim about net level change.
        var condition = new Relation(landmark, observedPosition);
        var rule = new Relation(new Relation(condition, effect), effect);
        var variables = StructuralConstraint.Closed(observedPosition, effect);
        var binding = StructuralPattern.Bind(observedPosition, higher);

        Relation ConditionRead(IElement observation) => StructuralConstraint.Match(rule.Left, variables,
            StructuralConstraint.Closed(new Relation(observation, f.Outgoing)), binding);
        Relation Activate(IElement observation) => StructuralConstraint.Project(ConditionRead(observation), rule.Right);

        var above = new Relation(landmark, higher);
        AssertUnique(Activate(above), f.Outgoing);
        var single = SingleInfluenceRule();
        // The activated effect exposes its exhaustive single-influence scope.
        // Joining retains its identity instead of independently choosing an effect.
        var responsePort = new Relation(f.Outgoing, StructuralConstraint.Closed(f.Outgoing));
        var downstream = StructuralConstraint.Join(ConditionRead(above),
            new Relation(effect, single.Rule.Left), single.Variables,
            StructuralConstraint.Closed(responsePort));
        AssertUnique(StructuralConstraint.Project(downstream, single.Rule.Right), f.Decreasing);
        Assert.False(StructuralConstraint.TryReadUnique(
            Activate(new Relation(landmark, lower)), out _, out _));
        var unavailable = StructuralConstraint.Project(StructuralConstraint.Match(rule.Left, variables,
            StructuralConstraint.Open(), binding), rule.Right);
        Assert.False(StructuralConstraint.TryReadUnique(unavailable, out _, out _));
        Assert.Empty(rule.Carriers());
        Assert.Empty(rule.CarrierLocations());
    }

    [Fact]
    public void AnInferredEffectRemainsARelationParticipantInAFurtherStructuralProjection()
    {
        var f = new ProcessWorld();
        var schema = SingleInfluenceRule();
        var observation = StructuralConstraint.Match(schema.Rule.Left, schema.Variables,
            StructuralConstraint.Closed(StructuralConstraint.Closed(f.Outgoing)));
        var target = new Relation(schema.Rule.Right, new Relation(f.Outgoing, schema.Rule.Right));
        var composed = StructuralConstraint.Project(observation, target);

        Assert.True(StructuralConstraint.TryReadUnique(composed, out var result, out _));
        var surface = Assert.IsType<Relation>(result);
        Assert.Same(f.Decreasing, surface.Left);
        var participation = Assert.IsType<Relation>(surface.Right);
        Assert.Same(f.Outgoing, participation.Left);
        Assert.Same(f.Decreasing, participation.Right);
        Assert.Empty(surface.Carriers());
    }

    [Fact]
    public void AMeasuredInputRefinesTheSameQualitativeModelWithoutInventingAnOutputMagnitude()
    {
        var f = new ProcessWorld();
        var active = StructuralConstraint.Closed(f.Incoming, f.Outgoing);
        var dominance = new Relation(f.Incoming, f.Outgoing);
        var before = ReadDominance(active, StructuralConstraint.Closed(dominance));
        AssertUnique(before, f.Increasing);

        var incomingMagnitude = new Carrier();
        incomingMagnitude.Add(8);
        var measurement = new Relation(f.Incoming, incomingMagnitude.ValueSlot);
        var refined = new Relation(new Relation(active, dominance), measurement);
        var after = ReadDominance(active, StructuralConstraint.Closed(dominance));

        AssertUnique(after, f.Increasing);
        Assert.Same(f.Incoming, measurement.Left);
        Assert.Same(f.Increasing, f.Incoming.Right);
        Assert.Same(f.Decreasing, f.Outgoing.Right);
        Assert.Single(refined.Carriers());
        Assert.Equal(8UL, incomingMagnitude.Value);
        Assert.Equal(0, incomingMagnitude.Tension);
        Assert.Empty(f.Outgoing.Carriers());
        // This measurement adds detail to an already sufficient qualitative
        // premise. It supplies no numerical outflow or numerical net rate.
    }

    private static (Relation Rule, Relation Variables) SingleInfluenceRule()
    {
        var process = new Placeholder();
        var direction = new Placeholder();
        var active = StructuralConstraint.Closed(new Relation(process, direction));
        Assert.True(StructuralConstraint.TryReadScope(active, out _, out var seam, out _));
        return (new Relation(active, direction), StructuralConstraint.Closed(process, direction, seam));
    }

    private static Relation ApplyRule(Relation rule, Relation variables, IElement fact) =>
        StructuralConstraint.Project(StructuralConstraint.Match(rule.Left, variables,
            StructuralConstraint.Closed(fact)), rule.Right);

    private static Relation ReadDominance(Relation active, Relation dominanceSupport)
    {
        var firstActive = new Placeholder();
        var secondActive = new Placeholder();
        var winningProcess = new Placeholder();
        var losingProcess = new Placeholder();
        var winningEffectSlot = new Placeholder();
        var losingEffectSlot = new Placeholder();
        var winningDirection = new Placeholder();
        var losingDirection = new Placeholder();
        var pair = StructuralConstraint.Closed(firstActive, secondActive);
        Assert.True(StructuralConstraint.TryReadScope(pair, out _, out var seam, out _));
        var variables = StructuralConstraint.Closed(firstActive, secondActive, seam,
            winningProcess, losingProcess, winningEffectSlot, losingEffectSlot,
            winningDirection, losingDirection);
        var winningEffect = new Relation(winningProcess, winningDirection);
        var losingEffect = new Relation(losingProcess, losingDirection);
        var domination = new Relation(winningEffectSlot, losingEffectSlot);
        var rule = new Relation(new Relation(pair, domination), winningDirection);
        // The advertised inspection surface includes every active effect,
        // without filtering or selecting a winning participant in C#.
        var activeEntries = Entries(active);
        var selfCorrespondences = StructuralConstraint.Closed(activeEntries
            .Select(effect => (IElement)new Relation(effect, effect)).ToArray());
        // Both orientations are exposed; this traversal chooses no winner.
        // Distinct positions are occurrence correspondence, not magnitude codes.
        var orderedPairs = StructuralConstraint.Closed(activeEntries.SelectMany((first, from) =>
            activeEntries.Where((_, to) => from != to)
                .Select(second => (IElement)new Relation(first, second))).ToArray());

        // Every stage is the same general local matching law. In particular,
        // no C# branch selects a winning process or interprets a direction.
        var read = StructuralConstraint.Match(pair, variables, StructuralConstraint.Closed(active));
        read = StructuralConstraint.Join(read, domination, variables, dominanceSupport);
        read = StructuralConstraint.Join(read, domination, variables, orderedPairs);
        read = StructuralConstraint.Join(read, new Relation(winningEffectSlot, winningEffect),
            variables, selfCorrespondences);
        read = StructuralConstraint.Join(read, new Relation(losingEffectSlot, losingEffect),
            variables, selfCorrespondences);
        return StructuralConstraint.Project(read, rule.Right);
    }

    private static void AssertUnique(Relation read, IElement expected)
    {
        Assert.True(StructuralConstraint.TryReadUnique(read, out var result, out _));
        Assert.Same(expected, result);
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private sealed class ProcessWorld
    {
        public ProcessWorld()
        {
            var lower = new Placeholder();
            var higher = new Placeholder();
            Increasing = new Relation(lower, higher);
            Decreasing = new Relation(higher, lower);
            Stable = new Relation(lower, lower);
            Directions = StructuralConstraint.Closed(Increasing, Decreasing, Stable);
            Incoming = new Relation(new Placeholder(), Increasing);
            Outgoing = new Relation(new Placeholder(), Decreasing);
        }

        public Relation Increasing { get; }
        public Relation Decreasing { get; }
        public Relation Stable { get; }
        public Relation Directions { get; }
        public Relation Incoming { get; }
        public Relation Outgoing { get; }
    }
}
