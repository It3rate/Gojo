using Gojo.Core;
using Gojo.Structures;
using Xunit.Abstractions;
using C = BreakDrivenPrediction.Continuation;
using E = BreakDrivenPrediction.EventCoupling;
using S = BreakDrivenPrediction.EventSource;

namespace BreakDrivenPrediction.Tests;

public sealed class EventCouplingTests(ITestOutputHelper output)
{
    [Fact]
    public void PublicPacketCannotReachThePrivateProducerState()
    {
        var source = S.Create(0);
        for (var i = 0; i < 8; i++)
        {
            var packet = S.Packet(source);
            var graph = GojoEncodedSnapshot.CaptureGraph(packet);
            Assert.DoesNotContain(graph.Carriers, row => row.Id == S.Sensor(source).Id);
            Assert.DoesNotContain(graph.Carriers, row => row.Id == S.Clock(source).Id);
            Assert.DoesNotContain(graph.TickRefs, row => row.Id == S.Sensor(source).ValueSlot.Tick.Id);
            Assert.DoesNotContain(graph.TickRefs, row => row.Id == S.Sensor(source).LiveTensionSlot.Tick.Id);
            Assert.Same(S.Port(source), C.Parts(packet)[0]);
            AdvanceSource(ref source, i % 2 == 0 ? 1 : -1);
        }
    }

    [Fact]
    public void WarmupOpenReadingIsDifferentFromCompleteZeroAndOne()
    {
        var source = S.Create(0);
        var root = Coupled(source);
        Assert.IsType<Placeholder>(C.Parts(S.Packet(source))[3]);
        var open = E.ReadReceipt(root, S.Packet(source));
        Assert.IsType<Placeholder>(C.Parts(open)[4]);
        Assert.IsType<CarrierLocation>(C.Parts(open)[5]);
        Step(ref source, ref root, 1, 1);
        Assert.IsType<Placeholder>(C.Parts(S.Packet(source))[3]);
        Step(ref source, ref root, 1, 1);
        Assert.Equal(0, Scalar(C.Parts(S.Packet(source))[3]).Ticks);
        var zero = E.ReadReceipt(root, S.Packet(source));
        Assert.Equal(0, Scalar(C.Parts(zero)[4]).Ticks);
        Assert.IsType<CarrierLocation>(C.Parts(zero)[5]);
        var missing = E.ReadReceipt(root, new Placeholder());
        Assert.IsNotType<CarrierLocation>(C.Parts(missing)[4]);
        Assert.IsType<Placeholder>(C.Parts(missing)[5]);
        Step(ref source, ref root, 1, -1);
        Assert.Equal(1, Scalar(C.Parts(S.Packet(source))[3]).Ticks);
        Assert.Equal(1, Scalar(C.Parts(E.ReadReceipt(root, S.Packet(source)))[4]).Ticks);
    }

    [Fact]
    public void RepeatedReadsArePureAndLaterProducerUpdatesCannotRewriteAnIssue()
    {
        var (source, root) = ParticipatingPair();
        var packet = S.Packet(source);
        var before = GojoNumericFormat.Encode(source, root, packet);
        var issue = E.Issue(root, packet);
        E.ReadReceipt(root, packet);
        E.Issue(root, packet);
        Assert.Equal(before, GojoNumericFormat.Encode(source, root, packet));
        var frozen = Scalar(C.Parts(issue)[4]);
        var frozenValue = frozen.Ticks;
        var packetBefore = GojoNumericFormat.Encode(packet);
        AdvanceSource(ref source, 1);
        Assert.Equal(packetBefore, GojoNumericFormat.Encode(packet));
        Assert.Equal(frozenValue, frozen.Ticks);
        var future = E.ReadReceipt(root, S.Packet(source));
        Assert.IsType<Placeholder>(C.Parts(future)[5]);
        var sourceBefore = GojoNumericFormat.Encode(source);
        Observe(ref root, issue, frozenValue);
        Assert.Equal(sourceBefore, GojoNumericFormat.Encode(source));
        Assert.Equal(frozenValue, frozen.Ticks);
    }

