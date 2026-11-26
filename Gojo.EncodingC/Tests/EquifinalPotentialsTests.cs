using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Equifinal miniature configurations with distinct downstream
/// potentials (combining ops_claude's ConfigurationPotentialTests 3-square
/// fixture with ops_sol's OperationScopeTests grouped workspace commit &amp;
/// post-capture boundary independence).</summary>
public sealed class EquifinalPotentialsTests
{
    private const int Grid = 7;
    private static readonly Plane Here = Plane.Of(1, 0);

    private sealed record Figure(int Vertices, int Width, int Height, bool Closed)
    {
        public bool Square => Closed && Vertices == 4 && Width > 0 && Width == Height;
        public bool Rectangle => Closed && Vertices == 4 && Width > 0 && Height > 0 && Width != Height;

        public static Figure Of(IReadOnlyList<(int X, int Y)> points, bool closed)
        {
            if (points.Count == 0) return new Figure(0, 0, 0, closed);
            int minX = points[0].X, maxX = points[0].X, minY = points[0].Y, maxY = points[0].Y;
            for (var i = 1; i < points.Count; i++)
            {
                if (points[i].X < minX) minX = points[i].X;
                if (points[i].X > maxX) maxX = points[i].X;
                if (points[i].Y < minY) minY = points[i].Y;
                if (points[i].Y > maxY) maxY = points[i].Y;
            }
            return new Figure(points.Count, maxX - minX, maxY - minY, closed);
        }
    }

    // ---- 1. Lines: crossing column/row bit lines paired at every grid cell ----

    private static readonly TransitionLaw CellAnd = Operations.Bind(
        Operations.Product(Shape.Bit, Shape.Bit, Shape.Bit), [Shape.Bit, Shape.Bit, Shape.Bit], [0, 1, -1], [-1, -1, 2]);

    private static Figure BuildLinesSquare(int[] cols, int[] rows)
    {
        var store = new Store(3, Grid * Grid);
        for (var lane = 0; lane < Grid * Grid; lane++)
        {
            store.Write(0, lane, cols.Contains(lane % Grid));
            store.Write(1, lane, rows.Contains(lane / Grid));
        }
        new Engine().Add(Tension.Wire(CellAnd, [new Binding(store, 0), new Binding(store, 1), new Binding(store, 2)], Plane.Ones(Grid * Grid))).Step();
        var corners = Enumerable.Range(0, Grid * Grid)
            .Where(lane => store.Read(2, lane))
            .Select(lane => (lane % Grid, lane / Grid))
            .ToArray();
        return Figure.Of(corners, closed: true);
    }

    // ---- 2. Path: a mover stepping along its heading and turning by J (derived from Odv) ----

    private static OdvWord J(OdvWord w) => w.PositiveLeft ? w.FaceFlip() : w.Reorient();

    private static readonly Shape Coord = Shape.Unary(Grid - 1);

    private static readonly TransitionLaw StepHeading = TransitionLaw.FromFunction([Odv.Word, Coord, Coord], s =>
    {
        var w = new OdvWord((byte)s[0]);
        var d = w.Negative ? -1 : 1;
        var (x, y) = w.PositiveLeft ? (s[1] + d, s[2]) : (s[1], s[2] + d);
        return x is < 0 or >= Grid || y is < 0 or >= Grid ? s : [s[0], x, y];
    }, new Accounting(true, new Unchanged(0)));

    private static TransitionLaw TurnAtEnd(int turns, int grow) => TransitionLaw.FromFunction([Coord, Coord, Odv.Word], s =>
    {
        if (s[1] > 1) return [s[0], s[1] - 1, s[2]];
        var w = new OdvWord((byte)s[2]);
        for (var i = 0; i < turns; i++) w = J(w);
        var nextSide = s[0] + grow > Grid - 1 ? Grid - 1 : s[0] + grow;
        return [nextSide, nextSide, w.Code];
    }, new Accounting(true));

