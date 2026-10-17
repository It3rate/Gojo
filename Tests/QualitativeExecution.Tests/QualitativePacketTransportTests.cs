using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativePacketTransportTests
{
    [Fact]
    public void FreshImportCreatesOneCurrentOwnerAndAnExplicitConservedReceipt()
    {
        var f = new Fixture(); var packet = new Placeholder(); var receipt = f.Transport.Import(new Placeholder(), f.A, S(packet));
        Assert.True(f.Transport.TryReadAccepted(receipt, out var after, out var accepted)); Assert.Same(after, f.Transport.State);
        Assert.Same(packet, Assert.Single(E(accepted))); Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.A))));
        Assert.Same(packet, Assert.Single(E(f.Transport.ImportedPackets))); AssertAccounting(f.Transport, receipt); AssertOwnedOnce(f.Transport);
        Assert.DoesNotContain(Traverse(new Relation(f.Transport.Surface, f.Transport.State)), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void PreflightIsPureUntilTheExactAcceptedPatchCommits()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var before = f.Transport.State; var timeline = f.Transport.Timeline;
        var receipt = f.Transport.Prepare(new Placeholder(), S(new Relation(packet, f.AB)), new Placeholder());
        Assert.True(f.Transport.TryReadAccepted(receipt, out var after, out _));
        Assert.Same(before, f.Transport.State); Assert.Same(timeline, f.Transport.Timeline); Assert.Empty(E(f.Transport.Unresolved));
        Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.A)))); Assert.Empty(E(f.Transport.PortPackets(f.B)));
        Assert.True(f.Transport.TryCommit(receipt)); Assert.Same(after, f.Transport.State);
        Assert.Empty(E(f.Transport.PortPackets(f.A))); Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.B)))); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void SamePacketCanReturnThroughNewLegalTransitionsWithoutBeingImportedAgain()
    {
        var f = new Fixture(); var packet = Seed(f, f.A);
        var outward = f.Transport.Move(new Placeholder(), S(new Relation(packet, f.AB)));
        var back = f.Transport.Move(new Placeholder(), S(new Relation(packet, f.BA)));
        Assert.True(f.Transport.TryReadAccepted(outward, out _, out _)); Assert.True(f.Transport.TryReadAccepted(back, out _, out _));
        Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.A)))); Assert.Single(E(f.Transport.ImportedPackets));
        Assert.Equal(3, E(f.Transport.Timeline).Length); Assert.Empty(E(f.Transport.Unresolved)); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void DuplicateImportAndReplayCannotCreateAnotherOwner()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var before = f.Transport.State;
        var duplicate = f.Transport.Import(new Placeholder(), f.B, S(packet));
        Assert.False(f.Transport.TryReadAccepted(duplicate, out _, out _)); Assert.Same(before, f.Transport.State);
        AssertAccounting(f.Transport, duplicate);
        var actual = f.Transport.Move(new Placeholder(), S(new Relation(packet, f.AB))); var current = f.Transport.State;
        Assert.False(f.Transport.TryCommit(actual)); Assert.Same(current, f.Transport.State); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void StaleAlternativeAcceptedBranchCannotReplaceTheCurrentHead()
    {
        var f = new Fixture(); var packet = Seed(f, f.A);
        var first = f.Transport.Prepare(new Placeholder(), S(new Relation(packet, f.AB)), new Placeholder());
        var alternative = f.Transport.Prepare(new Placeholder(), S(new Relation(packet, f.AB)), new Placeholder());
        Assert.True(f.Transport.TryReadAccepted(first, out _, out _)); Assert.True(f.Transport.TryReadAccepted(alternative, out _, out _));
        Assert.True(f.Transport.TryCommit(first)); var current = f.Transport.State;
        Assert.False(f.Transport.TryCommit(alternative)); Assert.Same(current, f.Transport.State);
        Assert.True(f.Transport.TryReadAccepted(alternative, out _, out _)); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void ForeignRouteLookalikeAndWrongCurrentSourceAreRejected()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var before = f.Transport.State;
        var requests = new[] {
            S(new Relation(packet, new Relation(f.A, f.B))),
            S(new Relation(packet, f.BA)),
            S(new Relation(new Placeholder(), f.AB))
        };
        foreach (var moves in requests)
        {
            var receipt = f.Transport.Prepare(new Placeholder(), moves, new Placeholder());
            Assert.False(f.Transport.TryReadAccepted(receipt, out _, out _)); Assert.Same(before, f.Transport.State);
            AssertAccounting(f.Transport, receipt);
        }
        AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void SharedTargetCapacityRejectsTheWholeBatchWithoutAHiddenPartialMove()
    {
        var f = new Fixture(); var first = Seed(f, f.A); var second = Seed(f, f.B); var before = f.Transport.State;
        var receipt = f.Transport.Prepare(new Placeholder(), S(new Relation(first, f.AC), new Relation(second, f.BC)), new Placeholder());
        Assert.False(f.Transport.TryReadAccepted(receipt, out _, out _));
        Assert.True(f.Transport.TryReadStep(receipt, out var actualBefore, out _, out var after, out var accepted, out var blocked, out var pending));
        Assert.Same(before, actualBefore); Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.Equal(2, E(blocked).Length);
        Assert.Empty(E(pending)); Assert.Empty(E(f.Transport.PortPackets(f.C))); Assert.Same(before, f.Transport.State);
        AssertAccounting(f.Transport, receipt); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void SimultaneousExchangeCanUseSitesReleasedByTheSameAtomicBatch()
    {
        var f = new Fixture(); var first = Seed(f, f.A); var second = Seed(f, f.B);
        var receipt = f.Transport.Prepare(new Placeholder(), S(new Relation(first, f.AB), new Relation(second, f.BA)), new Placeholder());
        Assert.True(f.Transport.TryReadAccepted(receipt, out _, out var moved)); Assert.Equal(2, E(moved).Length);
        Assert.True(f.Transport.TryCommit(receipt)); Assert.Same(second, Assert.Single(E(f.Transport.PortPackets(f.A))));
        Assert.Same(first, Assert.Single(E(f.Transport.PortPackets(f.B)))); AssertAccounting(f.Transport, receipt); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void SharedStorageSiteIdentityCompetesAcrossDifferentPorts()
    {
        var a = new Placeholder(); var b = new Placeholder(); var site = new Placeholder(); var route = new Relation(a, b);
        var transport = new QualitativePacketTransport(S(new Relation(a, S(site)), new Relation(b, S(site))), S(route));
        var packet = new Placeholder(); transport.Import(new Placeholder(), a, S(packet)); var before = transport.State;
        var overflow = transport.Import(new Placeholder(), b, S(new Placeholder()));
        Assert.False(transport.TryReadAccepted(overflow, out _, out _)); Assert.Same(before, transport.State); AssertAccounting(transport, overflow);
        var move = transport.Move(new Placeholder(), S(new Relation(packet, route)));
        Assert.True(transport.TryReadAccepted(move, out _, out _)); Assert.Same(packet, Assert.Single(E(transport.PortPackets(b))));
        AssertOwnedOnce(transport);
    }

    [Fact]
    public void FullTargetRetainsThePacketAtItsSourceUntilAFreshMoveCanBeAccepted()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var occupant = Seed(f, f.B);
        var blocked = f.Transport.Prepare(new Placeholder(), S(new Relation(packet, f.AB)), new Placeholder());
        Assert.False(f.Transport.TryReadAccepted(blocked, out _, out _)); Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.A))));
        f.Transport.Move(new Placeholder(), S(new Relation(occupant, f.BC)));
        Assert.True(f.Transport.TryReadAccepted(f.Transport.Move(new Placeholder(), S(new Relation(packet, f.AB))), out _, out _));
        Assert.Same(packet, Assert.Single(E(f.Transport.PortPackets(f.B)))); AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void OpenOrDuplicatedMoveInputsKeepTheirRemainderWithoutMutation()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var before = f.Transport.State;
        var move = new Relation(packet, f.AB);
        foreach (var moves in new[] { StructuralConstraint.Open(move), S(move, move) })
        {
            var receipt = f.Transport.Prepare(new Placeholder(), moves, new Placeholder());
            Assert.False(f.Transport.TryReadAccepted(receipt, out _, out _));
            Assert.True(f.Transport.TryReadStep(receipt, out _, out _, out var after, out var accepted, out _, out var pending));
            Assert.Same(before, after); Assert.Empty(E(accepted)); Assert.NotEmpty(E(pending)); Assert.Same(before, f.Transport.State);
        }
        AssertOwnedOnce(f.Transport);
    }

    [Fact]
    public void ForgedAfterStateAcceptedScopeAndDetachedRequestCannotAuthorizeCommit()
    {
        var f = new Fixture(); var packet = Seed(f, f.A); var receipt = f.Transport.Prepare(new Placeholder(), S(new Relation(packet, f.AB)), new Placeholder());
        Assert.True(f.Transport.TryReadStep(receipt, out var before, out var request, out var after, out _, out _, out _));
        var context = Assert.IsType<Relation>(receipt.Left); var result = Assert.IsType<Relation>(receipt.Right);
        var outcome = Assert.IsType<Relation>(result.Left);
        var fakeAccepted = new Relation(receipt.Left, new Relation(new Relation(outcome.Left, S(new Placeholder())), result.Right));
        var fakeAfter = new Relation(receipt.Left, new Relation(new Relation(before, outcome.Right), result.Right));
        var fakeRequest = new Relation(new Relation(new Relation(before, new Relation(request.Left, request.Right)), context.Right), receipt.Right);
        foreach (var forged in new[] { fakeAccepted, fakeAfter, fakeRequest })
        { Assert.False(f.Transport.TryReadAccepted(forged, out _, out _)); Assert.False(f.Transport.TryCommit(forged)); Assert.Same(before, f.Transport.State); }
        Assert.True(f.Transport.TryCommit(receipt)); Assert.Same(after, f.Transport.State); AssertOwnedOnce(f.Transport);
    }

    private sealed class Fixture
    {
        public IElement A { get; } = new Placeholder(); public IElement B { get; } = new Placeholder(); public IElement C { get; } = new Placeholder();
        public Relation AB { get; } public Relation BA { get; } public Relation AC { get; } public Relation BC { get; }
        public QualitativePacketTransport Transport { get; }
        public Fixture()
        {
            AB = new(A, B); BA = new(B, A); AC = new(A, C); BC = new(B, C);
            Transport = new(S(new Relation(A, S(new Placeholder())), new Relation(B, S(new Placeholder())), new Relation(C, S(new Placeholder()))), S(AB, BA, AC, BC));
        }
    }
    private static IElement Seed(Fixture fixture, IElement port)
    {
        var packet = new Placeholder(); var receipt = fixture.Transport.Import(new Placeholder(), port, S(packet));
        Assert.True(fixture.Transport.TryReadAccepted(receipt, out _, out _)); return packet;
    }
    private static void AssertAccounting(QualitativePacketTransport transport, Relation receipt)
    {
        Assert.True(transport.TryReadAccounting(receipt, out var incoming, out var accepted, out var blocked, out var remainder));
        Assert.Equal(E(incoming).Length, E(accepted).Length + E(blocked).Length + E(remainder).Length);
    }
    private static void AssertOwnedOnce(QualitativePacketTransport transport)
    {
        Assert.True(transport.TryReadState(transport.State, out _, out var placements, out var imported, out _));
        Assert.Equal(E(imported).Length, E(placements).Length);
        foreach (var packet in E(imported)) Assert.Single(E(placements).Cast<Relation>(), row => ReferenceEquals(row.Left, packet));
        var account = transport.Account(); Assert.Empty(E(Assert.IsType<Relation>(Assert.IsType<Relation>(account.Right).Right)));
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
