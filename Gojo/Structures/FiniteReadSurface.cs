using Gojo.Core;
using Gojo.ConceptSurface;
using Gojo.Equations;

namespace Gojo.Structures;

/// <summary>
/// An explicit finite, lossy publication law. Capacity, resolution, support and
/// excluded detail are connected structure; these helpers read that local
/// contract without executing private suppliers or wider exact arithmetic.
/// This is not a replacement for the existing exact equation readers.
/// </summary>
public static class FiniteReadSurface
{
    /// <summary>Creates a zero-containing tick domain and a positive publication
    /// quantum. Invalid or incomplete domains remain structure but cannot be read.</summary>
    public static Relation Capacity(Carrier root, Carrier axis, long minimum, long maximum, long quantum = 1) =>
        BoundedWholeSurface.Create(root, axis, Relation.Point(axis, minimum), Relation.Point(axis, maximum),
            Relation.Segment(axis, 0, quantum));

    /// <summary>Publishes a frozen input in the selected finite domain. Clipping
    /// retains an open support endpoint; coarsening retains a rounding cell,
    /// never the exact discarded complement. Source evidence stays opaque.</summary>
    public static Relation Publish(Carrier root, Relation capacity, CarrierLocation input, IElement source)
    {
        if (!TryLimits(capacity, out var limits) || input.Carrier is null || input.Tick is null ||
            !input.Carrier.SameIdentityAs(limits.Axis))
            return Unresolved(root, source, capacity);
        var inputTicks = input.Ticks;
        var clipped = inputTicks < limits.Min ? -1 : inputTicks > limits.Max ? 1 : 0;
        var value = clipped < 0 ? limits.Min : clipped > 0 ? limits.Max : inputTicks;
        var support = new Support(clipped < 0 ? null : value, clipped > 0 ? null : value);
        return Observe(root, limits, source, value, support, clipped, new Placeholder());
    }

    public static Relation Add(Carrier root, Relation capacity, Relation left, Relation right) =>
        Combine(root, capacity, left, right, subtract: false);

    public static Relation Subtract(Carrier root, Relation capacity, Relation left, Relation right) =>
        Combine(root, capacity, left, right, subtract: true);

    private static Relation Combine(Carrier root, Relation capacity, Relation left, Relation right, bool subtract)
    {
        var selected = Group(left, right);
        if (!TryLimits(capacity, out var limits) ||
            !TryOperand(left, limits, out var a, out var aSupport) ||
            !TryOperand(right, limits, out var b, out var bSupport))
            return Unresolved(root, selected, capacity);

        var equation = Equation.Sum(limits.Axis, Equation.UnitPort(limits.Axis), subtract
            ? Equation.WeightedPort(limits.Axis, 0, 1, 0, -1) : Equation.UnitPort(limits.Axis));
        // The same publication is one sample, including its unresolved detail.
        // Equal-looking independent publications do not establish that identity.
        if (subtract && ReferenceEquals(left, right))
            return Observe(root, limits, selected, 0, new Support(0, 0), 0, equation);

        var nominal = subtract ? SubtractTicks(a, b, limits) : AddTicks(a, b, limits);
        var low = Bound(aSupport.Lower, subtract ? bSupport.Upper : bSupport.Lower, lower: true);
        var high = Bound(aSupport.Upper, subtract ? bSupport.Lower : bSupport.Upper, lower: false);
        return Observe(root, limits, selected, nominal.Value, new Support(low, high), nominal.Clipped, equation);

        long? Bound(long? x, long? y, bool lower)
        {
            if (!x.HasValue || !y.HasValue) return null;
            var result = subtract ? SubtractTicks(x.Value, y.Value, limits) : AddTicks(x.Value, y.Value, limits);
            return lower && result.Clipped < 0 || !lower && result.Clipped > 0 ? null : result.Value;
        }
    }

