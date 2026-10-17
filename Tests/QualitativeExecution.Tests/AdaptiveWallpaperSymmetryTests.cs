using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AdaptiveWallpaperSymmetryTests
{
    [Theory]
    [InlineData("cushion", false)]
    [InlineData("cushion", true)]
    [InlineData("key", false)]
    [InlineData("key", true)]
    [InlineData("rosette", false)]
    [InlineData("rosette", true)]
    public void ActualClosedMotifInkHasAnUncalibratedQuarterTurnAtTheFrameOrigin(string family, bool reflected)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        var node = new AdaptiveMotifNode("origin", grammar, S(), reflected ? grammar.Minus : grammar.Plus);
        var machine = new AdaptiveWallpaperMachine("one motif", grammar, [node], [], S());
        var observer = new AdaptiveWallpaperSymmetry(machine); var read = observer.Evaluate();
        Assert.True(observer.TryReadPassed(read));
        Assert.True(observer.TryReadResult(read, out var nodes, out var components, out var seams, out var blocked, out var unknown));
        Assert.Single(E(nodes)); Assert.Single(E(components)); Assert.Empty(E(seams)); Assert.Empty(E(blocked)); Assert.Empty(E(unknown));
        Assert.DoesNotContain(Traverse(read), item => item is Carrier or CarrierLocation);
        Assert.Contains(Traverse(read), item => ReferenceEquals(item, node.Current.Join.Graph.Surface));
    }

    [Fact]
    public void AQuarterTurnAboutTheFrameOriginCannotBeBorrowedByAnOffCentreMotif()
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "cushion");
        var node = new AdaptiveMotifNode("offset", grammar, W(grammar, grammar.Frame.Right, 1), grammar.Plus);
        var machine = new AdaptiveWallpaperMachine("offset motif", grammar, [node], [], S());
        var observer = new AdaptiveWallpaperSymmetry(machine); var read = observer.Evaluate();
        Assert.False(observer.TryReadPassed(read));
        Assert.True(observer.TryReadResult(read, out _, out _, out _, out var blocked, out _)); Assert.NotEmpty(E(blocked));
    }

    [Fact]
    public void PerMotifProofsComposeAcrossARequiredSeamOrbitIncludingEndpointReversal()
    {
        var (machine, _) = Grid(); var observer = new AdaptiveWallpaperSymmetry(machine); var read = observer.Evaluate();
        Assert.True(observer.TryReadPassed(read));
        Assert.True(observer.TryReadResult(read, out var nodes, out var components, out var seams, out var blocked, out var unknown));
        Assert.Equal(9, E(nodes).Length); Assert.Equal(9, E(components).Length); Assert.Equal(machine.Links.Count, E(seams).Length);
        Assert.Empty(E(blocked)); Assert.Empty(E(unknown));
        Assert.Contains(E(seams).Cast<Relation>(), proof =>
        {
            var detail = (Relation)proof.Right; var links = (Relation)detail.Left;
            var target = machine.Links.Single(link => ReferenceEquals(link.Surface, links.Right));
            var nodeRows = (Relation)((Relation)detail.Right).Left;
            return ReferenceEquals(((Relation)nodeRows.Left).Right, target.To.Cell) &&
                ReferenceEquals(((Relation)nodeRows.Right).Right, target.From.Cell);
        });
        Assert.DoesNotContain(Traverse(read), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void SymmetricInkDoesNotDischargeAnAsymmetricRequiredSeamSet()
    {
        var (complete, nodes) = Grid();
        var incomplete = new AdaptiveWallpaperMachine("missing obligation orbit", complete.Grammar, nodes,
            complete.Links.Skip(1).ToArray(), S());
        var observer = new AdaptiveWallpaperSymmetry(incomplete); var read = observer.Evaluate();
        Assert.False(observer.TryReadPassed(read));
        Assert.True(observer.TryReadResult(read, out _, out var components, out _, out var blocked, out _));
        Assert.Equal(nodes.Length, E(components).Length); Assert.NotEmpty(E(blocked));
    }

    [Fact]
    public void HistoricalSymmetryKeepsItsCapturedRevisionAndFreshReadsSeeAnAsymmetricReplacement()
    {
        var (machine, nodes) = Grid(); var before = new AdaptiveWallpaperSymmetry(machine); var oldRead = before.Evaluate();
        Assert.True(before.TryReadPassed(oldRead));
        var changed = nodes[0]; var grammar = machine.Grammar;
        var lawRead = FiniteLaw.Evaluate(changed.Policy, S(changed.Current.Template.Surface), S(changed.Current.Template.Surface));
        var question = QualitativeQuestion.Ask(lawRead,
            S(new Relation(grammar.Plus.Surface, grammar.Plus.Surface), new Relation(grammar.Minus.Surface, grammar.Minus.Surface)), new Placeholder());
        changed.Commit(changed.Propose(question)); machine.Round();
        Assert.Same(grammar.Minus, changed.Current.Template);
        Assert.True(before.TryReadPassed(oldRead));
        var after = new AdaptiveWallpaperSymmetry(machine); Assert.False(after.TryReadPassed(after.Evaluate()));
        Assert.NotSame(before.Snapshot, after.Snapshot);
    }

    [Fact]
    public void CoincidentUnrelatedAnchorsRemainAmbiguousInsteadOfChoosingTheFirstNode()
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "cushion");
        var a = new AdaptiveMotifNode("a", grammar, S(), grammar.Plus); var b = new AdaptiveMotifNode("b", grammar, S(), grammar.Plus);
        var machine = new AdaptiveWallpaperMachine("ambiguous", grammar, [a, b], [], S());
        var observer = new AdaptiveWallpaperSymmetry(machine); var read = observer.Evaluate();
        Assert.False(observer.TryReadPassed(read));
        Assert.True(observer.TryReadResult(read, out _, out _, out _, out _, out var unknown)); Assert.NotEmpty(E(unknown));
    }

    [Fact]
    public void AGraphMapWithoutItsActualPositionProofsCannotCertifyTheQuarterTurn()
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "key");
        var node = new AdaptiveMotifNode("origin", grammar, S(), grammar.Plus);
        var machine = new AdaptiveWallpaperMachine("forgery control", grammar, [node], [], S());
        var observer = new AdaptiveWallpaperSymmetry(machine); var read = observer.Evaluate(); Assert.True(observer.TryReadPassed(read));
        observer.TryReadResult(read, out var nodes, out var components, out var seams, out var blocked, out var unknown);
        var component = Assert.IsType<Relation>(Assert.Single(E(components))); var detail = Assert.IsType<Relation>(component.Right);
        var graphRead = Assert.IsType<Relation>(detail.Right); var graphResult = Assert.IsType<Relation>(graphRead.Right);
        var proofAndOutcome = Assert.IsType<Relation>(graphResult.Right); var proofs = Assert.IsType<Relation>(proofAndOutcome.Left);
        var forgedGraphRead = new Relation(graphRead.Left, new Relation(graphResult.Left,
            new Relation(new Relation(S(), proofs.Right), proofAndOutcome.Right)));
        var forgedComponent = new Relation(component.Left, new Relation(detail.Left, forgedGraphRead));
        var forged = new Relation(read.Left, new Relation(new Relation(nodes, new Relation(S(forgedComponent), seams)), new Relation(blocked, unknown)));
        Assert.False(observer.TryReadPassed(forged));
    }

    private static (AdaptiveWallpaperMachine Machine, AdaptiveMotifNode[] Nodes) Grid()
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "cushion");
        var nodes = new List<AdaptiveMotifNode>();
        for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++)
            nodes.Add(new AdaptiveMotifNode($"{x},{y}", grammar,
                S([.. E(W(grammar, x < 0 ? grammar.Frame.Left : grammar.Frame.Right, Math.Abs(x) * 8)),
                    .. E(W(grammar, y < 0 ? grammar.Frame.Down : grammar.Frame.Up, Math.Abs(y) * 8))]), grammar.Plus));
        var links = new List<AdaptiveMotifLink>();
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
        {
            if (x < 2) links.Add(new(nodes[y * 3 + x], nodes[y * 3 + x + 1], grammar.East, grammar.West));
            if (y < 2) links.Add(new(nodes[y * 3 + x], nodes[(y + 1) * 3 + x], grammar.North, grammar.South));
        }
        return (new("C4 local graph composition", grammar, nodes, links, S()), nodes.ToArray());
    }
    private static Relation W(AdaptiveMotifGrammar grammar, IElement direction, int count) =>
        S(Enumerable.Range(0, count).Select(_ => (IElement)QualitativeMotionFrame.Move(direction, grammar.Reach)).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }
}
