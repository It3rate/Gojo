using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class UnitArrangementTests
{
    [Fact]
    public void APlacedProgramPublishesActualAcknowledgedBoundaryBindingsAndKeepsMaterialAccounted()
    {
        var f = new Fixture(); var program = f.Path("open step", S(), [f.R, f.U]);
        var node = new UnitArrangementNode("unit", [program], program, f.R);
        Assert.True(UnitArrangementConnection.TryReadPublication(node, f.Start, out var start, out var a));
        Assert.True(UnitArrangementConnection.TryReadPublication(node, f.End, out var end, out var b));
        Assert.Same(node.Head, ((Relation)start.Right).Left); Assert.Same(node.Head, ((Relation)end.Right).Left);
        Assert.True(SamePosition(f.Frame, (Relation)a.Right, f.R));
        Assert.True(SamePosition(f.Frame, (Relation)b.Right, f.C(f.R, f.R, f.U)));
        Assert.Equal(2, E(node.Current.Graph.Strokes).Length); Assert.Single(E(node.Current.Execution.CompletionEvents));
        Assert.Equal(4, E(node.Current.Execution.Sequence.ImportedPackets).Length);
        Assert.Equal(4, E(node.Current.Execution.Sequence.ReleasedPackets).Length);
        Assert.Empty(E(node.Current.Execution.Sequence.RetainedPackets)); Assert.Empty(E(node.Current.Execution.Pending));
        AssertNoNumeric(node.Current.Surface);
    }

    [Fact]
    public void AConnectionNegotiatesThroughOpaquePublishedContractsAndRejectsItsStaleAgreement()
    {
        var f = new Fixture(); var leftProgram = f.Path("left", S(), [f.R, f.U]);
        var original = f.Path("original", f.U, [f.R, f.D]); var mirrored = original.Transform("mirror", f.Mirror());
        var signal = new Placeholder(); var domain = S(original.Surface, mirrored.Surface);
        var policy = FiniteLaw.Create(domain, S(signal), domain,
            FiniteLaw.Row(original.Surface, signal, S(mirrored.Surface)), FiniteLaw.Row(mirrored.Surface, signal, S(original.Surface)));
        var left = new UnitArrangementNode("left unit", [leftProgram], leftProgram, S());
        var right = new UnitArrangementNode("right unit", [original, mirrored], original, f.C(f.R, f.U, f.U), policy);
        var opaqueLeft = new Opaque(left); var opaqueRight = new Opaque(right);
        var connection = new UnitArrangementConnection(f.Frame, opaqueLeft, f.End, opaqueRight, f.Start);
        connection.Observe(); Assert.False(connection.IsAligned); Assert.Single(E(connection.Blocked)); Assert.Empty(E(connection.AcceptedJoins));
        var before = right.Current; connection.Negotiate(opaqueRight, signal);
        Assert.True(connection.IsAligned); Assert.NotSame(before, right.Current); Assert.Same(mirrored, right.Current.Program);
        Assert.Equal(2, E(right.Revisions).Length); Assert.Equal(4, E(right.Drawing.Strokes).Length);
        var agreement = Assert.IsType<Relation>(Assert.Single(E(connection.AcceptedJoins)));
        Assert.True(connection.IsCurrentAgreement(agreement)); Assert.True(opaqueLeft.Reads > 0); Assert.True(opaqueRight.Reads > 0);
        Assert.False(connection.IsCurrentAgreement(new Relation(agreement.Left, agreement.Right)));
        var proposal = opaqueRight.Propose(signal, agreement); opaqueRight.Commit(proposal);
        Assert.False(connection.IsCurrentAgreement(agreement)); Assert.False(connection.IsAligned);
        connection.Observe(); Assert.Empty(E(connection.AcceptedJoins)); Assert.Single(E(connection.Blocked));
    }

    [Fact]
    public void AnAbsentPolicyLanePreservesTheHeadInkAndBlockedMismatch()
    {
        var f = new Fixture(); var program = f.Path("fixed", S(), [f.R]);
        var left = new UnitArrangementNode("left", [program], program, S());
        var right = new UnitArrangementNode("fixed right", [program], program, f.C(f.R, f.R));
        var seam = new UnitArrangementConnection(f.Frame, left, f.End, right, f.Start);
        var head = right.Head; var ink = right.Drawing.Strokes;
        seam.Negotiate(right, new Placeholder());
        Assert.False(seam.IsAligned); Assert.Same(head, right.Head); Assert.Same(ink, right.Drawing.Strokes);
        Assert.Single(E(seam.Blocked)); Assert.NotEmpty(E(right.Unresolved)); Assert.Empty(E(right.Timeline));
    }

    [Fact]
    public void AProposalCannotOverwriteAnActuatorThatChangedAfterTheQuestion()
    {
        var f = new Fixture(); var first = f.Path("first", S(), [f.R]); var next = f.Path("next", S(), [f.U]);
        var signal = new Placeholder(); var domain = S(first.Surface, next.Surface);
        var policy = FiniteLaw.Create(domain, S(signal), domain, FiniteLaw.Row(first.Surface, signal, S(next.Surface)));
        var node = new UnitArrangementNode("unit", [first, next], first, S(), policy);
        var head = node.Head; var proposal = node.Propose(signal, S());
        node.Drawing.Apply(new Placeholder(), S(node.Drawing.Draw(f.D)), S()); var latest = node.Drawing.State;
        node.Commit(proposal);
        Assert.Same(head, node.Head); Assert.Same(latest, node.Drawing.State); Assert.Same(proposal, Assert.Single(E(node.Pending)));
        Assert.Empty(E(node.Timeline)); Assert.NotEmpty(E(node.Unresolved));
    }

    [Fact]
    public void RefinedChainsKeepCoarseCornerLandmarksWhileAddingActualFineStrokes()
    {
        var f = new Fixture(); var words = new[] { f.C(f.R, f.U), f.C(f.R, f.D), f.C(f.L, f.D), f.C(f.L, f.U) };
        var roles = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var path = new Relation(S(), S(words)); var ports = S(words.Select((word, i) => (IElement)new Relation(roles[i], new Relation(path, word))).ToArray());
        var coarse = new UnitArrangementProgram(f.Frame, "diamond", S(path), ports, S(path));
        var rows = S(words.Select(word => (IElement)new Relation(word,
            S(E(word).Select(move => (IElement)S(move)).ToArray()))).ToArray());
        var fine = coarse.Refine("stepped contour", rows);
        var a = new UnitArrangementNode("coarse", [coarse], coarse, S()); var b = new UnitArrangementNode("fine", [fine], fine, S());
        Assert.Equal(4, E(a.Current.Graph.Strokes).Length); Assert.Equal(8, E(b.Current.Graph.Strokes).Length);
        Assert.True(a.Current.Join.TryReadPassed(a.Current.Join.Evaluate())); Assert.True(b.Current.Join.TryReadPassed(b.Current.Join.Evaluate()));
        foreach (var role in roles)
        {
            Assert.True(UnitArrangementConnection.TryReadPublication(a, role, out _, out var old));
            Assert.True(UnitArrangementConnection.TryReadPublication(b, role, out _, out var refined));
            Assert.NotSame(old.Left, refined.Left); Assert.True(SamePosition(f.Frame, (Relation)old.Right, (Relation)refined.Right));
        }
        Assert.Same(coarse.Surface, ((Relation)fine.Evidence).Left);
        Assert.Throws<ArgumentException>(() => coarse.Refine("wrong endpoint", S(new Relation(words[0], S(f.R)))));
        AssertNoNumeric(b.Current.Surface);
    }

    [Fact]
    public void TransformRequiresCompleteSubstitutionAndPublishesItsActualWordImages()
    {
        var f = new Fixture(); var program = f.Path("step", f.U, [f.R, f.U]);
        var node = new UnitArrangementNode("mirror through placement", [program], program, f.R, transform: f.Mirror());
        Assert.Same(program, node.Current.Program); Assert.NotSame(program, node.Current.ExecutedProgram);
        Assert.True(UnitArrangementConnection.TryReadPublication(node, f.Start, out _, out var start));
        Assert.True(SamePosition(f.Frame, (Relation)start.Right, f.C(f.R, f.D)));
        var incomplete = new QualitativePatternTransform(f.Frame, f.Frame, S(new Relation(E(f.R)[0], f.R)));
        Assert.Throws<ArgumentException>(() => program.Transform("incomplete", incomplete));
    }

    [Fact]
    public void AStaleOrForeignPublicationCannotPretendToBeTheCurrentHead()
    {
        var f = new Fixture(); var first = f.Path("first", S(), [f.R]); var next = f.Path("next", S(), [f.U]);
        var signal = new Placeholder(); var domain = S(first.Surface, next.Surface);
        var node = new UnitArrangementNode("unit", [first, next], first, S(),
            FiniteLaw.Create(domain, S(signal), domain, FiniteLaw.Row(first.Surface, signal, S(next.Surface))));
        var retained = node.Advertisement(); node.Commit(node.Propose(signal, retained));
        var stale = new Opaque(node, retained);
        Assert.False(UnitArrangementConnection.TryReadPublication(stale, f.Start, out _, out _));
        Assert.True(UnitArrangementConnection.TryReadPublication(node, f.Start, out _, out _));
        Assert.False(UnitArrangementConnection.TryReadPublication(node, new Placeholder(), out _, out _));
    }

    [Fact]
    public void SharedChordIdentitiesCanBeTransformedAndRefinedAcrossIndependentPaths()
    {
        var f = new Fixture(); var shared = f.R;
        var first = new Relation(S(), S(shared)); var second = new Relation(f.U, S(shared));
        var original = new UnitArrangementProgram(f.Frame, "two shared recipes", S(first, second),
            S(new Relation(f.Start, new Relation(first, shared)), new Relation(f.End, new Relation(second, second))));
        var mirror = original.Transform("mirrored", f.Mirror());
        var fine = original.Refine("one detour per occurrence", S(new Relation(shared, S(shared, f.U, f.D))));
        var mirrored = new UnitArrangementNode("mirror", [mirror], mirror, S());
        var refined = new UnitArrangementNode("fine", [fine], fine, S());
        Assert.Equal(2, E(mirrored.Current.Graph.Strokes).Length); Assert.Equal(6, E(refined.Current.Graph.Strokes).Length);
        Assert.True(UnitArrangementConnection.TryReadPublication(refined, f.End, out _, out _));
    }

    [Fact]
    public void ARepeatedChordNeedsAnUnambiguousOccurrenceSelectorBeforeAnyReplacementCanDraw()
    {
        var f = new Fixture(); var shared = f.R; var valid = f.Path("valid", S(), [shared]);
        var path = new Relation(S(), S(shared, shared));
        var ambiguous = new UnitArrangementProgram(f.Frame, "ambiguous selector", S(path),
            S(new Relation(f.Start, new Relation(path, shared))));
        var signal = new Placeholder(); var domain = S(valid.Surface, ambiguous.Surface);
        var node = new UnitArrangementNode("unit", [valid, ambiguous], valid, S(),
            FiniteLaw.Create(domain, S(signal), domain, FiniteLaw.Row(valid.Surface, signal, S(ambiguous.Surface))));
        var state = node.Drawing.State; var head = node.Head; var ink = node.Drawing.Strokes;
        var proposal = node.Propose(signal, S()); node.Commit(proposal);
        Assert.Same(state, node.Drawing.State); Assert.Same(head, node.Head); Assert.Same(ink, node.Drawing.Strokes);
        Assert.Single(E(node.Revisions)); Assert.Same(proposal, Assert.Single(E(node.Pending)));
        Assert.Throws<ArgumentException>(() => ambiguous.Transform("bad", f.Mirror()));
        // Repeated participation itself is valid when the selected landmark is
        // the completed path, rather than an ambiguous word occurrence.
        var terminalOnly = new UnitArrangementProgram(f.Frame, "repeated", S(path), S(new Relation(f.End, new Relation(path, path))));
        var transformed = terminalOnly.Transform("mirror", f.Mirror());
        var repeated = new UnitArrangementNode("repeated", [transformed], transformed, S());
        Assert.Equal(2, E(repeated.Current.Graph.Strokes).Length);
    }

    [Fact]
    public void DuplicatePublicationRolesAndFalseContourClaimsFailBeforeReplacementActuation()
    {
        var f = new Fixture(); var valid = f.Path("valid", S(), [f.R]);
        var path = new Relation(S(), S(f.U));
        var duplicate = new UnitArrangementProgram(f.Frame, "duplicate", S(path),
            S(new Relation(f.Start, new Relation(path, path)), new Relation(f.Start, new Relation(path, path))));
        var falseClosure = new UnitArrangementProgram(f.Frame, "false contour", S(path), S(), S(path));
        var signalA = new Placeholder(); var signalB = new Placeholder(); var domain = S(valid.Surface, duplicate.Surface, falseClosure.Surface);
        var policy = FiniteLaw.Create(domain, S(signalA, signalB), domain,
            FiniteLaw.Row(valid.Surface, signalA, S(duplicate.Surface)), FiniteLaw.Row(valid.Surface, signalB, S(falseClosure.Surface)));
        foreach (var signal in new[] { signalA, signalB })
        {
            var node = new UnitArrangementNode("unit", [valid, duplicate, falseClosure], valid, S(), policy);
            var before = node.Drawing.State; var head = node.Head;
            node.Commit(node.Propose(signal, S()));
            Assert.Same(before, node.Drawing.State); Assert.Same(head, node.Head); Assert.Single(E(node.Revisions));
            Assert.Empty(E(node.Timeline)); Assert.NotEmpty(E(node.Unresolved));
        }
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public IElement Start { get; } = new Placeholder();
        public IElement End { get; } = new Placeholder();
        public Relation R => S(QualitativeMotionFrame.Move(Frame.Right, Reach));
        public Relation L => S(QualitativeMotionFrame.Move(Frame.Left, Reach));
        public Relation U => S(QualitativeMotionFrame.Move(Frame.Up, Reach));
        public Relation D => S(QualitativeMotionFrame.Move(Frame.Down, Reach));
        public Relation C(params Relation[] words) => S(words.SelectMany(E).ToArray());
        public UnitArrangementProgram Path(string name, Relation start, Relation[] words)
        {
            var path = new Relation(start, S(words));
            return new(Frame, name, S(path), S(new Relation(Start, new Relation(path, words[0])), new Relation(End, new Relation(path, path))));
        }
        public QualitativePatternTransform Mirror() => new(Frame, Frame, S(
            new Relation(E(R)[0], R), new Relation(E(L)[0], L), new Relation(E(U)[0], D), new Relation(E(D)[0], U)));
    }

    private sealed class Opaque(IUnitArrangementEndpoint inner, Relation? retained = null) : IUnitArrangementEndpoint
    {
        public int Reads { get; private set; }
        public Relation Surface => inner.Surface;
        public IElement Head => inner.Head;
        public Relation Advertisement() { Reads++; return retained ?? inner.Advertisement(); }
        public Relation Propose(IElement signal, IElement cause) => inner.Propose(signal, cause);
        public Relation Commit(Relation proposal) => inner.Commit(proposal);
    }

    private static bool SamePosition(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var inverse = S(E(b).Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(
            E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
        return frame.TryReadResult(frame.Reduce(a, inverse), out var result, out _, out var pending) && E(result).Length == 0 && E(pending).Length == 0;
    }
    private static void AssertNoNumeric(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item)) { if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item); if (item is Relation row) { todo.Push(row.Left); todo.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