    private static Figure BuildPathSquare(int side, int turns = 1, int grow = 0)
    {
        var st = new Store(0, 1);
        Field F(Shape shape, int value)
        {
            var f = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => st.AddPlane())]);
            f.Write(st, 0, value);
            return f;
        }
        var start = OdvWord.Of(positiveLeft: true, negative: false, detectable: true);
        Field heading = F(Odv.Word, start.Code), x = F(Coord, 2), y = F(Coord, 2), len = F(Coord, side), left = F(Coord, side);
        var eng = new Engine()
            .Add(Tension.Wire(StepHeading, [heading.On(st), x.On(st), y.On(st)], Here))
            .Add(Tension.Wire(TurnAtEnd(turns, grow), [len.On(st), left.On(st), heading.On(st)], Here));
        var vertices = new List<(int, int)>();
        var closed = false;
        for (var step = 0; step < 48 && !closed; step++)
        {
            var prev = heading.Read(st, 0);
            eng.Step();
            var at = (x.Read(st, 0)!.Value, y.Read(st, 0)!.Value);
            if (heading.Read(st, 0) != prev && !vertices.Contains(at)) vertices.Add(at);
            closed = at == (2, 2) && heading.Read(st, 0) == start.Code && left.Read(st, 0) == len.Read(st, 0);
        }
        return Figure.Of(vertices, closed);
    }

    // ---- 3. Circle: unsigned live-landmark kick-drift orbit (weight k picks polygon!) ----

    private static Figure BuildCircleOrbit(int k, int amplitude = 1)
    {
        const int Track = 8, Mark = 4;
        var st = new Store(0, 1);
        Field U(int val)
        {
            var f = new Field(Shape.Unary(Track), [.. Enumerable.Range(0, Track).Select(_ => st.AddPlane())]);
            f.Write(st, 0, val);
            return f;
        }
        Field x = U(Mark + amplitude), v = U(Mark), lm = U(Mark), rest = U(Mark);
        var eng = new Engine();
        if (k != 0) eng.Add(Tension.Wire(Numbers.LandmarkShear(Track, k), [v.On(st), lm.On(st), x.On(st)], Here));
        eng.Add(Tension.Wire(Numbers.LandmarkShear(Track, 1), [x.On(st), v.On(st), rest.On(st)], Here));

        (int, int) Read() => (x.Read(st, 0)!.Value - Mark, v.Read(st, 0)!.Value - Mark);
        var start = Read();
        var pts = new List<(int, int)> { start };
        for (var step = 0; step < 12; step++)
        {
            eng.Step();
            var cur = Read();
            if (cur == start) return Figure.Of(pts, closed: true);
            pts.Add(cur);
        }
        return Figure.Of(pts.Distinct().ToArray(), closed: false);
    }

    [Fact]
    public void ThreeMiniatureSquareConfigurationsHaveBarelyOverlappingPerturbationNeighbourhoods()
    {
        // All three configurations start by making a closed 4-vertex square!
        Assert.True(BuildLinesSquare([1, 3], [1, 3]).Square);
        Assert.True(BuildPathSquare(side: 2).Square);
        Assert.Equal((4, true), (BuildCircleOrbit(k: 2).Vertices, BuildCircleOrbit(k: 2).Closed));

        // 1-change neighbourhood of Lines: rectangles (moving 1 line) and 6-corner grids (adding 1 line)
        Figure[] linesN = [BuildLinesSquare([1, 4], [1, 3]), BuildLinesSquare([1, 3, 5], [1, 3]), BuildLinesSquare([1], [1, 3])];
        // 1-change neighbourhood of Path: scaled squares, segments (J^2), straight lines (J^0), spirals (grow=1)
        Figure[] pathN = [BuildPathSquare(1), BuildPathSquare(3), BuildPathSquare(2, turns: 2), BuildPathSquare(2, turns: 0), BuildPathSquare(1, grow: 1)];
        // 1-change neighbourhood of Circle: point (k=0), hexagon (k=1), triangle (k=3), runaway (k=4)
        Figure[] circleN = [BuildCircleOrbit(0), BuildCircleOrbit(1), BuildCircleOrbit(3), BuildCircleOrbit(4)];

        // Only Lines produces a rectangle in one local change!
        Assert.Contains(linesN, f => f.Rectangle);
        Assert.DoesNotContain(pathN, f => f.Rectangle);

        // Only Circle produces a closed triangle (3 vertices) or hexagon (6 vertices) in one weight change!
        Assert.Contains(circleN, f => f.Closed && f.Vertices == 3);
        Assert.Contains(circleN, f => f.Closed && f.Vertices == 6);
        Assert.DoesNotContain(linesN, f => f.Vertices == 3);
        Assert.DoesNotContain(pathN, f => f.Closed && f.Vertices == 3);

        // Only Path opens into a turning spiral!
        Assert.Contains(pathN, f => !f.Closed && f.Vertices >= 6);
        Assert.DoesNotContain(linesN, f => !f.Closed);
    }

    [Fact]
    public void ScopedWorkspaceCommitPreservesSharedJointRefusesSplitCommitAndInvalidatesOnStaleGain()
    {
        // ops_sol OperationScopeTests contract: scaling two connected span endpoints about a datum
        // inside a selected Workspace with Fix() keeping datum & gain in the commit basis.
        const int Datum = 0, Joint = 1, Tip = 2, Carried = 3, Sites = 4, Cap = 10;
        var target = Plane.Of(Sites, Joint, Tip);
        var scaleLaw = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Unary(2), Shape.Unary(Cap), Shape.Unary(Cap)],
            before =>
            {
                var (d, g) = (before[0], before[1]);
                var j = d + g * (before[2] - d);
                var t = d + g * (before[3] - d);
                return j is >= 0 and <= Cap && t is >= 0 and <= Cap ? [d, g, j, t] : null;
            },
            new Accounting(false, new Unchanged(0), new Unchanged(1)));

        var source = new Store(0, Sites);
        var origin = Field.Bit(source.AddPlane());
        var gain = new Field(Shape.Unary(2), source.AddPlane(), source.AddPlane());
        var coord = new Field(Shape.Unary(Cap), [.. Enumerable.Range(0, Cap).Select(_ => source.AddPlane())]);
        origin.Write(source, Datum, 1);
        gain.Write(source, Datum, 2);
        coord.Write(source, Joint, 2);
        coord.Write(source, Tip, 3);
        coord.Write(source, Carried, 5);

        Workspace MakePreview()
        {
            var ws = new Workspace(source).Select(target);
            var datumMask = Plane.Of(Sites, Datum);
            foreach (var p in origin.Planes.Concat(gain.Planes)) ws.Fix(p, datumMask);
            return ws;
        }

        WorkspaceStep RunScale(Workspace ws)
        {
            var w = new Wiring(scaleLaw, origin.On(ws), gain.On(ws), coord.On(ws), coord.On(ws));
            w.Add(Datum, Datum, Joint, Tip);
            return ws.Apply(w);
        }

        // 1. Atomic commit succeeds for whole target, refuses split commit of only Joint!
        var ws1 = MakePreview();
        Assert.True(RunScale(ws1).Step.Unadmitted.None);
        Assert.Equal((3, 5, 5), (coord.Read(ws1, Joint), coord.Read(ws1, Tip), coord.Read(ws1, Carried)));

        var split = ws1.Commit(Plane.Of(Sites, Joint));
        Assert.False(split.Committed);
        Assert.Equal(Plane.Of(Sites, Tip), split.Split);

        Assert.True(ws1.Commit().Committed);
        Assert.Equal((3, 5, 5), (coord.Read(source, Joint), coord.Read(source, Tip), coord.Read(source, Carried)));

        // 2. Mutating the bound gain on source invalidates the whole prepared patch!
        var ws2 = MakePreview();
        Assert.True(RunScale(ws2).Step.Unadmitted.None);
        gain.Write(source, Datum, 1);
        var stale = ws2.Commit();
        Assert.False(stale.Committed);
        Assert.Equal(Plane.Of(Sites, Datum), stale.Stale);
        Assert.Equal((3, 5, 5), (coord.Read(source, Joint), coord.Read(source, Tip), coord.Read(source, Carried)));
    }
}
