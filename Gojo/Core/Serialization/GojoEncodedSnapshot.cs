namespace Gojo.Core;

/// <summary>
/// Storage table selector for an encoded element reference. This is not an
/// operation tag; it only says which physical table stores the referenced
/// Gojo element.
/// </summary>
public enum GojoEncodedElementKind : byte
{
    None = 0,
    Carrier = 1,
    TickRef = 2,
    CarrierLocation = 3,
    Relation = 4,
    UnitShape = 5
}

/// <summary>Pointer into one table of a packed Gojo snapshot.</summary>
public readonly record struct GojoEncodedElementRef(GojoEncodedElementKind Kind, int Index)
{
    public static GojoEncodedElementRef None { get; } = new(GojoEncodedElementKind.None, -1);

    public bool IsNone => Kind == GojoEncodedElementKind.None || Index < 0;
}

/// <summary>Packed carrier row for diagnostics and future executable backends.</summary>
public readonly record struct GojoEncodedCarrier(
    int Index,
    ulong Id,
    bool IsInverse,
    bool IsUnknown,
    ulong Value,
    long Tension,
    int UnitShapeIndex,
    int InverseCarrierIndex);

/// <summary>Packed shared coordinate row.</summary>
public readonly record struct GojoEncodedTickRef(int Index, ulong Id, long Ticks);

/// <summary>Packed carrier-grounded coordinate row.</summary>
public readonly record struct GojoEncodedCarrierLocation(
    int Index,
    int CarrierIndex,
    int TickRefIndex);

/// <summary>Packed ordered relation row.</summary>
public readonly record struct GojoEncodedRelation(
    int Index,
    ulong Id,
    GojoEncodedElementRef Left,
    GojoEncodedElementRef Right);

/// <summary>Packed unit-shape row pointing to its structural surface.</summary>
public readonly record struct GojoEncodedUnitShape(
    int Index,
    ulong Id,
    GojoEncodedElementRef Surface);

