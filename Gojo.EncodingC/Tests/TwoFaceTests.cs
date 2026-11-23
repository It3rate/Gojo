using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Traffic both ways on one road, and a bridge used one way at a
/// time. The road has two faces: plane 0 is the forward face, flowing with the
/// road from home (0) to the store (4); plane 1 is the opposed face, flowing
/// forward on its own orientation, which is the road reoriented, so read on
/// the road it goes against it. Each face has its own step word. Units on
/// different faces never meet in one bit, so both flow at once.</summary>
public sealed class TwoFaceTests
{
    private const int Sites = 5, Cap = 3;
    private const int Forward = 0, Opposed = 1;
    private static readonly int With = Sides.With;

    /// <summary>Adds one when the observed bit is set. Roles: seen (read), count.</summary>
    private static readonly TransitionLaw Tally = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Unary(Cap)], s => s[0] == 1 && s[1] < Cap ? [1, s[1] + 1] : s, new Accounting(true, new Unchanged(0), new NeverDecreases(1)));

    private sealed record Road(Engine Engine, Store Lanes, Store Control, Wiring ForwardMoves, Wiring OpposedMoves);

    /// <summary>A road with a unit on each face, forward at home and opposed at
    /// the store. Rows run from each face's far end back, one site a step. With
    /// <paramref name="bridge"/>, entering site 2 is one at a time: each face
    /// enters only while the other's lane there is clear and the active
    /// orientation, control plane 4, is its own.</summary>
    private static Road Build(bool bridge)
    {
        var lanes = new Store(2, Sites);
        lanes.Write(Forward, 0, true);
        lanes.Write(Opposed, 4, true);
        var control = new Store(5, 1);
        new Field(Sides.Word, 0, 1).Write(control, 0, With);
        new Field(Sides.Word, 2, 3).Write(control, 0, With);
        control.Write(4, 0, true);
        var engine = new Engine();
        // Rows run from each face's far end back, across wirings too, so a unit
        // that steps onto the bridge is not moved on again in the same step.
        Wiring Moves(int face, int[] stepPlanes, (int From, int To)[] rows, int? enter, int other)
        {
            Wiring Plain()
            {
                var plain = new Wiring(Paths.Advance(With), new Binding(lanes, face), new Binding(control, stepPlanes), new Binding(lanes, face));
                plain.Record();
                return plain;
            }
            var first = Plain();
            var current = first;
            foreach (var (from, to) in rows)
            {
                if (bridge && to == enter)
                {
                    engine.Add(current);
                    var inTurn = new Wiring(Paths.AdvanceInTurn(face == Forward),
                        new Binding(lanes, face), new Binding(control, stepPlanes), new Binding(lanes, face), new Binding(lanes, other), new Binding(control, 4));
                    inTurn.Add(from, 0, to, to, 0);
                    engine.Add(inTurn);
                    current = Plain();
                }
                else current.Add(from, 0, to);
            }
            engine.Add(current);
            return first;
        }
        var forward = Moves(Forward, [0, 1], [(3, 4), (2, 3), (1, 2), (0, 1)], 2, Opposed);
        var opposed = Moves(Opposed, [2, 3], [(1, 0), (2, 1), (3, 2), (4, 3)], 2, Forward);
        return new Road(engine.Hold(lanes, control), lanes, control, forward, opposed);
    }

    private static int At(Road road, int face) => Assert.Single(road.Lanes.Read(face).Indexes());

    /// <summary>Two units, one on each face, start at opposite ends and both
    /// move every step. They pass each other mid-road and arrive at the far
    /// ends together: one unit on each face throughout. One face alone could
    /// not carry this: a face has one use direction at a time.</summary>
    [Fact]
    public void TwoFacesCarryTrafficBothWaysAtOnce()
    {
        var road = Build(bridge: false);
        var trail = new List<(int, int)>();

        for (var step = 0; step < 4; step++)
        {
            road.Engine.Step();
            trail.Add((At(road, Forward), At(road, Opposed)));
        }

        Assert.Equal([(1, 3), (2, 2), (3, 1), (4, 0)], trail);
        Assert.Equal((1, 1), (road.Lanes.Read(Forward).Count, road.Lanes.Read(Opposed).Count));
    }

    /// <summary>A strip over the edge between sites 2 and 3 that tells the faces
    /// apart counts one crossing on each. Their gross is two. Their net, the
    /// forward count read as a side of the opposed count, is at zero: balanced
    /// traffic, not no traffic. Each read keeps what the other folds away.</summary>
    [Fact]
    public void NetAndGrossAreTwoFoldsOfTheSameTwoFaces()
    {
        var road = Build(bridge: false);
        var counts = new Store(2 * Cap, 1);
        var forwardRows = road.ForwardMoves.Rows.Publish([1], [road.ForwardMoves.Recorded!.Changed]);
        var opposedRows = road.OpposedMoves.Rows.Publish([2], [road.OpposedMoves.Recorded!.Changed]);
        var tallyForward = new Wiring(Tally, new Binding(forwardRows, 0), new Binding(counts, 0, 1, 2));
        tallyForward.Add(0, 0);
        var tallyOpposed = new Wiring(Tally, new Binding(opposedRows, 0), new Binding(counts, 3, 4, 5));
        tallyOpposed.Add(0, 0);
        var strip = new Engine().Add(tallyForward).Add(tallyOpposed);

        for (var step = 0; step < 4; step++)
        {
            road.Engine.Step();
            strip.Step();
        }

        var count = new Field(Shape.Unary(Cap), 0, 1, 2);
        var (crossedForward, crossedOpposed) = (count.Read(counts, 0), new Field(Shape.Unary(Cap), 3, 4, 5).Read(counts, 0));
        Assert.Equal((1, 1), (crossedForward, crossedOpposed));
        Assert.Equal(2, crossedForward + crossedOpposed);
        var compare = Sides.Compare(Cap);
        var net = Enumerable.Range(0, 4).Where(w => compare.BitAt(compare.TupleIndex([crossedForward!.Value, crossedOpposed!.Value, w]))).ToArray();
        Assert.Equal([Sides.AtFromBefore, Sides.AtFromAfter], net);
    }

    /// <summary>A single-lane bridge at site 2. Both units arrive beside it at
    /// once; the active orientation, a bit set to the forward face, lets that
    /// face's unit on while the other waits beside it. Once the bridge is clear
    /// and the bit is set for the opposed face, it crosses. The two lanes are
    /// never both on the bridge, and nothing is lost.</summary>
    [Fact]
    public void ASingleLaneBridgeIsUsedOneWayAtATime()
    {
        var road = Build(bridge: true);
        road.Lanes.Write(Forward, 0, false);
        road.Lanes.Write(Forward, 1, true);
        road.Lanes.Write(Opposed, 4, false);
        road.Lanes.Write(Opposed, 3, true);

        road.Engine.Step();
        Assert.Equal((2, 3), (At(road, Forward), At(road, Opposed)));

        road.Engine.Step();
        Assert.Equal((3, 3), (At(road, Forward), At(road, Opposed)));

        road.Control.Write(4, 0, false);
        road.Engine.Step();
        Assert.Equal((4, 2), (At(road, Forward), At(road, Opposed)));

        road.Engine.Step();
        Assert.Equal((4, 1), (At(road, Forward), At(road, Opposed)));
        Assert.Equal((1, 1), (road.Lanes.Read(Forward).Count, road.Lanes.Read(Opposed).Count));
    }
}
