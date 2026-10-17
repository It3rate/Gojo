using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AdaptiveMotifGrammarTests
{
    [Theory]
    [InlineData("cushion")]
    [InlineData("key")]
    [InlineData("rosette")]
    public void EveryExposedPortSelectsAnActualUniqueChordOccurrenceInAnUnmeasuredPath(string family)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        Assert.DoesNotContain(Traverse(grammar.Surface), item => item is Carrier or CarrierLocation);
        foreach (var template in new[] { grammar.Plus, grammar.Minus })
        {
            var paths = E(template.Paths).Cast<Relation>().ToArray();
            var words = paths.SelectMany(path => E((Relation)path.Right)).ToArray();
            Assert.Equal(words.Length, words.Distinct(ReferenceEqualityComparer.Instance).Count());
            var ports = E(template.Ports).Cast<Relation>().ToArray(); Assert.Equal(4, ports.Length);
            foreach (var role in new[] { grammar.East, grammar.North, grammar.West, grammar.South })
            {
                var port = Assert.Single(ports, port => ReferenceEquals(port.Left, role));
                var selection = Assert.IsType<Relation>(port.Right);
                var path = Assert.IsType<Relation>(selection.Left);
                Assert.Same(paths[0], path);
                Assert.Contains(E((Relation)path.Right), word => ReferenceEquals(word, selection.Right));
            }
            foreach (var word in paths.SelectMany(path => new[] { (Relation)path.Left }.Concat(E((Relation)path.Right).Cast<Relation>())))
                foreach (var move in E(word).Cast<Relation>())
                { Assert.True(grammar.Frame.Direction(move, out _, out _, out var reach)); Assert.Same(grammar.Reach, reach); }
        }
    }

    [Theory]
    [InlineData("cushion")]
    [InlineData("key")]
    [InlineData("rosette")]
    public void ClosedContoursDoNotImmediatelyRetraceTheirOwnPreviousChordOrWrapSeam(string family)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        foreach (var template in new[] { grammar.Plus, grammar.Minus })
            foreach (var path in E(template.Paths).Cast<Relation>())
            {
                var words = E((Relation)path.Right).Cast<Relation>().ToArray();
                var closure = grammar.Frame.Reduce(S(), S(words.SelectMany(E).ToArray()));
                Assert.True(Zero(grammar.Frame, closure));
                for (var i = 0; i < words.Length; i++)
                    Assert.False(Zero(grammar.Frame, grammar.Frame.Reduce(words[i], words[(i + 1) % words.Length])));
            }
    }

    [Theory]
    [InlineData("cushion")]
    [InlineData("key")]
    [InlineData("rosette")]
    public void NeighbourBoundaryWordsRequireOpposedHandednessRatherThanMerelyEqualPortNames(string family)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        var pitchX = Word(grammar, grammar.Frame.Right, 8); var pitchY = Word(grammar, grammar.Frame.Up, 8);
        var plusEast = Position(grammar, grammar.Plus, grammar.East);
        var plusNorth = Position(grammar, grammar.Plus, grammar.North);
        var minusWest = Position(grammar, grammar.Minus, grammar.West);
        var minusSouth = Position(grammar, grammar.Minus, grammar.South);
        Assert.True(Equal(grammar, plusEast, Concat(pitchX, minusWest)));
        Assert.True(Equal(grammar, plusNorth, Concat(pitchY, minusSouth)));
        Assert.False(Equal(grammar, plusEast, Concat(pitchX, Position(grammar, grammar.Plus, grammar.West))));
        Assert.False(Equal(grammar, plusNorth, Concat(pitchY, Position(grammar, grammar.Plus, grammar.South))));
    }

    [Theory]
    [InlineData("cushion")]
    [InlineData("key")]
    [InlineData("rosette")]
    public void MirrorCorrespondenceRetainsExactInputOutputWordsAndBoundaryRolePermutation(string family)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        var maps = Assert.IsType<Relation>(grammar.Correspondence.Right);
        var pathsAndWords = Assert.IsType<Relation>(maps.Left);
        var roleAndReads = Assert.IsType<Relation>(maps.Right);
        foreach (var mapping in E(Assert.IsType<Relation>(pathsAndWords.Right)).Cast<Relation>())
        {
            var read = Assert.Single(E(Assert.IsType<Relation>(roleAndReads.Right)).Cast<Relation>(),
                read => read.Left is Relation request && ReferenceEquals(request.Right, mapping.Left));
            Assert.True(grammar.MirrorTransform.TryReadComplete(read, out var actual)); Assert.Same(mapping.Right, actual);
        }
        foreach (var role in E(Assert.IsType<Relation>(roleAndReads.Left)).Cast<Relation>())
        {
            var plus = Position(grammar, grammar.Plus, role.Left); var minus = Position(grammar, grammar.Minus, role.Right);
            var projection = grammar.MirrorTransform.Apply(plus);
            Assert.True(grammar.MirrorTransform.TryReadComplete(projection, out var reflected));
            Assert.True(Equal(grammar, reflected, minus));
        }
    }

    private static Relation Position(AdaptiveMotifGrammar grammar, AdaptiveMotifGrammar.Template template, IElement role)
    {
        var port = E(template.Ports).Cast<Relation>().Single(port => ReferenceEquals(port.Left, role));
        var selection = (Relation)port.Right; var path = (Relation)selection.Left; var position = (Relation)path.Left;
        foreach (var chord in E((Relation)path.Right).Cast<Relation>())
        {
            if (ReferenceEquals(chord, selection.Right)) return position;
            var read = grammar.Frame.Reduce(position, chord);
            Assert.True(grammar.Frame.TryReadResult(read, out position, out _, out _));
        }
        throw new InvalidOperationException("A port must retain its actual before-chord selection.");
    }
    private static bool Equal(AdaptiveMotifGrammar grammar, Relation a, Relation b) => Zero(grammar.Frame, grammar.Frame.Reduce(a, Inverse(grammar, b)));
    private static bool Zero(QualitativeMotionFrame frame, Relation read) =>
        frame.TryReadResult(read, out var rest, out _, out var pending) && E(rest).Length == 0 && E(pending).Length == 0;
    private static Relation Inverse(AdaptiveMotifGrammar grammar, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(grammar.Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation Word(AdaptiveMotifGrammar grammar, IElement direction, int count) =>
        S(Enumerable.Range(0, count).Select(_ => (IElement)QualitativeMotionFrame.Move(direction, grammar.Reach)).ToArray());
    private static Relation Concat(params Relation[] words) => S(words.SelectMany(E).ToArray());
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        {
            if (!visited.Add(item)) continue; yield return item;
            if (item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
