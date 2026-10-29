using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Six finite, reflection-invariant reads of one public request group.
/// Scalar projections, signed partitions, rank evidence and ratio frames remain
/// attached. No category, geometry target or private actuator is queried.</summary>
public static class RequestContext
{
    public const long RequestScale = 256;
    public static readonly string[] Names = ["Peak / 128", "Net in median direction", "Minority count / count",
        "Net magnitude / total magnitude", "Median magnitude / peak", "Count / 8"];

    public static Relation Read(Relation[] requests, Carrier requestBasis, Carrier[] outputs)
    {
        if (outputs.Length != LocalResponseProgram.InputCount || requests.Length == 0 || requests.Length > 4096) return Pending(G(requests));
        var axis = new Carrier(); var countAxis = new Carrier(); var conversions = new List<IElement>();
        var scope = Relation.Segment(requestBasis, -4096, 4096);
        var scaled = new List<CarrierLocation>();
        var inputMap = Equation.Affine(requestBasis, 0, 1, axis, 0, RequestScale);
        foreach (var request in requests)
        {
            if (!ProjectionResidue.TryRead(F(request)[3], out var basis, out _, out _) || !basis.SameIdentityAs(requestBasis)) return Pending(request);
            var raw = Fraction.Read(F(request)[3]);
            if (raw < new Fraction(-4096) || raw > new Fraction(4096)) return Pending(G(request, scope));
            var projection = Equation.PredictWitness(new Carrier(), inputMap, F(request)[3], axis);
            var exact = Fraction.Read(F(projection)[2]); var value = new CarrierLocation(axis, exact.Truncate(1));
            scaled.Add(value);
            conversions.Add(ReadWitness.Create(new Carrier(), request, inputMap, value, (exact - new Fraction(value.Ticks)).Encode(axis), projection));
        }
        var ordered = scaled.OrderBy(x => x.Ticks).ToArray();
        var orderClaims = ordered.Zip(ordered.Skip(1), (a, b) => (IElement)OperationClaim.AtMost(new Carrier(), a, b, new Carrier())).ToArray();
        var signs = scaled.Select(x => (IElement)OperationClaim.AtLeast(new Carrier(), x, new CarrierLocation(axis, 0), new Carrier())).ToArray();
        var positive = scaled.Where(x => x.Ticks > 0).ToArray(); var negative = scaled.Where(x => x.Ticks < 0).ToArray();
        var absolute = scaled.Select(x => new CarrierLocation(axis, checked(Math.Abs(x.Ticks)))).ToArray();
        var peak = absolute.Max(x => x.Ticks); var total = absolute.Sum(x => x.Ticks); var net = scaled.Sum(x => x.Ticks);
        var midpoint = ordered.Length / 2;
        var medianNumerator = ordered.Length % 2 == 1 ? ordered[midpoint].Ticks : checked(ordered[midpoint - 1].Ticks + ordered[midpoint].Ticks);
        var medianD = ordered.Length % 2 == 1 ? 1 : 2;
        var medianDirection = Math.Sign(medianNumerator);
        var ratios = new (long N, long D, Carrier Axis, IElement Source)[]
        {
            (peak, 128 * RequestScale, axis, Group(absolute)),
            (checked(total + net * medianDirection), Math.Max(1, checked(2 * total)), axis,
                G(Group(scaled), new Relation(Relation.Segment(axis, 0, medianDirection), Group(ordered)))),
            (Math.Min(positive.Length, negative.Length), requests.Length, countAxis, G(Group(positive), Group(negative))),
            (Math.Abs(net), Math.Max(1, total), axis, Group(scaled)),
            (Math.Abs(medianNumerator), Math.Max(1, checked(medianD * peak)), axis,
                G(Group(ordered), new CarrierLocation(countAxis, midpoint), new CarrierLocation(countAxis, medianD))),
            (requests.Length, 8, countAxis, G(requests))
        };
        var values = new List<IElement>(); var reads = new List<IElement>();
        for (var i = 0; i < ratios.Length; i++)
        {
            var r = ratios[i]; var map = Equation.Affine(r.Axis, 0, r.D, outputs[i], 0, LocalResponseProgram.InputScale);
            var sample = new CarrierLocation(r.Axis, r.N); var projected = Equation.PredictWitness(new Carrier(), map, sample, outputs[i]);
            var exact = Fraction.Read(F(projected)[2]); var clipped = Fraction.Clamp(exact, Fraction.Zero, new Fraction(LocalResponseProgram.InputScale));
            var whole = clipped.Truncate(1); var value = new CarrierLocation(outputs[i], whole); values.Add(value);
            reads.Add(ReadWitness.Create(new Carrier(), G(r.Source, sample), map, value,
                G((exact - clipped).Encode(outputs[i]), (clipped - new Fraction(whole)).Encode(outputs[i])), projected));
        }
        // The magnitude and sum reads retain their actual selected operands.
        // Ordered signs and frames distinguish opposed participation from a
        // reversed route; no hidden sign flag changes a carrier face.
        var magnitudes = scaled.Select(x => (IElement)Equation.PredictWitness(new Carrier(),
            Equation.Affine(axis, 0, x.Ticks < 0 ? -1 : 1, axis, 0, 1), x, axis)).ToArray();
        var peakClaims = absolute.Select(x => (IElement)OperationClaim.AtLeast(new Carrier(), new CarrierLocation(axis, peak), x, new Carrier())).ToArray();
        var folds = G(new Relation(Relation.Segment(axis, 0, 1), Group(positive)),
            new Relation(Relation.Segment(axis, 0, -1), Group(negative)),
            G(magnitudes), G(new CarrierLocation(axis, peak), G(peakClaims)),
            Sum(absolute), Sum(scaled.ToArray()), G(signs));
        return ReadWitness.Create(new Carrier(), G(requests), G(inputMap, G(outputs), scope), G(values.ToArray()), G(),
            G(G(conversions.ToArray()), folds, G(Group(ordered), G(orderClaims)), G(reads.ToArray())));

        Relation Pending(IElement why) => ReadWitness.Create(new Carrier(), G(requests), G(requestBasis, G(outputs)),
            new Placeholder(), G(why), G());

        Relation Sum(CarrierLocation[] terms)
        {
            IElement value = new CarrierLocation(axis, 0); var history = new List<IElement>();
            var equation = Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis));
            foreach (var term in terms)
            {
                var read = Equation.PredictWitness(new Carrier(), equation, new Relation(value, term), axis);
                history.Add(read); value = F(read)[2];
            }
            return ReadWitness.Create(new Carrier(), Group(terms), equation, value, G(), G(history.ToArray()));
        }
        static Relation Group(IEnumerable<CarrierLocation> terms) => G(terms.Cast<IElement>().ToArray());
    }

    public static double[] Values(Relation context) => F(F(context)[2]).Select(value =>
        CoupledSettlement.Number(value) / LocalResponseProgram.InputScale).ToArray();

    public static Relation Rebind(Relation context, Carrier[] outputs)
    {
        var values = F(F(context)[2]); var reads = values.Select((value, i) =>
        {
            ProjectionResidue.TryRead(value, out var axis, out _, out _);
            return Equation.PredictWitness(new Carrier(), Equation.Affine(axis, 0, LocalResponseProgram.InputScale,
                outputs[i], 0, LocalResponseProgram.InputScale), value, outputs[i]);
        }).ToArray();
        return ReadWitness.Create(new Carrier(), context, G(outputs), G(reads.Select(r => F(r)[2]).ToArray()), G(), G(reads));
    }
}
