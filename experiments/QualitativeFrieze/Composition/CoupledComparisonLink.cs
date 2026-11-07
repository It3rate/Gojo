using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// An explicitly connected sensor between two advertised extent ports. Its
/// private witness retains the sampled values and their correspondence. Only a
/// comparison support, event identity and opaque sensor identity are published
/// to the actuator. This observer is not a material-transfer route.
/// </summary>
public sealed class CoupledComparisonLink
{
    // link = Closed(public identity, source port, receiver port,
    //   one-tick correspondence or open correspondence,
    //   output sensor, output owner, comparison basis, comparison domain)
    // port = owning carrier's actual live value location -> owning carrier
    // sample = advertised port -> frozen location on the observation basis
    // witness = (link -> event) -> Closed(source sample, receiver sample,
    //   comparison result, selected publication)
    // The packet has NO edge back to the link, samples, source port or witness.

    public Relation Surface { get; }

    public sealed record LinkRead(IElement PublicIdentity, Relation SourcePort,
        Relation ReceiverPort, Relation Correspondence, IElement Sensor,
        IElement Owner, Carrier ComparisonBasis, Relation Domain, bool Comparable);

    public sealed record Observation(Relation Packet, byte Mask, Relation Witness,
        long? SourceSample, long? ReceiverSample, bool Comparable);

    private CoupledComparisonLink(Relation surface) => Surface = surface;

    public static CoupledComparisonLink Create(Relation sourcePort, CoupledExtentMachine receiver)
    {
        if (!Port(sourcePort, out var source, out _) || !Port(receiver.ExtentPort, out var target, out _))
            throw new ArgumentException("A comparison link needs two actual advertised value ports.");
        var sourceTick = Relation.Segment(source, 0, 1);
        var targetTick = Relation.Segment(target, 0, 1);
        // This finite experiment selects unit-for-unit observation only when
        // the stable tick promises match. An incompatible basis stays open.
        Relation correspondence = source.UnitShape.Equals(target.UnitShape)
            ? new(sourceTick, targetTick)
            : new(sourceTick, StructuralConstraint.Open(targetTick));
        var surface = S(new Placeholder(), sourcePort, receiver.ExtentPort,
            correspondence, receiver.Sensor, receiver.Owner,
            receiver.ComparisonBasis, receiver.ComparisonDomain);
        return FromSurface(surface);
    }

    public static CoupledComparisonLink FromSurface(Relation surface) => TryRead(surface, out _)
        ? new(surface) : throw new ArgumentException("Expected an exact comparison-link surface.", nameof(surface));

    public static bool TryRead(Relation surface, out LinkRead read)
    {
        read = null!;
        if (!Closed(surface, out var rows) || rows is not
            [IElement identity, Relation sourcePort, Relation targetPort, Relation correspondence,
             IElement sensor, IElement owner, Carrier basis, Relation domain] ||
            identity is not Placeholder || sensor is not Placeholder || owner is not Placeholder ||
            !Port(sourcePort, out var source, out _) ||
            !Port(targetPort, out var target, out _) || !Closed(domain, out var states) ||
            ReferenceEquals(basis, source) || ReferenceEquals(basis, target) ||
            states.Length != 3 || states.Where((item, i) => item is not CarrierLocation p ||
                !ReferenceEquals(p.Carrier, basis) || p.Ticks != i - 1).Any()) return false;

        if (correspondence.Left is not Relation from || !Span(from, source, 0, 1)) return false;
        var comparable = correspondence.Right is Relation to && Span(to, target, 0, 1) &&
            source.UnitShape.Equals(target.UnitShape);
        if (!comparable && (correspondence.Right is not Relation open ||
            !StructuralConstraint.TryReadScope(open, out var candidates, out var seam, out var tail) ||
            seam.Equals(tail) || candidates.Length != 1 || candidates[0] is not Relation candidate ||
            !Span(candidate, target, 0, 1))) return false;

        read = new(identity, sourcePort, targetPort, correspondence, sensor, owner, basis, domain, comparable);
        return true;
    }

    /// <summary>
    /// Samples the actual ports in Surface. Withholding and coarsening belong
    /// to the supplied observation apparatus. No argument can forge a selected
    /// singleton comparison. The diagnostic witness may retain exact samples
    /// even when that detail is deliberately absent from the packet.
    /// </summary>
    public Observation Observe(bool available = true, bool ambiguous = false, IElement? eventIdentity = null)
    {
        if (!TryRead(Surface, out var read) || !Port(read.SourcePort, out var source, out var sourceValue) ||
            !Port(read.ReceiverPort, out var target, out var targetValue))
            throw new InvalidOperationException("The connected comparison surface no longer reads.");
        var occurrence = eventIdentity ?? new Placeholder();
        if (occurrence is not Placeholder)
            throw new ArgumentException("An observation event must be an opaque identity, not a private source graph.", nameof(eventIdentity));
        var sourceSample = new Relation(read.SourcePort, new CarrierLocation(new Carrier(source.UnitShape), sourceValue));
        var targetSample = new Relation(read.ReceiverPort, new CarrierLocation(new Carrier(target.UnitShape), targetValue));
        var exact = !read.Comparable ? (byte)0 : targetValue > sourceValue ? (byte)1 : targetValue == sourceValue ? (byte)2 : (byte)4;
        var mask = !read.Comparable || !available ? (byte)0 : ambiguous ? (byte)7 : exact;
        Closed(read.Domain, out var states);
        Relation Support(byte selected) => selected == 0 ? StructuralConstraint.Open() :
            S(states.Where((_, i) => (selected & 1 << i) != 0).ToArray());
        var publishedSupport = Support(mask);
        // Deliberately no sample values or source graph in packet evidence.
        var packet = QualitativePublication.Create(read.Sensor, read.Owner, occurrence,
            read.ComparisonBasis, read.Domain, publishedSupport, read.PublicIdentity);
        var exactRead = new Relation(read.Correspondence,
            read.Comparable ? Support(exact) : StructuralConstraint.Open(read.Correspondence));
        var witness = new Relation(new Relation(Surface, occurrence),
            S(sourceSample, targetSample, exactRead, packet));
        return new(packet, mask, witness, sourceValue, targetValue, read.Comparable);
    }

    private static bool Port(Relation port, out Carrier carrier, out long value)
    {
        carrier = null!; value = 0;
        if (port is not { Left: CarrierLocation location, Right: Carrier owning } ||
            owning.IsUnknown || !ReferenceEquals(location.Carrier, owning) || !location.SameTickRefAs(owning.ValueSlot)) return false;
        carrier = owning; value = location.Ticks; return true;
    }

    private static bool Span(Relation span, Carrier carrier, long start, long end) =>
        span.Left is Relation a && span.Right is Relation b && Point(a, carrier, start) && Point(b, carrier, end);
    private static bool Point(Relation point, Carrier carrier, long coordinate) =>
        point.Left is CarrierLocation location && ReferenceEquals(point.Right, carrier) &&
        ReferenceEquals(location.Carrier, carrier) && location.Ticks == coordinate;
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
