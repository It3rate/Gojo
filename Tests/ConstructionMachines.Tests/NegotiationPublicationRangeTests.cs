using Gojo.Core;
using Gojo.Experiments.ConstructionMachines;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class NegotiationPublicationRangeTests
{
    [Fact]
    public void AWholeCoordinateCandidateWithAnUnrepresentablePublicationJumpCannotMutateItsActuator()
    {
        var c = new ConstructionRecipes();
        var actuator = Program.Parameter(1);
        var expression = Interpolate(c.Basis, c.Point(-long.MaxValue, 0), c.Point(-long.MaxValue + 2, 0),
            actuator.ValueSlot, 1, actuator, 0, long.MaxValue);
        var definition = TransformSurface.Create(new Carrier(), G(actuator), G(expression),
            G(new Placeholder()), G(), G(c.Basis, c.Metric));
        var local = new NegotiatingMachine(definition);
        var target = new NegotiatingMachine(TransformSurface.Create(new Carrier(), G(), G(c.Point(0, 0)),
            G(new Placeholder()), G(), G(c.Basis, c.Metric)));
        var port = local.Port(0, 0); var targetPort = target.Port(0, 0);
        local.AddResponse(port, actuator, responseN: 1, responseD: 1,
            min: 0, max: long.MaxValue, maxStep: long.MaxValue);
        var observation = local.Publish(port);
        var link = new NegotiationLink(port, targetPort);
        var request = NegotiationLink.Requests(link.Plan(observation, target.Publish(targetPort)))[0];
        var oldSite = (CarrierLocation)F(local.Point(0))[0];

        Assert.True(local.Apply([request], out var receipt));

        Assert.Equal(1UL, actuator.Value);
        Assert.Equal(0L, actuator.Tension);
        Assert.Equal(-long.MaxValue + 2, oldSite.Ticks);
        Assert.True(oldSite.SameTickRefAs((CarrierLocation)F(local.Point(0))[0]));
        var outcome = Assert.Single(NegotiatingMachine.Outcomes(receipt));
        Assert.True(ProjectionResidue.TryRead(outcome.Accepted, out _, out var accepted, out _));
        Assert.Equal(0, accepted);
        Assert.True(ProjectionResidue.TryRead(outcome.CandidateBlocked, out _, out var blocked, out var denominator));
        Assert.Equal((long.MaxValue - 2) * denominator, blocked);
        Assert.NotEmpty(F(F(receipt)[3]));
        Assert.False(local.Apply([request], out _));
        Assert.Equal(1UL, actuator.Value);
    }
}
