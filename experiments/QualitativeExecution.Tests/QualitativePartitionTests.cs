using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativePartitionTests
{
    [Fact]
    public void BothRolesAreActualSymmetricOccupiedParticipation()
    {
        var f = new Fixture(false, false, true, true);
        var read = Read(f.Partition);
        Assert.Equal(f.Labels.Take(2), E(read.First)); Assert.Equal(f.Labels.Skip(2), E(read.Second));
        Assert.Empty(E(read.Pending));
        var reversed = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, f.Pair.Reverse(), f.Partition.Labels);
        var reverseRead = Read(reversed);
        Assert.Equal(E(read.First), E(reverseRead.Second)); Assert.Equal(E(read.Second), E(reverseRead.First));
        Assert.All(f.Labels, label => Assert.True(Contains(f.Partition.Surface, label)));
        AssertNoNumbers(f.Partition.Surface); AssertNoNumbers(f.Partition.Evaluate());
    }

    [Fact]
    public void OneActualOrderedInterfaceParticipatesInBothRegionBoundaries()
    {
        var f = new Fixture(false, true);
        var read = Read(f.Partition);
        Assert.Equal(2, E(read.Regions).Length);
        Assert.Equal(2, E(read.Interfaces).Length);
        var boundary = Assert.IsType<Relation>(E(read.Interfaces)[0]);
        Assert.True(f.Partition.TryReadInterface(boundary, out var edge, out var from, out var to));
        Assert.Same(f.Labels[0], from); Assert.Same(f.Labels[1], to);
        Assert.Same(f.Cells[0], edge.Left); Assert.Same(f.Cells[1], edge.Right);
        foreach (var region in E(read.Regions).Cast<Relation>())
        {
            Assert.True(f.Partition.TryReadRegion(region, out var role, out var members, out var edges));
            Assert.Single(E(members));
            Assert.Contains(E(edges), item => ReferenceEquals(item, boundary));
            Assert.True(ReferenceEquals(role, f.A) || ReferenceEquals(role, f.B));
        }
    }

    [Fact]
    public void EqualRoleCountsDoNotEstablishExchangeUnderTheIdentityMap()
    {
        var f = new Fixture(false, false, false, false, true, true, true, true);
        var partition = Read(f.Partition);
        Assert.Equal(E(partition.First).Length, E(partition.Second).Length);
        var map = f.Map(i => i); var exchange = Exchange(f.Partition, map);
        Assert.False(f.Partition.TryReadExchangePassed(exchange.Read));
        Assert.NotEmpty(E(exchange.Unmatched)); Assert.Empty(E(exchange.Pending));
        Assert.True(Contains(exchange.Unmatched, E(map)[0]));
    }

    [Fact]
    public void ExplicitHalfShiftIsABijectiveTopologyPreservingRoleExchange()
    {
        var f = new Fixture(false, false, false, false, true, true, true, true);
        var map = f.Map(i => (i + 4) % f.Cells.Length);
        var read = Exchange(f.Partition, map);
        Assert.True(f.Partition.TryReadExchangePassed(read.Read));
        Assert.Empty(E(read.Unmatched)); Assert.Empty(E(read.Pending));
        Assert.True(Contains(read.Read, map)); Assert.True(Contains(read.Read, f.Partition.RoleExchange));
        foreach (var row in E(map)) Assert.True(Contains(read.Matched, row));
        AssertNoNumbers(read.Read);
    }

    [Fact]
    public void AColorExchangeNeedNotBeAnInvolution()
    {
        var f = new Fixture(false, false, true, true, false, false, true, true);
        // The quarter-cycle permutation has four applications before identity;
        // each application still exchanges the two occupied roles.
        var map = f.Map(i => (i + 2) % f.Cells.Length);
        var read = Exchange(f.Partition, map);
        Assert.True(f.Partition.TryReadExchangePassed(read.Read));
        Assert.Empty(E(read.Unmatched)); Assert.Empty(E(read.Pending));
    }

    [Fact]
    public void WrongShiftPreservesCountsAndTopologyButFailsRoleCorrespondence()
    {
        var f = new Fixture(false, false, false, false, true, true, true, true);
        var map = f.Map(i => (i + 1) % f.Cells.Length);
        var read = Exchange(f.Partition, map);
        Assert.False(f.Partition.TryReadExchangePassed(read.Read));
        Assert.NotEmpty(E(read.Matched)); Assert.NotEmpty(E(read.Unmatched)); Assert.Empty(E(read.Pending));
        Assert.All(E(read.Unmatched).Cast<Relation>(), failure =>
            Assert.Contains(E(map), row => ReferenceEquals(row, failure.Left)));
    }

    [Fact]
    public void SplitSameRoleRegionsRemainDistinctConnectedObjects()
    {
        var f = new Fixture(false, false, true, true, false, false, true, true);
        var read = Read(f.Partition);
        Assert.Equal(4, E(read.Regions).Length);
        var roles = new List<IElement>();
        foreach (var region in E(read.Regions).Cast<Relation>())
        {
            Assert.True(f.Partition.TryReadRegion(region, out var role, out var cells, out var boundary));
            roles.Add(role); Assert.Equal(2, E(cells).Length); Assert.NotEmpty(E(boundary)); AssertClosed(cells);
        }
        Assert.Equal(2, roles.Count(role => ReferenceEquals(role, f.A)));
        Assert.Equal(2, roles.Count(role => ReferenceEquals(role, f.B)));
        Assert.Equal(8, E(read.Interfaces).Length);
    }

    [Fact]
    public void ColorSwappingBijectionStillFailsWhenItDoesNotPreserveAdjacency()
    {
        var f = new Fixture(false, true, false, true, false, true);
        var map = f.Map(i => i % 2 == 0 ? i + 1 : i - 1);
        var read = Exchange(f.Partition, map);
        Assert.False(f.Partition.TryReadExchangePassed(read.Read)); Assert.NotEmpty(E(read.Unmatched));
        Assert.Empty(E(read.Pending));
        var sourceEdges = E(f.Partition.Adjacency);
        Assert.Contains(E(read.Unmatched).Cast<Relation>(), failure => sourceEdges.Any(edge => ReferenceEquals(edge, failure.Left)));
        Assert.All(E(map), row => Assert.Contains(E(read.Matched).Cast<Relation>(), match => ReferenceEquals(match.Left, row)));
    }

    [Fact]
    public void EdgeDirectionRemainsMeaningfulForExchangeEvenThoughRegionsUseIncidence()
    {
        var f = new Fixture(false, false, true, true);
        var directed = S(Enumerable.Range(0, f.Cells.Length)
            .Select(i => (IElement)new Relation(f.Cells[i], f.Cells[(i + 1) % f.Cells.Length])).ToArray());
        var partition = new QualitativePartition(f.Partition.Cells, directed, f.Pair, f.Partition.Labels);
        var reflection = f.Map(i => f.Cells.Length - 1 - i);
        var directedRead = Exchange(partition, reflection);
        Assert.False(partition.TryReadExchangePassed(directedRead.Read)); Assert.NotEmpty(E(directedRead.Unmatched));
        Assert.Empty(E(directedRead.Pending));
        Assert.True(f.Partition.TryReadExchangePassed(f.Partition.EvaluateExchange(reflection)));
    }

    [Fact]
    public void NonInjectiveMapCannotPassDespiteLocallyMatchingRows()
    {
        var f = new Fixture(false, false, true, true);
        var map = f.Map(i => i < 2 ? 2 : 0);
        var read = Exchange(f.Partition, map);
        Assert.False(f.Partition.TryReadExchangePassed(read.Read)); Assert.NotEmpty(E(read.Unmatched));
        Assert.All(E(map), row => Assert.True(Contains(read.Read, row)));
    }

    [Fact]
    public void MissingAndOpenLabelsStayUnknownRatherThanChoosingTheOtherRole()
    {
        var f = new Fixture(false, false, true, true);
        foreach (var labels in new[] { S(f.Labels.Take(3).Cast<IElement>().ToArray()), StructuralConstraint.Open(f.Labels) })
        {
            var partition = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, f.Pair, labels);
            var read = Read(partition);
            Assert.NotEmpty(E(read.Pending)); AssertOpen(read.First); AssertOpen(read.Second);
            Assert.DoesNotContain(E(read.Second).Cast<Relation>(), label => ReferenceEquals(label.Left, f.Cells[3]));
            var exchange = Exchange(partition, f.Map(i => (i + 2) % f.Cells.Length));
            Assert.False(partition.TryReadExchangePassed(exchange.Read)); Assert.NotEmpty(E(exchange.Pending));
        }
    }

    [Fact]
    public void UnknownNeighborKeepsKnownRegionMembershipAndBoundaryOpen()
    {
        var f = new Fixture(false, true, false);
        var labels = S(f.Labels[0], f.Labels[2]);
        var partition = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, f.Pair, labels);
        var read = Read(partition);
        Assert.Equal(2, E(read.First).Length); Assert.Empty(E(read.Second)); AssertOpen(read.Second);
        // In a three-cell cycle the known first-role cells already touch one
        // another. The unknown cell may extend that region or become role B.
        var region = Assert.IsType<Relation>(Assert.Single(E(read.Regions)));
        Assert.True(partition.TryReadRegion(region, out _, out var members, out var boundary));
        AssertOpen(members); AssertOpen(boundary); Assert.NotEmpty(E(read.Pending));
    }

    [Fact]
    public void ConflictingForeignAndCollapsedRolesCannotSupplyATotalPartition()
    {
        var f = new Fixture(false, true);
        foreach (var (pair, labels) in new[]
        {
            (f.Pair, S(f.Labels[0], f.Labels[1], new Relation(f.Cells[0], f.B))),
            (f.Pair, S(new Relation(f.Cells[0], new Placeholder()), f.Labels[1])),
            (new Relation(f.A, f.A), S(f.Labels))
        })
        {
            var partition = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, pair, labels);
            var read = Read(partition);
            Assert.NotEmpty(E(read.Pending));
            var exchange = Exchange(partition, f.Map(i => 1 - i));
            Assert.False(partition.TryReadExchangePassed(exchange.Read)); Assert.NotEmpty(E(exchange.Pending));
        }
    }

    [Fact]
    public void MissingAndOpenMapsRemainUnresolvedWhileForeignBindingsFail()
    {
        var f = new Fixture(false, false, true, true);
        var complete = E(f.Map(i => (i + 2) % f.Cells.Length));
        foreach (var map in new[] { S(complete.Take(3).ToArray()), StructuralConstraint.Open(complete) })
        {
            var read = Exchange(f.Partition, map);
            Assert.False(f.Partition.TryReadExchangePassed(read.Read)); Assert.NotEmpty(E(read.Pending));
        }
        var foreign = S(new Relation(f.Cells[0], new Placeholder()), complete[1], complete[2], complete[3]);
        var failed = Exchange(f.Partition, foreign);
        Assert.False(f.Partition.TryReadExchangePassed(failed.Read)); Assert.NotEmpty(E(failed.Unmatched));
    }

    [Fact]
    public void LookalikeCellsDoNotAcquireExistingCellIdentity()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var first = new Relation(a, b); var second = new Relation(b, a);
        var roleA = new Placeholder(); var roleB = new Placeholder();
        var partition = new QualitativePartition(S(first, second), S(new Relation(first, second), new Relation(second, first)),
            new Relation(roleA, roleB), S(new Relation(first, roleA), new Relation(second, roleB)));
        var impostor = new Relation(a, b);
        var map = S(new Relation(impostor, second), new Relation(second, first));
        var read = Exchange(partition, map);
        Assert.False(partition.TryReadExchangePassed(read.Read)); Assert.NotEmpty(E(read.Unmatched));
        AssertNoNumbers(read.Read);
    }

    [Fact]
    public void OpenTopologyPreservesKnownLabelsAndKeepsComponentsUnsettled()
    {
        var f = new Fixture(false, false, true, true);
        var partition = new QualitativePartition(f.Partition.Cells, StructuralConstraint.Open(E(f.Partition.Adjacency)), f.Pair, f.Partition.Labels);
        var read = Read(partition);
        AssertClosed(read.First); AssertClosed(read.Second); AssertOpen(read.Regions); AssertOpen(read.Interfaces);
        Assert.NotEmpty(E(read.Pending));
        Assert.False(partition.TryReadExchangePassed(partition.EvaluateExchange(f.Map(i => (i + 2) % f.Cells.Length))));
    }

    [Fact]
    public void ReadersRejectForeignFramesWrongRegionRolesAndForgedExchangeSuccess()
    {
        var f = new Fixture(false, false, true, true);
        var read = f.Partition.Evaluate();
        var other = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, f.Pair, f.Partition.Labels);
        Assert.False(other.TryReadResult(read, out _, out _, out _, out _, out _));
        var wrongRegion = new Relation(new Relation(f.Partition.Surface, f.B), new Relation(S(f.Cells[0]), S()));
        Assert.False(f.Partition.TryReadRegion(wrongRegion, out _, out _, out _));
        var exchange = f.Partition.EvaluateExchange(f.Map(i => i));
        var forged = new Relation(exchange.Left, new Relation(S(), new Relation(S(), S())));
        Assert.True(f.Partition.TryReadExchange(forged, out _, out _, out _, out _));
        Assert.False(f.Partition.TryReadExchangePassed(forged));
        Assert.False(other.TryReadExchange(exchange, out _, out _, out _, out _));
    }

    [Fact]
    public void ReadersDoNotAcceptDisconnectedRegionsOrSuppressedUnknownAndInterfaceEvidence()
    {
        var f = new Fixture(false, false, true, true, false, false, true, true);
        var read = Read(f.Partition);
        var disconnected = new Relation(new Relation(f.Partition.Surface, f.A),
            new Relation(S(f.Cells[0], f.Cells[1], f.Cells[4], f.Cells[5]), read.Interfaces));
        Assert.False(f.Partition.TryReadRegion(disconnected, out _, out _, out _));
        var chopped = new Relation(new Relation(f.Partition.Surface, f.A),
            new Relation(StructuralConstraint.Open(f.Cells[0]), StructuralConstraint.Open()));
        Assert.False(f.Partition.TryReadRegion(chopped, out _, out _, out _));

        var original = f.Partition.Evaluate();
        var result = Assert.IsType<Relation>(original.Right);
        var detail = Assert.IsType<Relation>(result.Right);
        var missingInterfaces = new Relation(original.Left, new Relation(result.Left,
            new Relation(detail.Left, new Relation(S(), S()))));
        Assert.False(f.Partition.TryReadResult(missingInterfaces, out _, out _, out _, out _, out _));

        var partial = new QualitativePartition(f.Partition.Cells, f.Partition.Adjacency, f.Pair, S(f.Labels.Take(7).Cast<IElement>().ToArray()));
        var partialRead = Read(partial); var actual = partial.Evaluate();
        var payload = Assert.IsType<Relation>(actual.Right);
        var fakeClosed = new Relation(actual.Left, new Relation(
            new Relation(new Relation(f.A, S(E(partialRead.First))), new Relation(f.B, S(E(partialRead.Second)))), payload.Right));
        Assert.False(partial.TryReadResult(fakeClosed, out _, out _, out _, out _, out _));
    }

    private sealed class Fixture
    {
        public readonly IElement A = new Placeholder(); public readonly IElement B = new Placeholder();
        public readonly IElement[] Cells; public readonly Relation[] Labels;
        public readonly Relation Pair; public readonly QualitativePartition Partition;
        public Fixture(params bool[] selectedSecondRole)
        {
            Cells = selectedSecondRole.Select(_ => (IElement)new Placeholder()).ToArray();
            Pair = new Relation(A, B);
            Labels = Cells.Select((cell, i) => new Relation(cell, selectedSecondRole[i] ? B : A)).ToArray();
            var edges = new List<IElement>();
            var included = new HashSet<(int, int)>();
            for (var i = 0; i < Cells.Length; i++)
            {
                var next = (i + 1) % Cells.Length;
                if (included.Add((i, next))) edges.Add(new Relation(Cells[i], Cells[next]));
                if (included.Add((next, i))) edges.Add(new Relation(Cells[next], Cells[i]));
            }
            Partition = new QualitativePartition(S(Cells), S(edges.ToArray()), Pair, S(Labels));
        }
        public Relation Map(Func<int, int> to) =>
            S(Cells.Select((cell, i) => (IElement)new Relation(cell, Cells[to(i)])).ToArray());
    }
    private static (Relation First, Relation Second, Relation Regions, Relation Interfaces, Relation Pending) Read(QualitativePartition partition)
    {
        Assert.True(partition.TryReadResult(partition.Evaluate(), out var first, out var second, out var regions, out var interfaces, out var pending));
        return (first, second, regions, interfaces, pending);
    }
    private static (Relation Read, Relation Matched, Relation Unmatched, Relation Pending) Exchange(QualitativePartition partition, Relation map)
    {
        var read = partition.EvaluateExchange(map);
        Assert.True(partition.TryReadExchange(read, out var actual, out var matched, out var unmatched, out var pending));
        Assert.Same(map, actual); return (read, matched, unmatched, pending);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertOpen(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)); Assert.NotEqual(seam, tail); }
    private static void AssertClosed(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)); Assert.Equal(seam, tail); }
    private static bool Contains(IElement root, IElement wanted) => Traverse(root).Any(element => ReferenceEquals(element, wanted));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            yield return current;
            if (current is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        { Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element); Assert.IsNotType<TickRef>(element); }
    }
}
