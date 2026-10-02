using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Captures an acyclic exposed relation surface and its numeric sites, keeping
/// internal aliases. Retains carriers and stable unit promises; does not walk
/// private attachments or capture a whole live settlement world.
/// </summary>
internal sealed class ExposedStructureSnapshot
{
    private readonly Dictionary<Relation, IElement> relations = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<TickRef, TickRef> ticks = new(ReferenceEqualityComparer.Instance);

    internal IElement Copy(IElement element)
    {
        if (element is CarrierLocation location)
        {
            if (!ticks.TryGetValue(location.Tick, out var tick)) ticks.Add(location.Tick, tick = new TickRef(location.Ticks));
            return new CarrierLocation(location.Carrier, tick);
        }
        if (element is not Relation relation) return element;
        if (relations.TryGetValue(relation, out var previous)) return previous;
        var result = new Relation(Copy(relation.Left), Copy(relation.Right));
        relations.Add(relation, result); return result;
    }
}
