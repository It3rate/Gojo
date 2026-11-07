using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class CoupledComparisonLinkTests
{
    [Fact]
    public void Native_link_identifies_the_actual_public_source_and_samples_live_ports()
    {
        var source = new Carrier(); source.Add(4);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var port = new Relation(source.ValueSlot, source);
        var link = CoupledComparisonLink.Create(port, target);
        Assert.True(CoupledComparisonLink.TryRead(link.Surface, out var read));
        Assert.Same(port, read.SourcePort); Assert.Same(target.ExtentPort, read.ReceiverPort);
        Assert.Same(target.Sensor, read.Sensor); Assert.True(read.Comparable);
        var sample = link.Observe();
        Assert.Equal(4, sample.Mask); Assert.Equal(4, sample.SourceSample); Assert.Equal(1, sample.ReceiverSample);
        var accepted = target.Step(sample.Packet);
        Assert.Equal(1, accepted.AcceptedDelta);
        source.Remove(4);
        Assert.Equal(1, link.Observe().Mask);
    }

    [Fact]
    public void Exact_samples_and_private_link_are_not_reachable_from_the_transmitted_packet()
    {
        var source = new Carrier(); source.Add(5);
        var target = CoupledExtentMachine.Create(2, 8, unit: source.UnitShape);
        var port = new Relation(source.ValueSlot, source);
        var link = CoupledComparisonLink.Create(port, target);
        var sample = link.Observe();
        var packet = Reachable(sample.Packet).ToArray();
        Assert.DoesNotContain(packet, x => ReferenceEquals(x, source));
        Assert.DoesNotContain(packet, x => ReferenceEquals(x, port));
        Assert.DoesNotContain(packet, x => ReferenceEquals(x, target.ExtentPort));
        Assert.DoesNotContain(packet, x => ReferenceEquals(x, link.Surface));
        Assert.DoesNotContain(packet, x => ReferenceEquals(x, sample.Witness));
        Assert.True(QualitativePublication.TryRead(sample.Packet, out _, out _, out _, out _, out _, out _, out var evidence));
        Assert.IsType<Placeholder>(evidence);
        var witness = Reachable(sample.Witness).ToArray();
        Assert.Contains(witness, x => ReferenceEquals(x, port));
        Assert.Contains(witness, x => ReferenceEquals(x, target.ExtentPort));
    }

    [Fact]
    public void Samples_keep_their_coordinates_after_the_live_values_change()
    {
        var source = new Carrier(); source.Add(4);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        var sample = link.Observe();
        var fields = Entries((Relation)sample.Witness.Right);
        var oldSource = (CarrierLocation)((Relation)fields[0]).Right;
        var oldTarget = (CarrierLocation)((Relation)fields[1]).Right;
        target.Step(sample.Packet); source.Remove(3);
        Assert.Equal(4, oldSource.Ticks); Assert.Equal(1, oldTarget.Ticks);
        Assert.Equal(1, link.Observe().Mask);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 7)]
    public void Withheld_or_ambiguous_publications_do_not_leak_the_exact_comparison(bool available, bool ambiguous, byte expected)
    {
        var source = new Carrier(); source.Add(4);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        var sample = link.Observe(available, ambiguous);
        Assert.Equal(expected, sample.Mask);
        var step = target.Step(sample.Packet);
        Assert.False(step.Resolved); Assert.Equal(1, target.Snapshot().Value);
        Assert.Equal(4, sample.SourceSample); // Private diagnostic evidence only.
    }

    [Fact]
    public void Incompatible_units_retain_an_open_correspondence_and_cannot_command_growth()
    {
        var source = new Carrier(); source.Add(5);
        var target = CoupledExtentMachine.Create(1, 8);
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        Assert.True(CoupledComparisonLink.TryRead(link.Surface, out var read));
        Assert.False(read.Comparable);
        var sample = link.Observe(ambiguous: true);
        Assert.False(sample.Comparable); Assert.Equal(0, sample.Mask);
        Assert.False(target.Step(sample.Packet).Resolved);
        Assert.Equal(1, target.Snapshot().Value);
        var fields = Entries((Relation)sample.Witness.Right);
        var a = (CarrierLocation)((Relation)fields[0]).Right;
        var b = (CarrierLocation)((Relation)fields[1]).Right;
        Assert.False(a.Carrier.UnitShape.Equals(b.Carrier.UnitShape));
    }

    [Fact]
    public void Coincident_frozen_coordinates_cannot_impersonate_an_advertised_live_value_slot()
    {
        var source = new Carrier(); source.Add(3);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var fake = new Relation(new CarrierLocation(source, 3), source);
        Assert.Throws<ArgumentException>(() => CoupledComparisonLink.Create(fake, target));
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        var rows = Entries(link.Surface); rows[1] = fake;
        Assert.False(CoupledComparisonLink.TryRead(StructuralConstraint.Closed(rows), out _));
    }

    [Fact]
    public void A_delayed_packet_keeps_its_original_sign_instead_of_reading_the_new_source()
    {
        var source = new Carrier(); source.Add(4);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        var old = link.Observe(); source.Remove(4);
        Assert.Equal(1, link.Observe().Mask);
        var step = target.Step(old.Packet);
        Assert.Equal(1, step.RequestedDelta); Assert.Equal(2, target.Snapshot().Value);
        Assert.Equal(4, old.SourceSample);
        Assert.False(target.Step(old.Packet).ConsumedEvent);
    }

    [Fact]
    public void Native_transport_restores_the_link_without_borrowing_original_live_ports()
    {
        var source = new Carrier(); source.Add(4);
        var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var link = CoupledComparisonLink.Create(new Relation(source.ValueSlot, source), target);
        var restored = CoupledComparisonLink.FromSurface(Assert.IsType<Relation>(Assert.Single(
            GojoNumericFormat.Decode(GojoNumericFormat.Encode(link.Surface)))));
        Assert.True(CoupledComparisonLink.TryRead(restored.Surface, out var read));
        Assert.NotSame(link.Surface, restored.Surface);
        Assert.NotSame(source, read.SourcePort.Right); Assert.Equal(4, restored.Observe().Mask);
        source.Remove(4);
        Assert.Equal(1, link.Observe().Mask); Assert.Equal(4, restored.Observe().Mask);
    }

    [Fact]
    public void Event_and_output_identities_cannot_smuggle_the_private_source_into_a_packet()
    {
        var source = new Carrier(); var target = CoupledExtentMachine.Create(1, 8, unit: source.UnitShape);
        var port = new Relation(source.ValueSlot, source);
        var link = CoupledComparisonLink.Create(port, target);
        Assert.Throws<ArgumentException>(() => link.Observe(eventIdentity: port));
        var rows = Entries(link.Surface); rows[4] = source;
        Assert.False(CoupledComparisonLink.TryRead(StructuralConstraint.Closed(rows), out _));
    }

    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation r) { stack.Push(r.Left); stack.Push(r.Right); }
        }
    }
}
