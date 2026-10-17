using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class GraphCoverageSupplierTests
{
    [Fact]
    public void ExposedDisconnectedStrokesCanSupplyCoverageWithoutAPrivateCursorOrClock()
    {
        var frame = new QualitativeMotionFrame(); var x = new Carrier(); var y = new Carrier();
        Relation Point(long a, long b) => new(new Relation(frame.Horizontal, new CarrierLocation(x, a)),
            new Relation(frame.Vertical, new CarrierLocation(y, b)));
        var first = new Relation(new Placeholder(), new Relation(Point(0, 0), Point(4, 4)));
        var second = new Relation(new Placeholder(), new Relation(Point(0, 12), Point(4, 8)));
        var exposed = new Relation(frame.Surface, S(first, second));
        var read = FriezeCoverage.Evaluate(frame, exposed, S(first, second), 8, minimumY: -2, maximumY: 14);
        Assert.Equal(0, read.Projection.Unresolved);
        Assert.True(read.Coverage.TryReadResult(read.Read, out var empty, out var single, out _, out _, out var centers, out var pending));
        Assert.NotEmpty(FiniteLaw.Entries(empty)); Assert.NotEmpty(FiniteLaw.Entries(single));
        Assert.NotEmpty(FiniteLaw.Entries(centers)); Assert.Empty(FiniteLaw.Entries(pending));
        Assert.Contains(Traverse(read.Surface), item => ReferenceEquals(item, exposed));
        Assert.Contains(Traverse(read.Surface), item => ReferenceEquals(item, first));
        Assert.Contains(Traverse(read.Surface), item => ReferenceEquals(item, second));
    }

    [Fact]
    public void AForeignAxisFrameCannotTurnUninterpretedStrokesIntoKnownWhitespace()
    {
        var frame = new QualitativeMotionFrame(); var foreign = new QualitativeMotionFrame();
        var x = new Carrier(); var y = new Carrier();
        Relation Point(long a, long b) => new(new Relation(frame.Horizontal, new CarrierLocation(x, a)),
            new Relation(frame.Vertical, new CarrierLocation(y, b)));
        var stroke = new Relation(new Placeholder(), new Relation(Point(0, 0), Point(4, 4)));
        var source = new Relation(frame.Surface, S(stroke));
        var read = FriezeCoverage.Evaluate(foreign, source, S(stroke), 8, minimumY: -2, maximumY: 14);
        Assert.True(read.Projection.Unresolved > 0);
        Assert.True(read.Coverage.TryReadResult(read.Read, out var empty, out _, out _, out _, out _, out var pending));
        Assert.Empty(FiniteLaw.Entries(empty)); Assert.NotEmpty(FiniteLaw.Entries(pending));
        Assert.Contains(Traverse(read.Surface), item => ReferenceEquals(item, source));
    }

    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation row) { stack.Push(row.Left); stack.Push(row.Right); } }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
