using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>Counterfactual supervision for an authored scalar receiver. These
/// patches are separate from the later coupled-world transfer evaluation.</summary>
public static class LocalResponseData
{
    public sealed record Patch(int Id, string Split, long[] Requests, Relation Surface, double[] X, double[] Rewards, long[] Moves);
    public sealed record Sets(Patch[] Train, Patch[] Validation, Patch[] Test, Patch[] Stress, Relation Receipt);

    public static Sets Generate(bool quick = false)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal); var next = 0;
        if (!quick)
        {
            var development = Generate(true);
            foreach (var patch in development.Train.Concat(development.Validation).Concat(development.Test).Concat(development.Stress))
                seen.Add(Key(patch.Requests));
        }
        Patch[] GenerateSet(string split, int count, int seed, int range)
        {
            var random = new Random(seed); var patches = new List<Patch>();
            while (patches.Count < count)
            {
                var n = random.Next(3, 9); var r = new long[n]; var centre = random.Next(2, range / 2);
                var family = random.Next(4);
                for (var i = 0; i < n; i++) r[i] = family switch
                {
                    0 => i == n - 1 ? -random.Next(Math.Max(3, range / 2), range + 1) : random.Next(1, Math.Max(3, centre)),
                    1 => (i % 2 == 0 ? 1 : -1) * random.Next(1, range + 1),
                    2 => random.Next(-range / 4, range / 4 + 1),
                    _ => random.Next(0, 3) == 0 ? -random.Next(1, range + 1) : random.Next(1, range / 2 + 1)
                };
                if (!r.Any(x => x > 0) || !r.Any(x => x < 0)) continue;
                if (random.Next(2) == 0) r = r.Select(x => -x).ToArray();
                // Whole request multisets, including their reflection, cannot
                // cross splits. The generator family is never a model input.
                if (!seen.Add(Key(r))) continue;
                patches.Add(Create(next++, split, r));
            }
            return patches.ToArray();
        }
        var seeds = quick ? new[] { 301, 719, 1229, 2017 } : [3109, 3739, 4517, 5501];
        var train = GenerateSet("training", quick ? 192 : 2048, seeds[0], 128);
        var validation = GenerateSet("validation", quick ? 48 : 384, seeds[1], 128);
        var test = GenerateSet("test", quick ? 96 : 768, seeds[2], 128);
        var stress = GenerateSet("wider requests", quick ? 48 : 384, seeds[3], 256);
        var countAxis = new Carrier(); var receipt = G(new Placeholder(), Counts(train.Length, validation.Length, test.Length, stress.Length),
            G(seeds.Select(seed => (IElement)new CarrierLocation(countAxis, seed)).ToArray()),
            Relation.Segment(countAxis, 0, 128), Relation.Segment(countAxis, 0, 256));
        return new(train, validation, test, stress, receipt);

        Relation Counts(params int[] counts) => G(counts.Select(x => (IElement)new CarrierLocation(new Carrier(), x)).ToArray());
    }

    public static string Key(long[] requests)
    {
        var a = string.Join(',', requests.Order()); var b = string.Join(',', requests.Select(x => -x).Order());
        return string.CompareOrdinal(a, b) < 0 ? a : b;
    }

    public static Patch Create(int id, string split, long[] requests)
    {
        var axis = new Carrier(); var clock = new Carrier(); var port = G(new CarrierLocation(axis, 128), Relation.Segment(axis, 0, 1), clock, new Placeholder());
        var own = G(port, new CarrierLocation(clock, 0), new CarrierLocation(axis, 128), new Placeholder());
        var source = requests.Select(delta =>
        {
            var target = Equation.PredictWitness(new Carrier(), Equation.Constant(axis, checked(128 + delta)), new Placeholder(), axis);
            return EvidenceObligation.Signal(port, own, target, G()).Request;
        }).ToArray();
        var featureAxes = Enumerable.Range(0, LocalResponseProgram.InputCount).Select(_ => new Carrier()).ToArray();
        var context = RequestContext.Read(source, axis, featureAxes); var costs = new List<IElement>();
        var rewards = new List<double>(); var moves = new List<long>();
        var total = requests.Sum(x => Math.Abs(x)); var scoreAxis = new Carrier();
        var before = Audit(128); var beforeCost = (CarrierLocation)F(before)[2];
        foreach (var cap in LocalResponseProgram.Caps)
        {
            var actuator = Program.Parameter(128); var selected = Relation.Segment(axis, -cap, cap);
            var clipped = source.Select(r => Fraction.Clamp(Fraction.Read(F(r)[3]), new Fraction(-cap), new Fraction(cap))).ToArray();
            var sum = clipped.Aggregate(Fraction.Zero, (a, b) => a + b);
            var map = Equation.Affine(axis, 0, 4, actuator, 0, 1);
            var projected = Equation.PredictWitness(new Carrier(), map, sum.Encode(axis), actuator);
            var wanted = Fraction.Read(F(projected)[2]); var legal = Fraction.Clamp(wanted, new Fraction(-8), new Fraction(8));
            var move = legal.Truncate(1);
            var receipt = Carrier.ReceiveTogether(G(new CarrierLocation(actuator, move)));
            var after = Audit(checked((long)actuator.Value)); var afterCost = (CarrierLocation)F(after)[2];
            var difference = Equation.PredictWitness(new Carrier(), Equation.Difference(scoreAxis, scoreAxis, scoreAxis),
                new Relation(afterCost, beforeCost), scoreAxis);
            var rewardAxis = new Carrier(); var normalize = Equation.Affine(scoreAxis, 0, Math.Max(1, total), rewardAxis, 0, LocalResponseProgram.OutputScale);
            var reward = Equation.PredictWitness(new Carrier(), normalize, F(difference)[2], rewardAxis);
            var branch = ReadWitness.Create(new Carrier(), G(source), selected, G(new CarrierLocation(axis, move), after, reward),
                G((wanted - legal).Encode(actuator), (legal - new Fraction(move)).Encode(actuator)),
                G(G(clipped.Select(x => x.Encode(axis)).ToArray()), sum.Encode(axis), map, projected, receipt,
                    Relation.Segment(actuator, -8, 8), Relation.Segment(actuator, 0, 1), difference));
            costs.Add(branch); moves.Add(move); rewards.Add(CoupledSettlement.Number(F(reward)[2]) / LocalResponseProgram.OutputScale);
        }
        var surface = G(new CarrierLocation(new Carrier(), id), G(source), context, before, G(costs.ToArray()));
        return new(id, split, requests.ToArray(), surface, RequestContext.Values(context), rewards.ToArray(), moves.ToArray());

        Relation Audit(long position)
        {
            var claims = requests.Select(delta => OperationClaim.Between(new Carrier(), new CarrierLocation(axis, position),
                new CarrierLocation(axis, 128 + delta), new CarrierLocation(axis, 128 + delta), new Carrier(axis.UnitShape))).ToArray();
            IElement sum = new CarrierLocation(scoreAxis, 0); var trace = new List<IElement>();
            foreach (var claim in claims)
            {
                if (!OperationClaim.TryReadResolvedBetweenViolation(claim, out var violation)) throw new InvalidOperationException("A teacher interval is unresolved.");
                var convert = Equation.PredictWitness(new Carrier(), Equation.Affine(violation.Carrier, 0, 1, scoreAxis, 0, 1), violation, scoreAxis);
                var add = Equation.PredictWitness(new Carrier(), Equation.Sum(scoreAxis, Equation.UnitPort(scoreAxis), Equation.UnitPort(scoreAxis)),
                    new Relation(sum, F(convert)[2]), scoreAxis); sum = F(add)[2]; trace.Add(G(convert, add));
            }
            return ReadWitness.Create(new Carrier(), G(claims), G(port, new CarrierLocation(axis, position)), sum, G(), G(trace.ToArray()));
        }
    }
}
