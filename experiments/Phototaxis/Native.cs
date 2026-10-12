using ConnectionAssembly;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using System.Runtime.CompilerServices;

namespace Phototaxis;

/// <summary>Small constructors/readers of the protocol's connected structure.</summary>
public static class Native
{
    private static readonly ConditionalWeakTable<Carrier, Relation> aimEquations = new();
    public static Relation G(params IElement[] parts) => NativeMath.Group(parts);
    public static IElement[] Parts(Relation group) => NativeMath.Parts(group);
    public static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
    public static Relation Sum(params (IElement Value, long Coefficient)[] terms) => NativeMath.Sum(terms);
    public static Relation SumTo(Carrier output, params (IElement Value, long Coefficient)[] terms) => NativeMath.SumTo(output, terms);
    public static Relation Product(IElement a, IElement b) => NativeMath.Product(a, b);
    public static Relation Signed(Carrier carrier) => SumTo(new Carrier(carrier.UnitShape), (carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1));
    public static Relation Present(Carrier value, Carrier presence) => NativeMath.Present(value.ValueSlot, presence.ValueSlot);
    public static Relation Read(IElement source) => NativeMath.Read(source);
    public static Relation[] ReadMany(params IElement[] sources)
    {
        var execution = NativeExecution.Current;
        if (execution is not null) execution.Batches++;
        return (execution?.Backend ?? NativeExecutionBackend.Compact) switch
        {
            NativeExecutionBackend.Planned => LocalReadPlan.ReadMany(sources),
            NativeExecutionBackend.Reference => NativeMath.ReadMany(sources),
            _ => CompactReadPlan.ReadMany(sources, execution)
        };
    }
    public static Relation ExecutionCorrespondence(IElement source) => CompactReadPlan.Correspondence(source);
    public static Relation ReferenceRead(IElement source) => NativeMath.Read(source);

    public static Relation Import(Relation recipe, Carrier target)
    {
        var read = ReadMany(recipe)[0];
        return G(recipe, read, TryValue(read, out var value) ? Aim(target, value) :
            Relation.UnresolvedRead(recipe, read, target));
    }

    public static bool TryValue(Relation read, out long ticks)
    {
        if (Equation.TryReadPrediction(read, out var value, out _) && !value.Carrier.IsUnknown)
        { ticks = value.Ticks; return true; }
        ticks = 0;
        return false;
    }

    public static long Held(Carrier carrier) => checked((long)carrier.Value + carrier.Tension);

    public static Relation Scale(IElement source, long numerator, long denominator, Carrier? output = null)
    {
        var input = L(NativeMath.Output(source), 0);
        output ??= new Carrier();
        return G(Equation.Affine(input.Carrier, 0, denominator, output, 0, numerator), input, output, G(new Relation(input, source)));
    }

    public static Relation Divide(IElement numerator, IElement denominator, Carrier output)
    {
        var n = L(NativeMath.Output(numerator), 0);
        var d = L(NativeMath.Output(denominator), 0);
        return G(Equation.Affine(new Relation(L(n.Carrier, 0), new CarrierLocation(n.Carrier, d.Tick)),
            new Relation(L(output, 0), L(output, 1))), n, output,
            G(new Relation(n, numerator), new Relation(d, denominator)));
    }

    public static Carrier Limited(Carrier basis, long maximum)
    {
        var carrier = new Carrier(basis.UnitShape);
        carrier.Attach(new Relation(Relation.Point(carrier, maximum), Relation.Point(carrier, maximum)));
        return carrier;
    }

    // Unlike Carrier.Set, this accounts for unresolved signed pressure too.
    public static Relation Aim(Carrier carrier, long desired)
    {
        var equation = aimEquations.GetValue(carrier, static c => Equation.Sum(c,
            Equation.UnitPort(c), Equation.WeightedPort(c, 0, 1, 0, -1), Equation.WeightedPort(c, 0, 1, 0, -1)));
        var input = new Relation(new Relation(L(carrier, desired), L(carrier, (long)carrier.Value)), L(carrier, carrier.Tension));
        if (!equation.TryReadOperationFrom(input, carrier, out var delta))
            delta = Relation.UnresolvedRead(input, equation, carrier);
        return delta.Left is CarrierLocation arrival
            ? Carrier.ReceiveTogether(G(arrival)) : delta;
    }

    public static Relation Transfer(Carrier source, Carrier target, long requested)
    {
        var calibration = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, 1));
        var request = RefinementTransfer.Create(new Carrier(), calibration, L(source, Math.Max(0, requested)));
        RefinementTransfer.TryApply(request, out var witness);
        return witness;
    }

    public static bool AtLeast(CarrierLocation actual, CarrierLocation expected, out Relation claim)
    {
        var basis = new Carrier(actual.Carrier.UnitShape);
        var a = Equation.Affine(actual.Carrier, 0, 1, basis, 0, 1).Project(actual, basis);
        var b = Equation.Affine(expected.Carrier, 0, 1, basis, 0, 1).Project(expected, basis);
        claim = OperationClaim.AtLeast(new Carrier(), a.Left, b.Left, new Carrier(basis.UnitShape));
        return OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    }
}

/// <summary>Distinct physical units shared by the connected bath.</summary>
public sealed class Units
{
    public Carrier Light { get; } = new();
    public Carrier Force { get; } = new();
    public Carrier Calorie { get; } = new();
    public Carrier Attention { get; } = new();
    public Carrier Clock { get; } = new();
    public Carrier Position { get; } = new();
    public Carrier Heading { get; } = new();
    public Carrier Feature { get; } = new();
    public Carrier Return { get; } = new();
    public Carrier Count { get; } = new();
    public Carrier Work { get; } = new();
    public Relation Root => Native.G(Light, Force, Calorie, Attention, Clock, Position, Heading, Feature, Return, Count, Work);
}
