using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeCoverageTests
{
    [Fact]
    public void NoneOneAndManyRetainTheActualCoverageRowsAndCausalWitnesses()
    {
        var cells = NewCells(3);
        var first = new Relation(new Placeholder(), new Placeholder());
        var second = new Relation(new Placeholder(), new Placeholder());
        var none = Row(cells[0]); var one = Row(cells[1], first); var many = Row(cells[2], first, second);
        var f = new QualitativeCoverage(S(cells), S(), S(none, one, many));
        var read = Read(f);

        Assert.Same(none, Assert.Single(E(read.Empty)));
        Assert.Same(one, Assert.Single(E(read.Single)));
        Assert.Same(many, Assert.Single(E(read.Multiple)));
        Assert.True(Contains(f.Surface, first)); Assert.True(Contains(f.Surface, second));
        Assert.Same(f.Surface, f.Evaluate().Left);
        AssertNoNumbers(f.Surface);
        AssertNoNumbers(f.Evaluate());
    }

    [Fact]
    public void RepeatedCausalIdentityIsNotCountedAsFreshEvidence()
    {
        var cell = new Placeholder(); var witness = new Relation(new Placeholder(), new Placeholder());
        var repeated = Row(cell, witness, witness, witness);
        var f = new QualitativeCoverage(S(cell), S(), S(repeated));
        var read = Read(f);
        Assert.Same(repeated, Assert.Single(E(read.Single))); Assert.Empty(E(read.Multiple));
        Assert.Equal(3, E(Assert.IsType<Relation>(repeated.Right)).Length);
    }

    [Fact]
    public void EqualLookingFootprintsAreDistinctParticipationWithoutProvingRetracing()
    {
        var cell = new Placeholder(); var a = new Placeholder(); var b = new Placeholder();
        var strokeA = new Relation(a, b); var strokeB = new Relation(a, b);
        var row = Row(cell, strokeA, strokeB);
        var f = new QualitativeCoverage(S(cell), S(), S(row));
        var read = Read(f);
        Assert.Same(row, Assert.Single(E(read.Multiple)));
        Assert.NotSame(strokeA, strokeB);
        Assert.Same(strokeA, E(Assert.IsType<Relation>(row.Right))[0]);
        Assert.Same(strokeB, E(Assert.IsType<Relation>(row.Right))[1]);
        // The coarse collision offers two causal footprints. It contains no
        // correspondence asserting exact path equality or independence.
        AssertNoNumbers(read.Multiple);
    }

    [Fact]
    public void MissingAndOpenRowsNeverBecomeEmptyCells()
    {
        var cells = NewCells(3);
        var missing = Row(cells[0]);
        var incomplete = new Relation(cells[1], StructuralConstraint.Open(new Placeholder()));
        var f = new QualitativeCoverage(S(cells), Chain(cells), S(missing, incomplete));
        var read = Read(f);
        Assert.Same(missing, Assert.Single(E(read.Empty)));
        Assert.Empty(E(read.Single)); Assert.Empty(E(read.Multiple));
        Assert.Empty(E(read.Layers)); Assert.Empty(E(read.Centers));
        Assert.True(Contains(read.Unresolved, cells[1])); Assert.True(Contains(read.Unresolved, cells[2]));
    }

    [Fact]
    public void OpenOuterCoverageDoesNotCertifyExactOccupancy()
    {
        var cell = new Placeholder(); var row = Row(cell);
        var f = new QualitativeCoverage(S(cell), S(), StructuralConstraint.Open(row));
        var read = Read(f);
        Assert.Empty(E(read.Empty)); Assert.Empty(E(read.Single)); Assert.Empty(E(read.Multiple));
        Assert.NotEmpty(E(read.Unresolved)); Assert.Empty(E(read.Centers));
    }

    [Fact]
    public void MultiSourceShellsChooseTheNearestDeclaredOccupiedLayer()
    {
        var cells = NewCells(7);
        var f = new QualitativeCoverage(S(cells), Chain(cells), Complete(cells, 0, 6));
        var read = Read(f); var layers = E(read.Layers).Cast<Relation>().ToArray();
        Assert.Equal(4, layers.Length);
        AssertMembers(f, layers[0], cells[0], cells[6]);
        AssertMembers(f, layers[1], cells[1], cells[5]);
        AssertMembers(f, layers[2], cells[2], cells[4]);
        AssertMembers(f, layers[3], cells[3]);
        Assert.True(f.TryReadLayer(layers[0], out var root, out _)); Assert.Empty(E(Assert.IsType<Relation>(root)));
        for (var i = 1; i < layers.Length; i++)
        {
            Assert.True(f.TryReadLayer(layers[i], out var prior, out _)); Assert.Same(layers[i - 1], prior);
        }
        var center = Assert.IsType<Relation>(Assert.Single(E(read.Centers)));
        Assert.True(f.TryReadCenter(center, out var selected, out var plateau));
        Assert.Same(layers[3], selected); Assert.Same(cells[3], Assert.Single(E(plateau)));
        Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void CenterCanBeAnEntireConnectedPlateauWithoutAnArbitraryMidpointChoice()
    {
        var cells = NewCells(6);
        var f = new QualitativeCoverage(S(cells), Chain(cells), Complete(cells, 0, 5));
        var read = Read(f);
        var center = Assert.IsType<Relation>(Assert.Single(E(read.Centers)));
        Assert.True(f.TryReadCenter(center, out var layer, out var plateau));
        Assert.Equal(new[] { cells[2], cells[3] }, E(plateau));
        AssertMembers(f, layer, cells[2], cells[3]);
        Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void SeparateSameLayerLocalMaximaRemainDistinctCenters()
    {
        var cells = NewCells(5);
        // Star: occupied center with four empty leaves. All leaves have the
        // same ordinal clearance but no same-layer edge joins their plateaus.
        var edges = new List<IElement>();
        foreach (var leaf in cells.Skip(1))
        { edges.Add(new Relation(cells[0], leaf)); edges.Add(new Relation(leaf, cells[0])); }
        var f = new QualitativeCoverage(S(cells), S(edges.ToArray()), Complete(cells, 0));
        var read = Read(f);
        Assert.Equal(4, E(read.Centers).Length);
        var selectedLayers = new List<Relation>();
        foreach (var center in E(read.Centers).Cast<Relation>())
        {
            Assert.True(f.TryReadCenter(center, out var layer, out var members));
            Assert.Single(E(members)); selectedLayers.Add(layer);
        }
        Assert.All(selectedLayers, layer => Assert.Same(selectedLayers[0], layer));
    }

    [Fact]
    public void DirectedAdjacencyIsNotSilentlyReversed()
    {
        var cells = NewCells(2);
        var coverage = Complete(cells, 1);
        var oneWay = new QualitativeCoverage(S(cells), S(new Relation(cells[0], cells[1])), coverage);
        var first = Read(oneWay);
        Assert.Single(E(first.Layers)); Assert.Empty(E(first.Centers)); Assert.NotEmpty(E(first.Unresolved));
        var twoWay = new QualitativeCoverage(S(cells), Chain(cells), coverage);
        var second = Read(twoWay);
        Assert.Equal(2, E(second.Layers).Length); Assert.Single(E(second.Centers)); Assert.Empty(E(second.Unresolved));
    }

    [Fact]
    public void PeriodicNeighborCyclesTerminateAndKeepTheNearestOrdinalShell()
    {
        var cells = NewCells(6);
        var chain = E(Chain(cells)).ToList();
        chain.Add(new Relation(cells[^1], cells[0])); chain.Add(new Relation(cells[0], cells[^1]));
        var f = new QualitativeCoverage(S(cells), S(chain.ToArray()), Complete(cells, 0));
        var read = Read(f); var layers = E(read.Layers).Cast<Relation>().ToArray();
        Assert.Equal(4, layers.Length);
        AssertMembers(f, layers[1], cells[1], cells[5]); AssertMembers(f, layers[2], cells[2], cells[4]);
        AssertMembers(f, layers[3], cells[3]); Assert.Single(E(read.Centers)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void UnknownPotentialSourcesInvalidateOnlyClearanceTheyCouldShorten()
    {
        var cells = NewCells(7);
        var rows = E(Complete(cells, 0)).Take(6).ToArray(); // Last cell's coverage is unknown.
        var f = new QualitativeCoverage(S(cells), Chain(cells), S(rows));
        var read = Read(f); var layers = E(read.Layers).Cast<Relation>().ToArray();
        Assert.Equal(4, layers.Length);
        AssertMembers(f, layers[0], cells[0]); AssertMembers(f, layers[1], cells[1]);
        AssertMembers(f, layers[2], cells[2]); AssertMembers(f, layers[3], cells[3]);
        Assert.Equal(5, E(read.Empty).Length);
        Assert.Empty(E(read.Centers));
        Assert.True(Contains(read.Unresolved, cells[4])); Assert.True(Contains(read.Unresolved, cells[5]));
        Assert.True(Contains(read.Unresolved, cells[6]));
    }

    [Fact]
    public void ClosedEmptyUnreachableComponentsAreNotAssignedZeroClearanceOrCenters()
    {
        var cells = NewCells(4);
        var edges = S(new Relation(cells[1], cells[2]), new Relation(cells[2], cells[1]));
        var f = new QualitativeCoverage(S(cells), edges, Complete(cells, 0));
        var read = Read(f);
        Assert.Equal(3, E(read.Empty).Length); Assert.Single(E(read.Layers)); Assert.Empty(E(read.Centers));
        Assert.True(Contains(read.Unresolved, cells[1])); Assert.True(Contains(read.Unresolved, cells[2]));
        Assert.True(Contains(read.Unresolved, cells[3]));
        var entirelyEmpty = new QualitativeCoverage(S(cells), Chain(cells), Complete(cells));
        var emptyRead = Read(entirelyEmpty);
        Assert.Equal(4, E(emptyRead.Empty).Length); Assert.Empty(E(emptyRead.Layers)); Assert.Empty(E(emptyRead.Centers));
        Assert.NotEmpty(E(emptyRead.Unresolved));
    }

    [Fact]
    public void OpenOrEscapingTopologyDoesNotCertifyFiniteEmptyClearance()
    {
        var cells = NewCells(2);
        foreach (var adjacency in new[]
        {
            StructuralConstraint.Open(new Relation(cells[0], cells[1])),
            S(new Relation(cells[0], cells[1]), new Relation(cells[1], new Placeholder()))
        })
        {
            var f = new QualitativeCoverage(S(cells), adjacency, Complete(cells, 0));
            var read = Read(f);
            Assert.Single(E(read.Empty)); Assert.Single(E(read.Single));
            Assert.Single(E(read.Layers)); Assert.Empty(E(read.Centers)); Assert.NotEmpty(E(read.Unresolved));
        }
    }

    [Fact]
    public void DuplicateCoverageClaimsAreUnknownAndDuplicateCellDomainsAreRejected()
    {
        var cell = new Placeholder(); var a = Row(cell); var b = Row(cell, new Placeholder());
        var f = new QualitativeCoverage(S(cell), S(), S(a, b));
        var read = Read(f);
        Assert.Empty(E(read.Empty)); Assert.Empty(E(read.Single)); Assert.Empty(E(read.Multiple));
        Assert.True(Contains(read.Unresolved, a)); Assert.True(Contains(read.Unresolved, b));
        var duplicateCells = new QualitativeCoverage(S(cell, cell), S(), S(a));
        var duplicateRead = Read(duplicateCells);
        Assert.Empty(E(duplicateRead.Empty)); Assert.Empty(E(duplicateRead.Layers)); Assert.NotEmpty(E(duplicateRead.Unresolved));
    }

    [Fact]
    public void ReadersRequireTheExactSourceFrameAndAdmittedLayerMembership()
    {
        var cells = NewCells(3); var coverage = Complete(cells, 0);
        var f = new QualitativeCoverage(S(cells), Chain(cells), coverage);
        var witness = f.Evaluate(); var read = Read(f);
        var other = new QualitativeCoverage(f.Cells, f.Adjacency, f.Coverage);
        Assert.False(other.TryReadResult(witness, out _, out _, out _, out _, out _, out _));
        Assert.False(f.TryReadResult(new Relation(new Placeholder(), new Placeholder()), out _, out _, out _, out _, out _, out _));
        var layer = Assert.IsType<Relation>(E(read.Layers)[1]);
        var foreignMember = new Relation(layer.Left, S(new Placeholder()));
        Assert.False(f.TryReadLayer(foreignMember, out _, out _));
        var foreignCenter = new Relation(new Relation(f.Surface, layer), S(new Placeholder()));
        Assert.False(f.TryReadCenter(foreignCenter, out _, out _));
        var payload = Assert.IsType<Relation>(witness.Right);
        var forgedClassifications = new Relation(witness.Left,
            new Relation(new Relation(S(), new Relation(read.Empty, S())), payload.Right));
        Assert.False(f.TryReadResult(forgedClassifications, out _, out _, out _, out _, out _, out _));
    }

    private static (Relation Empty, Relation Single, Relation Multiple, Relation Layers, Relation Centers, Relation Unresolved) Read(QualitativeCoverage f)
    {
        Assert.True(f.TryReadResult(f.Evaluate(), out var empty, out var single, out var multiple,
            out var layers, out var centers, out var unresolved));
        return (empty, single, multiple, layers, centers, unresolved);
    }
    private static void AssertMembers(QualitativeCoverage f, Relation layer, params IElement[] expected)
    {
        Assert.True(f.TryReadLayer(layer, out _, out var members)); Assert.Equal(expected, E(members));
    }
    private static IElement[] NewCells(int count) => Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
    private static Relation Complete(IElement[] cells, params int[] occupied) =>
        S(cells.Select((cell, i) => (IElement)(occupied.Contains(i) ? Row(cell, new Placeholder()) : Row(cell))).ToArray());
    private static Relation Row(IElement cell, params IElement[] witnesses) => new(cell, S(witnesses));
    private static Relation Chain(IElement[] cells)
    {
        var edges = new List<IElement>();
        for (var i = 1; i < cells.Length; i++)
        { edges.Add(new Relation(cells[i - 1], cells[i])); edges.Add(new Relation(cells[i], cells[i - 1])); }
        return S(edges.ToArray());
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : throw new InvalidOperationException("Expected scope.");
    private static bool Contains(IElement root, IElement target)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (ReferenceEquals(node, target)) return true;
            if (!seen.Add(node) || node is not Relation pair) continue;
            pending.Push(pair.Left); pending.Push(pair.Right);
        }
        return false;
    }
    private static void AssertNoNumbers(IElement root)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node)) continue;
            Assert.IsNotType<Carrier>(node); Assert.IsNotType<CarrierLocation>(node); Assert.IsNotType<TickRef>(node);
            if (node is Relation pair) { pending.Push(pair.Left); pending.Push(pair.Right); }
        }
    }
}
