using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeActionAlgebraTests
{
    [Fact]
    public void SignedDirectionQuarterCycleActsAcrossAxesWithoutNumericParticipants()
    {
        var f = new Fixture(); Assert.True(f.Algebra.TryApply(f.R, f.Frame.Right, out var result, out var witness)); Assert.Same(f.Frame.Up, result);
        Assert.True(Contains(witness, f.R)); Assert.True(Contains(witness, f.Frame.Right));
        Assert.DoesNotContain(Traverse(witness), element => element is Carrier or CarrierLocation);
        Equal(f, S(f.R, f.R, f.R, f.R), S(), true);
        Equal(f, S(f.R, f.R), S(), false); Equal(f, S(f.M, f.M), S(), true);
        var inverse = Map(f, f.Algebra.Inverse(f.R)); Equal(f, S(f.M, f.R, f.M), S(inverse), true);
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, S(f.R, f.R, f.R, f.R)), out var order)); Assert.True(order);
        Assert.Same(f.Algebra.Domain, f.Algebra.Surface.Left); Assert.Same(f.Algebra.Identity, f.Algebra.Surface.Right);
    }

    [Fact]
    public void CompositionOrderIsFirstThenSecondAndEveryPathConsumesActualRows()
    {
        var f = new Fixture(); var read = f.Algebra.Compose(f.R, f.M);
        var map = Map(f, read);
        Assert.True(f.Algebra.TryApply(map, f.Frame.Right, out var output, out _)); Assert.Same(f.Frame.Down, output);
        Assert.True(f.Algebra.TryReadResult(read, out _, out _, out _, out var paths));
        foreach (var path in E(paths).Cast<Relation>())
        {
            var row = Assert.IsType<Relation>(path.Left); var used = E(Assert.IsType<Relation>(path.Right)).Cast<Relation>().ToArray(); Assert.Equal(2, used.Length);
            Assert.Contains(E(f.R), actual => ReferenceEquals(actual, used[0])); Assert.Contains(E(f.M), actual => ReferenceEquals(actual, used[1]));
            Assert.Same(row.Left, used[0].Left); Assert.Same(used[0].Right, used[1].Left); Assert.Same(row.Right, used[1].Right);
        }
        var reversed = Map(f, f.Algebra.Compose(f.M, f.R));
        Assert.True(f.Algebra.TryApply(reversed, f.Frame.Right, out var other, out _)); Assert.Same(f.Frame.Up, other);
        Assert.NotSame(output, other);
    }

    [Fact]
    public void InverseRetainsActualRowReversalAndBothInverseProductsAreIdentity()
    {
        var f = new Fixture(); var read = f.Algebra.Inverse(f.R); var inverse = Map(f, read);
        Equal(f, S(f.R, inverse), S(), true); Equal(f, S(inverse, f.R), S(), true);
        var derivation = Assert.IsType<Relation>(Assert.IsType<Relation>(read.Left).Right);
        var maps = Assert.IsType<Relation>(derivation.Left); Assert.Same(f.R, maps.Left);
        foreach (var link in E(Assert.IsType<Relation>(derivation.Right)).Cast<Relation>())
        {
            var reverse = Assert.IsType<Relation>(link.Left); var original = Assert.IsType<Relation>(link.Right);
            Assert.Contains(E(f.R), row => ReferenceEquals(row, original));
            Assert.Contains(E(Assert.IsType<Relation>(maps.Right)), row => ReferenceEquals(row, reverse));
            Assert.Same(original.Right, reverse.Left); Assert.Same(original.Left, reverse.Right);
        }
    }

    [Fact]
    public void TwoIndependentFlipsFormV4RatherThanTheFourStepCycleC4()
    {
        var f = new Fixture(); Equal(f, S(f.H, f.H), S(), true); Equal(f, S(f.M, f.M), S(), true);
        Assert.True(f.Algebra.TryReadEquality(f.Algebra.Commutes(f.H, f.M), out var commutes)); Assert.True(commutes);
        Equal(f, S(f.H, f.M), S(f.R, f.R), true); Equal(f, S(f.H, f.M), S(f.R), false);
        var both = Map(f, f.Algebra.Compose(f.H, f.M));
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(both, S(both, both)), out var two)); Assert.True(two);
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, S(f.R, f.R)), out var notTwo)); Assert.False(notTwo);
        Assert.True(f.Algebra.TryReadEquality(f.Algebra.Commutes(f.R, f.M), out var noncommuting)); Assert.False(noncommuting);
    }

    [Fact]
    public void ExactOrderRequiresAllProperPositivePrefixesAndAnActualRepeatedAction()
    {
        var f = new Fixture();
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.Algebra.Identity, S(f.Algebra.Identity)), out var one)); Assert.True(one);
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.Algebra.Identity,
            S(f.Algebra.Identity, f.Algebra.Identity, f.Algebra.Identity, f.Algebra.Identity)), out var falseFour)); Assert.False(falseFour);
        Assert.True(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, S(f.R, f.R, f.R, f.R, f.R, f.R, f.R, f.R)), out var falseEight)); Assert.False(falseEight);
        Assert.False(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, S()), out _));
        Assert.False(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, StructuralConstraint.Open(f.R, f.R, f.R, f.R)), out _));
        var lookalike = S(E(f.R)); Assert.False(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(f.R, S(f.R, f.R, f.R, lookalike)), out _));
    }

    [Fact]
    public void AssociativityIdentityAndInverseLawsHoldOnActualParticipants()
    {
        var f = new Fixture(); var inverse = Map(f, f.Algebra.Inverse(f.R));
        var rm = Map(f, f.Algebra.Compose(f.R, f.M)); var mi = Map(f, f.Algebra.Compose(f.M, inverse));
        Equal(f, S(rm, inverse), S(f.R, mi), true);
        Equal(f, S(f.Algebra.Identity, f.R), S(f.R), true); Equal(f, S(f.R, f.Algebra.Identity), S(f.R), true);
        var inverseTwice = Map(f, f.Algebra.Inverse(inverse)); Equal(f, S(inverseTwice), S(f.R), true);
    }

    [Fact]
    public void EqualActionResultsRetainDifferentNativeWordsAndRows()
    {
        var f = new Fixture(); var copy = S(E(f.R).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, row.Right)).ToArray());
        Equal(f, S(copy), S(f.R), true);
        var first = f.Algebra.Evaluate(S(f.R)); var second = f.Algebra.Evaluate(S(copy));
        Assert.True(f.Algebra.TryReadResult(first, out _, out _, out _, out var a));
        Assert.True(f.Algebra.TryReadResult(second, out _, out _, out _, out var b));
        var one = Assert.Single(E(Assert.IsType<Relation>(Assert.IsType<Relation>(E(a)[0]).Right)));
        var other = Assert.Single(E(Assert.IsType<Relation>(Assert.IsType<Relation>(E(b)[0]).Right)));
        Assert.NotSame(one, other); Assert.Same(E(f.R)[0], one); Assert.Same(E(copy)[0], other);
    }

    [Fact]
    public void OpenMissingForeignAndNonbijectiveMapsKeepTheirWholeObligations()
    {
        var f = new Fixture(); var rows = E(f.R).Cast<Relation>().ToArray();
        var foreign = new Relation(f.Frame.Right, new Placeholder());
        var repeatedSource = S(rows[0], rows[0], rows[2], rows[3]);
        var collapsedTarget = S(new Relation(f.Frame.Right, f.Frame.Up), new Relation(f.Frame.Up, f.Frame.Up), rows[2], rows[3]);
        foreach (var map in new[] { StructuralConstraint.Open(E(f.R)), S(rows.Take(3).Cast<IElement>().ToArray()),
            S(foreign, rows[1], rows[2], rows[3]), repeatedSource, collapsedTarget, S(new Placeholder()) })
        {
            var read = f.Algebra.Evaluate(S(map));
            Assert.True(f.Algebra.TryReadResult(read, out var result, out var blocked, out var pending, out var paths));
            Assert.False(Closed(result)); Assert.Empty(E(result)); Assert.Empty(E(paths)); Assert.True(E(blocked).Length + E(pending).Length > 0);
            Assert.True(Contains(read, map)); Assert.False(f.Algebra.TryApply(map, f.Frame.Right, out _, out _));
            Assert.False(f.Algebra.TryReadEquality(f.Algebra.Compare(S(map), S()), out _));
            Assert.False(f.Algebra.TryReadOrder(f.Algebra.CheckOrder(map, S(map, map, map, map)), out _));
            var inverse = f.Algebra.Inverse(map); Assert.True(f.Algebra.TryReadResult(inverse, out var unsupported, out _, out _, out _)); Assert.False(Closed(unsupported));
        }
    }

    [Fact]
    public void OpenWordDoesNotPromoteItsKnownPrefixToAFinalAction()
    {
        var f = new Fixture(); var word = StructuralConstraint.Open(f.R);
        var read = f.Algebra.Evaluate(word); Assert.True(f.Algebra.TryReadResult(read, out var result, out _, out var pending, out var paths));
        Assert.False(Closed(result)); Assert.Empty(E(result)); Assert.Empty(E(paths)); Assert.NotEmpty(E(pending)); Assert.True(Contains(read, word));
        var domain = StructuralConstraint.Open(E(f.Algebra.Domain)); var open = new QualitativeActionAlgebra(domain);
        Assert.True(open.TryReadResult(open.Evaluate(S()), out var identity, out _, out var openDomain, out _)); Assert.False(Closed(identity)); Assert.NotEmpty(E(openDomain));
    }

    [Fact]
    public void LookalikeDirectionRelationsDoNotAcquireActualParticipantIdentity()
    {
        var f = new Fixture(); var lookalike = new Relation(f.Frame.Right.Left, f.Frame.Right.Right);
        Assert.False(f.Algebra.TryApply(f.R, lookalike, out _, out var failure)); Assert.True(Contains(failure, lookalike));
        var map = S(new Relation(lookalike, f.Frame.Up), E(f.R)[1], E(f.R)[2], E(f.R)[3]);
        Assert.True(f.Algebra.TryReadResult(f.Algebra.Evaluate(S(map)), out var unsupported, out var blocked, out _, out _));
        Assert.False(Closed(unsupported)); Assert.NotEmpty(E(blocked));
        var duplicate = new QualitativeActionAlgebra(S(f.Frame.Right, f.Frame.Right));
        Assert.True(duplicate.TryReadResult(duplicate.Evaluate(S()), out var badIdentity, out _, out var pending, out _)); Assert.False(Closed(badIdentity)); Assert.NotEmpty(E(pending));
    }

    [Fact]
    public void ForgedResultPathsAndCopiedEqualitySuccessCannotPassGuardedReaders()
    {
        var f = new Fixture(); var read = f.Algebra.Evaluate(S(f.R));
        Assert.True(f.Algebra.TryReadResult(read, out var result, out var blocked, out var pending, out var paths));
        var wrongPath = new Relation(E(result)[0], S(new Relation(f.Frame.Right, f.Frame.Up)));
        var forged = new Relation(read.Left, new Relation(result, new Relation(blocked, new Relation(pending, S(wrongPath, E(paths)[1], E(paths)[2], E(paths)[3])))));
        Assert.False(f.Algebra.TryReadResult(forged, out _, out _, out _, out _));
        var omitted = new Relation(read.Left, new Relation(result, new Relation(blocked, new Relation(pending, S()))));
        Assert.False(f.Algebra.TryReadResult(omitted, out _, out _, out _, out _));
        var different = f.Algebra.Compare(S(f.R), S()); var actual = Assert.IsType<Relation>(different.Right); var rest = Assert.IsType<Relation>(actual.Right);
        var copiedSuccess = new Relation(different.Left, new Relation(rest.Left, new Relation(S(), rest.Right)));
        Assert.False(f.Algebra.TryReadEquality(copiedSuccess, out _));
        var foreign = new Fixture(); Assert.False(foreign.Algebra.TryReadResult(read, out _, out _, out _, out _));
    }

    [Fact]
    public void OrderReaderRejectsAnOmittedProperPrefixOrDifferentWordWithTheSameFinalMap()
    {
        var f = new Fixture(); var read = f.Algebra.CheckOrder(f.R, S(f.R, f.R, f.R, f.R));
        var body = Assert.IsType<Relation>(read.Right); var prefixes = E(Assert.IsType<Relation>(body.Left));
        var removed = new Relation(read.Left, new Relation(S(prefixes.Skip(1).ToArray()), body.Right)); Assert.False(f.Algebra.TryReadOrder(removed, out _));
        var substitute = f.Algebra.Compare(S(f.R, f.Algebra.Identity), S());
        var changed = new Relation(read.Left, new Relation(S(prefixes[0], substitute, prefixes[2], prefixes[3]), body.Right));
        Assert.False(f.Algebra.TryReadOrder(changed, out _));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public QualitativeActionAlgebra Algebra { get; }
        public Relation R { get; }
        public Relation M { get; }
        public Relation H { get; }
        public Fixture()
        {
            Algebra = new(S(Frame.Right, Frame.Up, Frame.Left, Frame.Down));
            R = S(new Relation(Frame.Right, Frame.Up), new Relation(Frame.Up, Frame.Left), new Relation(Frame.Left, Frame.Down), new Relation(Frame.Down, Frame.Right));
            M = S(new Relation(Frame.Right, Frame.Right), new Relation(Frame.Up, Frame.Down), new Relation(Frame.Left, Frame.Left), new Relation(Frame.Down, Frame.Up));
            H = S(new Relation(Frame.Right, Frame.Left), new Relation(Frame.Up, Frame.Up), new Relation(Frame.Left, Frame.Right), new Relation(Frame.Down, Frame.Down));
        }
    }
    private static Relation Map(Fixture f, Relation read)
    {
        Assert.True(f.Algebra.TryReadResult(read, out var map, out var blocked, out var pending, out _));
        Assert.True(Closed(map)); Assert.Empty(E(blocked)); Assert.Empty(E(pending)); return map;
    }
    private static void Equal(Fixture f, Relation left, Relation right, bool expected)
    { Assert.True(f.Algebra.TryReadEquality(f.Algebra.Compare(left, right), out var equal)); Assert.Equal(expected, equal); }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(root);
        while (work.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); } }
    }
    private static bool Contains(IElement root, IElement item) => Traverse(root).Any(element => ReferenceEquals(element, item));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
