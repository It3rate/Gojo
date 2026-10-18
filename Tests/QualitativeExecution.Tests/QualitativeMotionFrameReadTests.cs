using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeMotionFrameReadTests
{
    [Fact]
    public void CompleteNativeFrameHasAnExplicitBijectionIntoAFreshStandardFrame()
    {
        var source = new QualitativeMotionFrame();
        Assert.True(QualitativeMotionFrameRead.TryReadStandard(source.Surface, out var read, out var witness));
        Assert.Same(source.Surface, read.SourceSurface); Assert.NotSame(source.Surface, read.Target.Surface); Assert.Same(witness, read.Witness);
        Assert.Same(read.Target.Surface, read.Rebind(source.Surface));
        Assert.Same(read.Target.Right, read.Rebind(source.Right)); Assert.Same(read.Target.Left, read.Rebind(source.Left));
        Assert.Same(read.Target.Up, read.Rebind(source.Up)); Assert.Same(read.Target.Origin, read.Rebind(source.Origin));
        Assert.Equal(((Placeholder)read.Target.Origin).Dual(), read.Rebind(((Placeholder)source.Origin).Dual()));
        var pairs = E(read.Correspondence).Cast<Relation>().ToArray();
        Assert.Equal(pairs.Length, pairs.Select(pair => pair.Left).Distinct(Participants.Instance).Count());
        Assert.Equal(pairs.Length, pairs.Select(pair => pair.Right).Distinct(Participants.Instance).Count());
        Assert.Contains(pairs, pair => ReferenceEquals(pair.Left, source.CancellationLaws) && ReferenceEquals(pair.Right, read.Target.CancellationLaws));
        Assert.Contains(pairs, pair => ReferenceEquals(pair.Left, source.Exchange) && ReferenceEquals(pair.Right, read.Target.Exchange));
    }

    [Fact]
    public void ATransportedNativeLawIsReadWithoutOriginalWrapperOrRuntimeIds()
    {
        var original = new QualitativeMotionFrame();
        var restored = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(original.Surface))));
        Assert.NotSame(original.Surface, restored);
        Assert.True(QualitativeMotionFrameRead.TryReadStandard(restored, out var read));
        Assert.Same(read.Target.Surface, read.Rebind(restored));
        var frameHead = Assert.IsType<Relation>(restored.Left);
        var axes = Assert.IsType<Relation>(frameHead.Right);
        Assert.Same(read.Target.Right, read.Rebind(axes.Left)); Assert.Same(read.Target.Up, read.Rebind(axes.Right));
    }

    [Theory]
    [InlineData("opposites")]
    [InlineData("cancellation-reach")]
    [InlineData("exchange-order")]
    [InlineData("exchange-membership")]
    [InlineData("axis-alias")]
    [InlineData("lookalike-direction")]
    [InlineData("open-opposites")]
    [InlineData("inverse-role")]
    [InlineData("unsupported-origin")]
    public void MissingOrDifferentLawCannotAcquireTheStandardRead(string change)
    {
        var source = new QualitativeMotionFrame(); var law = (Relation)source.Surface.Right;
        Relation wrong;
        switch (change)
        {
            case "opposites":
                var opposites = E(source.Opposites); opposites[0] = new Relation(source.Right, source.Up);
                wrong = WithOpposites(S(opposites)); break;
            case "cancellation-reach":
                var rows = E(source.CancellationLaws); var first = (Relation)rows[0]; var rule = (Relation)first.Right;
                var moves = E((Relation)rule.Left); var move = (Relation)moves[1]; moves[1] = new Relation(move.Left, new Placeholder());
                rows[0] = new Relation(first.Left, new Relation(S(moves), rule.Right));
                wrong = WithLaw(source.Opposites, S(rows), source.Exchange); break;
            case "exchange-order":
                var exchangeWords = (Relation)source.Exchange.Right;
                wrong = WithLaw(source.Opposites, source.CancellationLaws,
                    new Relation(source.Exchange.Left, new Relation(exchangeWords.Left, exchangeWords.Left))); break;
            case "exchange-membership":
                var selection = (Relation)source.Exchange.Left;
                wrong = WithLaw(source.Opposites, source.CancellationLaws,
                    new Relation(new Relation(selection.Left, S()), source.Exchange.Right)); break;
            case "axis-alias":
                wrong = new Relation(new Relation(source.Origin, new Relation(source.Horizontal, source.Horizontal)), law); break;
            case "lookalike-direction":
                var oppositeRows = E(source.Opposites); var opposite = (Relation)oppositeRows[0];
                oppositeRows[0] = new Relation(new Relation(source.Right.Left, source.Right.Right), opposite.Right);
                wrong = WithOpposites(S(oppositeRows)); break;
            case "open-opposites": wrong = WithOpposites(StructuralConstraint.Open(E(source.Opposites))); break;
            case "inverse-role": wrong = new Relation(new Relation(((Placeholder)source.Origin).Dual(), ((Relation)source.Surface.Left).Right), law); break;
            case "unsupported-origin": wrong = new Relation(new Relation(new Carrier(), ((Relation)source.Surface.Left).Right), law); break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        Assert.False(QualitativeMotionFrameRead.TryReadStandard(wrong, out _, out var failure));
        var failureBody = Assert.IsType<Relation>(failure.Right);
        Assert.True(StructuralConstraint.TryReadScope(Assert.IsType<Relation>(failureBody.Right), out var reasons, out var seam, out var tail));
        Assert.NotEqual(seam, tail); Assert.NotEmpty(reasons);

        Relation WithOpposites(Relation oppositeScope) => WithLaw(oppositeScope, source.CancellationLaws, source.Exchange);
        Relation WithLaw(Relation oppositeScope, Relation cancellations, Relation exchange) =>
            new(source.Surface.Left, new Relation(oppositeScope, new Relation(cancellations, exchange)));
    }

    [Fact]
    public void SharedSelectorsAndPathClosuresSurviveSeparateReadsThroughTheSameMemo()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var start = new Placeholder(); var end = new Placeholder();
        var right = S(QualitativeMotionFrame.Move(frame.Right, reach)); var left = S(QualitativeMotionFrame.Move(frame.Left, reach));
        var a = new Relation(S(), S(right, left)); var b = new Relation(right, S(left, right));
        var paths = S(a, b); var ports = S(new Relation(start, new Relation(a, right)), new Relation(end, new Relation(b, right)));
        Assert.True(QualitativeMotionFrameRead.TryReadStandard(frame.Surface, out var read));
        var mappedPaths = Assert.IsType<Relation>(read.Rebind(paths));
        var mappedPorts = Assert.IsType<Relation>(read.Rebind(ports));
        var mappedClosures = Assert.IsType<Relation>(read.Rebind(S(a, b)));
        var p = E(mappedPaths).Cast<Relation>().ToArray(); var picks = E(mappedPorts).Cast<Relation>().Select(port => (Relation)port.Right).ToArray();
        Assert.Same(p[0], picks[0].Left); Assert.Same(p[1], picks[1].Left); Assert.Same(picks[0].Right, picks[1].Right);
        Assert.Same(picks[0].Right, E((Relation)p[0].Right)[0]); Assert.Same(picks[0].Right, E((Relation)p[1].Right)[1]);
        Assert.Same(p[0], E(mappedClosures)[0]); Assert.Same(p[1], E(mappedClosures)[1]);
        var mappedMove = Assert.IsType<Relation>(Assert.Single(E((Relation)picks[0].Right)));
        Assert.Same(read.Target.Right, mappedMove.Left); Assert.Same(reach, mappedMove.Right);
        var program = new UnitArrangementProgram(read.Target, "restored definition", mappedPaths, mappedPorts, mappedClosures, read.Witness);
        var node = new UnitArrangementNode("new occurrence", [program], program, S());
        Assert.Equal(4, E(node.Current.Graph.Strokes).Length); Assert.Equal(2, E(node.Current.Join.DeclaredJoins).Length);
        Assert.True(UnitArrangementConnection.TryReadPublication(node, start, out _, out _));
    }

    [Fact]
    public void TwoFrameReadsAreFreshWhileUnboundImportedParametersRemainTheSameParticipants()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var slot = new Placeholder();
        var word = S(QualitativeMotionFrame.Move(frame.Right, reach), slot);
        Assert.True(QualitativeMotionFrameRead.TryReadStandard(frame.Surface, out var first));
        Assert.True(QualitativeMotionFrameRead.TryReadStandard(frame.Surface, out var second));
        var a = Assert.IsType<Relation>(first.Rebind(word)); var b = Assert.IsType<Relation>(second.Rebind(word));
        Assert.NotSame(a, b); Assert.NotSame(first.Target.Right, second.Target.Right);
        Assert.Same(reach, ((Relation)E(a)[0]).Right); Assert.Same(reach, ((Relation)E(b)[0]).Right);
        Assert.Same(slot, E(a)[1]); Assert.Same(slot, E(b)[1]); Assert.Same(a, first.Rebind(word));
    }

    [Fact]
    public void CyclicAndMeasuredProgramsDoNotPublishPartlyReboundResults()
    {
        var frame = new QualitativeMotionFrame(); Assert.True(QualitativeMotionFrameRead.TryReadStandard(frame.Surface, out var read));
        var source = new Relation(new Placeholder(), new Placeholder()); var snapshot = GojoEncodedSnapshot.CaptureGraph(source);
        var rows = snapshot.Relations.ToArray(); var index = snapshot.Root.Index;
        rows[index] = rows[index] with { Right = new(GojoEncodedElementKind.Relation, index) };
        var cycle = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Restore(snapshot with { Relations = rows })));
        Assert.False(QualitativeMotionFrameRead.TryReadStandard(cycle, out _));
        Assert.False(read.TryRebind(cycle, out var unchanged, out var failure)); Assert.Same(cycle, unchanged);
        Assert.True(StructuralConstraint.TryReadScope((Relation)((Relation)failure.Right).Left, out var published, out var seam, out var tail));
        Assert.NotEqual(seam, tail); Assert.Empty(published);
        var measured = new Relation(new Carrier(), frame.Right);
        Assert.False(read.TryRebind(measured, out unchanged, out _)); Assert.Same(measured, unchanged);
        Assert.Throws<ArgumentException>(() => read.Rebind(measured));
        Assert.Same(read.Target.Right, read.Rebind(frame.Right));
    }

    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var values, out _, out _) ? values : [];
    private sealed class Participants : IEqualityComparer<IElement>
    {
        internal static Participants Instance { get; } = new();
        public bool Equals(IElement? a, IElement? b) => ReferenceEquals(a, b) || a is Placeholder p && b is Placeholder q && p.Equals(q);
        public int GetHashCode(IElement element) => element is Placeholder p ? p.GetHashCode() : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(element);
    }
}
