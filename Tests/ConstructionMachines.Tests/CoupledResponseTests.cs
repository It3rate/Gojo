using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;
using Gojo.Experiments.ConstructionMachines;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.ConstructionMachines.Tests;

public sealed class CoupledResponseTests
{
    private static (NegotiatingMachine Machine,Carrier A,Carrier B) Pair()
    {
        var c=new ConstructionRecipes();var a=Program.Parameter(10);var b=Program.Parameter(20);
        var point=c.Plus(c.Times(c.Point(1,0),a.ValueSlot,a),c.Times(c.Point(1,0),b.ValueSlot,b));
        var definition=TransformSurface.Create(new Carrier(),G(a,b),G(point),G(new Placeholder()),G(),G(c.Basis));
        return (new NegotiatingMachine(definition),a,b);
    }
    private static Relation Request(NegotiatingMachine machine,long target)
    {
        var port=machine.Port(0,0);var axis=((CarrierLocation)F(port)[0]).Carrier;
        var desired=Equation.PredictWitness(new Carrier(),Equation.Constant(axis,target),new Placeholder(),axis);
        return EvidenceObligation.Signal(port,machine.Publish(port),desired,G()).Request;
    }

    [Fact] public void OneExposedReadCanDriveAnExplicitCalibratedFamilyAndRestoreIt()
    {
        var (machine,a,b)=Pair();
        var family=machine.AddCoupledResponses(machine.Port(0,0),[
            new(a,1,2),new(b,1,2)],responseN:1,responseD:1);
        Assert.Equal(2,F(F(family)[1]).Length);
        Assert.True(machine.Apply([Request(machine,40)],out var receipt));
        Assert.Equal(15UL,a.Value);Assert.Equal(25UL,b.Value);Assert.Equal(2,NegotiatingMachine.Outcomes(receipt).Count);
        foreach(var outcome in NegotiatingMachine.Outcomes(receipt))
        {
            Assert.True(ProjectionResidue.TryRead(outcome.Requested,out _,out var requested,out var rd));
            Assert.True(ProjectionResidue.TryRead(outcome.Accepted,out _,out var accepted,out var ad));
            Assert.Equal(5*rd,requested);Assert.Equal(5*ad,accepted);
            foreach(var remainder in new[]{outcome.BoundsBlocked,outcome.CapacityDeferred,outcome.QuantumRemainder,outcome.CandidateBlocked})
            {Assert.True(ProjectionResidue.TryRead(remainder,out _,out var n,out _));Assert.Equal(0,n);}
        }
        var restored=NegotiatingMachine.Restore((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(machine.Surface))[0]);
        Assert.True(restored.Apply([Request(restored,36)],out _));
        Assert.Equal(36,Program.XY(restored.Point(0))[0]);
    }

    [Fact] public void InvalidFamilyDoesNotPartiallyAttachItsFirstRoute()
    {
        var (machine,a,_)=Pair();var port=machine.Port(0,0);
        Assert.Throws<ArgumentException>(()=>machine.AddCoupledResponses(port,[new(a),new(a)]));
        Assert.Empty(machine.Responses);machine.AddResponse(port,a);Assert.Single(machine.Responses);
    }

    [Fact] public void OrdinaryResponseStillRejectsImplicitFanout()
    {
        var (machine,a,b)=Pair();var port=machine.Port(0,0);machine.AddResponse(port,a);
        Assert.Throws<ArgumentException>(()=>machine.AddResponse(port,b));
        Assert.Single(machine.Responses);
    }
}
