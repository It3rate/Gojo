using System.Numerics;
using Gojo.Core;

namespace Gojo.Experiments.RefinementPerformance;

/// <summary>
/// Narrow derived backend for the scalar affine-port sums/products used by
/// this benchmark. Native equations remain authoritative. The caller guards
/// the captured graph at a round boundary and retains samples for native
/// reconstruction of source-basis residue when evidence is requested.
/// </summary>
public sealed class NumericEquationPlan
{
    private readonly Port[] ports;
    private readonly bool product;
    private readonly NumericSourceGuard guard;
    public Relation Source { get; }
    public Carrier Output { get; }
    public int InputCount => ports.Length;
    public bool IsCurrent => guard.IsCurrent;

    private NumericEquationPlan(Relation source, Carrier output, Port[] ports, bool product)
    {
        Source = source; Output = output; this.ports = ports; this.product = product;
        guard = new NumericSourceGuard(source);
    }

    public static NumericEquationPlan? Capture(Relation source, Carrier output)
    {
        if (output.IsUnknown || output.IsInverse) return null;
        var product = false;
        if (Point(source.Right, out var axis, out var point) && ReferenceEquals(axis, output) && point == 1) { }
        else if (Frame(source.Right, out axis, out var start, out var end) &&
            ReferenceEquals(axis, output) && start == 0 && end == 1) product = true;
        else return null;
        var ports = new List<Port>();
        if (!Collect(source.Left, ports) || ports.Count == 0) return null;
        return new NumericEquationPlan(source, output, ports.ToArray(), product);
    }

    public Carrier InputAxis(int index) => ports[index].Axis;

    /// <summary>Executes after the caller checks IsCurrent. This does not
    /// publish a point and does not discard the retained input samples.</summary>
    public long Evaluate(ReadOnlySpan<long> samples)
    {
        if (samples.Length != ports.Length) throw new ArgumentException("Wrong native operand count.", nameof(samples));
        try
        {
            Int128 result = product ? 1 : 0;
            for (var index = 0; index < ports.Length; index++)
            {
                var port = ports[index];
                var projected = checked((long)(port.TargetStart +
                    checked(((Int128)samples[index] - port.SourceStart) * port.TargetExtent) / port.SourceExtent));
                result = product ? checked(result * projected) : checked(result + projected);
            }
            return checked((long)result);
        }
        catch (OverflowException)
        {
            // Match the native exact reader's range rather than silently
            // clipping or making a wide intermediate an invented failure.
            BigInteger result = product ? BigInteger.One : BigInteger.Zero;
            for (var index = 0; index < ports.Length; index++)
            {
                var port = ports[index];
                var projected = checked((long)(port.TargetStart +
                    ((BigInteger)samples[index] - port.SourceStart) * port.TargetExtent / port.SourceExtent));
                result = product ? result * projected : result + projected;
            }
            return checked((long)result);
        }
    }

    private static bool Collect(IElement element, List<Port> ports)
    {
        if (element is not Relation relation) return false;
        if (Frame(relation.Left, out var source, out var start, out var end) &&
            Frame(relation.Right, out var target, out var targetStart, out var targetEnd))
        {
            // Opposed carrier participation needs the native component/face
            // algebra, beyond this scalar forward-axis execution projection.
            if (source.IsUnknown || source.IsInverse || !target.IsUnknown || start == end) return false;
            ports.Add(new Port(source, start, checked(end - start), targetStart, checked(targetEnd - targetStart)));
            return true;
        }
        return Collect(relation.Left, ports) && Collect(relation.Right, ports);
    }

    internal static bool Frame(IElement element, out Carrier axis, out long start, out long end)
    {
        if (element is Relation relation && Point(relation.Left, out var first, out start) &&
            Point(relation.Right, out var last, out end) && ReferenceEquals(first, last))
        { axis = first; return true; }
        axis = null!; start = end = 0; return false;
    }

    internal static bool Point(IElement element, out Carrier axis, out long ticks)
    {
        if (element is Relation { Left: CarrierLocation point, Right: Carrier carrier } &&
            ReferenceEquals(point.Carrier, carrier))
        { axis = carrier; ticks = point.Ticks; return true; }
        axis = null!; ticks = 0; return false;
    }

