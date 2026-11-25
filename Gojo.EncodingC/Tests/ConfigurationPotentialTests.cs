using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>One output from several configurations, each with its own
/// potential (an exploratory fixture; ops_claude GojoOperationsComparison-2026-11.md).
/// A square is made three ways: as two pairs of crossing lines whose corners are
/// the pairing of a column with a row; as a path, a mover that turns by the
/// orientation generator J after each side; and as points around a circle, an
/// orbit of kick and drift whose weight sets how many points it visits. All
/// three give four vertices a quarter turn apart. What differs is what is one
/// change away. Moving one line makes rectangles and adding one makes grids;
/// changing the path's side scales it, changing its turn makes a segment or a
/// line, and letting the side grow at each turn makes a spiral; changing the
/// circle's weight makes a triangle or a hexagon. Potential is read numerically:
/// perturb one count, weight or law, and see which outputs are reachable.</summary>
public sealed class ConfigurationPotentialTests
{
    private const int Grid = 7;

    private static readonly Plane Here = Plane.Of(1, 0);

    /// <summary>A shape read off a configuration: its vertices, the box they
    /// span, and whether the configuration came back to where it began.</summary>
    private sealed record Figure(int Vertices, int Width, int Height, bool Closed)
    {
        public static Figure Of(IReadOnlyCollection<(int X, int Y)> points, bool closed) =>
            points.Count == 0 ? new(0, 0, 0, closed)
                : new(points.Count, points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y), closed);

        public bool Square => Vertices == 4 && Closed && Width == Height;