    /// <summary>Multiplies tick amounts in the explicitly selected unit-product
    /// frame. Closed support uses four corners; open inputs conservatively retain
    /// unresolved support. No hidden exact product is calculated on clipping.</summary>
    public static Relation Multiply(Carrier root, Relation capacity, Relation left, Relation right)
    {
        var selected = Group(left, right);
        if (!TryLimits(capacity, out var limits) ||
            !TryOperand(left, limits, out var a, out var aSupport) ||
            !TryOperand(right, limits, out var b, out var bSupport))
            return Unresolved(root, selected, capacity);
        var equation = Equation.Product(limits.Axis, Equation.UnitPort(limits.Axis), Equation.UnitPort(limits.Axis));
        var nominal = MultiplyTicks(a, b, limits);
        var support = new Support(null, null);
        if (aSupport == new Support(0, 0) || bSupport == new Support(0, 0)) support = new Support(0, 0);
        else if (aSupport.Lower.HasValue && aSupport.Upper.HasValue && bSupport.Lower.HasValue && bSupport.Upper.HasValue)
        {
            var corners = new[]
            {
                MultiplyTicks(aSupport.Lower.Value, bSupport.Lower.Value, limits),
                MultiplyTicks(aSupport.Lower.Value, bSupport.Upper.Value, limits),
                MultiplyTicks(aSupport.Upper.Value, bSupport.Lower.Value, limits),
                MultiplyTicks(aSupport.Upper.Value, bSupport.Upper.Value, limits)
            };
            support = new Support(corners.Any(x => x.Clipped < 0) ? null : corners.Min(x => x.Value),
                corners.Any(x => x.Clipped > 0) ? null : corners.Max(x => x.Value));
        }
        return Observe(root, limits, selected, nominal.Value, support, nominal.Clipped, equation);
    }

    public static bool TryReadValue(Relation publication, out CarrierLocation value) =>
        TryPublication(publication, out _, out value, out _);

    /// <summary>Reads only this finite publication's authored endpoint roles.
    /// A null endpoint is open, not a numeric infinity or a generic meaning
    /// assigned to every Placeholder elsewhere in Gojo.</summary>
    public static bool TryReadSupport(Relation publication, out CarrierLocation? lower, out CarrierLocation? upper)
    {
        if (TryPublication(publication, out var limits, out _, out var support))
        {
            lower = support.Lower is long low ? new CarrierLocation(limits.Axis, low) : null;
            upper = support.Upper is long high ? new CarrierLocation(limits.Axis, high) : null;
            return true;
        }
        lower = upper = null; return false;
    }

    /// <summary>Answers only when the exposed support settles the threshold.
    /// False return means unresolved demand, not a false comparison. Inputs and
    /// private provenance are never inspected to recover discarded precision.</summary>
    public static bool TryGreaterThan(Relation publication, long threshold, out bool answer)
    {
        if (TryPublication(publication, out _, out _, out var support))
        {
            if (support.Lower is long low && low > threshold) { answer = true; return true; }
            if (support.Upper is long high && high <= threshold) { answer = false; return true; }
        }
        answer = default; return false;
    }

    private static Relation Observe(Carrier root, Limits limits, IElement selected, long value,
        Support support, int clipped, IElement equation)
    {
        // A clipped boundary is the organism's available nominal answer. Do not
        // round it away from the endpoint. Other outputs deliberately lose detail.
        if (clipped == 0 && limits.Quantum > 1)
        {
            value = value / limits.Quantum * limits.Quantum;
            if (support.Lower is long low) support = support with { Lower = Cell(low, limits).Lower };
            if (support.Upper is long high) support = support with { Upper = Cell(high, limits).Upper };
        }
        var variation = new Relation(Endpoint(support.Lower), Endpoint(support.Upper));
        var excluded = support == new Support(value, value)
            ? (IElement)Relation.Point(limits.Axis, 0) : new Relation(selected, variation);
        var frozenCapacity = Capacity(new Carrier(), limits.Axis, limits.Min, limits.Max, limits.Quantum);
        return QualifiedReadSurface.Create(root, selected, Relation.Segment(limits.Axis, 0, 1),
            new CarrierLocation(limits.Axis, value), variation, new Placeholder(), frozenCapacity,
            excluded, new Relation(equation, frozenCapacity));

        IElement Endpoint(long? ticks) => ticks is long amount ? new CarrierLocation(limits.Axis, amount) : new Placeholder();
    }

    private static Support Cell(long value, Limits limits)
    {
        var nominal = value / limits.Quantum * limits.Quantum;
        var width = limits.Quantum - 1;
        var lower = nominal <= 0 ? nominal < limits.Min + width ? limits.Min : nominal - width : nominal;
        var upper = nominal >= 0 ? nominal > limits.Max - width ? limits.Max : nominal + width : nominal;
        return new Support(lower, upper);
    }

    // Guards establish that the following long operation fits the selected
    // domain. The discarded value is never evaluated in a wider lane.
    private static (long Value, int Clipped) AddTicks(long a, long b, Limits limits)
    {
        if (b > 0 && a > limits.Max - b) return (limits.Max, 1);
        if (b < 0 && a < limits.Min - b) return (limits.Min, -1);
        return (a + b, 0);
    }

