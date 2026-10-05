using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace ChangeSubscriptions;

/// <summary>
/// A per-use comparison surface with a bounded retry ladder. State10 = G(use,
/// source, loss basis, opportunity basis, policy, current block, retry state,
/// total evidence, latest completed block, reusable input/read equations).
/// Policy = G(resolved block size, ordered G(rung,delay) ladder). The supplied
/// ladder is 0,2,4,8,16,32 opportunities, not a learned timing law.
/// </summary>
public static class ComparativeRetry
{
    public static Relation Create(IElement useSurface, IElement sourceSurface, Carrier lossBasis,
        Carrier opportunityClock, int blockSize = 4, int maximumRung = 5)
    {
        if (blockSize < 1) throw new ArgumentOutOfRangeException(nameof(blockSize));
        if (maximumRung is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(maximumRung));
        var countAxis = new Carrier(); var rung = new Carrier();
        var blockCount = new Carrier(countAxis.UnitShape); var blockBenefit = new Carrier(lossBasis.UnitShape);
        var deadline = new Carrier(opportunityClock.UnitShape);
        var evaluated = new Carrier(countAxis.UnitShape); var benefit = new Carrier(lossBasis.UnitShape);
        var helpful = new Carrier(countAxis.UnitShape); var harmful = new Carrier(countAxis.UnitShape);
        var neutral = new Carrier(countAxis.UnitShape); var unknown = new Carrier(countAxis.UnitShape);
        var costlyTies = new Carrier(countAxis.UnitShape);
        var lastCount = new Carrier(countAxis.UnitShape); var lastBenefit = new Carrier(lossBasis.UnitShape);
        var lastTime = new Carrier(opportunityClock.UnitShape); var completed = new Carrier(countAxis.UnitShape);
        var own = L(lossBasis, 0); var supplied = L(lossBasis, 0);
        var delta = N.SumTo(lossBasis, (own, 1), (supplied, -1));
        // The finite table is supplied structure. No host exponent determines
        // a delay after this builder has connected the ladder.
        var ladder = G(new[] { 0L, 2, 4, 8, 16, 32 }.Take(maximumRung + 1).Select((delay, index) =>
            (IElement)G(L(rung, index), L(opportunityClock, delay))).ToArray());
        return G(useSurface, sourceSurface, L(lossBasis, 0), L(opportunityClock, 0),
            G(L(countAxis, blockSize), ladder), G(blockCount, blockBenefit), G(rung, deadline),
            G(evaluated, benefit, helpful, harmful, neutral, unknown, costlyTies),
            G(lastCount, lastBenefit, lastTime, completed),
            G(own, supplied, delta, SignedRecipe(blockBenefit, lossBasis), SignedRecipe(benefit, lossBasis),
                SignedRecipe(deadline, opportunityClock), SignedRecipe(lastBenefit, lossBasis), SignedRecipe(lastTime, opportunityClock)));
    }

    public static IElement Use(Relation state) => Parts(state)[0];
    public static IElement Source(Relation state) => Parts(state)[1];
    public static Relation Policy(Relation state) => (Relation)Parts(state)[4];
    public static Carrier BlockCount(Relation state) => (Carrier)Parts((Relation)Parts(state)[5])[0];
    public static Carrier BlockBenefit(Relation state) => (Carrier)Parts((Relation)Parts(state)[5])[1];
    public static Carrier Rung(Relation state) => (Carrier)Parts((Relation)Parts(state)[6])[0];
    private static Carrier Deadline(Relation state) => (Carrier)Parts((Relation)Parts(state)[6])[1];
    private static Carrier Total(Relation state, int index) => (Carrier)Parts((Relation)Parts(state)[7])[index];
    public static Carrier Evaluated(Relation state) => Total(state, 0);
    public static Carrier Benefit(Relation state) => Total(state, 1);
    public static Carrier Helpful(Relation state) => Total(state, 2);
    public static Carrier Harmful(Relation state) => Total(state, 3);
    public static Carrier Neutral(Relation state) => Total(state, 4);
    public static Carrier Unknown(Relation state) => Total(state, 5);
    public static Carrier CostlyTies(Relation state) => Total(state, 6);
    public static Relation LastBlock(Relation state) => (Relation)Parts(state)[8];
    public static Carrier CompletedBlocks(Relation state) => (Carrier)Parts(LastBlock(state))[3];
    private static Relation Equations(Relation state) => (Relation)Parts(state)[9];
    private static Carrier LossBasis(Relation state) => ((CarrierLocation)Parts(state)[2]).Carrier;
    private static Carrier Clock(Relation state) => ((CarrierLocation)Parts(state)[3]).Carrier;
    public static CarrierLocation RetryAfter(Relation state) => Value(N.Read(Parts(Equations(state))[5]));

