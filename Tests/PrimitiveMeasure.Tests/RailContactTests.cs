namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RailContactTests
{
    private static RailOrderPort Rail(int[] order, int[][]? claims = null) =>
        new(new LandmarkRun(2, order, claims), 0, 1, 2, 3);

    [Fact]
    public void Existing_reader_narrows_on_each_contact_and_reopens_after_release()
    {
        var dock = new RailContacts(Rail([0,1,2,3]), Rail([0,2,1,3]));
        var span = dock.Compare((0,1), (0,1));
        var tipPoint = dock.Compare((0,0), (0,0));
        Assert.Equal(0, span.ReadPairs());
        dock.Touch(0,0);
        Assert.Equal(8, tipPoint.Inspect().TipPairs);
        Assert.Equal(3, span.Inspect().Faces);
        Assert.Equal(14, span.ReadPairs());
        dock.Touch(0,0);
        Assert.Equal(3, span.Inspect().Faces);
        dock.Touch(2,2);
        Assert.Equal(1, span.Inspect().Faces);
        Assert.Equal(4, span.ReadPairs());
        Assert.Equal(8, span.Inspect().IntersectionPairs);
        Assert.Equal(new byte[]{0x49,0xB2,0x01}, dock.Bytes());
        Assert.Equal(new byte[]{0x44}, span.Bytes());
        dock.Touch(1,1);
        Assert.Equal(0, span.ReadPairs());
        Assert.Equal(4, span.Inspect().Contacts);
        dock.Release(3);
        Assert.Equal(4, span.ReadPairs());
        dock.Release(2);
        Assert.Equal(14, span.ReadPairs());
    }

    [Theory]
    [InlineData(false, 1)] [InlineData(true, 2)]
    public void A_second_contact_settles_the_face_but_an_internal_rank_is_no_distance(bool reverse, byte face)
    {
        var dock = new RailContacts(Rail([0,1,2,3]), Rail(reverse ? [3,2,1,0] : [0,1,2,3]));
        dock.Touch(0,0); dock.Touch(2,2);
        var span = dock.Compare((0,1), (0,1)).Inspect();
        Assert.Equal(face, span.Faces);
        Assert.Equal(14, span.TipPairs);
        Assert.Equal(14, span.ExtentPairs);
        Assert.Equal(8, span.IntersectionPairs);
        Assert.Equal(8, dock.Compare((2,0), (0,2)).ReadPairs());
    }

    [Fact]
    public void Extent_comparison_depends_on_starts_and_preserves_projected_boundary_cases()
    {
        var dock = new RailContacts(Rail([0,1,2,3]), Rail([0,1,2,3]));
        for (int i = 0; i < 4; i++) dock.Touch(i,i);
        var shifted = dock.Compare((0,2), (1,3)).Inspect();
        Assert.Equal(4, shifted.TipPairs);
        Assert.Equal(14, shifted.ExtentPairs);
        Assert.Equal(8, shifted.IntersectionPairs);
        Assert.Equal(2, dock.Compare((0,2), (1,2)).ReadPairs());
        Assert.Equal(2, dock.Compare((0,1), (1,2)).Inspect().IntersectionPairs);
        Assert.Equal(1, dock.Compare((0,1), (2,3)).Inspect().IntersectionPairs);
        var points = dock.Compare((0,0), (2,2)).Inspect();
        Assert.Equal(8, points.ExtentPairs);
        Assert.Equal(4, points.TipPairs);
        Assert.Equal(1, points.IntersectionPairs);
        Assert.Equal(4, dock.Compare((1,1), (0,3)).ReadPairs());
    }

    [Fact]
    public void Derived_exclusivity_can_guard_real_draws_and_old_permission_cannot_pay_twice()
    {
        var left = Rail([0,1,2,3], [[0,1],[0,2],[1,3],[2,3]]);
        var dock = new RailContacts(left, Rail([0,2,1,3]));
        var span = dock.Compare((0,1), (0,1));
        var links = new PairedLinks(2, (3,0), (3,1), span.Map(0x24));
        var winners = links.From(3);
        var y = GlyphJunction.YCollect(3);
        Assert.Equal(0x14, y.Advance());
        var draw = new ObservedDraw(y, winners.Test(6));
        dock.Touch(0,0);
        Assert.Null(draw.Request());
        dock.Touch(2,1); // B tip reaches a different A stop from the A tip.
        Assert.Equal(6, span.ReadPairs());
        Assert.Equal(6, winners.ReadPairs());
        Assert.Equal(3, winners.Test(2).ReadSupport());
        Assert.Equal(3, winners.Test(4).ReadSupport());
        Assert.Single(draw.Request()!, s => s.Transfer.Moved);
        Assert.Equal(2, y.Stem.ReadSupport());
        Assert.DoesNotContain(draw.Request()!, s => s.Transfer.Moved);
        draw.Park();
        dock.Release(1);
        Assert.Null(draw.Request()); // previous clear gate is still just a sample
        Assert.Equal(4, draw.Stored);
        Assert.Equal(0x10, y.Flow.Stored);
        dock.SeatLeft(1,1); dock.SeatRight(1,1); // equal spans, resolved refusal
        Assert.Equal(8, span.ReadPairs());
        Assert.Equal(1, winners.ReadPairs());
        Assert.DoesNotContain(draw.Request()!, s => s.Transfer.Moved);
        dock.Release(1);
        dock.Touch(2,1); dock.Touch(1,2); // extra witness from B orders the two A stops
        Assert.Equal(4, span.ReadPairs());
        Assert.Single(draw.Request()!, s => s.Transfer.Moved);
        Assert.Equal(6, draw.Stored);
        Assert.Equal(1, y.Stem.ReadSupport());
        Assert.Equal(4, winners.ReadPairs());
    }

    [Fact]
    public void Only_opposing_halves_of_the_same_socket_witness_contact()
    {
        var a = new RailOrderPort(new(3, [6,2,4]), 4,6);
        var b = new RailOrderPort(new(3, [5,7,3]), 5,3);
        var dock = new RailContacts(a, b);
        var first = dock.OpenSocket();
        var second = dock.OpenSocket();
        var span = dock.Compare((1,0), (0,1));
        dock.SeatLeft(first, 1); dock.SeatRight(second, 1);
        Assert.Equal(0, dock.Count);
        Assert.Equal(0, span.ReadPairs());
        dock.SeatRight(first, 0);
        Assert.Equal(1, dock.Count);
        Assert.Equal(14, span.ReadPairs());
        dock.SeatLeft(second, 0);
        Assert.Equal(8, span.ReadPairs());
        dock.SeatRight(first, null);
        Assert.Equal(1, dock.Count);
        Assert.Equal(14, span.ReadPairs());
        Assert.Throws<ArgumentOutOfRangeException>(() => dock.SeatLeft(first, 6));
        Assert.Equal(new[]{2,0}, dock.SeatCodes(first));
    }

    [Fact]
    public void A_contact_cycle_can_require_both_owners_and_more_than_two_rows()
    {
        var left = Rail([0,1,2,3], [[0,1],[2,3]]);
        var right = Rail([0,1,2,3], [[1,2],[3,0]]);
        var dock = new RailContacts(left, right);
        for (int i = 0; i < 4; i++) dock.Touch(i,i);
        Assert.Equal(2, dock.Compare((0,1), (2,3)).Inspect().Faces);
        dock.Touch(1,2); // one stop cannot match two distinct B stops
        Assert.Empty(dock.Compare((0,1), (2,3)).Inspect().Placements);
    }

    [Fact]
    public void Projected_orders_match_all_small_interleavings_including_unwitnessed_coincidences()
    {
        var a = new LandmarkRun(2, [2,0,1], [[0,1],[0,2]]);
        var b = new LandmarkRun(2, [1,0,2], [[1,2]]);
        foreach (var pins in new int[][][] { [[0,0]], [[0,0],[2,2]], [[1,2],[2,1],[0,0]] })
        {
            var dock = new RailContacts(new(a,0,1,2), new(b,0,1,2));
            foreach (var pin in pins) dock.Touch(pin[0],pin[1]);
            var expected = new HashSet<SpanPlacement>();
            foreach (var ao in a.Orders())
                foreach (var bo in b.Orders())
                    foreach (bool reverse in new[] { false, true })
                    {
                        var order = reverse ? bo.Reverse().ToArray() : bo;
                        int[] ap = new int[3], bp = new int[3];
                        // Independently lay the two rails on successive positions.
                        // Advance A, B, or both at the next position.
                        void Lay(int i, int j, int position)
                        {
                            if (i == 3 && j == 3)
                            {
                                if (pins.Any(pin => ap[pin[0]] != bp[pin[1]])) return;
                                int[] ends = [ap[0], ap[1], bp[1], bp[2]];
                                var levels = ends.Distinct().Order().ToArray();
                                int code = 0;
                                for (int k = 0; k < 4; k++) code |= Array.IndexOf(levels, ends[k]) << (2*k);
                                expected.Add(new(reverse, (byte)code));
                                return;
                            }
                            if (i < 3) { ap[ao[i]] = position; Lay(i+1,j,position+1); }
                            if (j < 3) { bp[order[j]] = position; Lay(i,j+1,position+1); }
                            if (i < 3 && j < 3)
                            {
                                ap[ao[i]] = bp[order[j]] = position;
                                Lay(i+1,j+1,position+1);
                            }
                        }
                        Lay(0,0,0);
                    }
            Assert.True(expected.SetEquals(dock.Compare((0,1),(1,2)).Inspect().Placements));
        }
    }
}
