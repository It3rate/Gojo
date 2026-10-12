using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;

namespace UtilityEcology.Tests;

public sealed class InterpretationProviderTests
{
    [Fact]
    public void NoInterpretationBecomesPresenceAdviceOnlyFromBoundPhysicalConsequences()
    {
        var world = new EcologyWorld(); var provider = new InterpretationProvider(world.Sources[0], world.PresenceAxes[0], world.OrderAxes[0], world.ActionAxis, world.ResourceAxis, true, false);
        var trial = new EcologyTrial(0, 0, 0, 2, 1, 1); var first = world.Request(trial);
        Assert.False(WorldPublication.TryValue(provider.Interpret(first, world.Compare, 0), provider.AdviceAxis, out _));
        for (var index = 0; index < 8; index++)
        {
            trial = trial with { Index = index + 1 }; var sample = world.Request(trial);
            var offered = provider.Interpret(sample, world.Compare, index + 1);
            var command = WorldPublication.Value(world.ActionAxis, index % 2 == 0 ? -1 : 1, new Carrier());
            var physical = NativeGraph.BindFeedback(command, world.Execute(trial, command), world.ResourceAxis);
            provider.Observe(sample, offered, command, physical, true, world.Compare);
        }
        Assert.Equal(8, provider.PresenceModel.Updates);
        var read = provider.Interpret(world.Request(trial with { Index = 20 }), world.Compare, 20);
        Assert.True(WorldPublication.TryValue(read, provider.AdviceAxis, out var direction)); Assert.Equal(1, direction);
        Assert.Null(provider.OrdinalModel); Assert.Equal(0, world.ComparisonQueries);
        Assert.True(ReadWitness.TryRead(read, out var boundary, out _, out _, out _, out _)); Assert.IsType<Carrier>(boundary);
    }

    [Fact]
    public void UnboundOutcomeCannotTrainOrCreateFalseSuccessfulEvidence()
    {
        var world = new EcologyWorld(); var provider = new InterpretationProvider(world.Sources[0], world.PresenceAxes[0], world.OrderAxes[0], world.ActionAxis, world.ResourceAxis, true, false);
        var trial = new EcologyTrial(0, 0, 0, 3, 1, 1); var sample = world.Request(trial); var offered = provider.Interpret(sample, world.Compare, 0);
        var command = WorldPublication.Value(world.ActionAxis, 1, new Carrier());
        provider.Observe(sample, offered, command, world.Execute(trial, command), true, world.Compare);
        Assert.Equal(0, provider.Updates); Assert.Equal(0, provider.PresenceModel.Updates);
    }
}
