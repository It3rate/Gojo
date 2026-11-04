namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class PairedReachTests
{
    private static LandmarkRun Rail() => new(2, [2, 0, 3, 1]);
    private static PairedReach Gauge(PresencePairPort settings) => new(Rail(), settings, (0, 3), (0, 3));
    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return support; }
    }

    [Theory]
    [InlineData(0x06, 0x06, 0x06, 1)] // exactly one extends
    [InlineData(0x0F, 0x0F, 0x0E, 3)] // either is free
    [InlineData(0x09, 0x09, 0x08, 3)] // same setting, unknown which
    [InlineData(0, 0, 0, 0)]
    public void Gauge_reads_the_intersection_and_compares_reaches_without_a_unit(
        byte admitted, byte farPairs, byte comparisons, byte bothAtFar)
    {
        var gauge = Gauge(PresencePairPort.Bind(admitted));
        Assert.Equal(new byte[] { 0x72 }, gauge.Rail.SiteBytes());
        Assert.Equal(new byte[] { 0xCC }, gauge.TipBytes());
        Assert.Equal(admitted == 0 ? 0 : 8, gauge.At(gauge.Rail, 2).ReadPairs()); // origin
        Assert.Equal(admitted == 0 ? 0 : 8, gauge.At(gauge.Rail, 0).ReadPairs()); // near
        Assert.Equal(farPairs, gauge.At(gauge.Rail, 3).ReadPairs());
        Assert.Equal(bothAtFar, gauge.At(gauge.Rail, 3).Test(0x08).ReadSupport());
        Assert.Equal(admitted == 0 ? 0 : 1, gauge.At(gauge.Rail, 1).ReadPairs()); // beyond both
        Assert.Equal(comparisons, gauge.CompareTips().ReadPairs());
    }

    [Fact]
    public void The_same_live_gauge_reads_two_independently_moved_tips()
    {
        var left = new PassageFixture(3, 1);
        var right = new PassageFixture(3, 1);
        left.Add(left.Forward, 0, 1, 2);
        right.Add(right.Forward, 0, 1, 2);
        var settings = PresencePairPort.Bind(0x0F, left.Owner.Connect(left.Ports[2]), right.Owner.Connect(right.Ports[2]));
        var gauge = new PairedReach(Rail(), settings, (0, 3), (2, 3));
        var order = gauge.CompareTips();
        var near = gauge.At(gauge.Rail, 0);
        Assert.Equal(2, order.ReadPairs()); // left initially farther
        Assert.Equal(2, near.ReadPairs());
        Assert.True(right.TryStep(right.Observe(), out _));
        Assert.Equal(4, order.ReadPairs()); // right passes left
        Assert.Equal(8, near.ReadPairs());
        Assert.True(left.TryStep(left.Observe(), out _));
        Assert.Equal(8, order.ReadPairs()); // shared projected tip, still two dials
        Assert.Equal(4, left.Stored);
        Assert.Equal(4, right.Stored);
    }

    [Fact]
    public void A_foreign_basis_or_missing_observation_cannot_certify_an_empty_span()
    {
        var gauge = Gauge(PresencePairPort.Bind(6));
        Assert.Equal(0, gauge.At(Rail(), 3).Test(8).ReadSupport());
        Assert.Equal(0, gauge.At(gauge.Rail, 7).ReadPairs());
        Assert.Equal(0, Gauge(PresencePairPort.Bind(6, new Observation(0))).CompareTips().ReadPairs());
        Assert.Equal(0, PresencePairPort.Bind(6, new Observation(0b110)).ReadPairs());
        Assert.Equal(0, PresencePairPort.Bind(6, new Observation(2), new Observation(2)).ReadPairs());
        Assert.Throws<ArgumentException>(() => new PairedReach(new(2, [2, 0, 3], [[2, 0], [2, 3]]),
            PresencePairPort.Bind(6), (0, 3), (0, 3)));
    }

    [Fact]
    public void A_shared_unknown_is_sampled_once_and_cannot_take_opposite_settings()
    {
        var shared = new Observation(3);
        Assert.Equal(9, PresencePairPort.Bind(15, shared, shared).ReadPairs());
        Assert.Equal(1, shared.Reads);
        Assert.Equal(0, PresencePairPort.Bind(6, shared, shared).ReadPairs());
        Assert.Equal(15, PresencePairPort.Bind(15, shared, new Observation(3)).ReadPairs());
    }

    [Fact]
    public void The_same_joint_read_can_publish_two_possible_attachments_of_one_mark()
    {
        var evidence = new Observation(3);
        var links = new PairedLinks(3, (4, 0), (4, 2), PresencePairPort.Bind(6, evidence));
        Assert.Equal(new byte[] { 0x04, 0x05 }, links.LinkBytes());
        var owners = links.From(4);
        Assert.Equal(6, owners.ReadPairs());
        Assert.Equal(2, owners.Test(6).ReadSupport());
        Assert.Equal(3, links.From(4, 0).Test(14).ReadSupport());
        Assert.Equal(3, links.From(4, 2).Test(14).ReadSupport());
        Assert.Equal(1, links.From(0, 4).Test(14).ReadSupport());
        var excludedLeft = new PairedLinks(3, (4, 0), (4, 2), PresencePairPort.Bind(6, new Observation(1)));
        Assert.Equal(2, excludedLeft.From(4, 2).Test(14).ReadSupport());
    }

    [Theory]
    [InlineData(6, 0x06, true)]   // exactly one is known at the far stop
    [InlineData(15, 0x06, false)] // same scalar marginals, uncertain joint predicate
    [InlineData(6, 0x0A, false)]  // which one reaches it is still uncertain
    public void A_peer_uses_only_the_published_relation_to_release_its_own_pulse(
        byte admitted, byte truth, bool delivered)
    {
        var gauge = Gauge(PresencePairPort.Bind(admitted));
        var far = gauge.At(gauge.Rail, 3);
        Assert.Equal(3, far.Test(0x0A).ReadSupport());
        Assert.Equal(3, far.Test(0x0C).ReadSupport());
        var handoff = new GuardedOffer(far.Test(truth));
        Assert.Equal(delivered, handoff.Request().Moved);
        Assert.False(handoff.Request().Moved);
        Assert.Equal(delivered ? 2 : 0, handoff.Receiver);
        Assert.Equal(delivered ? 0 : 1, handoff.Sender & 3);
        Assert.Equal(admitted, far.ReadPairs());
    }
}
