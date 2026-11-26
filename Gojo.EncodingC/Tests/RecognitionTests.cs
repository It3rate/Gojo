using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Unified Polynomial + Recognition tests combining exact BigInteger
/// vanishing-ideal nullspace recovery (any partition weight including 5, 10,
/// and 60), hidden free-residue auto-discovery, min/max fold fits, walls and
/// mutated corners as exceptions with MDL, wall-aware minimax probing,
/// closed-loop learning on incomplete LearnedTables, trace-based mechanism
/// discrimination, and decoupled observational Flows.Sweep.</summary>
public sealed class RecognitionTests
{
    private static readonly Shape Bit = Shape.Bit;
    private static readonly Shape Three = Shape.Unary(3);
    private static readonly Shape Six = Shape.Unary(6);
    private static readonly Shape Nine = Shape.Unary(9);
    private static readonly Plane Here = Plane.Of(1, 0);

    private static IEnumerable<int[]> Grid(int maxA, int maxB, Func<int, int, int> f) =>
        from a in Enumerable.Range(0, maxA + 1)
        from b in Enumerable.Range(0, maxB + 1)
        select new[] { a, b, f(a, b) };

    [Fact]
    public void PolynomialIsCanonicalNumbersAndLensDescribesIt()
    {
        var heap = Polynomial.Balance([Three, Three, Six], 1, 1, -1);
        Assert.True(new Polynomial([Three, Three, Six], (2, [1, 0, 0]), (1, [0, 1, 0]), (1, [0, 1, 0]), (-2, [0, 0, 1]), (0, [1, 1, 0])).Same(heap));
        Assert.True(new Polynomial([Three, Three, Six], (-1, [1, 0, 0]), (-1, [0, 1, 0]), (1, [0, 0, 1])).Same(heap));
        Assert.Equal((1, 0L), (heap.Degree, heap.Value([2, 3, 5])));

        var product = Polynomial.Product(3, 3);
        Assert.Equal(2, product.Degree);
        Assert.True(product.Relation.SameTable(RelationLaw.FromPredicate(product.Roles, v => v[0] * v[1] == v[2])));

        var lens = new Lens();
        Assert.Equal("a + b − c = 0", lens.Describe(heap));
        Assert.Equal("ab − c = 0", lens.Describe(product));
        Assert.Equal("a + b + c − 5d − e = 0", lens.Describe(Numbers.DigitSum(5)));
        lens.Name(heap, 0, "have").Name(heap, 1, "short").Name(heap, 2, "want");
        Assert.Equal("have + short − want = 0", lens.Describe(heap));
    }

    [Fact]
    public void ExactNullspaceRecoversUndirectedEquationsIncludingBase5Base10AndBase60()
    {
        var sum = Recognition.Relations([Three, Three, Six], Recognition.Witnesses(Counts.Sum(3, 3), [0, 1, -1], [-1, -1, 2]));
        Assert.True(Assert.Single(sum).Same(Polynomial.Balance([Three, Three, Six], 1, 1, -1)));

        var two = Shape.Unary(2);
        var weighted = Recognition.Relations([Three, two, Shape.Unary(7)], Recognition.Witnesses(Counts.Sum(3, 2, 2), [0, 1, -1], [-1, -1, 2]));
        Assert.Equal([1, 2, -1], Assert.Single(weighted).Terms.Select(t => t.Weight));

        // Simultaneous half-adder over (a, b, a&b, a^b): a + b - 2c - d = 0.
        var bits = from a in new[] { 0, 1 } from b in new[] { 0, 1 } select new[] { a, b, a & b, a ^ b };
        Assert.Equal([1, 1, -2, -1], Assert.Single(Recognition.Relations([Bit, Bit, Bit, Bit], bits)).Terms.Select(t => t.Weight));

        // Arbitrary partitions recover directly as exact integer weights: 5, 10, and 60 (BigInteger nullspace never overflows!).
        foreach (var partition in new[] { 5, 10, 60 })
        {
            var d = Numbers.Digit(partition, unary: false);
            var c = Numbers.Carry(unary: false);
            var tuples = from a in Enumerable.Range(0, partition)
                         from b in Enumerable.Range(0, partition)
                         from cin in new[] { 0, 1 }
                         select new[] { a, b, cin, (a + b + cin) / partition, (a + b + cin) % partition };
            var recovered = Assert.Single(Recognition.Relations([d, d, c, c, d], tuples));
            Assert.True(recovered.Same(Numbers.DigitSum(partition, unary: false)));
        }

        // Degree-2 product ab - c = 0.
        var times = Grid(3, 3, (a, b) => a * b).ToArray();
        Assert.Empty(Recognition.Relations([Three, Three, Nine], times, maxDegree: 1));
        Assert.True(Assert.Single(Recognition.Relations([Three, Three, Nine], times)).Same(Polynomial.Product(3, 3)));
    }

