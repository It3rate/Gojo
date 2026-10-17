using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativePeriodicCellTests
{
    [Fact]
    public void ACheckedCellRetainsTwoNonzeroGeneratorsTheirInversesAndItsZeroPerimeter()
    {
        var cell = new QualitativePeriodicCell(); var read = cell.Evaluate();
        Assert.True(cell.TryReadPassed(read));
        Assert.True(cell.TryReadResult(read, out var generators, out var inverses, out var seams,
            out var corners, out var returns, out var windings, out var pending));
        Assert.Equal(2, E(generators).Length); Assert.Equal(2, E(inverses).Length); Assert.Equal(2, E(seams).Length);
        Assert.Single(E(corners)); Assert.Single(E(returns)); Assert.Equal(2, E(windings).Length); Assert.Empty(E(pending));
        foreach (var item in E(generators))
        {
            var generator = Assert.IsType<Relation>(item); var projection = Assert.IsType<Relation>(generator.Right);
            Assert.True(cell.Frame.TryReadResult(Assert.IsType<Relation>(projection.Right), out var residual, out _, out var unresolved));
            Assert.NotEmpty(E(residual)); Assert.Empty(E(unresolved));
        }
        foreach (var item in E(inverses)) Zero(cell.Frame, Assert.IsType<Relation>(Assert.IsType<Relation>(item).Right));
        Zero(cell.Frame, Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.Single(E(returns))).Right));
        Assert.Empty(read.Carriers()); Assert.Empty(read.CarrierLocations());
        Assert.True(Contains(read, cell.Frame.Surface)); Assert.True(Contains(read, cell.Frame.Opposites));
    }

    [Fact]
    public void BothOrderedSeamCompositionsReachTheSameActualCornerWithExplicitGluing()
    {
        var cell = new QualitativePeriodicCell(); var read = cell.Evaluate();
        Assert.True(cell.TryReadResult(read, out _, out _, out _, out var corners, out _, out _, out _));
        var witness = Assert.IsType<Relation>(Assert.Single(E(corners)));
        var alternatives = Assert.IsType<Relation>(witness.Left);
        var xy = E(Assert.IsType<Relation>(alternatives.Left)).Cast<Relation>().ToArray();
        var yx = E(Assert.IsType<Relation>(alternatives.Right)).Cast<Relation>().ToArray();
        Assert.Same(xy[0].Right, xy[1].Left); Assert.Same(yx[0].Right, yx[1].Left);
        Assert.Same(xy[0].Left, yx[0].Left); Assert.Same(xy[1].Right, yx[1].Right);
        Assert.NotSame(xy[0].Right, yx[0].Right); // the intermediate actual corners differ
        Assert.All(xy, row => Assert.Contains(row, E((Relation)cell.SeamX.Right).Concat(E((Relation)cell.SeamY.Right))));
        var result = Assert.IsType<Relation>(witness.Right);
        Assert.Equal(4, E(Assert.IsType<Relation>(result.Right)).Length);
        Assert.Single(E(cell.Gluing).Cast<Relation>().Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void IdentifiedEndpointsDoNotEraseWholePeriodParticipation()
    {
        var cell = new QualitativePeriodicCell();
        Assert.True(cell.TryReadResult(cell.Evaluate(), out _, out _, out _, out _, out _, out var windings, out _));
        foreach (var item in E(windings))
        {
            var winding = Assert.IsType<Relation>(item); var pathRead = Assert.IsType<Relation>(winding.Left);
            var glue = E(Assert.IsType<Relation>(winding.Right)).Cast<Relation>().ToArray();
            Assert.NotSame(glue[0].Left, glue[1].Left); Assert.Same(glue[0].Right, glue[1].Right);
            Assert.True(cell.Frame.TryReadResult(Assert.IsType<Relation>(pathRead.Right), out var residual, out var canceled, out _));
            Assert.NotEmpty(E(residual)); Assert.Empty(E(canceled));
        }
    }

    [Fact]
    public void SubdividedBoundariesRetainAllActualSiteAndSegmentCorrespondences()
    {
        var f = new Subdivided(); var read = f.Cell.Evaluate(); Assert.True(f.Cell.TryReadPassed(read));
        Assert.True(f.Cell.TryReadResult(read, out var generators, out _, out var seams, out _, out _, out _, out _));
        Assert.Equal(3, E(f.Cell.Gluing).Cast<Relation>().Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(E(generators), item => Assert.Equal(2, E((Relation)Assert.IsType<Relation>(Assert.IsType<Relation>(item).Left).Right).Length));
        foreach (var item in E(seams))
        {
            var proof = Assert.IsType<Relation>(Assert.IsType<Relation>(item).Right);
            Assert.Equal(3, E(Assert.IsType<Relation>(proof.Left)).Length); Assert.Equal(2, E(Assert.IsType<Relation>(proof.Right)).Length);
        }
    }

    [Fact]
    public void ReversedOrSwappedSeamsCannotCertifyTheTwoPeriodCell()
    {
        var cell = new QualitativePeriodicCell();
        var reversed = S(E((Relation)cell.SeamX.Right).Reverse().ToArray());
        Pending(Copy(cell, xMap: reversed));
        Pending(new(cell.Frame, cell.Bottom, cell.RightBoundary, cell.Top, cell.LeftBoundary, cell.SeamY, cell.SeamX, cell.Gluing));
        var inverseRows = S(E((Relation)cell.SeamX.Right).Cast<Relation>().Select(row => (IElement)row.Reverse()).ToArray());
        Pending(Copy(cell, xMap: inverseRows));
    }

    [Fact]
    public void OpenMissingDuplicateAndForeignSeamRowsRemainObligations()
    {
        var cell = new QualitativePeriodicCell(); var rows = E((Relation)cell.SeamX.Right);
        foreach (var map in new[]
        {
            StructuralConstraint.Open(rows), S(rows[0]), S(rows[0], rows[0]),
            S(new Relation(new Placeholder(), new Placeholder()), rows[1])
        }) Pending(Copy(cell, xMap: map));
    }

    [Fact]
    public void EqualTotalDisplacementDoesNotJustifyAnIncorrectIntermediateSeamMap()
    {
        var f = new Subdivided(); var rows = E((Relation)f.Cell.SeamX.Right).Cast<Relation>().ToArray();
        var incorrect = S(rows[0], new Relation(rows[1].Left, rows[2].Right), rows[2]);
        Pending(Copy(f.Cell, xMap: incorrect));
        var changed = QualitativePeriodicCell.Path((Relation)f.Cell.RightBoundary.Left,
            S(QualitativeMotionFrame.Move(f.Cell.Frame.Up, new Placeholder()), QualitativeMotionFrame.Move(f.Cell.Frame.Up, new Placeholder())));
        Pending(Copy(f.Cell, right: changed));
    }

    [Fact]
    public void SharedReachIdentityIsRequiredForOppositeBoundarySegmentPromises()
    {
        var cell = new QualitativePeriodicCell();
        var top = QualitativePeriodicCell.Path((Relation)cell.Top.Left, S(QualitativeMotionFrame.Move(cell.Frame.Right, new Placeholder())));
        Pending(Copy(cell, top: top));
        var equivalentWrapper = QualitativePeriodicCell.Path((Relation)cell.Top.Left,
            S(new Relation(Assert.IsType<Relation>(E(cell.XWord)[0]).Left, Assert.IsType<Relation>(E(cell.XWord)[0]).Right)));
        var samePromise = Copy(cell, top: equivalentWrapper); Assert.True(samePromise.TryReadPassed(samePromise.Evaluate()));
    }

    [Fact]
    public void MissingOrOpenGluingCannotInferSiteIdentityFromMatchingWords()
    {
        var cell = new QualitativePeriodicCell(); var rows = E(cell.Gluing);
        Pending(Copy(cell, glue: S())); Pending(Copy(cell, glue: S(rows[..^1])));
        Pending(Copy(cell, glue: StructuralConstraint.Open(rows)));
        var split = rows.Cast<Relation>().Select(row => (IElement)new Relation(row.Left, new Placeholder())).ToArray();
        Pending(Copy(cell, glue: S(split)));
    }

    [Fact]
    public void UnrelatedQuotientClassesCannotBeSilentlyCollapsed()
    {
        var f = new Subdivided(); var one = new Placeholder();
        var overglued = S(E(f.Cell.Gluing).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, one)).ToArray());
        Pending(Copy(f.Cell, glue: overglued));
    }

    [Fact]
    public void OneDirectionAndTwoCollinearGeneratorsDoNotProveAnIndependentBasisCell()
    {
        var cell = new QualitativePeriodicCell();
        var missing = QualitativePeriodicCell.Path((Relation)cell.LeftBoundary.Left, StructuralConstraint.Open());
        Pending(Copy(cell, left: missing));
        var sharedMove = E(cell.XWord)[0];
        var left = QualitativePeriodicCell.Path((Relation)cell.LeftBoundary.Left, S(sharedMove));
        var right = QualitativePeriodicCell.Path((Relation)cell.RightBoundary.Left, S(sharedMove));
        var collinear = Copy(cell, left: left, right: right); var read = collinear.Evaluate(); Pending(collinear);
        Assert.True(collinear.TryReadResult(read, out var generators, out _, out _, out var corners, out _, out var windings, out _));
        Assert.Equal(2, E(generators).Length); Assert.Empty(E(corners)); Assert.Empty(E(windings));
    }

    [Fact]
    public void ForeignFramesAndOpenOrDisconnectedBoundariesDoNotBecomeClosedCells()
    {
        var cell = new QualitativePeriodicCell(); var foreign = new QualitativeMotionFrame();
        var top = QualitativePeriodicCell.Path((Relation)cell.Top.Left, S(QualitativeMotionFrame.Move(foreign.Right, new Placeholder())));
        Pending(Copy(cell, top: top));
        var open = QualitativePeriodicCell.Path(StructuralConstraint.Open(E((Relation)cell.Bottom.Left)), cell.XWord);
        Pending(Copy(cell, bottom: open));
        var disconnected = QualitativePeriodicCell.Path(S(new Placeholder(), E((Relation)cell.Bottom.Left)[^1]), cell.XWord);
        Pending(Copy(cell, bottom: disconnected));
    }

    [Fact]
    public void AnExtraSharedBoundarySiteIsMaterialTopologyRatherThanAnUnseenCrossing()
    {
        var f = new Subdivided();
        var bottomSites = E((Relation)f.Cell.Bottom.Left); var leftSites = E((Relation)f.Cell.LeftBoundary.Left);
        var bottom = QualitativePeriodicCell.Path(S(bottomSites[0], leftSites[1], bottomSites[2]), f.Cell.XWord);
        Pending(Copy(f.Cell, bottom: bottom));
    }

    [Fact]
    public void GuardedReadersRejectDroppedProofsAndReplacedActualWordsButAllowRebuiltWrappers()
    {
        var cell = new QualitativePeriodicCell(); var read = cell.Evaluate();
        Assert.True(cell.TryReadPassed(new Relation(read.Left, read.Right)));
        Assert.False(new QualitativePeriodicCell().TryReadPassed(read));
        var body = Assert.IsType<Relation>(read.Right);
        Assert.False(cell.TryReadPassed(new Relation(read.Left, new Relation(S(), body.Right))));
        var generators = E(Assert.IsType<Relation>(body.Left)); var first = Assert.IsType<Relation>(generators[0]);
        var source = Assert.IsType<Relation>(first.Left);
        var fakeWord = S(E(Assert.IsType<Relation>(source.Right)));
        var replaced = new Relation(new Relation(source.Left, fakeWord), first.Right);
        var fake = new Relation(read.Left, new Relation(S(replaced, generators[1]), body.Right));
        Assert.False(cell.TryReadPassed(fake));
        var rebuilt = new Relation(new Relation(source.Left, source.Right), new Relation(
            Assert.IsType<Relation>(first.Right).Left, Assert.IsType<Relation>(first.Right).Right));
        var faithful = new Relation(read.Left, new Relation(S(rebuilt, generators[1]), body.Right));
        Assert.True(cell.TryReadPassed(faithful));
    }

    private static QualitativePeriodicCell Copy(QualitativePeriodicCell c, Relation? bottom = null, Relation? right = null,
        Relation? top = null, Relation? left = null, Relation? xMap = null, Relation? yMap = null, Relation? glue = null)
    {
        bottom ??= c.Bottom; right ??= c.RightBoundary; top ??= c.Top; left ??= c.LeftBoundary;
        return new(c.Frame, bottom, right, top, left,
            QualitativePeriodicCell.Seam(left, right, xMap ?? (Relation)c.SeamX.Right),
            QualitativePeriodicCell.Seam(bottom, top, yMap ?? (Relation)c.SeamY.Right), glue ?? c.Gluing);
    }
    private static void Pending(QualitativePeriodicCell cell)
    {
        var read = cell.Evaluate(); Assert.False(cell.TryReadPassed(read));
        Assert.True(cell.TryReadResult(read, out _, out _, out _, out _, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
    }
    private static void Zero(QualitativeMotionFrame frame, Relation read)
    { Assert.True(frame.TryReadResult(read, out var residual, out _, out var pending)); Assert.Empty(E(residual)); Assert.Empty(E(pending)); }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static bool Contains(IElement tree, IElement sought) => ReferenceEquals(tree, sought) || tree is Relation r && (Contains(r.Left, sought) || Contains(r.Right, sought));

    private sealed class Subdivided
    {
        public QualitativePeriodicCell Cell { get; }
        public Subdivided()
        {
            var frame = new QualitativeMotionFrame();
            var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder(); var d = new Placeholder();
            var ab = new Placeholder(); var bc = new Placeholder(); var dc = new Placeholder(); var ad = new Placeholder();
            var h = new Placeholder(); var v = new Placeholder(); var q = new Placeholder(); var qh = new Placeholder(); var qv = new Placeholder();
            var x = QualitativeMotionFrame.Move(frame.Right, h); var y = QualitativeMotionFrame.Move(frame.Up, v);
            var bottom = QualitativePeriodicCell.Path(S(a, ab, b), S(x, x)); var right = QualitativePeriodicCell.Path(S(b, bc, c), S(y, y));
            var top = QualitativePeriodicCell.Path(S(d, dc, c), S(x, x)); var left = QualitativePeriodicCell.Path(S(a, ad, d), S(y, y));
            var sx = QualitativePeriodicCell.Seam(left, right, S(new Relation(a, b), new Relation(ad, bc), new Relation(d, c)));
            var sy = QualitativePeriodicCell.Seam(bottom, top, S(new Relation(a, d), new Relation(ab, dc), new Relation(b, c)));
            Cell = new(frame, bottom, right, top, left, sx, sy, QualitativePeriodicCell.Glue(
                new Relation(a, q), new Relation(b, q), new Relation(c, q), new Relation(d, q),
                new Relation(ab, qh), new Relation(dc, qh), new Relation(ad, qv), new Relation(bc, qv)));
        }
    }
}
