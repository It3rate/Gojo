using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class StraightCapitalTests
{
    [Fact]
    public void Roster_and_ambiguities_are_stable_across_pen_histories()
    {
        string[] labels = ["A","E","F","H","I","I","K","L","M","N","T","V","W","X","X","Y","Z"];
        string[] unbound = ["A","E","F","H/I","I","H/I","K","L/V","M/W","N/Z","T","L/V","M/W","X","X","Y","N/Z"];
        Assert.Equal(labels.Length, StraightCapitals.Names.Count);
        for (var i = 0; i < labels.Length; i++)
        foreach (var hand in new[] { false, true })
        {
            var name = StraightCapitals.Names[i];
            var ink = StraightCapitals.Build(name, otherHand: hand);
            Assert.Equal(new[] { labels[i] }, StraightCapitals.Candidates(ink));
            Assert.Equal(unbound[i], string.Join("/", StraightCapitals.Candidates(StraightCapitals.Build(name, false, hand))));
            // Independently check the authored pen joins, including reversed visits.
            int? pen = null;
            foreach (var use in ink.PenUses())
            {
                var run = ink.Run(use >> 2).Sites();
                if ((use & 1) != 0) Array.Reverse(run);
                if ((use & 2) == 0) Assert.Equal(pen, run[0]);
                pen = run[^1];
            }
        }
    }

    [Fact]
    public void Conventional_read_follows_supplied_frame_and_reversed_reference_roles()
    {
        var n = StraightCapitals.Build("N");
        var z = StraightCapitals.Build("Z");
        Assert.Equal(n.Run(1).SiteBytes(), z.Run(1).SiteBytes());
        Assert.Equal(new byte[] { 0x84 }, n.Frame!.AxisBytes());
        Assert.Equal(new byte[] { 0xD4,0x04 }, n.Frame.MotionBytes());
        Assert.Equal(new byte[] { 0xF1,0x01 }, z.Frame!.MotionBytes());
        Assert.Equal(new byte[] { 0x10,0x64 }, n.Frame.ContactBytes());
        Assert.Equal(new byte[] { 0x20,0x76 }, z.Frame.ContactBytes());
        Assert.Equal(new[] { 1,3 }, n.Frame.Motion(1));
        Assert.Equal(new[] { 3,1 }, n.Frame.Motion(1, reversed: true));
        var rebound = new StrokeGraph([n.Run(0),n.Run(1),n.Run(2)], [], [], z.Frame);
        Assert.Equal(new[] { "Z" }, StraightCapitals.Candidates(rebound));
        // The same N with new site addresses, run catalogue order and run directions.
        var renamed = new StrokeGraph([new(4,[7,9]),new(4,[4,12]),new(4,[9,4])], [], [],
            new LetterGround([(0,-1),(0,-1),(-1,1)], [12,4], [12,9], 4));
        Assert.Equal(new[] { "N" }, StraightCapitals.Candidates(renamed));
        // Moving a zero contact to an incompatible landmark rejects that mapping.
        var falseOrigin = new StrokeGraph([n.Run(0),n.Run(1),n.Run(2)], [], [],
            new LetterGround([(0,1),(1,-1),(0,1)], [0,1,2,3], [0,2], 2));
        Assert.Empty(StraightCapitals.Candidates(falseOrigin));
        var v = StraightCapitals.Build("V");
        Assert.Equal(new[] { 0 }, v.Frame!.ZeroLinks().Where(z => z[1] == 0).Select(z => z[0]));
        Assert.Equal(new[] { 1 }, v.Frame.ZeroLinks().Where(z => z[1] == 1).Select(z => z[0]));
        Assert.Equal(v.Incidence(), StraightCapitals.Build("V", false).Incidence());
    }

    [Fact]
    public void A_loop_surface_retains_a_closed_walk_of_original_spans_and_can_guard_a_passage()
    {
        var a = StraightCapitals.Build("A", false);
        var layer = new StrokeFeatures(a);
        var loop = Assert.Single(layer.Loops);
        Assert.Equal(new[] { 1,3,0,1 }, loop.Sites());
        Assert.Equal(new byte[] { 0x42,0x04,0 }, loop.Bytes());
        foreach (var edge in loop.Walk)
        {
            var run = a.Run(edge.Run).Sites();
            Assert.Equal(edge.From, run[edge.Slot + (edge.Reversed ? 1 : 0)]);
            Assert.Equal(edge.To, run[edge.Slot + (edge.Reversed ? 0 : 1)]);
        }
        Assert.Equal(new[] { "A" }, StraightCapitals.CandidatesByLoop(true));
        var open = new StrokeFeatures(new StrokeGraph([a.Run(0),a.Run(1),new(3,[5,3])], [], []));
        Assert.Empty(open.Loops);
        var circuit = new PulseCircuit(2);
        int idle = circuit.State(true), inside = circuit.State(), guard = circuit.Guard();
        circuit.Link(circuit.AddTable(PassageLaw.Controlled()), idle, guard, inside);
        circuit.Bind(guard, open);
        Assert.False(circuit.Pulse().Moved);
        circuit.Bind(guard, layer);
        Assert.True(circuit.Pulse().Moved);
        var partial = new StrokeFeatures(CapitalStudy.Build("E", partialE: true));
        Assert.False(partial.ExactWalks);
        Assert.Equal(1, partial.ReadSupport()); // zero cycle rank for either permitted spine order
        var conflict = new StrokeFeatures(new StrokeGraph([new(2, [0,1], [[0,1],[1,0]])], [], []));
        Assert.Equal(0, conflict.ReadSupport());
    }

    [Fact]
    public void Matching_crossed_projections_does_not_join_two_strands()
    {
        var separate = StraightCapitals.Build("X");
        var joined = StraightCapitals.Build("Joined X");
        var fork = new StrokeFeatures(StraightCapitals.Build("K"));
        Assert.Equal(new[] { 1,4 }, Assert.Single(separate.ProjectedCrossings()));
        Assert.Equal(2, separate.Incidence().Components);
        Assert.DoesNotContain(new StrokeFeatures(separate).Joints, j => j.Arms > 2);
        Assert.Equal(1, joined.Incidence().Components);
        Assert.Empty(joined.ProjectedCrossings());
        Assert.Equal(2, Assert.Single(new StrokeFeatures(joined).Joints, j => j.Arms == 4).Through);
        Assert.Equal(2, Assert.Single(fork.Joints, j => j.Arms == 4).Ends);
        Assert.False(separate.Fits(joined));
        Assert.Equal(1, new StrokeFeatures(StraightCapitals.Build("L")).Joints.Count(j => j.ProvedCorner));
        Assert.DoesNotContain(new StrokeFeatures(StraightCapitals.Build("L", false)).Joints, j => j.ProvedCorner);
    }
}