/// <summary>
/// Read-only packed view of a Gojo structure. This is deliberately a derived
/// inspection product; carriers, tick refs, unit shapes, and relation surfaces
/// remain the source of meaning.
/// </summary>
public sealed record GojoEncodedSnapshot(
    GojoEncodedElementRef[] Roots,
    GojoEncodedCarrier[] Carriers,
    GojoEncodedTickRef[] TickRefs,
    GojoEncodedCarrierLocation[] CarrierLocations,
    GojoEncodedRelation[] Relations,
    GojoEncodedUnitShape[] UnitShapes)
{
    public GojoEncodedElementRef Root =>
        Roots.Length == 0 ? GojoEncodedElementRef.None : Roots[0];

    /// <summary>Captures one element tree and unit-shape surfaces reachable from it.</summary>
    public static GojoEncodedSnapshot Capture(IElement root) =>
        new Encoder().Capture(root);

    /// <summary>
    /// Captures a carrier and attached relation world reachable through local
    /// carrier attachments.
    /// </summary>
    public static GojoEncodedSnapshot CaptureWorld(Carrier anchor) =>
        new Encoder().CaptureWorld(anchor);

    private sealed class Encoder
    {
        private readonly Dictionary<Carrier, int> carrierIndexes = new(ReferenceComparer<Carrier>.Instance);
        private readonly Dictionary<TickRef, int> tickRefIndexes = new(ReferenceComparer<TickRef>.Instance);
        private readonly Dictionary<LocationKey, int> locationIndexes = [];
        private readonly Dictionary<Relation, int> relationIndexes = new(ReferenceComparer<Relation>.Instance);
        private readonly Dictionary<UnitShape, int> unitShapeIndexes = new(ReferenceComparer<UnitShape>.Instance);

        private readonly List<Carrier> carrierObjects = [];
        private readonly List<GojoEncodedElementRef> roots = [];
        private readonly List<GojoEncodedCarrier> carriers = [];
        private readonly List<GojoEncodedTickRef> tickRefs = [];
        private readonly List<GojoEncodedCarrierLocation> carrierLocations = [];
        private readonly List<GojoEncodedRelation> relations = [];
        private readonly List<GojoEncodedUnitShape> unitShapes = [];

        public GojoEncodedSnapshot Capture(IElement root)
        {
            AddRoot(Encode(root));
            return Build();
        }

        public GojoEncodedSnapshot CaptureWorld(Carrier anchor)
        {
            AddRoot(EncodeCarrier(anchor));

            for (var index = 0; index < carrierObjects.Count; index++)
            {
                var carrier = carrierObjects[index];
                if (carrier.IsUnknown)
                {
                    continue;
                }

                foreach (var relation in carrier.AttachedRelations)
                {
                    AddRoot(EncodeRelation(relation));
                }
            }

            return Build();
        }

        private GojoEncodedElementRef Encode(IElement element) =>
            element switch
            {
                Carrier carrier => EncodeCarrier(carrier),
                TickRef tickRef => EncodeTickRef(tickRef),
                CarrierLocation location => EncodeLocation(location),
                Relation relation => EncodeRelation(relation),
                _ => GojoEncodedElementRef.None
            };

        private GojoEncodedElementRef EncodeCarrier(Carrier carrier)
        {
            if (carrierIndexes.TryGetValue(carrier, out var existing))
            {
                return new GojoEncodedElementRef(GojoEncodedElementKind.Carrier, existing);
            }

            var index = carriers.Count;
            carrierIndexes.Add(carrier, index);
            carrierObjects.Add(carrier);
            carriers.Add(new GojoEncodedCarrier(
                index,
                carrier.Id,
                carrier.IsInverse,
                carrier.IsUnknown,
                carrier.Value,
                carrier.Tension,
                UnitShapeIndex: -1,
                InverseCarrierIndex: -1));

            var unitShape = EncodeUnitShape(carrier.UnitShape);
            var inverseCarrier = carrier.ExistingInverse is null
                ? GojoEncodedElementRef.None
                : EncodeCarrier(carrier.ExistingInverse);

            carriers[index] = new GojoEncodedCarrier(
                index,
                carrier.Id,
                carrier.IsInverse,
                carrier.IsUnknown,
                carrier.Value,
                carrier.Tension,
                unitShape.Index,
                inverseCarrier.IsNone ? -1 : inverseCarrier.Index);

            return new GojoEncodedElementRef(GojoEncodedElementKind.Carrier, index);
        }

        private GojoEncodedElementRef EncodeTickRef(TickRef tickRef)
        {
            if (tickRefIndexes.TryGetValue(tickRef, out var existing))
            {
                return new GojoEncodedElementRef(GojoEncodedElementKind.TickRef, existing);
            }

            var index = tickRefs.Count;
            tickRefIndexes.Add(tickRef, index);
            tickRefs.Add(new GojoEncodedTickRef(index, tickRef.Id, tickRef.Ticks));
            return new GojoEncodedElementRef(GojoEncodedElementKind.TickRef, index);
        }

        private GojoEncodedElementRef EncodeLocation(CarrierLocation location)
        {
            if (location.Carrier is null || location.Tick is null)
            {
                return GojoEncodedElementRef.None;
            }

            var carrier = EncodeCarrier(location.Carrier);
            var tickRef = EncodeTickRef(location.Tick);
            if (carrier.IsNone || tickRef.IsNone)
            {
                return GojoEncodedElementRef.None;
            }

            var key = new LocationKey(carrier.Index, tickRef.Index);
            if (locationIndexes.TryGetValue(key, out var existing))
            {
                return new GojoEncodedElementRef(GojoEncodedElementKind.CarrierLocation, existing);
            }

            var index = carrierLocations.Count;
            locationIndexes.Add(key, index);
            carrierLocations.Add(new GojoEncodedCarrierLocation(index, carrier.Index, tickRef.Index));
            return new GojoEncodedElementRef(GojoEncodedElementKind.CarrierLocation, index);
        }

        private GojoEncodedElementRef EncodeRelation(Relation relation)
        {
            if (relationIndexes.TryGetValue(relation, out var existing))
            {
                return new GojoEncodedElementRef(GojoEncodedElementKind.Relation, existing);
            }

            var index = relations.Count;
            relationIndexes.Add(relation, index);
            relations.Add(new GojoEncodedRelation(
                index,
                relation.Id,
                GojoEncodedElementRef.None,
                GojoEncodedElementRef.None));

            var left = Encode(relation.Left);
            var right = Encode(relation.Right);
            relations[index] = new GojoEncodedRelation(index, relation.Id, left, right);
            return new GojoEncodedElementRef(GojoEncodedElementKind.Relation, index);
        }

        private GojoEncodedElementRef EncodeUnitShape(UnitShape unitShape)
        {
            if (unitShapeIndexes.TryGetValue(unitShape, out var existing))
            {
                return new GojoEncodedElementRef(GojoEncodedElementKind.UnitShape, existing);
            }

            var index = unitShapes.Count;
            unitShapeIndexes.Add(unitShape, index);
            unitShapes.Add(new GojoEncodedUnitShape(index, unitShape.Id, GojoEncodedElementRef.None));

            var surface = Encode(unitShape.SurfaceRoot);
            unitShapes[index] = new GojoEncodedUnitShape(index, unitShape.Id, surface);
            return new GojoEncodedElementRef(GojoEncodedElementKind.UnitShape, index);
        }

        private void AddRoot(GojoEncodedElementRef root)
        {
            if (!root.IsNone && !roots.Contains(root))
            {
                roots.Add(root);
            }
        }

        private GojoEncodedSnapshot Build() =>
            new(
                [.. roots],
                [.. carriers],
                [.. tickRefs],
                [.. carrierLocations],
                [.. relations],
                [.. unitShapes]);
    }

    private readonly record struct LocationKey(int CarrierIndex, int TickRefIndex);

    private sealed class ReferenceComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceComparer<T> Instance { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