        public bool Rectangle => Vertices == 4 && Closed && Width != Height;
    }

    // ---- Lines: the corners are the pairing of a column line with a row line ----

    /// <summary>Every cell of a 7 × 7 grid is a lane; a lane is on a column
    /// line, on a row line, and a corner where both hold: the product of two
    /// bits, make hold at rung 0, written in one step.</summary>
    private static Figure Lines(int[] columns, int[] rows)
    {
        var store = new Store(0, Grid * Grid);
        int column = store.AddPlane(), row = store.AddPlane(), corner = store.AddPlane();
        for (var lane = 0; lane < Grid * Grid; lane++)
        {
            Field.Bit(column).Write(store, lane, columns.Contains(lane % Grid) ? 1 : 0);
            Field.Bit(row).Write(store, lane, rows.Contains(lane / Grid) ? 1 : 0);
        }
        var one = Shape.Unary(1);
        var hold = Operations.Bind(Operations.Product(1, 1), [one, one, one], [0, 1, -1], [-1, -1, 2]);
        new Engine().Add(Tension.Wire(hold, [new Binding(store, column), new Binding(store, row), new Binding(store, corner)], Plane.Ones(Grid * Grid))).Step();
        var corners = Enumerable.Range(0, Grid * Grid).Where(lane => store.Read(corner, lane)).Select(lane => (lane % Grid, lane / Grid)).ToArray();
        return Figure.Of(corners, closed: true);
    }

    // ---- Path: a mover that turns by a power of J after each side ----

    private static readonly Shape Heading = Odv.Word, Coordinate = Shape.Unary(Grid - 1), Side = Shape.Unary(Grid - 1);

    private static OdvWord J(OdvWord w) => w.PositiveLeft ? w.FaceFlip() : w.Reorient();

    /// <summary>One unit along the heading: x on the forward face, y on the
    /// other, the side word's sign giving the way. At the edge the move holds.</summary>
    private static readonly TransitionLaw Move = TransitionLaw.FromFunction([Heading, Coordinate, Coordinate], s =>
    {
        var w = new OdvWord((byte)s[0]);
        var d = w.Negative ? -1 : 1;
        var (x, y) = w.PositiveLeft ? (s[1] + d, s[2]) : (s[1], s[2] + d);
        return x is < 0 or >= Grid || y is < 0 or >= Grid ? s : [s[0], x, y];
    }, new Accounting(true, new Unchanged(0)));

    /// <summary>Counts down the side; at its end turns by J to the power
    /// <paramref name="turn"/> and starts the next side, <paramref name="grow"/>
    /// longer.</summary>
    private static TransitionLaw Turn(int turn, int grow) => TransitionLaw.FromFunction([Side, Side, Heading], s =>
    {
        if (s[1] > 1) return [s[0], s[1] - 1, s[2]];
        var w = new OdvWord((byte)s[2]);
        for (var i = 0; i < turn; i++) w = J(w);
        var side = s[0] + grow > Grid - 1 ? Grid - 1 : s[0] + grow;
        return [side, side, w.Code];
    }, new Accounting(true));

    private static Figure Path(int side, int turn = 1, int grow = 0)
    {
        var store = new Store(0, 1);
        Field F(Shape shape, int value)
        {
            var f = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
            f.Write(store, 0, value);
            return f;
        }
        var start = OdvWord.Of(positiveLeft: true, negative: false, detectable: true);
        Field heading = F(Heading, start.Code), x = F(Coordinate, 2), y = F(Coordinate, 2), length = F(Side, side), left = F(Side, side);
        var engine = new Engine()
            .Add(Tension.Wire(Move, [heading.On(store), x.On(store), y.On(store)], Here))
            .Add(Tension.Wire(Turn(turn, grow), [length.On(store), left.On(store), heading.On(store)], Here));
        var vertices = new List<(int, int)>();
        var closed = false;
        for (var step = 0; step < 48 && !closed; step++)
        {
            var before = heading.Read(store, 0);
            engine.Step();
            var at = (x.Read(store, 0)!.Value, y.Read(store, 0)!.Value);
            if (heading.Read(store, 0) != before && !vertices.Contains(at)) vertices.Add(at);
            closed = at == (2, 2) && heading.Read(store, 0) == start.Code && left.Read(store, 0) == length.Read(store, 0);
        }
        return Figure.Of(vertices, closed);
    }

    // ---- Circle: an orbit of kick and drift, the weight setting its points ----

    private const int Track = 8, Mark = 4;

    private static Figure Circle(int weight, int amplitude = 1)
    {
        var count = Shape.Unary(Track);
        var store = new Store(0, 1);
        Field F(int value)
        {
            var f = new Field(count, [.. Enumerable.Range(0, Track).Select(_ => store.AddPlane())]);
            f.Write(store, 0, value);
            return f;
        }
        Field x = F(Mark + amplitude), v = F(Mark), landmark = F(Mark), rest = F(Mark);
        var engine = new Engine();
        if (weight != 0)
            engine.Add(Tension.Wire(Operations.Bind(Operations.Balance([count, count, count, count], 1, weight, -weight, -1), [count, count, count],
                [0, 1, 2, -1], [-1, -1, -1, 0]), [v.On(store), landmark.On(store), x.On(store)], Here));
        engine.Add(Tension.Wire(Operations.Bind(Operations.Balance([count, count, count, count], 1, 1, -1, -1), [count, count, count],
            [0, 1, 2, -1], [-1, -1, -1, 0]), [x.On(store), v.On(store), rest.On(store)], Here));

        (int, int) Read() => (x.Read(store, 0)!.Value - Mark, v.Read(store, 0)!.Value - Mark);
        var start = Read();
        var points = new List<(int, int)> { start };
        for (var step = 0; step < 12; step++)
        {
            engine.Step();
            if (Read() == start) return Figure.Of(points, closed: true);
            points.Add(Read());
        }
        return Figure.Of(points.Distinct().ToArray(), closed: false);
    }

    [Fact]
    public void ThreeConfigurationsMakeTheSameSquare()
    {
        Assert.True(Lines([1, 3], [1, 3]).Square);
        Assert.True(Path(side: 2).Square);
        Assert.Equal((4, true), (Circle(weight: 2).Vertices, Circle(weight: 2).Closed));
    }

    /// <summary>Lines: moving one line gives a rectangle, adding one a grid of
    /// six corners, removing one leaves two. No single change gives anything but
    /// a box of corners.</summary>
    [Fact]
    public void LinesAreOneChangeFromRectanglesAndGrids()
    {
        Assert.True(Lines([1, 4], [1, 3]).Rectangle);
        Assert.Equal(6, Lines([1, 3, 5], [1, 3]).Vertices);
        Assert.Equal(2, Lines([1], [1, 3]).Vertices);
    }

    /// <summary>Path: another side length scales the square; turning by J² goes
    /// back and forth along a segment; not turning (J⁰) runs straight to the
    /// edge; turning by J³ draws the same square the other way round; and a
    /// side that grows at each turn opens into a spiral that never closes.</summary>
    [Fact]
    public void APathIsOneChangeFromScaledSquaresSegmentsAndSpirals()
    {
        Assert.Equal([1, 3, 4], new[] { 1, 3, 4 }.Where(s => Path(s).Square).Select(s => Path(s).Width));
        Assert.Equal(2, Path(2, turn: 2).Vertices);
        Assert.Equal(0, Path(2, turn: 0).Vertices);
        Assert.True(Path(2, turn: 3).Square);
        var spiral = Path(1, grow: 1);
        Assert.False(spiral.Closed);
        Assert.True(spiral.Vertices >= 6);
    }

    /// <summary>Circle: the weight sets the polygon, six points at 1, four at 2,
    /// three at 3; at 0 nothing pulls and it stays a point; at 4 it does not
    /// return. A larger start scales the same square.</summary>
    [Fact]
    public void ACircleIsOneChangeFromTrianglesAndHexagons()
    {
        Assert.Equal([1, 6, 4, 3], new[] { 0, 1, 2, 3 }.Select(k => Circle(k).Vertices));
        Assert.All(new[] { 0, 1, 2, 3 }, k => Assert.True(Circle(k).Closed));
        Assert.False(Circle(4).Closed);
        Assert.Equal((4, true), (Circle(2, amplitude: 2).Vertices, Circle(2, amplitude: 2).Closed));
    }

    /// <summary>The same square, three neighbourhoods that barely overlap.
    /// Lines and the path share the plane, so their boxes compare: only lines
    /// make a rectangle in one change. A closed odd polygon comes only from the
    /// circle. Lines never open; the path opens into a spiral that keeps
    /// turning, the circle only by running away. (The circle's points are
    /// position and velocity, so its box is not compared with theirs.) Which
    /// configuration a structure holds decides what it can become with one
    /// change.</summary>
    [Fact]
    public void EachConfigurationReachesWhatTheOthersCannot()
    {
        Figure[] lines = [Lines([1, 4], [1, 3]), Lines([2, 3], [1, 3]), Lines([1, 3, 5], [1, 3]), Lines([1], [1, 3]), Lines([1, 3], [1, 4])];
        Figure[] path = [Path(1), Path(3), Path(2, turn: 0), Path(2, turn: 2), Path(2, turn: 3), Path(1, grow: 1)];
        Figure[] circle = [Circle(0), Circle(1), Circle(3), Circle(4), Circle(2, amplitude: 2)];

        Assert.Contains(lines, f => f.Rectangle);
        Assert.DoesNotContain(path, f => f.Rectangle);

        Assert.Contains(circle, f => f.Closed && f.Vertices == 3);
        Assert.DoesNotContain(lines, f => f.Vertices == 3);
        Assert.DoesNotContain(path, f => f.Closed && f.Vertices == 3);

        Assert.Contains(path, f => !f.Closed && f.Vertices >= 6);
        Assert.DoesNotContain(lines, f => !f.Closed);
    }
}