    [Fact]
    public void HiddenFreeResidueDiscoveryRecoversShortfallClampedOrAndTighterInPlaceAccumulate()
    {
        // Shortfall(3, 3): auto-discovers the hidden free excess role (capacity 3) with exact SameTable!
        var sf = Recognition.DiscoverBoundBalance(Counts.Shortfall(3, 3));
        Assert.NotNull(sf);
        Assert.True(sf!.ExactTableMatch);
        Assert.True(sf.FreeRole >= 0);
        Assert.Equal(3, sf.FreeCapacity);
        Assert.True(sf.CompressesByMdl);

        // Separate-output clamped bit OR (a, b -> min(a+b, 1)): genuinely needs a 1-bit free overflow role!
        var bitOr = TransitionLaw.FromFunction([Bit, Bit, Bit], s => [s[0], s[1], (s[0] | s[1])],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var orFit = Recognition.DiscoverBoundBalance(bitOr);
        Assert.NotNull(orFit);
        Assert.True(orFit!.ExactTableMatch);
        Assert.True(orFit.FreeRole >= 0);
        Assert.Equal(1, orFit.FreeCapacity);

        // In-place Accumulate(4): holding at capacity equals clamping, so 0-residue fit is exact & tighter by MDL!
        var accFit = Recognition.DiscoverBoundBalance(Counts.Accumulate(4));
        Assert.NotNull(accFit);
        Assert.True(accFit!.ExactTableMatch);
        Assert.Equal(-1, accFit.FreeRole);
        Assert.True(accFit.CompressesByMdl);

        // Unsigned live-landmark shear with blocked bit: auto-detects blocked role and recovers exact balance!
        var shear = Numbers.LandmarkShear(6, weight: 1, withBlocked: true);
        var shearFit = Recognition.DiscoverBoundBalance(shear);
        Assert.NotNull(shearFit);
        Assert.True(shearFit!.ExactTableMatch);
        Assert.Equal(3, shearFit.BlockedRole);
        Assert.True(shearFit.CompressesByMdl);
    }

    [Fact]
    public void WallsWrongCornersAndMinMaxFoldsSeparateCleanlyFromIrregularNoise()
    {
        // 1. Walls are exceptions, not new laws: Accumulate(3) and kick shear.
        var accW = Recognition.Witnesses(Counts.Accumulate(3), [1, 0, -1], [-1, -1, 1]);
        var accFit = Recognition.Fits([Three, Bit, Three], 2, accW, maxDegree: 1, maxExceptions: 1)[0];
        Assert.True(accFit.Equation.Same(Polynomial.Balance([Three, Bit, Three], 1, 1, -1)));
        Assert.Equal([3, 1, 3], accW[Assert.Single(accFit.Exceptions)]);

        // 2. Mutated corner (3,2) -> 5: rejects exact fit, compresses to ab + 1 exception with huge MDL savings.
        var mutated = TransitionLaw.FromFunction([Three, Three, Nine],
            s => [s[0], s[1], s[0] == 3 && s[1] == 2 ? 5 : s[0] * s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var mutW = Recognition.Witnesses(mutated, [0, 1, -1], [-1, -1, 2]);
        Assert.Empty(Recognition.Fits([Three, Three, Nine], 2, mutW, maxTerms: 2));
        var mutFit = Recognition.Fits([Three, Three, Nine], 2, mutW, maxTerms: 2, maxExceptions: 1)[0];
        Assert.True(mutFit.Equation.Same(Polynomial.Product(3, 3)));
        Assert.Equal([3, 2, 5], mutW[Assert.Single(mutFit.Exceptions)]);
        var cmp = Recognition.Compare(mutated, mutFit.Equation, [0, 1, -1], [-1, -1, 2]);
        Assert.False(cmp.Same);
        Assert.Equal(10, cmp.Differences);
        Assert.True(cmp.EquationBits * 50 < cmp.TableBits);

        // 3. Non-polynomial min/max folds (Claude Round 14 wishlist): cleanly discovered by FitsFold!
        var minTable = Grid(3, 3, (a, b) => a <= b ? a : b).ToArray();
        var maxTable = Grid(3, 3, (a, b) => a >= b ? a : b).ToArray();
        Assert.Empty(Recognition.Fits([Three, Three, Three], 2, minTable, maxTerms: 2, maxDegree: 2, maxWeight: 2));
        var minFit = Assert.Single(Recognition.FitsFold([Three, Three, Three], 2, minTable));
        Assert.False(minFit.IsMax);
        Assert.Empty(minFit.Exceptions);
        var maxFit = Assert.Single(Recognition.FitsFold([Three, Three, Three], 2, maxTable));
        Assert.True(maxFit.IsMax);
        Assert.Empty(maxFit.Exceptions);

        // 4. Irregular noise stays a table (no short polynomial or fold fit).
        int[] noise = [3, 0, 5, 1, 6, 2, 4, 0, 1, 6, 0, 3, 2, 5, 6, 1];
        var noisy = Grid(3, 3, (a, b) => noise[4 * a + b]).ToArray();
        Assert.Empty(Recognition.Relations([Three, Three, Six], noisy));
        Assert.Empty(Recognition.Fits([Three, Three, Six], 2, noisy, maxTerms: 2, maxWeight: 3));
        Assert.Empty(Recognition.FitsFold([Three, Three, Six], 2, noisy));
    }

    [Fact]
    public void ActiveProbingClosesLoopOnIncompleteLearnedTableWithWallAwarenessAndTraces()
    {
        // 1. Two witnesses (0,0)->0, (2,2)->4 fit both sum and product; minimax Probe isolates ab in <= 3 questions!
        Shape[] roles = [Three, Three, Nine];
        var cands = Recognition.Fits(roles, 2, [[0, 0, 0], [2, 2, 4]], maxTerms: 2, maxWeight: 2).Select(f => f.Equation).ToList();
        Assert.Contains(cands, c => c.Same(Polynomial.Balance(roles, 1, 1, -1)));
        Assert.Contains(cands, c => c.Same(Polynomial.Product(3, 3)));
        var questions = 0;
        while (Recognition.Probe(cands, 2) is { } input)
        {
            var answer = input[0] * input[1];
            cands = [.. cands.Where(c => Recognition.Predict(c, 2, input) == answer)];
            questions++;
        }
        Assert.InRange(questions, 1, 3);
        Assert.Contains(cands, c => c.Same(Polynomial.Product(3, 3)));

        // 2. Closed-loop active discovery on an INCOMPLETE LearnedTable (>50% unknown) WITH wall-aware probing!
        // Book law: debt' = debt + rate, holding at the wall debt + rate > 9.
        var book = Operations.Bind(Operations.Balance([Nine, Three, Nine], 1, 1, -1), [Three, Nine], [1, 0, -1], [-1, -1, 1]);
        var table = new LearnedTable(book.Roles);
        void Run(int rate, int debt, int ticks)
        {
            var store = new Store(0, 1);
            Field r = new(Three, store.AddPlane(), store.AddPlane(), store.AddPlane());
            Field d = new(Nine, [.. Enumerable.Range(0, 9).Select(_ => store.AddPlane())]);
            r.Write(store, 0, rate);
            d.Write(store, 0, debt);
            var engine = new Engine().Add(Tension.Wire(book, [r.On(store), d.On(store)], Here));
            for (var tick = 0; tick < ticks; tick++) table.Witness(engine.Step().Steps[0], Here);
        }

        Shape[] bookRoles = [Three, Nine, Nine];
        int[] reads = [0, 1, -1], writes = [-1, -1, 1];
        Run(rate: 1, debt: 0, ticks: 3);
        var bookCands = Recognition.Fits(bookRoles, 2, Recognition.Witnesses(table, reads, writes), maxTerms: 2, maxWeight: 2)
            .Select(f => f.Equation).ToList();
        Assert.True(bookCands.Count > 1);

        // Wall-aware probe steers away from boundary holds (debt + rate > 9) so holds never falsely eliminate the true law!
        var experiments = 0;
        while (Recognition.Probe(bookCands, 2, isWall: v => v[0] + v[1] > 9) is { } probe)
        {
            Run(probe[0], probe[1], ticks: 1);
            var w = Recognition.Witnesses(table, reads, writes);
            bookCands = [.. bookCands.Where(c => w.All(x => c.Holds(x)))];
            experiments++;
        }
        Assert.InRange(experiments, 1, 3);
        var discovered = Assert.Single(bookCands);
        Assert.True(discovered.Same(Polynomial.Balance(bookRoles, 1, 1, -1)));
        Assert.True(Recognition.Compare(book, discovered, reads, writes).Same);
        Assert.True(table.UnknownCount > table.StateCount * table.StateCount / 2);

        // 3. Traces distinguish coasting (debt' = debt + rate) from stretching (out = gain · source),
        // and a single probe distinguishes a captured repeat from a live-sampled repeat!
        Shape[] repeatRoles = [Nine, Three, Three, Nine];
        var sampled = Operations.Bind(Operations.Balance([Nine, Three, Nine], 1, 1, -1), [Nine, Three, Three], [0, 2, -1], [-1, -1, 0]);
        var quiet = Recognition.Witnesses(sampled, [0, 1, 2, -1], [-1, -1, -1, 0])
            .Where(t => t[1] == t[2] && t[0] + t[2] <= 8).ToArray();
        var repCands = Recognition.Fits(repeatRoles, 3, quiet, maxTerms: 2, maxDegree: 1, maxWeight: 1).Select(f => f.Equation).ToList();
        var probeRep = Recognition.Probe(repCands, 3, isWall: v => v[0] + v[1] > 9 || v[0] + v[2] > 9);
        Assert.NotNull(probeRep);
        Assert.NotEqual(probeRep![1], probeRep[2]); // probe immediately asks where captured != live source!
        var ansRep = sampled.Tuple(sampled.Afters(sampled.TupleIndex([probeRep[0], probeRep[1], probeRep[2]]))[0])[0];
        var survivor = Assert.Single(repCands, c => Recognition.Predict(c, 3, probeRep) == ansRep);
        Assert.True(survivor.Same(new Polynomial(repeatRoles, (1, [1, 0, 0, 0]), (1, [0, 0, 1, 0]), (-1, [0, 0, 0, 1]))));
    }

    [Fact]
    public void ObservationalSweepReceiptNeverHoldsConservedMaterial()
    {
        var store = new Store(0, 1);
        Field U(int cap, int val = 0)
        {
            var f = new Field(Shape.Unary(cap), [.. Enumerable.Range(0, cap).Select(_ => store.AddPlane())]);
            f.Write(store, 0, val);
            return f;
        }
        var src = U(3, 3);
        var dst = U(3, 0);
        var other = U(4, 4);
        var receipt = U(5, 0); // smaller than 3 * 4 = 12 area!
        var eng = new Engine().Add(Tension.Wire(Flows.Sweep(3, 3, 4, 5), [src.On(store), dst.On(store), other.On(store), receipt.On(store)], Here));
        Assert.NotNull(eng.Settle(10));
        Assert.Equal(0, src.Read(store, 0));
        Assert.Equal(3, dst.Read(store, 0));
        Assert.Equal(5, receipt.Read(store, 0));
    }
}
