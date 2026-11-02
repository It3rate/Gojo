using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public class HouseSeamTests
{
    [Theory]
    [InlineData(false, 2, false, true)]
    [InlineData(true, 1, true, false)]
    public void Rung_order_preserves_the_source_and_the_origin_of_the_bridge(bool template, byte packed, bool sourceClosed, bool resultClosed)
    {
        var stroke = new StrokeRung(template);
        var pair = stroke.Correspondence();
        Assert.Equal(new[] { packed }, stroke.RungBytes());
        Assert.Equal(sourceClosed, stroke.Views.IsClosed(pair[0]));
        Assert.Equal(resultClosed, stroke.Views.IsClosed(pair[1]));
        Assert.Equal(new[] { 0, 3 }, stroke.Views.Range(0));
        Assert.Equal(new[] { 0, 4 }, stroke.Views.Range(1));
        Assert.Equal(new[] { 3, 0 }, stroke.Catalogue.DirectedUse(3));
    }

    [Fact]
    public void Equal_first_and_last_sites_cannot_hide_a_broken_interior_join()
    {
        var catalogue = new OrderedBoundary(3, [[0, 1], [2, 0]], [0, 2]);
        Assert.False(new WalkWindows(catalogue, (0, 2)).IsClosed(0));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 13)]
    public void Equal_endpoint_pairs_give_identical_closed_face_walks(bool shared, int roomUse)
    {
        var house = new HouseSeam(shared);
        Assert.Equal(0, house.Roof.SignedUse);
        Assert.Equal(roomUse, house.Room.SignedUse);
        Assert.Equal(house.Roof.Ends().Reverse(), house.Room.Ends());
        int[] Walk(int face)
        {
            var range = house.FaceRange(face);
            var sites = new List<int>();
            for (var i = range[0]; i < range[1]; i++)
            {
                var use = house.SignedUse(i);
                var ends = house.EdgeEnds(use >> 1);
                if ((use & 1) != 0) Array.Reverse(ends);
                if (sites.Count == 0) sites.Add(ends[0]);
                Assert.Equal(sites[^1], ends[0]);
                sites.Add(ends[1]);
            }
            return sites.ToArray();
        }
        Assert.Equal(new[] { 0, 1, 2, 0 }, Walk(0));
        Assert.Equal(new[] { 1, 0, 3, 4, 1 }, Walk(1));
        Assert.True(house.Faces.IsClosed(0));
        Assert.True(house.Faces.IsClosed(1));
        Assert.Equal(shared ? new[] { 0, 1, 1, 1, 1, 1 } : new[] { 1, 1, 1, 1, 1, 1, -1 }, house.EdgeBalance());
    }

    [Fact]
    public void An_attached_reader_recovers_the_cut_in_reverse_without_rewriting_the_source()
    {
        var house = new HouseSeam(true);
        var reader = house.Room;
        var edges = house.EdgeBytes();
        var uses = house.UseBytes();
        Assert.Equal(new[] { 1, 0 }, reader.Landmarks());
        house.AddRoofCut();
        house.AddRoofCut();
        Assert.Equal(new[] { 0, 5, 1 }, house.Roof.Landmarks());
        Assert.Equal(new[] { 1, 5, 0 }, reader.Landmarks());
        Assert.False(house.BindRoomToRoofCut());
        Assert.Equal(new byte[] { 0x28 }, house.CutBytes());
        Assert.Empty(house.BindingBytes());
        Assert.Equal(new[] { 1, 0 }, reader.Ends());
        Assert.Equal(edges, house.EdgeBytes());
        Assert.Equal(uses, house.UseBytes());
    }

    [Fact]
    public void Binding_only_the_landmark_preserves_separate_edge_identity_and_samples()
    {
        var house = new HouseSeam(false);
        Assert.False(house.BindRoomToRoofCut());
        var before = house.EdgeBalance();
        house.AddRoofCut();
        Assert.Equal(new[] { 1, 0 }, house.Room.Landmarks());
        Assert.True(house.BindRoomToRoofCut());
        Assert.False(house.BindRoomToRoofCut());
        Assert.Equal(new byte[] { 6 }, house.BindingBytes());
        Assert.Equal(new[] { 1, 5, 0 }, house.Room.Landmarks());
        Assert.Equal(before, house.EdgeBalance());
        house.SetRoofSample(true);
        Assert.Equal(7, house.Room.Flow());
        Assert.Equal(3, house.Roof.Flow());
    }

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, 7)]
    public void A_second_meter_distinguishes_frame_reversal_from_changed_flow_sample(bool shared, byte roomAfter)
    {
        var house = new HouseSeam(shared);
        var actual = house.SampleBytes();
        Assert.Equal(1, house.Roof.Flow());
        Assert.Equal(7, house.Roof.Flow(flipMeter: true));
        Assert.Equal(7, house.Room.Flow());
        Assert.Equal(actual, house.SampleBytes());
        house.SetRoofSample(true);
        Assert.Equal(3, house.Roof.Flow());
        Assert.Equal(roomAfter, house.Room.Flow());
        Assert.Equal(ReadSupport.Of(roomAfter), house.Room.ReadSupport());
        house.SetRoofSample(false, present: false);
        Assert.Equal(0, house.Roof.Flow() & ThreeBitMeasure.Value);
        Assert.Equal(shared ? 0 : 1, house.Room.Flow() & ThreeBitMeasure.Value);
    }
}
