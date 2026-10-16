using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeWeaveTests
{
    [Fact]
    public void AuthoredFieldsProveRoleExchangeWithoutAnyMeasuredSourceParticipants()
    {
        foreach (var weave in new[] { QualitativeWeave.SteppedChevron(), QualitativeWeave.NestedFret(), QualitativeWeave.FoldedBlocks() })
        {
            Assert.DoesNotContain(Traverse(weave.Surface), value => value is Carrier or CarrierLocation);
            Assert.True(weave.Partition.TryReadExchangePassed(weave.EvaluateHalfShift()));
            Assert.True(weave.Partition.TryReadResult(weave.Partition.Evaluate(), out var first, out var second,
                out var regions, out var interfaces, out var pending));
            Assert.Equal(E(first).Length, E(second).Length); Assert.Empty(E(pending));
            Assert.NotEmpty(E(regions)); Assert.NotEmpty(E(interfaces)); Assert.Empty(E(weave.GenerationUnresolved));
            Assert.True(Contains(weave.Surface, weave.SeedRows)); Assert.True(Contains(weave.Surface, weave.ExchangeLaw));
            Assert.NotSame(weave.ColumnPitch, weave.RowPitch);
        }
    }

    [Fact]
    public void EverySecondHalfLabelRetainsTheActualFiniteLawReadAndSourceCellCorrespondence()
    {
        var first = new Placeholder(); var second = new Placeholder();
        var a = S(first, second, first); var b = S(second, second, first);
        var weave = new QualitativeWeave(S(a, b), first, second);
        Assert.Equal(6, E(weave.GenerationEvidence).Length);
        var actualCells = E(weave.Cells);
        var labels = E(weave.Partition.Labels).Cast<Relation>().ToArray();
        foreach (var item in E(weave.GenerationEvidence).Cast<Relation>())
        {
            var selected = Assert.IsType<Relation>(item.Left);
            var seed = Assert.IsType<Relation>(selected.Left); var correspondence = Assert.IsType<Relation>(selected.Right);
            var read = Assert.IsType<Relation>(item.Right);
            Assert.True(ReferenceEquals(seed.Left, a) || ReferenceEquals(seed.Left, b));
            Assert.NotSame(correspondence.Left, correspondence.Right);
            Assert.Contains(actualCells, cell => ReferenceEquals(cell, correspondence.Left));
            Assert.Contains(actualCells, cell => ReferenceEquals(cell, correspondence.Right));
            Assert.True(Contains(read, weave.ExchangeLaw));
            Assert.True(FiniteLaw.TryReadResult(read, out var output, out var unresolved, out var consumed));
            Assert.Empty(E(unresolved)); Assert.Single(E(consumed));
            var produced = Assert.Single(E(output));
            var targetLabel = Assert.Single(labels, label => ReferenceEquals(label.Left, correspondence.Right));
            Assert.Same(produced, targetLabel.Right);
            var originalLabel = Assert.Single(labels, label => ReferenceEquals(label.Left, correspondence.Left));
            Assert.Same(seed.Right, originalLabel.Right); Assert.NotSame(originalLabel.Right, targetLabel.Right);
        }
    }

    [Fact]
    public void HalfShiftPreservesActualDirectedSeamNeighborsAndExposedRowEndpoints()
    {
        var first = new Placeholder(); var second = new Placeholder();
        var weave = new QualitativeWeave(S(S(first, second), S(second, first), S(first, first)), first, second);
        var columns = E(weave.Columns); var rows = E(weave.Rows);
        var coordinates = E(weave.Coordinates).Cast<Relation>().ToArray();
        IElement Cell(int x, int y) => Assert.Single(coordinates, clause => clause.Right is Relation pair &&
            ReferenceEquals(pair.Left, columns[x]) && ReferenceEquals(pair.Right, rows[y])).Left;
        var edges = E(weave.Adjacency).Cast<Relation>().ToArray();
        for (var y = 0; y < rows.Length; y++)
        {
            Assert.Contains(edges, edge => ReferenceEquals(edge.Left, Cell(columns.Length - 1, y)) && ReferenceEquals(edge.Right, Cell(0, y)));
            for (var x = 0; x < columns.Length; x++)
            {
                if (y + 1 < rows.Length)
                    Assert.Contains(edges, edge => ReferenceEquals(edge.Left, Cell(x, y)) && ReferenceEquals(edge.Right, Cell(x, y + 1)));
                else Assert.DoesNotContain(edges, edge => ReferenceEquals(edge.Left, Cell(x, y)) && ReferenceEquals(edge.Right, Cell(x, 0)));
            }
        }
        Assert.Equal(columns.Length, E(weave.ColumnOrder).Length);
        Assert.Equal(rows.Length - 1, E(weave.RowOrder).Length);
        Assert.True(weave.Partition.TryReadExchangePassed(weave.EvaluateHalfShift()));
    }

    [Fact]
    public void RepeatedStepsSharePitchIdentityWithoutClaimingASquareOrAnArea()
    {
        var weave = QualitativeWeave.SteppedChevron();
        var horizontal = E(weave.ColumnOrder); var vertical = E(weave.RowOrder);
        var bindings = E(weave.PitchBindings).Cast<Relation>().ToArray();
        Assert.All(horizontal, edge => Assert.Same(weave.ColumnPitch,
            Assert.Single(bindings, binding => ReferenceEquals(binding.Left, edge)).Right));
        Assert.All(vertical, edge => Assert.Same(weave.RowPitch,
            Assert.Single(bindings, binding => ReferenceEquals(binding.Left, edge)).Right));
        Assert.NotSame(weave.ColumnPitch, weave.RowPitch);
        Assert.True(Contains(weave.Surface, weave.ColumnPitch)); Assert.True(Contains(weave.Surface, weave.RowPitch));
    }

    [Fact]
    public void HalfShiftIsAFixedPointFreeInvolutionOnActualCells()
    {
        var weave = QualitativeWeave.NestedFret(); var map = E(weave.HalfShift).Cast<Relation>().ToArray();
        Assert.Equal(E(weave.Cells).Length, map.Length);
        foreach (var row in map)
        {
            Assert.NotSame(row.Left, row.Right);
            Assert.Same(row.Left, Assert.Single(map, next => ReferenceEquals(next.Left, row.Right)).Right);
        }
        var other = QualitativeWeave.NestedFret();
        Assert.False(weave.Partition.TryReadExchangePassed(weave.Partition.EvaluateExchange(other.HalfShift)));
    }

    [Fact]
    public void UnknownSeedRoleRetainsItsFailedLocalReadWithoutInventingItsPartnerColor()
    {
        var first = new Placeholder(); var second = new Placeholder(); var unknown = new Placeholder();
        var weave = new QualitativeWeave(S(S(first, unknown)), first, second);
        var pendingGeneration = E(weave.GenerationEvidence).Cast<Relation>().Single(item =>
            item.Left is Relation { Left: Relation seed } && ReferenceEquals(seed.Right, unknown));
        var correspondence = (Relation)((Relation)pendingGeneration.Left).Right;
        Assert.DoesNotContain(E(weave.Partition.Labels).Cast<Relation>(), label => ReferenceEquals(label.Left, correspondence.Right));
        Assert.True(FiniteLaw.TryReadResult((Relation)pendingGeneration.Right, out var output, out var residue, out _));
        Assert.Empty(E(output)); Assert.NotEmpty(E(residue)); Assert.True(Open(output));
        Assert.NotEmpty(E(weave.GenerationUnresolved)); Assert.True(Contains(weave.Surface, unknown));
        Assert.False(weave.Partition.TryReadExchangePassed(weave.EvaluateHalfShift()));
    }

    [Fact]
    public void OpenOrRaggedMasksKeepAnOpenFieldRatherThanGuessingACylinder()
    {
        var first = new Placeholder(); var second = new Placeholder();
        foreach (var seed in new[] { StructuralConstraint.Open(S(first, second)),
            S(StructuralConstraint.Open(first, second)), S(S(first), S(first, second)), S() })
        {
            var weave = new QualitativeWeave(seed, first, second);
            Assert.True(Open(weave.Cells)); Assert.True(Open(weave.Columns)); Assert.True(Open(weave.Adjacency));
            Assert.Empty(E(weave.Partition.Labels)); Assert.Empty(E(weave.GenerationEvidence));
            Assert.NotEmpty(E(weave.GenerationUnresolved)); Assert.True(Contains(weave.Surface, seed));
            Assert.False(weave.Partition.TryReadExchangePassed(weave.EvaluateHalfShift()));
        }
    }

    [Fact]
    public void AVisibleIdentityLawCannotImpersonateTheRequiredRoleExchange()
    {
        var first = new Placeholder(); var second = new Placeholder(); var pair = new Relation(first, second);
        var identityLaw = FiniteLaw.Create(S(first, second), S(pair), S(first, second),
            FiniteLaw.Row(first, pair, S(first)), FiniteLaw.Row(second, pair, S(second)));
        var weave = new QualitativeWeave(S(S(first, second)), pair, identityLaw);
        Assert.Empty(E(weave.GenerationUnresolved));
        var read = weave.EvaluateHalfShift();
        Assert.True(weave.Partition.TryReadExchange(read, out _, out _, out var unmatched, out var pending));
        Assert.NotEmpty(E(unmatched)); Assert.Empty(E(pending)); Assert.False(weave.Partition.TryReadExchangePassed(read));
        Assert.True(Contains(weave.Surface, identityLaw));
    }

    [Fact]
    public void MissingLawRowsAndAliasedRolesCannotMakeACompleteExchangeProof()
    {
        var first = new Placeholder(); var second = new Placeholder(); var pair = new Relation(first, second);
        var incomplete = FiniteLaw.Create(S(first, second), S(pair), S(first, second), FiniteLaw.Row(first, pair, S(second)));
        var missing = new QualitativeWeave(S(S(first, second)), pair, incomplete);
        Assert.NotEmpty(E(missing.GenerationUnresolved)); Assert.False(missing.Partition.TryReadExchangePassed(missing.EvaluateHalfShift()));
        var aliased = new QualitativeWeave(S(S(first)), first, first);
        Assert.NotEmpty(E(aliased.GenerationUnresolved)); Assert.False(aliased.Partition.TryReadExchangePassed(aliased.EvaluateHalfShift()));
    }

    [Fact]
    public void SingleRowFieldKeepsItsUnusedVerticalPitchAndNoInventedVerticalWrap()
    {
        var first = new Placeholder(); var second = new Placeholder();
        var weave = new QualitativeWeave(S(S(first, second)), first, second);
        Assert.Single(E(weave.Rows)); Assert.Empty(E(weave.RowOrder));
        Assert.Equal(E(weave.Columns).Length, E(weave.Adjacency).Length);
        Assert.True(Contains(weave.Surface, weave.RowPitch));
        Assert.True(weave.Partition.TryReadExchangePassed(weave.EvaluateHalfShift()));
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Open(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && !seam.Equals(tail);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var value))
        {
            if (!seen.Add(value)) continue; yield return value;
            if (value is Relation relation) { stack.Push(relation.Right); stack.Push(relation.Left); }
            else if (value is CarrierLocation location) stack.Push(location.Carrier);
        }
    }
}