    /// <summary>A pure comparison on the caller's validation-opportunity axis.
    /// Reading this surface never advances the clock or strengthens evidence.</summary>
    public static Relation Ready(Relation state, CarrierLocation opportunity) =>
        OperationClaim.AtLeast(new Carrier(), Copy(opportunity), RetryAfter(state), new Carrier());

    /// <summary>Current partial block mean; zero resolved reads remains open.</summary>
    public static Relation Mean(Relation state) => ReadMean(state, BlockCount(state), Parts(Equations(state))[3]);
    public static Relation LastMean(Relation state) => ReadMean(state, (Carrier)Parts(LastBlock(state))[0], Parts(Equations(state))[6]);

    /// <summary>
    /// Admit a once-only paid outcome supplied by the enclosing connection.
    /// Its opportunity counter counts resolved actual input deltas, never
    /// notifications. Missing or incompatible loss observations do not enter
    /// the four-resolved-trial block. Completion preserves only the latest
    /// block in live state; the returned witness retains this transition.
    /// </summary>
    public static Relation Observe(Relation state, CarrierLocation opportunity, CarrierLocation? baselineLoss,
        CarrierLocation? suppliedLoss, CarrierLocation? paidCost, IElement provenance)
    {
        var evidence = new List<IElement>();
        var source = G(Copy(opportunity), baselineLoss.HasValue ? Copy(baselineLoss.Value) : new Placeholder(),
            suppliedLoss.HasValue ? Copy(suppliedLoss.Value) : new Placeholder(),
            paidCost.HasValue ? Copy(paidCost.Value) : new Placeholder(), provenance);
        var nowClaim = OperationClaim.Equality(new Carrier(), Copy(opportunity),
            L(Clock(state), opportunity.Ticks), new Carrier());
        evidence.Add(nowClaim);
        Relation paid = paidCost.HasValue
            ? OperationClaim.AtLeast(new Carrier(), Copy(paidCost.Value), L(paidCost.Value.Carrier, 1), new Carrier())
            : Relation.UnresolvedRead(source, state, LossBasis(state));
        evidence.Add(paid);
        // A notification or unpaid read is not a paid trial, even if it has a
        // numeric-looking outcome. Missing paid outcomes are accounted below.
        if (!Pass(nowClaim) || !Pass(paid)) return G(state, source, G(evidence.ToArray()),
            Relation.UnresolvedRead(source, state, LossBasis(state)));

        Relation baseline = baselineLoss.HasValue
            ? OperationClaim.AtLeast(new Carrier(), Copy(baselineLoss.Value), L(LossBasis(state), 0), new Carrier())
            : Relation.UnresolvedRead(source, state, LossBasis(state));
        Relation supplied = suppliedLoss.HasValue
            ? OperationClaim.AtLeast(new Carrier(), Copy(suppliedLoss.Value), L(LossBasis(state), 0), new Carrier())
            : Relation.UnresolvedRead(source, state, LossBasis(state));
        evidence.Add(baseline); evidence.Add(supplied);
        if (!Pass(baseline) || !Pass(supplied))
        {
            var increment = Calculate(Unknown(state), evidence, (L(Unknown(state), 1), 1));
            var settlement = Carrier.ReceiveTogether(G(increment));
            return G(state, source, G(evidence.ToArray()), settlement,
                Relation.UnresolvedRead(source, state, LossBasis(state)));
        }

        var equations = Parts(Equations(state));
        ((CarrierLocation)equations[0]).Tick.Set(baselineLoss!.Value.Ticks);
        ((CarrierLocation)equations[1]).Tick.Set(suppliedLoss!.Value.Ticks);
        var deltaRead = N.Read(equations[2]); evidence.Add(deltaRead);
        if (!Equation.TryReadPrediction(deltaRead, out var delta, out _))
            return G(state, source, G(evidence.ToArray()), Relation.UnresolvedRead(source, equations[2], LossBasis(state)));
        var positive = OperationClaim.AtLeast(new Carrier(), Copy(delta), L(LossBasis(state), 1), new Carrier());
        var negative = OperationClaim.AtMost(new Carrier(), Copy(delta), L(LossBasis(state), -1), new Carrier());
        var equal = OperationClaim.Equality(new Carrier(), Copy(delta), L(LossBasis(state), 0), new Carrier());
        evidence.Add(positive); evidence.Add(negative); evidence.Add(equal);
        var arrivals = new List<IElement>
        {
            Calculate(BlockCount(state), evidence, (L(BlockCount(state), 1), 1)),
            Calculate(BlockBenefit(state), evidence, (Copy(delta), 1)),
            Calculate(Evaluated(state), evidence, (L(Evaluated(state), 1), 1)),
            Calculate(Benefit(state), evidence, (Copy(delta), 1))
        };
        var classification = Pass(positive) ? Helpful(state) : Pass(negative) ? Harmful(state) : Neutral(state);
        arrivals.Add(Calculate(classification, evidence, (L(classification, 1), 1)));
        if (Pass(equal)) arrivals.Add(Calculate(CostlyTies(state), evidence, (L(CostlyTies(state), 1), 1)));
        evidence.Add(Carrier.ReceiveTogether(G(arrivals.ToArray())));
        var countBasis = ((CarrierLocation)Parts(Policy(state))[0]).Carrier;
        var count = Calculate(countBasis, evidence, (BlockCount(state).ValueSlot, 1));
        var complete = OperationClaim.AtLeast(new Carrier(), count, Parts(Policy(state))[0], new Carrier());
        evidence.Add(complete);
        if (!Pass(complete)) return G(state, source, Copy(delta), G(evidence.ToArray()), new Placeholder());

        var sumRead = N.Read(equations[3]); evidence.Add(sumRead); var sum = Value(sumRead);
        var goodBlock = OperationClaim.AtLeast(new Carrier(), Copy(sum), L(LossBasis(state), 1), new Carrier());
        var badBlock = OperationClaim.AtMost(new Carrier(), Copy(sum), L(LossBasis(state), -1), new Carrier());
        evidence.Add(goodBlock); evidence.Add(badBlock);
        var oldRung = Copy(Rung(state).ValueSlot); var oldDeadline = RetryAfter(state);
        var newRung = oldRung;
        var ladder = Parts((Relation)Parts(Policy(state))[1]).Cast<Relation>().ToArray();
        if (Pass(badBlock))
        {
            var candidate = Calculate(Rung(state), evidence, (oldRung, 1), (L(Rung(state), 1), 1));
            var maximum = (CarrierLocation)Parts(ladder[^1])[0];
            var bound = OperationClaim.AtMost(new Carrier(), candidate, maximum, new Carrier()); evidence.Add(bound);
            newRung = Pass(bound) ? candidate : Copy(maximum);
        }
        else if (Pass(goodBlock))
        {
            var candidate = Calculate(Rung(state), evidence, (oldRung, 1), (L(Rung(state), 1), -1));
            var minimum = (CarrierLocation)Parts(ladder[0])[0];
            var bound = OperationClaim.AtLeast(new Carrier(), candidate, minimum, new Carrier()); evidence.Add(bound);
            newRung = Pass(bound) ? candidate : Copy(minimum);
        }
        var desired = new List<(Carrier Target, CarrierLocation Value)>();
        if (Pass(goodBlock) || Pass(badBlock))
        {
            IElement delay = new Placeholder();
            foreach (var step in ladder)
            {
                var claim = OperationClaim.Equality(new Carrier(), Copy(newRung), Parts(step)[0], new Carrier());
                evidence.Add(claim);
                if (Pass(claim)) { delay = Parts(step)[1]; break; }
            }
            if (delay is not CarrierLocation selectedDelay)
                return G(state, source, G(evidence.ToArray()), Relation.UnresolvedRead(newRung, Policy(state), Rung(state)));
            var deadline = Calculate(Clock(state), evidence, (Copy(opportunity), 1), (Copy(selectedDelay), 1));
            desired.Add((Rung(state), newRung)); desired.Add((Deadline(state), deadline));
        }
        var latest = Parts(LastBlock(state));
        desired.Add(((Carrier)latest[0], count)); desired.Add(((Carrier)latest[1], sum));
        desired.Add(((Carrier)latest[2], Copy(opportunity)));
        desired.Add((BlockCount(state), L(BlockCount(state), 0)));
        desired.Add((BlockBenefit(state), L(BlockBenefit(state), 0)));
        var settlementInputs = desired.Select(change => (IElement)Calculate(change.Target, evidence,
            (change.Value, 1), (change.Target.ValueSlot, -1), (change.Target.LiveTensionSlot, -1))).ToList();
        settlementInputs.Add(Calculate(CompletedBlocks(state), evidence, (L(CompletedBlocks(state), 1), 1)));
        evidence.Add(Carrier.ReceiveTogether(G(settlementInputs.ToArray())));
        var completedWitness = G(Copy(count), Copy(sum), Copy(opportunity), oldRung, oldDeadline,
            Copy(Rung(state).ValueSlot), RetryAfter(state));
        return G(state, source, Copy(delta), G(evidence.ToArray()), completedWitness);
    }

