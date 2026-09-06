namespace Gojo.Core;

public static partial class GojoNumericFormat
{
    private static void Validate(GojoEncodedSnapshot graph)
    {
        if (!graph.IsCompleteGraph || graph.Roots is null || graph.Carriers is null || graph.TickRefs is null ||
            graph.CarrierLocations is null || graph.Relations is null || graph.UnitShapes is null || graph.Placeholders is null)
            throw new InvalidDataException("Numeric restoration requires a complete graph capture.");
        ValidateIds(graph.Carriers.Select(c => (c.Index, c.Id)), allowZero: true);
        ValidateIds(graph.TickRefs.Select(t => (t.Index, t.Id)));
        ValidateIds(graph.Relations.Select(r => (r.Index, r.Id)));
        ValidateIds(graph.UnitShapes.Select(u => (u.Index, u.Id)));
        foreach (var root in graph.Roots) Reference(root);
        var valueTicks = new HashSet<int>();
        foreach (var c in graph.Carriers)
        {
            Index(c.UnitShapeIndex, graph.UnitShapes.Length);
            Index(c.ValueTickRefIndex, graph.TickRefs.Length);
            if (c.InverseCarrierIndex != -1) Index(c.InverseCarrierIndex, graph.Carriers.Length);
            if (c.Value > long.MaxValue || graph.TickRefs[c.ValueTickRefIndex].Ticks != (long)c.Value ||
                !valueTicks.Add(c.ValueTickRefIndex) || c.IsUnknown != (c.Id == 0) || c.AttachedRelationIndexes is null)
                throw new InvalidDataException("Invalid carrier state or value-site identity.");
            var attached = new HashSet<int>();
            foreach (var index in c.AttachedRelationIndexes)
            {
                Index(index, graph.Relations.Length);
                if (!attached.Add(index)) throw new InvalidDataException("Duplicate carrier attachment.");
            }
            if (c.InverseCarrierIndex >= 0)
            {
                var opposite = graph.Carriers[c.InverseCarrierIndex];
                if (opposite.InverseCarrierIndex != c.Index || opposite.IsInverse == c.IsInverse)
                    throw new InvalidDataException("Asymmetric opposed-carrier connection.");
            }
        }
        for (var i = 0; i < graph.CarrierLocations.Length; i++)
        {
            var l = graph.CarrierLocations[i];
            if (l.Index != i) throw new InvalidDataException("Nonlocal location row index.");
            Index(l.CarrierIndex, graph.Carriers.Length); Index(l.TickRefIndex, graph.TickRefs.Length);
        }
        foreach (var r in graph.Relations) { Reference(r.Left); Reference(r.Right); }
        foreach (var u in graph.UnitShapes)
        {
            Reference(u.Surface);
            if (u.Descriptors is null) throw new InvalidDataException("Missing unit descriptor table.");
            foreach (var descriptor in u.Descriptors) Reference(descriptor);
        }
        var faces = new HashSet<(int, bool)>();
        for (var i = 0; i < graph.Placeholders.Length; i++)
        {
            var p = graph.Placeholders[i];
            Index(p.SlotIndex, graph.Placeholders.Length);
            if (p.Index != i || !faces.Add((p.SlotIndex, p.IsInverse)))
                throw new InvalidDataException("Invalid or duplicate placeholder face.");
        }
        foreach (var c in graph.Carriers.Where(c => c.IsUnknown))
        {
            var unit = graph.UnitShapes[c.UnitShapeIndex];
            if (c.IsInverse || c.Value != 0 || c.Tension != 0 || c.InverseCarrierIndex != -1 ||
                c.AttachedRelationIndexes.Length != 0 || unit.Descriptors.Length != 0 ||
                unit.Surface.Kind != GojoEncodedElementKind.Relation)
                throw new InvalidDataException("The wildcard frame cannot carry persisted physical state.");
            var point = graph.Relations[unit.Surface.Index];
            if (point.Left.Kind != GojoEncodedElementKind.CarrierLocation ||
                point.Right != new GojoEncodedElementRef(GojoEncodedElementKind.Carrier, c.Index))
                throw new InvalidDataException("Invalid wildcard unit promise.");
            var location = graph.CarrierLocations[point.Left.Index];
            if (location.CarrierIndex != c.Index || graph.TickRefs[location.TickRefIndex].Ticks != 1)
                throw new InvalidDataException("Invalid wildcard unit tick.");
        }

        // Frozen unit internals must not alias a publicly mutable root, value
        // slot or attached relation. Otherwise loading would expose a back door
        // through which a stable native unit promise could be rewritten.
        var frozen = Walk(graph.UnitShapes.SelectMany(u => u.Descriptors.Prepend(u.Surface)), frozenUnits: true);
        var exposedRoots = graph.Roots.Concat(graph.Carriers.Select(c =>
            new GojoEncodedElementRef(GojoEncodedElementKind.Carrier, c.Index)));
        var exposed = Walk(exposedRoots, frozenUnits: false);
        if (frozen.Overlaps(exposed)) throw new InvalidDataException("Mutable graph aliases frozen unit internals.");
        ValidateFrozenAcyclic();
        foreach (var carrier in graph.Carriers.Where(c => c.InverseCarrierIndex >= 0))
        {
            var source = graph.UnitShapes[carrier.UnitShapeIndex];
            var opposite = graph.UnitShapes[graph.Carriers[carrier.InverseCarrierIndex].UnitShapeIndex];
            if (source.Descriptors.Length != opposite.Descriptors.Length ||
                !source.Descriptors.Zip(opposite.Descriptors).All(pair => EqualSurface(pair.First, pair.Second)) ||
                !(OpposedInner(source.Surface, out var inner) ? EqualSurface(inner, opposite.Surface) :
                    OpposedInner(opposite.Surface, out inner) && EqualSurface(source.Surface, inner)))
                throw new InvalidDataException("Opposed carriers have inconsistent stable unit promises.");
        }

        bool OpposedInner(GojoEncodedElementRef surface, out GojoEncodedElementRef inner)
        {
            inner = default;
            if (surface.Kind != GojoEncodedElementKind.Relation) return false;
            var relation = graph.Relations[surface.Index];
            if (relation.Right.Kind != GojoEncodedElementKind.Relation) return false;
            var marker = graph.Relations[relation.Right.Index];
            if (marker.Left.Kind != GojoEncodedElementKind.CarrierLocation || marker.Right.Kind != GojoEncodedElementKind.Carrier)
                return false;
            var location = graph.CarrierLocations[marker.Left.Index];
            if (location.CarrierIndex != marker.Right.Index || !graph.Carriers[location.CarrierIndex].IsUnknown ||
                graph.TickRefs[location.TickRefIndex].Ticks != -1) return false;
            inner = relation.Left;
            return true;
        }

        // Unit equality compares coordinates and ordered structure while retaining
        // carrier and abstract-slot identity. Table row identity is not that read.
        bool EqualSurface(GojoEncodedElementRef left, GojoEncodedElementRef right)
        {
            var pending = new Stack<(GojoEncodedElementRef Left, GojoEncodedElementRef Right)>();
            var visited = new HashSet<(GojoEncodedElementRef, GojoEncodedElementRef)>();
            pending.Push((left, right));
            while (pending.TryPop(out var pair))
            {
                if (pair.Left == pair.Right || !visited.Add(pair)) continue;
                if (pair.Left.Kind != pair.Right.Kind) return false;
                switch (pair.Left.Kind)
                {
                    case GojoEncodedElementKind.Relation:
                        var a = graph.Relations[pair.Left.Index]; var b = graph.Relations[pair.Right.Index];
                        pending.Push((a.Left, b.Left)); pending.Push((a.Right, b.Right));
                        break;
                    case GojoEncodedElementKind.CarrierLocation:
                        var x = graph.CarrierLocations[pair.Left.Index]; var y = graph.CarrierLocations[pair.Right.Index];
                        if (x.CarrierIndex != y.CarrierIndex || graph.TickRefs[x.TickRefIndex].Ticks != graph.TickRefs[y.TickRefIndex].Ticks)
                            return false;
                        break;
                    case GojoEncodedElementKind.Placeholder:
                        var p = graph.Placeholders[pair.Left.Index]; var q = graph.Placeholders[pair.Right.Index];
                        if (p.SlotIndex != q.SlotIndex || p.IsInverse != q.IsInverse) return false;
                        break;
                    default: return false;
                }
            }
            return true;
        }

        void Reference(GojoEncodedElementRef reference)
        {
            var count = reference.Kind switch
            {
                GojoEncodedElementKind.Carrier => graph.Carriers.Length,
                GojoEncodedElementKind.TickRef => graph.TickRefs.Length,
                GojoEncodedElementKind.CarrierLocation => graph.CarrierLocations.Length,
                GojoEncodedElementKind.Relation => graph.Relations.Length,
                GojoEncodedElementKind.Placeholder => graph.Placeholders.Length,
                _ => throw new InvalidDataException("Missing or unsupported element reference.")
            };
            Index(reference.Index, count);
        }

        HashSet<GojoEncodedElementRef> Walk(IEnumerable<GojoEncodedElementRef> roots, bool frozenUnits)
        {
            var visited = new HashSet<GojoEncodedElementRef>();
            var mutable = new HashSet<GojoEncodedElementRef>();
            var pending = new Stack<GojoEncodedElementRef>(roots);
            while (pending.TryPop(out var item))
            {
                if (!visited.Add(item)) continue;
                switch (item.Kind)
                {
                    case GojoEncodedElementKind.Relation:
                        mutable.Add(item);
                        var relation = graph.Relations[item.Index];
                        pending.Push(relation.Left); pending.Push(relation.Right);
                        break;
                    case GojoEncodedElementKind.CarrierLocation:
                        mutable.Add(new(GojoEncodedElementKind.TickRef, graph.CarrierLocations[item.Index].TickRefIndex));
                        if (!frozenUnits) pending.Push(new(GojoEncodedElementKind.Carrier, graph.CarrierLocations[item.Index].CarrierIndex));
                        break;
                    case GojoEncodedElementKind.TickRef:
                        if (frozenUnits) throw new InvalidDataException("Stable unit ticks must be grounded in locations.");
                        mutable.Add(item);
                        break;
                    case GojoEncodedElementKind.Carrier when !frozenUnits:
                        var carrier = graph.Carriers[item.Index];
                        pending.Push(new(GojoEncodedElementKind.TickRef, carrier.ValueTickRefIndex));
                        foreach (var attachment in carrier.AttachedRelationIndexes)
                            pending.Push(new(GojoEncodedElementKind.Relation, attachment));
                        break;
                }
            }
            return mutable;
        }

        void ValidateFrozenAcyclic()
        {
            var relationIds = frozen.Where(r => r.Kind == GojoEncodedElementKind.Relation).Select(r => r.Index).ToHashSet();
            var degree = relationIds.ToDictionary(i => i, _ => 0);
            foreach (var i in relationIds)
                foreach (var child in Children(i)) degree[child]++;
            var ready = new Queue<int>(degree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            var count = 0;
            while (ready.TryDequeue(out var next))
            {
                count++;
                foreach (var child in Children(next)) if (--degree[child] == 0) ready.Enqueue(child);
            }
            if (count != relationIds.Count) throw new InvalidDataException("Cyclic mutable structure inside a frozen unit promise.");

            IEnumerable<int> Children(int i)
            {
                var relation = graph.Relations[i];
                if (relation.Left.Kind == GojoEncodedElementKind.Relation) yield return relation.Left.Index;
                if (relation.Right.Kind == GojoEncodedElementKind.Relation) yield return relation.Right.Index;
            }
        }
    }

    private static void ValidateIds(IEnumerable<(int Index, ulong Id)> rows, bool allowZero = false)
    {
        var ids = new HashSet<ulong>();
        var index = 0;
        foreach (var row in rows)
            if (row.Index != index++ || (!allowZero && row.Id == 0) || !ids.Add(row.Id))
                throw new InvalidDataException("Duplicate identity or invalid local table index.");
    }

    private static void Index(int index, int count)
    {
        if (index < 0 || index >= count) throw new InvalidDataException("Reference outside its Gojo numeric table.");
    }
}