    private static (long Value, int Clipped) SubtractTicks(long a, long b, Limits limits)
    {
        if (b > 0 && a < limits.Min + b) return (limits.Min, -1);
        if (b < 0 && a > limits.Max + b) return (limits.Max, 1);
        return (a - b, 0);
    }

    private static (long Value, int Clipped) MultiplyTicks(long a, long b, Limits limits)
    {
        if (a == 0 || b == 0) return (0, 0);
        if (a > 0 && b > 0 && a > limits.Max / b || a < 0 && b < 0 && a < limits.Max / b)
            return (limits.Max, 1);
        if (a > 0 && b < 0 && b < limits.Min / a || a < 0 && b > 0 && a < limits.Min / b)
            return (limits.Min, -1);
        return (a * b, 0);
    }

    private static bool TryOperand(Relation read, Limits limits, out long value, out Support support)
    {
        if (TryPublication(read, out var source, out var point, out support) && source.Axis.SameIdentityAs(limits.Axis) &&
            point.Ticks >= limits.Min && point.Ticks <= limits.Max &&
            (!support.Lower.HasValue || support.Lower.Value >= limits.Min && support.Lower.Value <= limits.Max) &&
            (!support.Upper.HasValue || support.Upper.Value >= limits.Min && support.Upper.Value <= limits.Max))
        { value = point.Ticks; return true; }
        value = default; support = default; return false;
    }

    private static bool TryPublication(Relation publication, out Limits limits, out CarrierLocation value, out Support support)
    {
        if (QualifiedReadSurface.TryReadPublished(publication, out _, out var basis, out var result,
                out var variation, out _, out var scope, out _, out _) && scope is Relation capacity &&
            TryLimits(capacity, out limits) && TryPoint(basis.Left, out var start) && TryPoint(basis.Right, out var end) &&
            start.Carrier.SameIdentityAs(limits.Axis) && end.Carrier.SameIdentityAs(limits.Axis) &&
            start.Ticks == 0 && end.Ticks == 1 && result is CarrierLocation point && point.Carrier is not null && point.Tick is not null &&
            point.Carrier.SameIdentityAs(limits.Axis) && point.Ticks >= limits.Min && point.Ticks <= limits.Max &&
            variation is Relation interval && TryEndpoint(interval.Left, limits.Axis, out var low) &&
            TryEndpoint(interval.Right, limits.Axis, out var high) &&
            (!low.HasValue || low.Value <= point.Ticks) && (!high.HasValue || high.Value >= point.Ticks))
        { value = point; support = new Support(low, high); return true; }
        limits = default; value = default; support = default; return false;
    }

    private static bool TryLimits(Relation capacity, out Limits limits)
    {
        if (BoundedWholeSurface.TryRead(capacity, out var whole, out var left, out var right, out var partition) &&
            whole is Carrier axis && !axis.IsUnknown && TryPoint(left, out var low) && TryPoint(right, out var high) &&
            low.Carrier.SameIdentityAs(axis) && high.Carrier.SameIdentityAs(axis) && low.Ticks <= 0 && high.Ticks >= 0 &&
            partition is Relation resolution && TryPoint(resolution.Left, out var origin) && TryPoint(resolution.Right, out var step) &&
            origin.Carrier.SameIdentityAs(axis) && step.Carrier.SameIdentityAs(axis) && origin.Ticks == 0 && step.Ticks > 0)
        { limits = new Limits(axis, low.Ticks, high.Ticks, step.Ticks); return true; }
        limits = default; return false;
    }

    private static bool TryEndpoint(IElement element, Carrier axis, out long? ticks)
    {
        if (element is Placeholder) { ticks = null; return true; }
        if (TryPoint(element, out var point) && point.Carrier.SameIdentityAs(axis)) { ticks = point.Ticks; return true; }
        ticks = default; return false;
    }

    private static bool TryPoint(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation location && location.Carrier is not null && location.Tick is not null)
        { point = location; return true; }
        if (element is Relation relation && relation.Left is CarrierLocation p && p.Carrier is not null && p.Tick is not null &&
            relation.Right is Carrier c && c.SameIdentityAs(p.Carrier))
        { point = p; return true; }
        point = default; return false;
    }

    private static Relation Unresolved(Carrier root, IElement selected, Relation capacity) =>
        ReadWitness.Create(root, selected, capacity, new Placeholder(), new Relation(selected, capacity), capacity);

    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);

    // Ephemeral inspection values, not canonical state. The relations above
    // remain the capacity and support source of meaning.
    private readonly record struct Limits(Carrier Axis, long Min, long Max, long Quantum);
    private readonly record struct Support(long? Lower, long? Upper);
}