    private static Relation ReadMean(Relation state, Carrier count, IElement numeratorRecipe)
    {
        var evidence = new List<IElement>();
        var numerator = N.Read(numeratorRecipe); evidence.Add(numerator);
        if (!Equation.TryReadPrediction(numerator, out var source, out _))
            return Relation.UnresolvedRead(state, numerator, LossBasis(state));
        var denominator = Calculate(LossBasis(state), evidence, (count.ValueSlot, 1), (count.LiveTensionSlot, 1));
        var observed = OperationClaim.AtLeast(new Carrier(), denominator, L(LossBasis(state), 1), new Carrier());
        evidence.Add(observed);
        var frame = new Relation(L(LossBasis(state), 0), Copy(denominator));
        var equation = Equation.Affine(frame, new Relation(L(LossBasis(state), 0), L(LossBasis(state), 1)));
        evidence.Add(equation);
        if (!Pass(observed) ||
            !Equation.TryPredict(equation, source, LossBasis(state), out var mean))
            return Relation.UnresolvedRead(G(state, G(evidence.ToArray())), frame, LossBasis(state));
        return new Relation(mean.Left, G(mean.Right, Copy(denominator), G(evidence.ToArray())));
    }

    private static Relation SignedRecipe(Carrier source, Carrier target) =>
        N.SumTo(target, (source.ValueSlot, 1), (source.LiveTensionSlot, 1));
    private static CarrierLocation Calculate(Carrier target, List<IElement> evidence, params (IElement Value, long Coefficient)[] terms)
    {
        var equation = N.SumTo(target, terms); var read = N.Read(equation); evidence.Add(G(equation, read)); return Value(read);
    }
    private static CarrierLocation Value(Relation read) => Equation.TryReadPrediction(read, out var value, out _)
        ? Copy(value) : throw new InvalidOperationException("The supplied retry arithmetic basis is unresolved.");
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    private static CarrierLocation L(Carrier carrier, long ticks) => new(carrier, ticks);
    private static CarrierLocation Copy(CarrierLocation value) => L(value.Carrier, value.Ticks);
    private static IElement[] Parts(Relation group) => N.Parts(group);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