    [Fact]
    public void FailureOpensOnlyALaterOpportunityAndCannotCreditItsOwnRecruitment()
    {
        var source = S.Create(0);
        var root = Coupled(source);
        var issue = E.Issue(root, S.Packet(source));
        Assert.False(E.HasProposal(root));
        Assert.IsType<Placeholder>(C.Parts(issue)[3]);
        var assessment = Observe(ref root, issue, -1);
        Assert.True(E.HasProposal(root));
        Assert.False(E.Participates(root));
        Assert.Equal(0UL, E.Credit(root).Value);
        Assert.IsType<Placeholder>(C.Parts(assessment)[3]);
        Assert.IsType<Placeholder>(C.Parts(issue)[8]);
        AdvanceSource(ref source, 1);
        Step(ref source, ref root, -1, -1);
        var later = E.Issue(root, S.Packet(source));
        Assert.IsType<CarrierLocation>(C.Parts(later)[3]);
        Assert.Equal(0UL, E.Credit(root).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeadingAndLaggingStreamsAreGradedAgainstTheSameFutureTarget(bool lagging)
    {
        var source = S.Create(0);
        var root = Coupled(source);
        // Supplied environment only. No array, run length or condition enters a learner.
        int[] lengths = [3, 5, 2, 4, 6, 3, 5, 2, 7, 3, 4, 6];
        var driver = lengths.SelectMany((length, run) => Enumerable.Repeat(run % 2 == 0 ? 1L : -1L, length)).ToArray();
        var opportunities = 0;
        var improved = 0;
        var harmed = 0;
        for (var i = 0; i < driver.Length; i++)
        {
            var packet = S.Packet(source);
            var issue = E.Issue(root, packet);
            var fields = C.Parts(issue);
            var baseIssue = C.Parts(Assert.IsType<Relation>(fields[0]));
            Assert.Equal(Scalar(baseIssue[6]).Ticks, Scalar(fields[2]).Ticks);
            Assert.Equal((long)i, Scalar(baseIssue[1]).Ticks);
            Assert.Equal(i + 1L, Scalar(baseIssue[2]).Ticks);
            var issued = Scalar(fields[4]).Ticks;
            var assessment = Observe(ref root, issue, driver[i]);
            var a = C.Parts(assessment);
            Assert.Equal(issued, Scalar(a[13]).Ticks);
            if (a[3] is Relation shadow)
            {
                opportunities++;
                var baseline = Assert.IsType<Relation>(a[2]);
                if (C.Pass(shadow) && !C.Pass(baseline)) improved++;
                if (!C.Pass(shadow) && C.Pass(baseline)) harmed++;
            }
            var next = lagging ? driver[i] : driver[Math.Min(i + 1, driver.Length - 1)];
            AdvanceSource(ref source, next);
        }
        Assert.True(opportunities > 0);
        Assert.Equal((ulong)driver.Length, C.Clock(E.Base(root)).Value);
        output.WriteLine($"lagging={lagging}: opportunities={opportunities}, improved={improved}, harmed={harmed}, support={E.Credit(root).Value}");
        // No aggregate accuracy requirement: the control is allowed to expose a weakness.
    }

    [Fact]
    public void AnAlreadyConsumedIssueCannotEarnCreditTwice()
    {
        var (source, root) = ParticipatingPair();
        var issue = E.Issue(root, S.Packet(source));
        var shadow = Scalar(C.Parts(issue)[3]).Ticks;
        Observe(ref root, issue, shadow);
        var before = GojoNumericFormat.Encode(root);
        var input = new CarrierLocation(C.Sensor(E.Base(root)), C.Level(C.Sensor(E.Base(root))).Ticks + shadow);
        Assert.False(E.TryObserve(root, issue, input, out var rejected, out _));
        Assert.Same(root, rejected);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("foreign")]
    [InlineData("open")]
    public void UnavailableDeliveryLeavesTheIndependentBaseUsableWithoutReward(string kind)
    {
        var (source, root) = ParticipatingPair();
        var fields = C.Parts(S.Packet(source));
        IElement packet = S.Packet(source);
        if (kind == "missing") packet = new Placeholder();
        if (kind == "stale")
        {
            var sequence = Scalar(fields[1]);
            fields[1] = new CarrierLocation(sequence.Carrier, sequence.Ticks - 1);
            packet = C.G(fields);
        }
        if (kind == "foreign")
        {
            var other = S.Create(0);
            for (var i = 0; i < 4; i++) AdvanceSource(ref other, i % 2 == 0 ? 1 : -1);
            packet = S.Packet(other);
        }
        if (kind == "open")
        {
            fields[3] = new Placeholder();
            packet = C.G(fields);
        }
        var prior = GojoNumericFormat.Encode(root);
        var issue = E.Issue(root, packet);
        Assert.Equal(prior, GojoNumericFormat.Encode(root));
        var f = C.Parts(issue);
        Assert.IsType<Placeholder>(f[3]);
        Assert.Equal(Scalar(f[2]).Ticks, Scalar(f[4]).Ticks);
        var credit = E.Credit(root).Value;
        var selection = C.Selected(E.Base(root));
        var clock = C.Clock(E.Base(root)).Value;
        var assessment = Observe(ref root, issue, Scalar(f[2]).Ticks);
        Assert.True(C.Pass(Assert.IsType<Relation>(C.Parts(assessment)[2])));
        Assert.IsType<Placeholder>(C.Parts(assessment)[3]);
        Assert.Same(selection, C.Selected(E.Base(root)));
        Assert.Equal(clock + 1, C.Clock(E.Base(root)).Value);
        Assert.Equal(credit, E.Credit(root).Value);
    }

    [Fact]
    public void FreshEventsCostSupportWithoutDemandAndCannotKeepExporting()
    {
        var (source, root) = ParticipatingPair();
        Assert.Equal(2UL, E.Credit(root).Value);
        E.Demand(root).Remove(1);
        Assert.Equal(0UL, E.Demand(root).Value);
        for (var i = 0; i < 3; i++)
        {
            var issue = E.Issue(root, S.Packet(source));
            var f = C.Parts(issue);
            Assert.Equal(Scalar(f[2]).Ticks, Scalar(f[4]).Ticks);
            var shadow = Scalar(f[3]).Ticks;
            Observe(ref root, issue, shadow);
            Assert.Equal((ulong)Math.Max(0, 1 - i), E.Credit(root).Value);
            Assert.Equal(0, E.Credit(root).Tension);
            Assert.False(E.Participates(root));
            AdvanceSource(ref source, i % 2 == 0 ? 1 : -1);
        }
    }

    [Fact]
    public void JointCheckpointKeepsPublicPortIdentityAndPendingForecastIndependent()
    {
        var (source, root) = ParticipatingPair();
        var issue = E.Issue(root, S.Packet(source));
        var before = GojoNumericFormat.Encode(source, root, issue);
        var decoded = GojoNumericFormat.Decode(before);
        var otherSource = Assert.IsType<Relation>(decoded[0]);
        var other = Assert.IsType<Relation>(decoded[1]);
        var otherIssue = Assert.IsType<Relation>(decoded[2]);
        Assert.Equal(before, GojoNumericFormat.Encode(source, root, issue));
        Assert.Same(S.Port(otherSource), C.Parts(E.Subscription(other))[0]);
        Assert.NotSame(S.Port(source), S.Port(otherSource));
        Assert.NotSame(S.Sensor(source), S.Sensor(otherSource));
        Assert.NotSame(C.Sensor(E.Base(root)), C.Sensor(E.Base(other)));
        Assert.Equal(E.Credit(root).Value, E.Credit(other).Value);
        Assert.Equal(E.Demand(root).Value, E.Demand(other).Value);
        Assert.Equal(S.Clock(source).Value, S.Clock(otherSource).Value);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(Scalar(C.Parts(issue)[4]).Ticks, Scalar(C.Parts(otherIssue)[4]).Ticks);
            var delta = i % 2 == 0 ? 1 : -1;
            var isolatedBefore = GojoNumericFormat.Encode(otherSource, other, otherIssue);
            var a = Observe(ref root, issue, delta);
            AdvanceSource(ref source, delta);
            Assert.Equal(isolatedBefore, GojoNumericFormat.Encode(otherSource, other, otherIssue));
            var b = Observe(ref other, otherIssue, delta);
            AdvanceSource(ref otherSource, delta);
            Assert.Equal(C.Pass(Assert.IsType<Relation>(C.Parts(a)[4])), C.Pass(Assert.IsType<Relation>(C.Parts(b)[4])));
            Assert.Equal(E.Credit(root).Value, E.Credit(other).Value);
            Assert.Equal(E.Credit(root).Tension, E.Credit(other).Tension);
            Assert.Equal(E.Participates(root), E.Participates(other));
            Assert.Equal(C.Level(C.Sensor(E.Base(root))).Ticks, C.Level(C.Sensor(E.Base(other))).Ticks);
            issue = E.Issue(root, S.Packet(source));
            otherIssue = E.Issue(other, S.Packet(otherSource));
        }
    }

    [Fact]
    public void BothReachableWorldsRemainBoundedAcrossLongerPrefixes()
    {
        var (source, root) = ParticipatingPair();
        var producerSizes = new List<long>();
        var consumerSizes = new List<long>();
        for (var i = 0; i < 256; i++)
        {
            var delta = i % 2 == 0 ? 1 : -1;
            Step(ref source, ref root, delta, delta);
            if (i + 1 is 64 or 128 or 256)
            {
                producerSizes.Add(GraphSize(source));
                consumerSizes.Add(GraphSize(root));
                Assert.DoesNotContain(GojoEncodedSnapshot.CaptureGraph(root).Carriers, row => row.Id == S.Sensor(source).Id);
            }
        }
        Assert.All(producerSizes.Skip(1), size => Assert.InRange(size, 1L, producerSizes[0] + 128));
        Assert.All(consumerSizes.Skip(1), size => Assert.InRange(size, 1L, consumerSizes[0] + 128));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NumericallyEqualForeignCoordinateBasesDoNotBecomeReadablePackets(int field)
    {
        var (source, root) = ParticipatingPair();
        var packet = C.Parts(S.Packet(source));
        if (field == 2)
        {
            Assert.True(TimeStepSequence.TryReadSpan(Assert.IsType<Relation>(packet[2]), out var a, out var b));
            packet[2] = Relation.Segment(new Carrier(a.Carrier.UnitShape), a.Ticks, b.Ticks);
        }
        else
        {
            var value = Scalar(packet[field]);
            packet[field] = new CarrierLocation(new Carrier(value.Carrier.UnitShape), value.Ticks);
        }
        var before = GojoNumericFormat.Encode(root);
        var receipt = E.ReadReceipt(root, C.G(packet));
        Assert.IsNotType<CarrierLocation>(C.Parts(receipt)[4]);
        Assert.IsType<Placeholder>(C.Parts(receipt)[5]);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Fact]
    public void ACurrentEndpointDoesNotMakeFutureSupportAvailable()
    {
        var (source, root) = ParticipatingPair();
        var packet = C.Parts(S.Packet(source));
        Assert.True(TimeStepSequence.TryReadSpan((Relation)packet[2], out _, out var end));
        packet[2] = Relation.Segment(end.Carrier, end.Ticks + 1, end.Ticks);
        var before = GojoNumericFormat.Encode(root);
        var receipt = E.ReadReceipt(root, C.G(packet));
        Assert.IsNotType<CarrierLocation>(C.Parts(receipt)[4]);
        Assert.IsType<Placeholder>(C.Parts(receipt)[5]);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Fact]
    public void ExhaustedPublicationSequenceRemainsUnavailableWithoutThrowing()
    {
        var (source, root) = ParticipatingPair();
        var watermark = Scalar(C.Parts(root)[8]);
        watermark.Tick.Set(long.MaxValue);
        var packet = C.Parts(S.Packet(source));
        packet[1] = new CarrierLocation(watermark.Carrier, long.MaxValue);
        var before = GojoNumericFormat.Encode(root);
        var receipt = E.ReadReceipt(root, C.G(packet));
        Assert.IsNotType<CarrierLocation>(C.Parts(receipt)[4]);
        Assert.IsType<Placeholder>(C.Parts(receipt)[5]);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    // These fixtures supply inputs and inspect results; they retain no learner state.
    // A threshold above the base's cap isolates event coupling from its timing aid.
    private static Relation Coupled(Relation source) => E.Create(C.Create(100, requiredSupport: 5), S.Port(source));

    private static (Relation Source, Relation Consumer) ParticipatingPair()
    {
        var source = S.Create(0);
        var root = Coupled(source);
        Step(ref source, ref root, -1, 1);  // First error opens the later opportunity.
        Step(ref source, ref root, -1, -1); // Warmup ends with a fresh reversal publication.
        Step(ref source, ref root, 1, 1);   // First prospective improvement.
        Step(ref source, ref root, -1, -1); // Second prospective improvement.
        Assert.Equal(2UL, E.Credit(root).Value);
        Assert.True(E.Participates(root));
        return (source, root);
    }

    private static Relation Step(ref Relation source, ref Relation root, long consumerDelta, long producerDelta)
    {
        var issue = E.Issue(root, S.Packet(source));
        var assessment = Observe(ref root, issue, consumerDelta);
        AdvanceSource(ref source, producerDelta);
        Assert.Equal(C.Clock(E.Base(root)).Value, S.Clock(source).Value);
        return assessment;
    }

    private static Relation Observe(ref Relation root, Relation issue, long delta)
    {
        var sensor = C.Sensor(E.Base(root));
        var observation = new CarrierLocation(sensor, C.Level(sensor).Ticks + delta);
        Assert.True(E.TryObserve(root, issue, observation, out var next, out var assessment));
        root = next;
        Assert.Equal(14, C.Parts(assessment).Length);
        return assessment;
    }

    private static void AdvanceSource(ref Relation source, long delta)
    {
        var sensor = S.Sensor(source);
        Assert.True(S.TryAdvance(source, new CarrierLocation(sensor, C.Level(sensor).Ticks + delta), out var next, out var packet));
        source = next;
        Assert.Same(packet, S.Packet(source));
    }

    private static CarrierLocation Scalar(IElement element) => Assert.IsType<CarrierLocation>(element);
    private static long GraphSize(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return (long)graph.Carriers.Length + graph.Relations.Length + graph.TickRefs.Length +
            graph.CarrierLocations.Length + graph.UnitShapes.Length + graph.Placeholders.Length;
    }
}
