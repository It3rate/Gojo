using Gojo.Core;
using Gojo.Encoding;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// One private native evaluation world, reused only while its source topology,
/// fixed sites and non-actuator states still match. This is an execution cache,
/// not a second description of the construction law. Attached settlement worlds
/// use the fully isolated preview instead: reversing a transfer is not generally
/// an undo operation. No scratch definition or live value slot leaves this cache.
/// </summary>
internal sealed class NativePreviewWorkspace
{
    private readonly Relation definition;
    private readonly (Relation Relation, IElement Left, IElement Right)[] topology;
    private readonly (TickRef Tick, long Value)[] sites;
    private readonly (Carrier Carrier, ulong Value, long Tension, bool Actuator)[] carriers;
    private readonly Dictionary<Carrier, Carrier> actuators;

    private NativePreviewWorkspace(Relation source, Carrier[] parameters,
        (Relation Relation, IElement Left, IElement Right)[] topology,
        (TickRef Tick, long Value)[] sites,
        (Carrier Carrier, ulong Value, long Tension, bool Actuator)[] carriers)
    {
        this.topology = topology; this.sites = sites; this.carriers = carriers;
        var packed = G(source, G(parameters));
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(packed))[0];
        var fields = F(restored);
        definition = (Relation)fields[0];
        actuators = parameters.Zip(F(fields[1]).Cast<Carrier>()).ToDictionary(p => p.First, p => p.Second);
    }

    public static NativePreviewWorkspace? TryCreate(Relation source, Carrier[] parameters)
    {
        var relations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var carriers = new HashSet<Carrier>(ReferenceEqualityComparer.Instance);
        var units = new HashSet<UnitShape>(ReferenceEqualityComparer.Instance);
        var ticks = new HashSet<TickRef>(ReferenceEqualityComparer.Instance);
        var parameterSet = parameters.ToHashSet();
        void Visit(IElement element, bool immutableUnit = false)
        {
            switch (element)
            {
                case Relation relation:
                    if (!relations.Add(relation)) return;
                    Visit(relation.Left, immutableUnit); Visit(relation.Right, immutableUnit);
                    break;
                case CarrierLocation location:
                    if (!immutableUnit) ticks.Add(location.Tick);
                    Visit(location.Carrier);
                    break;
                case Carrier carrier:
                    if (!carriers.Add(carrier)) return;
                    if (units.Add(carrier.UnitShape))
                    {
                        Visit(carrier.UnitShape.Surface, true);
                        foreach (var descriptor in carrier.UnitShape.DescriptorSurfaces) Visit(descriptor, true);
                    }
                    break;
                case TickRef tick:
                    if (!immutableUnit) ticks.Add(tick);
                    break;
            }
        }
        Visit(source);
        if (carriers.Any(c => c.AttachedRelations.Count != 0 || c.Tension != 0) ||
            parameters.Any(p => !carriers.Contains(p))) return null;
        foreach (var parameter in parameters) ticks.Remove(parameter.ValueSlot.Tick);
        return new NativePreviewWorkspace(source, parameters,
            relations.Select(r => (r, r.Left, r.Right)).ToArray(),
            ticks.Select(t => (t, t.Ticks)).ToArray(),
            carriers.Select(c => (c, c.Value, c.Tension, parameterSet.Contains(c))).ToArray());
    }

    public bool Matches() =>
        topology.All(s => ReferenceEquals(s.Relation.Left, s.Left) && ReferenceEquals(s.Relation.Right, s.Right)) &&
        sites.All(s => s.Tick.Ticks == s.Value) &&
        carriers.All(s => s.Carrier.AttachedRelations.Count == 0 && s.Carrier.Tension == 0 &&
            (s.Actuator || s.Carrier.Value == s.Value));

    public Relation Run((Carrier Actuator, long Delta)[] changes)
    {
        var arrivals = new List<IElement>();
        var requested = changes.ToDictionary(c => c.Actuator, c => c.Delta);
        foreach (var pair in actuators)
        {
            // Scratch reset and candidate input are a single local arrival.
            // Only unattached, zero-tension carriers are admitted to this cache.
            var target = checked((long)pair.Key.Value + requested.GetValueOrDefault(pair.Key));
            var delta = checked(target - (long)pair.Value.Value);
            if (delta != 0) arrivals.Add(new CarrierLocation(pair.Value, delta));
        }
        if (arrivals.Count > 0) Carrier.ReceiveTogether(G(arrivals.ToArray()));
        return MachineRunner.Run(definition);
    }

    public bool HasState(Carrier actuator, long target) =>
        actuators.TryGetValue(actuator, out var scratch) && scratch.Tension == 0 && scratch.Value == (ulong)target;
}
