using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Equations as numbers, and finding them (Gojo/docs/GojoOperationsSynthesis-2026-11.md,
/// §3.2 and §3.5). A polynomial is a few integers in canonical form; recognition
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
        Assert.True(Assert.Single(sum.System).Same(Polynomial.Balance([Three, Three, Six], 1, 1, -1)));
        Assert.True(sum.Checked && sum.Unexplained.Count == 0);

        var two = Shape.Unary(2);
        var weighted = Recognition.Relations([Three, two, Shape.Unary(7)], Recognition.Witnesses(Counts.Sum(3, 2, 2), [0, 1, -1], [-1, -1, 2]));
        Assert.Equal([1, 2, -1], Assert.Single(weighted.System).Terms.Select(t => t.Weight));

        var bits = from a in new[] { 0, 1 } from b in new[] { 0, 1 } select new[] { a, b, a & b, a ^ b };
        Assert.Equal([1, 1, -2, -1], Assert.Single(Recognition.Relations([Bit, Bit, Bit, Bit], bits).System).Terms.Select(t => t.Weight));

        Shape digit = Numbers.Digit(10), carry = Numbers.Carry;
        var column = from a in Enumerable.Range(0, 10) from b in Enumerable.Range(0, 10) from c in new[] { 0, 1 }
                     select new[] { a, b, c, (a + b + c) / 10, (a + b + c) % 10 };
        Assert.True(Assert.Single(Recognition.Relations([digit, digit, carry, carry, digit], column).System).Same(Numbers.DigitSum(10)));

        var times = Grid(3, 3, (a, b) => a * b).ToArray();
        Assert.Empty(Recognition.Relations([Three, Three, Nine], times, maxDegree: 1).System);
        Assert.True(Assert.Single(Recognition.Relations([Three, Three, Nine], times).System).Same(Polynomial.Product(3, 3)));
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
        Assert.Empty(Recognition.Relations([Three, Three, Six], table).System);
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

    /// <summary>The loop from settling to an equation, on a table that is
    /// never complete. A booking machine runs at a rate of 1, and a learned
    /// table counts what it saw. Those witnesses fit several equations, debt'
    /// = debt + 1 among them. The probe proposes a state the machine has not
    /// been in; running it there is the experiment, and the table counts that
    /// too. When no question is left, the one equation standing is the
    /// machine's own law, exactly, while most of the table is still unknown.</summary>
    [Fact]
    public void ALearnedTableBecomesAnEquationBeforeItIsComplete()
    {
        var here = Plane.Of(1, 0);
        var book = Operations.Bind(Operations.Balance([Nine, Three, Nine], 1, 1, -1), [Three, Nine], [1, 0, -1], [-1, -1, 1]);
        var table = new LearnedTable(book.Roles);
        void Run(int rate, int debt, int ticks)
        {
            var store = new Store(0, 1);
            Field r = new(Three, store.AddPlane(), store.AddPlane(), store.AddPlane()), d = new(Nine, [.. Enumerable.Range(0, 9).Select(_ => store.AddPlane())]);
            r.Write(store, 0, rate);
            d.Write(store, 0, debt);
            var engine = new Engine().Add(Tension.Wire(book, [r.On(store), d.On(store)], here));
            for (var tick = 0; tick < ticks; tick++) table.Witness(engine.Step().Steps[0], here);
        }

        Shape[] roles = [Three, Nine, Nine];
        int[] reads = [0, 1, -1], writes = [-1, -1, 1];
        Run(rate: 1, debt: 0, ticks: 3);
        var candidates = Recognition.Fits(roles, 2, Recognition.Witnesses(table, reads, writes), maxTerms: 2, maxWeight: 2).Select(f => f.Equation).ToList();
        Assert.True(candidates.Count > 1);
        Assert.Contains(candidates, c => c.Same(new Polynomial(roles, (1, [0, 1, 0]), (1, [0, 0, 0]), (-1, [0, 0, 1]))));

        var experiments = 0;
        while (Recognition.Probe(candidates, 2) is { } input)
        {
            Run(input[0], input[1], ticks: 1);
            var witnesses = Recognition.Witnesses(table, reads, writes);
            candidates = [.. candidates.Where(c => witnesses.All(w => c.Holds(w)))];
            experiments++;
        }
        Assert.InRange(experiments, 1, 3);
        var law = Assert.Single(candidates);
        Assert.True(law.Same(Polynomial.Balance(roles, 1, 1, -1)));
        Assert.True(Recognition.Compare(book, law, reads, writes).Same);
        Assert.True(table.UnknownCount > table.StateCount * table.StateCount / 2);
    }

    /// <summary>The lowest-degree equation can be weaker than the data (from
    /// ops_sol's review). Four bit tuples keep d = a and c = a·b. At degree 1
    /// only d − a = 0 holds, and it admits the forbidden (1, 1, 0, 1). The
    /// system adds the degree-2 equation that rules that out, and then
    /// describes the tuples exactly: nothing it admits went unwitnessed.</summary>
    [Fact]
    public void ASystemIsAsTightAsTheDataAndSaysWhereItIsNot()
    {
        int[][] tuples = [[0, 0, 0, 0], [0, 1, 0, 0], [1, 0, 0, 1], [1, 1, 1, 1]];
        var lowest = Recognition.Relations([Bit, Bit, Bit, Bit], tuples, maxDegree: 1);
        Assert.True(Assert.Single(lowest.System).Holds([1, 1, 0, 1]));
        Assert.Contains(lowest.Unexplained, t => t.SequenceEqual([1, 1, 0, 1]));

        var found = Recognition.Relations([Bit, Bit, Bit, Bit], tuples);
        Assert.True(found.Checked);
        Assert.Empty(found.Unexplained);
        Assert.True(found.System.Count >= 2);
        Assert.False(found.System.All(e => e.Holds([1, 1, 0, 1])));
    }

    /// <summary>Some laws are a balance only with a role they do not show (the
    /// hidden residue, adapted from ops_neon). Shortfall is have − want + short
    /// − excess = 0 with the excess kept as small as it can be; accumulate is
    /// count + signal − count' − overflow = 0; a clamped OR is a + b − or −
    /// overflow = 0. Each is found, and rebuilds its law exactly.</summary>
    [Fact]
    public void AHiddenResidueRoleIsFound()
    {
        var shortfall = Recognition.Residue(Counts.Shortfall(3, 3), [0, 1, -1], [-1, -1, 2]);
        Assert.NotNull(shortfall);
        Assert.True(shortfall!.Equation.Same(Polynomial.Balance([Three, Three, Three, Three], 1, -1, 1, -1)));

        var accumulate = Recognition.Residue(Counts.Accumulate(3), [1, 0, -1], [-1, -1, 1]);
        Assert.NotNull(accumulate);
        Assert.True(accumulate!.Equation.Same(Polynomial.Balance([Three, Bit, Three, Shape.Unary(1)], 1, 1, -1, -1)));

        var or = Operations.Bind(Operations.Balance([Bit, Bit, Bit, Bit], 1, 1, -1, -1), [Bit, Bit, Bit], [0, 1, -1, -1], [-1, -1, 2, -1], cost: [0, 0, 0, 1]);
        var clamped = Recognition.Residue(or, [0, 1, -1], [-1, -1, 2]);
        Assert.NotNull(clamped);
        Assert.True(Recognition.Compare(or, clamped!.Equation, clamped.Reads, clamped.Writes, clamped.Cost).Same);

        Assert.Null(Recognition.Residue(TransitionLaw.FromFunction([Three, Three, Six], v => [v[0], v[1], (v[0] * 5 + v[1] * 3) % 7],
            new Accounting(true, new Unchanged(0), new Unchanged(1))), [0, 1, -1], [-1, -1, 2], maxCapacity: 3));
    }

    /// <summary>Piecewise linear bounds need no fold kind or enum
    /// (GojoOperationsSynthesis-2026-11.md §7, item 7). Two hidden residue
    /// roles with unit cost recover <c>min(a, b)</c> as <c>a = c + e</c> and
    /// <c>b = c + f</c>, <c>max(a, b)</c> as <c>c = a + e</c> and
    /// <c>c = b + f</c>, and <c>|a − b|</c> as <c>c = a − b + e</c> and
    /// <c>c = b − a + f</c>, restoring each law's table and accounting exactly.
    /// An irregular table still returns null.</summary>
    [Fact]
    public void TwoHiddenResidueRolesRecoverMinMaxAndAbsoluteDifference()
    {
        var keepInputs = new Accounting(true, new Unchanged(0), new Unchanged(1));
        var minLaw = TransitionLaw.FromFunction([Three, Three, Three], v => [v[0], v[1], v[0] < v[1] ? v[0] : v[1]], keepInputs);
        var maxLaw = TransitionLaw.FromFunction([Three, Three, Three], v => [v[0], v[1], v[0] > v[1] ? v[0] : v[1]], keepInputs);
        var distLaw = TransitionLaw.FromFunction([Three, Three, Three], v => [v[0], v[1], v[0] >= v[1] ? v[0] - v[1] : v[1] - v[0]], keepInputs);

        // None of these is a single hidden residue balance, yet all three are exact two-residue balance systems.
        Assert.Null(Recognition.Residue(minLaw, [0, 1, -1], [-1, -1, 2], maxWeight: 1, maxCapacity: 3));
        Assert.Null(Recognition.Residue(maxLaw, [0, 1, -1], [-1, -1, 2], maxWeight: 1, maxCapacity: 3));

        var foundMin = Recognition.Residues(minLaw, [0, 1, -1], [-1, -1, 2], hiddenRoles: 2, maxWeight: 1, maxCapacity: 3);
        Assert.NotNull(foundMin);
        Assert.Equal(2, foundMin!.System.Count);
        Assert.Equal([0, 0, 0, 1, 1], foundMin.Cost);
        var minCompare = Recognition.Compare(minLaw, foundMin.System, foundMin.Reads, foundMin.Writes, foundMin.Cost);
        Assert.True(minCompare.Same && minCompare.KeepsAccounting && minCompare.EquationBits * 10 < minCompare.TableBits);
        Assert.True(Recognition.Restore(minLaw, foundMin.System, foundMin.Reads, foundMin.Writes, foundMin.Cost).SameTable(minLaw));

        var foundMax = Recognition.Residues(maxLaw, [0, 1, -1], [-1, -1, 2], hiddenRoles: 2, maxWeight: 1, maxCapacity: 3);
        Assert.NotNull(foundMax);
        Assert.Equal(2, foundMax!.System.Count);
        var maxCompare = Recognition.Compare(maxLaw, foundMax.System, foundMax.Reads, foundMax.Writes, foundMax.Cost);
        Assert.True(maxCompare.Same && maxCompare.KeepsAccounting);
        Assert.True(Recognition.Restore(maxLaw, foundMax.System, foundMax.Reads, foundMax.Writes, foundMax.Cost).SameTable(maxLaw));

        var foundDist = Recognition.Residues(distLaw, [0, 1, -1], [-1, -1, 2], hiddenRoles: 2, maxWeight: 1, maxCapacity: 6);
        Assert.NotNull(foundDist);
        Assert.Equal(2, foundDist!.System.Count);
        var distCompare = Recognition.Compare(distLaw, foundDist.System, foundDist.Reads, foundDist.Writes, foundDist.Cost);
        Assert.True(distCompare.Same && distCompare.KeepsAccounting);
        Assert.True(Recognition.Restore(distLaw, foundDist.System, foundDist.Reads, foundDist.Writes, foundDist.Cost).SameTable(distLaw));

        var noise = TransitionLaw.FromFunction([Three, Three, Three], v => [v[0], v[1], (v[0] * 3 + v[1] * 2 + 1) % 4], keepInputs);
        Assert.Null(Recognition.Residues(noise, [0, 1, -1], [-1, -1, 2], hiddenRoles: 2, maxWeight: 1, maxCapacity: 4));
    }

    /// <summary>Bits alone are not the law, and promises alone are not either
    /// (from ops_sol's reviews). The sum's equation rebuilds the sum's table,
    /// and Restore attaches the sum's accounting, so the rebuilt law makes the
    /// same promises. An equation that keeps those promises but behaves
    /// differently is refused. Bound to copy a move's source into its target,
    /// an equation breaks the move's preserved sum: the comparison says so,
    /// and Restore refuses.</summary>
    [Fact]
    public void ARestoredLawKeepsTheOriginalsPromises()
    {
        var sum = Counts.Sum(3, 3);
        var heap = Polynomial.Balance([Three, Three, Six], 1, 1, -1);
        Assert.True(Recognition.Compare(sum, heap, [0, 1, -1], [-1, -1, 2]).KeepsAccounting);
        var restored = Recognition.Restore(sum, heap, [0, 1, -1], [-1, -1, 2]);
        Assert.Same(sum.Accounting, restored.Accounting);
        Assert.True(restored.SameTable(sum));

        // Keeping the promises is not enough: c = a keeps both inputs unchanged
        // as the sum does, but behaves differently, and is refused (from ops_sol).
        var echo = new Polynomial([Three, Three, Six], (1, [1, 0, 0]), (-1, [0, 0, 1]));
        var echoed = Recognition.Compare(sum, echo, [0, 1, -1], [-1, -1, 2]);
        Assert.True(echoed.KeepsAccounting);
        Assert.False(echoed.Same);
        Assert.Throws<ArgumentException>(() => Recognition.Restore(sum, echo, [0, 1, -1], [-1, -1, 2]));

        var move = Flows.Move(3, 3);
        var copy = Polynomial.Balance([Three, Three], 1, -1);
        var compared = Recognition.Compare(move, copy, [0, -1], [-1, 1]);
        Assert.False(compared.Same);
        Assert.False(compared.KeepsAccounting);
        Assert.Throws<ArgumentException>(() => Recognition.Restore(move, copy, [0, -1], [-1, 1]));
    }

    [Fact]
    public void ABlockedBitSurvivesExactRebuildingAndSeparatesHoldsFromWitnesses()
    {
        var amount = Shape.Unary(2);
        var doubleAmount = Polynomial.Balance([amount, amount], 2, -1);
        int[] reads = [0, -1], writes = [-1, 0];
        var law = Operations.Bind(doubleAmount, [amount, Bit], reads, writes, blocked: 1);

        Assert.Equal([2, 1], Next(law, 2, 0));
        Assert.Equal([2, 0], Next(law, 1, 1));
        var withoutBlocked = Recognition.Compare(law, doubleAmount, reads, writes);
        Assert.False(withoutBlocked.Same);
        Assert.True(withoutBlocked.KeepsAccounting);
        Assert.Throws<ArgumentException>(() => Recognition.Restore(law, doubleAmount, reads, writes));
        var comparison = Recognition.Compare(law, doubleAmount, reads, writes, blocked: 1);
        Assert.True(comparison.Same);
        Assert.Equal(0, comparison.Differences);
        Assert.True(comparison.KeepsAccounting);
        var restored = Recognition.Restore(law, doubleAmount, reads, writes, blocked: 1);
        Assert.True(law.SameTable(restored));
        Assert.Same(law.Accounting, restored.Accounting);

        var learned = new LearnedTable(law.Roles);
        for (var before = 0; before < law.StateCount; before++)
            learned.Witness(before, Assert.Single(law.Afters(before)));
        Assert.Contains(Recognition.Witnesses(law, reads, writes), row => row.SequenceEqual([2, 2]));
        var authored = Recognition.Witnesses(law, reads, writes, blocked: 1);
        Assert.Equal(new int[][] { [0, 0], [1, 2] }, authored);
        Assert.Equal(authored, Recognition.Witnesses(learned, reads, writes, blocked: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Recognition.Witnesses(law, [0, -2], writes));
        Assert.Throws<ArgumentOutOfRangeException>(() => Recognition.Witnesses(learned, [0, -2], writes));
        Assert.Throws<ArgumentOutOfRangeException>(() => Recognition.Witnesses(law, reads, [-1, 2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Recognition.Witnesses(learned, reads, [-1, 2]));
    }

    /// <summary>A wall is not evidence against a law. A booking at a full debt
    /// holds, so the witness (rate 3, debt 8) → 8 disagrees with debt' = debt +
    /// rate. With no exception allowed the true law would be ruled out; with
    /// the wall counted as one exception it survives, and a law that is wrong
    /// elsewhere still does not.</summary>
    [Fact]
    public void AWallInTheWitnessesDoesNotRuleOutTheLaw()
    {
        Shape[] roles = [Three, Nine, Nine];
        Polynomial law = Polynomial.Balance(roles, 1, 1, -1), wrong = new(roles, (1, [0, 1, 0]), (1, [0, 0, 0]), (-1, [0, 0, 1]));
        int[][] witnesses = [[1, 0, 1], [2, 2, 4], [3, 4, 7], [3, 8, 8]];
        Assert.Empty(Recognition.Survivors([law, wrong], 2, witnesses));
        Assert.True(Assert.Single(Recognition.Survivors([law, wrong], 2, witnesses, maxExceptions: 1)).Same(law));
    }

    /// <summary>A domain too large to enumerate is never mistaken for a small
    /// one (from Sol's and Neon's final assessments). Four roles of 65,536
    /// values make 2^64 tuples, a product that wraps to zero in 64 bits:
    /// the system is returned unchecked rather than checked over nothing, and a
    /// question over that domain is refused rather than answered with none.</summary>
    [Fact]
    public void ADomainTooLargeToEnumerateIsNeverCheckedOverNothing()
    {
        var wide = Shape.Binary(65536);
        Shape[] roles = [wide, wide, wide, wide];
        var found = Recognition.Relations(roles, [[1, 2, 3, 6], [2, 3, 4, 9]], maxDegree: 1);
        Assert.False(found.Checked);
        Assert.NotEmpty(found.System);

        Shape[] question = [wide, wide, wide, wide, Shape.Bit];
        Polynomial same = new(question, (1, [1, 0, 0, 0, 0]), (-1, [0, 0, 0, 0, 1])), zero = new(question, (1, [0, 0, 0, 0, 1]));
        Assert.Throws<ArgumentException>(() => Recognition.Probe([same, zero], 4));
    }
}