    private readonly record struct Port(Carrier Axis, long SourceStart, long SourceExtent,
        long TargetStart, long TargetExtent);
}

/// <summary>Compiled read of ordered native interval conditions. It accepts
/// exact measured points only; it is not a replacement for general support,
/// ambiguous publications or the full SupportDecisionSurface witness reader.</summary>
public sealed class NumericIntervalPolicyPlan
{
    private readonly Rule[] rules;
    private readonly NumericSourceGuard guard;
    public Relation Source { get; }
    public bool IsCurrent => guard.IsCurrent;

    private NumericIntervalPolicyPlan(Relation source, Rule[] rules)
    { Source = source; this.rules = rules; guard = new NumericSourceGuard(source); }

    public static NumericIntervalPolicyPlan? Capture(Relation source)
    {
        if (!source.TryReadOrderedGroup(out var entries)) return null;
        var rules = new List<Rule>();
        foreach (var entry in entries)
        {
            if (entry is not Relation relation || !relation.TryReadOrderedGroup(out var fields) || fields.Length != 2 ||
                fields[0] is not Relation conditions || !conditions.TryReadOrderedGroup(out var checks) ||
                fields[1] is not CarrierLocation response) return null;
            var demands = new List<Demand>();
            foreach (var check in checks)
            {
                if (check is not Relation condition || !condition.TryReadOrderedGroup(out var parts) || parts.Length != 2 ||
                    !NumericEquationPlan.Frame(parts[1], out var axis, out var low, out var high) || low > high) return null;
                demands.Add(new Demand(parts[0], axis, low, high));
            }
            rules.Add(new Rule(relation, response, demands.ToArray()));
        }
        return new NumericIntervalPolicyPlan(source, rules.ToArray());
    }

    /// <summary>Checks exact publications supplied at the selected roles. The
    /// native rule identity returned here remains available in a later trace.</summary>
    public bool TrySelect(IElement firstRole, Carrier firstAxis, long firstValue,
        IElement? secondRole, Carrier? secondAxis, long secondValue,
        out CarrierLocation response, out Relation selectedRule)
    {
        if (secondRole is not null && Equals(firstRole, secondRole))
        { response = default; selectedRule = null!; return false; }
        foreach (var rule in rules)
        {
            var accepts = true;
            foreach (var demand in rule.Demands)
            {
                long value;
                if (Equals(demand.Role, firstRole) && ReferenceEquals(demand.Axis, firstAxis)) value = firstValue;
                else if (secondRole is not null && Equals(demand.Role, secondRole) &&
                    ReferenceEquals(demand.Axis, secondAxis)) value = secondValue;
                else { accepts = false; break; }
                if (value < demand.Minimum || value > demand.Maximum) { accepts = false; break; }
            }
            if (accepts) { response = rule.Response; selectedRule = rule.Source; return true; }
        }
        response = default; selectedRule = null!; return false;
    }

    private sealed record Rule(Relation Source, CarrierLocation Response, Demand[] Demands);
    private readonly record struct Demand(IElement Role, Carrier Axis, long Minimum, long Maximum);
}

internal sealed class NumericSourceGuard
{
    private readonly (Relation Site, IElement Left, IElement Right)[] relations;
    private readonly (TickRef Site, long Value)[] ticks;

    public NumericSourceGuard(IElement source)
    {
        var relationSites = new List<(Relation, IElement, IElement)>();
        var tickSites = new List<(TickRef, long)>();
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var element))
        {
            if (element is CarrierLocation point)
            {
                if (seen.Add(point.Tick)) tickSites.Add((point.Tick, point.Ticks));
            }
            else if (element is Relation relation && seen.Add(relation))
            {
                relationSites.Add((relation, relation.Left, relation.Right));
                stack.Push(relation.Left); stack.Push(relation.Right);
            }
        }
        relations = relationSites.ToArray(); ticks = tickSites.ToArray();
    }

    public bool IsCurrent
    {
        get
        {
            foreach (var site in relations)
                if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right)) return false;
            foreach (var site in ticks) if (site.Site.Ticks != site.Value) return false;
            return true;
        }
    }
}
