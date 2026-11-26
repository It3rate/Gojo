using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Equations as numbers, and finding them (ops_claude/GojoOperationsProposal-2026-11.md,
/// §3 and §4). A polynomial is a few integers in canonical form; recognition
/// runs Bind backward, from tuples to the equation that keeps them: exactly
/// from a full table, sparsely from a few witnesses, with the question that
/// tells candidates apart, and with what an equation does not explain kept as
/// exceptions rather than mistaken for a new law.</summary>
public sealed class RecognitionTests
{
    private static readonly Shape Three = Shape.Unary(3), Six = Shape.Unary(6), Nine = Shape.Unary(9), Bit = Shape.Bit;

    private static int[] Next(TransitionLaw law, params int[] state) =>
        law.Tuple(Assert.Single(law.Afters(law.TupleIndex(state))));

    private static IEnumerable<int[]> Grid(int a, int b, Func<int, int, int> c) =>
        from x in Enumerable.Range(0, a + 1) from y in Enumerable.Range(0, b + 1) select new[] { x, y, c(x, y) };

    /// <summary>The same relation written different ways is one set of numbers:
    /// equal monomials merge, a common factor and an overall sign come out, and
    /// zero terms vanish. Balance and product are its cases and keep their
    /// tables; the text is a Lens read, with any names the Lens gives.</summary>
    [Fact]
    public void APolynomialIsCanonicalNumbers()
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
        lens.Name(heap, 0, "have").Name(heap, 1, "short").Name(heap, 2, "want");
        Assert.Equal("have + short − want = 0", lens.Describe(heap));
    }

    /// <summary>From full tables, the lowest-degree relations come back
    /// exactly, unique and with no direction. Master's sum gives a + b − c, the
    /// weighted sum a + 2b − c, a truth table of AND and XOR beside their
    /// inputs gives the half adder's balance, a decimal column gives the radix
    /// as a weight, and a times table needs degree 2 to give ab − c.</summary>
    [Fact]
    public void FullTablesGiveTheirRelationsBack()
    {
        var sum = Recognition.Relations([Three, Three, Six], Recognition.Witnesses(Counts.Sum(3, 3), [0, 1, -1], [-1, -1, 2]));
        Assert.True(Assert.Single(sum).Same(Polynomial.Balance([Three, Three, Six], 1, 1, -1)));

        var two = Shape.Unary(2);
        var weighted = Recognition.Relations([Three, two, Shape.Unary(7)], Recognition.Witnesses(Counts.Sum(3, 2, 2), [0, 1, -1], [-1, -1, 2]));
        Assert.Equal([1, 2, -1], Assert.Single(weighted).Terms.Select(t => t.Weight));

        var bits = from a in new[] { 0, 1 } from b in new[] { 0, 1 } select new[] { a, b, a & b, a ^ b };
        Assert.Equal([1, 1, -2, -1], Assert.Single(Recognition.Relations([Bit, Bit, Bit, Bit], bits)).Terms.Select(t => t.Weight));

        Shape digit = Numbers.Digit(10), carry = Numbers.Carry;
        var column = from a in Enumerable.Range(0, 10) from b in Enumerable.Range(0, 10) from c in new[] { 0, 1 }
                     select new[] { a, b, c, (a + b + c) / 10, (a + b + c) % 10 };
        Assert.True(Assert.Single(Recognition.Relations([digit, digit, carry, carry, digit], column)).Same(Numbers.DigitSum(10)));

        var times = Grid(3, 3, (a, b) => a * b).ToArray();
        Assert.Empty(Recognition.Relations([Three, Three, Nine], times, maxDegree: 1));
        Assert.True(Assert.Single(Recognition.Relations([Three, Three, Nine], times)).Same(Polynomial.Product(3, 3)));
    }

    /// <summary>Where a law holds at a wall, the equation is what it does
    /// elsewhere and the wall is its exceptions. Accumulate is count + signal
    /// except where a full count holds; the oscillator's kick is v + L − x
    /// except exactly where that leaves the track.</summary>
    [Fact]
    public void AWallIsAnExceptionNotAnotherLaw()
    {
        var accumulate = Recognition.Witnesses(Counts.Accumulate(3), [1, 0, -1], [-1, -1, 1]);
        var fit = Recognition.Fits([Three, Bit, Three], 2, accumulate, maxDegree: 1, maxExceptions: 1)[0];
        Assert.True(fit.Equation.Same(Polynomial.Balance([Three, Bit, Three], 1, 1, -1)));
        Assert.Equal([3, 1, 3], accumulate[Assert.Single(fit.Exceptions)]);

        var count = Shape.Unary(8);
        var kick = Operations.Bind(Operations.Balance([count, count, count, count], 1, 1, -1, -1), [count, count, count], [0, 1, 2, -1], [-1, -1, -1, 0]);
        var witnesses = Recognition.Witnesses(kick, [0, 1, 2, -1], [-1, -1, -1, 0]);
        var walls = witnesses.Count(t => t[0] + t[1] - t[2] is < 0 or > 8);
        var best = Recognition.Fits([count, count, count, count], 3, witnesses, maxTerms: 3, maxDegree: 1, maxExceptions: walls)[0];
        Assert.True(best.Equation.Same(Polynomial.Balance([count, count, count, count], 1, 1, -1, -1)));
        Assert.Equal(walls, best.Exceptions.Count);
        Assert.All(best.Exceptions, i => Assert.True(witnesses[i][3] == witnesses[i][0] && witnesses[i][0] + witnesses[i][1] - witnesses[i][2] is < 0 or > 8));
    }

    /// <summary>A table with no regularity has no short equation, exact or
    /// sparse; it stays a table.</summary>
    [Fact]
    public void AnIrregularTableStaysATable()
    {
        int[] noise = [3, 0, 5, 1, 6, 2, 4, 0, 1, 6, 0, 3, 2, 5, 6, 1];
        var table = Grid(3, 3, (a, b) => noise[4 * a + b]).ToArray();
        Assert.Empty(Recognition.Relations([Three, Three, Six], table));
        Assert.Empty(Recognition.Fits([Three, Three, Six], 2, table, maxTerms: 2, maxWeight: 3));
    }

    /// <summary>A times table with one wrong corner, (3, 2) read as 5 (from
    /// ops_sol): no short equation keeps it exactly; with one exception allowed
    /// it is ab, the corner is the exception, and the law against ab bound the
    /// same way differs in the ten before-states at that corner. The equation
    /// and its exceptions are a small fraction of the table's bits.</summary>
    [Fact]
    public void OneWrongEntryIsAnExceptionNotANewLaw()
    {
        var law = TransitionLaw.FromFunction([Three, Three, Nine], s => [s[0], s[1], s[0] == 3 && s[1] == 2 ? 5 : s[0] * s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var witnesses = Recognition.Witnesses(law, [0, 1, -1], [-1, -1, 2]);
        Assert.Empty(Recognition.Fits([Three, Three, Nine], 2, witnesses, maxTerms: 2));

        var fit = Recognition.Fits([Three, Three, Nine], 2, witnesses, maxTerms: 2, maxExceptions: 1)[0];
        Assert.True(fit.Equation.Same(Polynomial.Product(3, 3)));
        Assert.Equal([3, 2, 5], witnesses[Assert.Single(fit.Exceptions)]);

        var comparison = Recognition.Compare(law, fit.Equation, [0, 1, -1], [-1, -1, 2]);
        Assert.False(comparison.Same);
        Assert.Equal(10, comparison.Differences);
        Assert.True(comparison.EquationBits * 50 < comparison.TableBits);
    }

    /// <summary>Two witnesses, (0, 0) → 0 and (2, 2) → 4, fit a sum and a
    /// product alike, and other short equations besides (from ops_sol's
    /// discovery proposal). Each time, the structure asks the input whose
    /// answer splits the candidates most, and the hidden times table answers.
    /// In three questions or fewer, only equations that agree with ab on every
    /// input are left.</summary>
    [Fact]
    public void AQuestionTellsASumFromAProduct()
    {
        Shape[] roles = [Three, Three, Nine];
        var candidates = Recognition.Fits(roles, 2, [[0, 0, 0], [2, 2, 4]], maxTerms: 2, maxWeight: 2).Select(f => f.Equation).ToList();
        Assert.Contains(candidates, c => c.Same(Polynomial.Balance(roles, 1, 1, -1)));
        Assert.Contains(candidates, c => c.Same(Polynomial.Product(3, 3)));

        var questions = 0;
        while (Recognition.Probe(candidates, 2) is { } input)
        {
            var answer = input[0] * input[1];
            candidates = [.. candidates.Where(c => Recognition.Predict(c, 2, input) == answer)];
            questions++;
        }
        Assert.InRange(questions, 1, 3);
        Assert.Contains(candidates, c => c.Same(Polynomial.Product(3, 3)));
        foreach (var (a, b) in from a in Enumerable.Range(0, 4) from b in Enumerable.Range(0, 4) select (a, b))
            Assert.All(candidates, c => Assert.Equal(a * b, Recognition.Predict(c, 2, [a, b, -1])));
    }

    /// <summary>The same products, made two ways, leave different traces.
    /// Coasting books a rate onto a debt once a tick; its steps fit debt' =
    /// debt + rate. Stretching makes the product at once; its steps fit
    /// out = gain · source. Neither trace fits the other's equation: the
    /// configuration is evidence of the mechanism, not only of the result.</summary>
    [Fact]
    public void TracesTellCoastingFromStretching()
    {
        var here = Plane.Of(1, 0);
        var coasting = new List<int[]>();
        var book = Operations.Bind(Operations.Balance([Nine, Three, Nine], 1, 1, -1), [Three, Nine], [1, 0, -1], [-1, -1, 1]);
        for (var rate = 0; rate <= 3; rate++)
        {
            var store = new Store(0, 1);
            Field r = new(Three, store.AddPlane(), store.AddPlane(), store.AddPlane()), debt = new(Nine, [.. Enumerable.Range(0, 9).Select(_ => store.AddPlane())]);
            r.Write(store, 0, rate);
            var engine = new Engine().Add(Tension.Wire(book, [r.On(store), debt.On(store)], here));
            for (var tick = 0; tick < 3; tick++)
            {
                var before = debt.Read(store, 0)!.Value;
                engine.Step();
                coasting.Add([rate, before, debt.Read(store, 0)!.Value]);
            }
        }

        var stretching = new List<int[]>();
        var stretch = Operations.Bind(Operations.Product(3, 3), [Three, Three, Nine], [0, 1, -1], [-1, -1, 2]);
        for (var gain = 0; gain <= 3; gain++)
            for (var source = 0; source <= 3; source++)
            {
                var store = new Store(0, 1);
                Field g = new(Three, store.AddPlane(), store.AddPlane(), store.AddPlane()), s = new(Three, store.AddPlane(), store.AddPlane(), store.AddPlane()),
                    o = new(Nine, [.. Enumerable.Range(0, 9).Select(_ => store.AddPlane())]);
                g.Write(store, 0, gain);
                s.Write(store, 0, source);
                new Engine().Add(Tension.Wire(stretch, [g.On(store), s.On(store), o.On(store)], here)).Step();
                stretching.Add([gain, source, o.Read(store, 0)!.Value]);
            }

        Shape[] coastRoles = [Three, Nine, Nine], stretchRoles = [Three, Three, Nine];
        Assert.True(Recognition.Fits(coastRoles, 2, coasting)[0].Equation.Same(Polynomial.Balance(coastRoles, 1, 1, -1)));
        Assert.True(Recognition.Fits(stretchRoles, 2, stretching)[0].Equation.Same(Polynomial.Product(3, 3)));
        Assert.DoesNotContain(Recognition.Fits(coastRoles, 2, coasting), f => f.Equation.Degree == 2 && f.Equation.Terms.Count == 2);
        Assert.DoesNotContain(Recognition.Fits(stretchRoles, 2, stretching), f => f.Equation.Degree == 1);
    }

    /// <summary>A repeat that captured its sample and one that reads the source
    /// each time agree while the source does not move (from ops_sol). Watched
    /// only then, both fit: total' = total + captured and total' = total +
    /// source. The probe asks for an input where captured and source differ,
    /// the hidden law answers, and one candidate is left: the one that reads
    /// the source.</summary>
    [Fact]
    public void AQuestionTellsACapturedRepeatFromASampledOne()
    {
        Shape[] roles = [Nine, Three, Three, Nine];
        var sampled = Operations.Bind(Operations.Balance([Nine, Three, Nine], 1, 1, -1), [Nine, Three, Three], [0, 2, -1], [-1, -1, 0]);
        var quiet = Recognition.Witnesses(sampled, [0, 1, 2, -1], [-1, -1, -1, 0]).Where(t => t[1] == t[2] && t[0] + t[2] <= 8).ToArray();
        var candidates = Recognition.Fits(roles, 3, quiet, maxTerms: 2, maxDegree: 1, maxWeight: 1).Select(f => f.Equation).ToList();
        Polynomial captured = new(roles, (1, [1, 0, 0, 0]), (1, [0, 1, 0, 0]), (-1, [0, 0, 0, 1])), source = new(roles, (1, [1, 0, 0, 0]), (1, [0, 0, 1, 0]), (-1, [0, 0, 0, 1]));
        Assert.Contains(candidates, c => c.Same(captured));
        Assert.Contains(candidates, c => c.Same(source));

        var input = Recognition.Probe(candidates, 3);
        Assert.NotNull(input);
        Assert.NotEqual(input![1], input[2]);
        var answer = Next(sampled, input[0], input[1], input[2])[0];
        var left = candidates.Where(c => Recognition.Predict(c, 3, input) == answer).ToList();
        Assert.True(Assert.Single(left).Same(source));
    }
}
