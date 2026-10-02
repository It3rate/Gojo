namespace Gojo.Core;

using System.Numerics;

public sealed partial class Relation
{
    #region Projection and Ports

    /// <summary>Retains an unfulfilled read, its input, requested surface and
    /// output connection without supplying a scalar answer.</summary>
    public static Relation UnresolvedRead(IElement input, IElement surface, Carrier reader) =>
        new(new Relation(new Placeholder(), reader), new Relation(surface, input));

    /// <summary>Projects a location through this relation; an incompatible
    /// projection remains an unresolved read rather than numeric zero.</summary>
    public Relation Project(CarrierLocation location, Carrier reader)
    {
        return TryProject(location, reader, out var projected)
            ? projected
            : UnresolvedRead(location, this, reader);
    }

    /// <summary>Projects only when this relation surface accepts source and reader frames.</summary>
    public bool TryProject(CarrierLocation location, Carrier reader, out Relation projected)
    {
        if (TryProjectBetweenSurfaces(Left, Right, location, reader, out projected))
        {
            return true;
        }

        projected = UnresolvedRead(location, this, reader);
        return false;
    }

    /// <summary>Reads a scalar projection result and its whole or scaled source residue.</summary>
    public bool TryReadProjection(out CarrierLocation target, out IElement residue)
    {
        if (Left is CarrierLocation location && ProjectionResidue.TryRead(Right, out _, out _, out _))
        {
            target = location;
            residue = Right;
            return true;
        }

        target = default;
        residue = default!;
        return false;
    }

    /// <summary>Projects between two relation surfaces in whichever direction matches.</summary>
    private static bool TryProjectBetweenSurfaces(
        IElement left,
        IElement right,
        CarrierLocation location,
        Carrier reader,
        out Relation projected)
    {
        if (!TryReadFrame(left, out var leftFrame) ||
            !TryReadFrame(right, out var rightFrame))
        {
            projected = ProjectionResult(new CarrierLocation(reader, 0), location);
            return false;
        }

        if (FrameAcceptsProjectionSource(leftFrame, location.Carrier) &&
            FrameAccepts(rightFrame, reader))
        {
            if (leftFrame.Start == leftFrame.End)
            {
                projected = ProjectionResult(new CarrierLocation(reader, 0), location);
                return false;
            }

            projected = ProjectBetween(
                location,
                reader,
                ResolveProjectionSourceFrame(leftFrame, location.Carrier),
                ResolveFrameCarrier(rightFrame, reader));
            return true;
        }

        if (FrameAcceptsProjectionSource(rightFrame, location.Carrier) &&
            FrameAccepts(leftFrame, reader))
        {
            if (rightFrame.Start == rightFrame.End)
            {
                projected = ProjectionResult(new CarrierLocation(reader, 0), location);
                return false;
            }

            projected = ProjectBetween(
                location,
                reader,
                ResolveProjectionSourceFrame(rightFrame, location.Carrier),
                ResolveFrameCarrier(leftFrame, reader));
            return true;
        }

        projected = ProjectionResult(new CarrierLocation(reader, 0), location);
        return false;
    }

    /// <summary>Imports value input through this relation as a slot port.</summary>
    public bool TrySetFrom(IElement input)
    {
        var imports = new List<SlotImport>();
        if (!TryCollectSlotImports(Left, Right, input, imports))
        {
            return false;
        }

        foreach (var import in imports)
        {
            import.Apply();
        }

        return imports.Count != 0;
    }

    /// <summary>Imports input by retargeting exposed structural coordinates.</summary>
    public bool TrySetStructureFrom(IElement input)
    {
        var imports = new List<StructuralSlotImport>();
        if (!TryCollectStructuralSlotImports(Left, Right, input, imports))
        {
            return false;
        }

        foreach (var import in imports)
        {
            import.Apply();
        }

        return imports.Count != 0;
    }

    #endregion

    #region Slot Import Helpers

    /// <summary>Collects value-port imports after validating the whole input shape.</summary>
    private static bool TryCollectSlotImports(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<SlotImport> imports)
    {
        if (input is Relation inputGroup &&
            sourceSurface is Relation sourceGroup &&
            targetSurface is Relation targetGroup)
        {
            return TryCollectSlotImports(sourceGroup.Left, targetGroup.Left, inputGroup.Left, imports) &&
                TryCollectSlotImports(sourceGroup.Right, targetGroup.Right, inputGroup.Right, imports);
        }

        if (input is not CarrierLocation inputLocation ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputLocation, out var projection) ||
            projection.Residue is not CarrierLocation residue)
        {
            return false;
        }

        imports.Add(new SlotImport(projection.Target.Carrier, projection.Target.Ticks, residue));
        return true;
    }

    /// <summary>Collects structural imports that retarget existing exposed coordinates.</summary>
    private static bool TryCollectStructuralSlotImports(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<StructuralSlotImport> imports)
    {
        if (input is Relation inputGroup &&
            sourceSurface is Relation sourceGroup &&
            targetSurface is Relation targetGroup &&
            IsGroupSurface(sourceSurface) &&
            IsGroupSurface(targetSurface) &&
            IsGroupSurface(input))
        {
            return TryCollectStructuralSlotImports(sourceGroup.Left, targetGroup.Left, inputGroup.Left, imports) &&
                TryCollectStructuralSlotImports(sourceGroup.Right, targetGroup.Right, inputGroup.Right, imports);
        }

        if (input is Relation && TryCollectAtomicStructuralFrameImport(sourceSurface, targetSurface, input, imports))
        {
            return true;
        }

        if (input is not CarrierLocation inputLocation ||
            !TryReadStructuralTarget(targetSurface, out var targetSite) ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputLocation, out var projection) ||
            projection.Residue is not CarrierLocation residue)
        {
            return false;
        }

        imports.Add(new StructuralSlotImport(targetSite, projection.Target.Ticks, residue));
        return true;
    }

    /// <summary>Imports a frame-readable structural value as one atomic extent.</summary>
    private static bool TryCollectAtomicStructuralFrameImport(
        IElement sourceSurface,
        IElement targetSurface,
        IElement input,
        List<StructuralSlotImport> imports)
    {
        if (!TryReadAtomicFrameInput(input, sourceSurface, out var inputExtent) ||
            !TryReadStructuralTarget(targetSurface, out var targetSite) ||
            !TryProjectToTargetSurface(sourceSurface, targetSurface, inputExtent, out var projection) ||
            projection.Residue is not CarrierLocation residue)
        {
            return false;
        }

        imports.Add(new StructuralSlotImport(targetSite, projection.Target.Ticks, residue));
        return true;
    }

    /// <summary>True for relation-shaped groups that are not simple frames.</summary>
    private static bool IsGroupSurface(IElement element) =>
        element is Relation && !TryReadFrame(element, out _);

    #endregion

    #region Location Transforms and Projection Math

    /// <summary>Collects and commits relation-location updates as one structural change.</summary>
    private bool TryTransformLocations(Func<CarrierLocation, LocationTransform> transform)
    {
        var updates = new List<LocationUpdate>();
        if (!TryCollectLocationUpdates(transform, new HashSet<ulong>(), updates))
        {
            return false;
        }

        foreach (var update in updates)
        {
            if (update.Current.Carrier.SameIdentityAs(update.Next.Carrier))
            {
                update.Current.Tick.Set(update.Next.Ticks);
                continue;
            }

            if (update.IsLeft)
            {
                update.Relation.Left = update.Next;
            }
            else
            {
                update.Relation.Right = update.Next;
            }
        }

        return updates.Count != 0;
    }

    /// <summary>Recursively collects location updates while avoiding relation cycles.</summary>
    private bool TryCollectLocationUpdates(
        Func<CarrierLocation, LocationTransform> transform,
        HashSet<ulong> visited,
        List<LocationUpdate> updates)
    {
        if (!visited.Add(Id))
        {
            return true;
        }

        return TryCollect(Left, isLeft: true) &&
            TryCollect(Right, isLeft: false);

        bool TryCollect(IElement element, bool isLeft)
        {
            if (element is CarrierLocation location)
            {
                var transformed = transform(location);
                if (!transformed.CanTransform)
                {
                    return false;
                }

                if (!transformed.Location.Equals(location))
                {
                    updates.Add(new LocationUpdate(this, isLeft, location, transformed.Location));
                }

                return true;
            }

            return element is not Relation relation ||
                relation.TryCollectLocationUpdates(transform, visited, updates);
        }
    }

    /// <summary>Projects a location from a source frame into a target frame.</summary>
    private static Relation ProjectBetween(
        CarrierLocation location,
        Carrier reader,
        CarrierFrame source,
        CarrierFrame target)
    {
        var sourceBasis = (BigInteger)source.End - source.Start;
        var targetBasis = (BigInteger)target.End - target.Start;
        if (sourceBasis.IsZero)
        {
            // Keep the absent calibration even for zero input. A zero residue
            // alone cannot distinguish an empty basis from measured zero.
            return new Relation(new CarrierLocation(reader, 0),
                Relation.OrderedGroup(new Carrier(), location,
                    Segment(source.Carrier, source.Start, source.End),
                    Segment(target.Carrier, target.Start, target.End)));
        }

        // A constant target frame can be observed but accounts for none of the
        // selected input. A zero source frame has no defined affine projection.
        if (targetBasis.IsZero)
        {
            return ProjectionResult(new CarrierLocation(reader, target.Start), location);
        }

        var relativeTicks = (BigInteger)location.Ticks - source.Start;
        var quotient = BigInteger.DivRem(relativeTicks * targetBasis, sourceBasis, out var scaledRemainder);
        var result = checked((long)(target.Start + quotient));
        var residue = ProjectionResidue.Create(location.Carrier, scaledRemainder, targetBasis);

        return ProjectionResult(
            new CarrierLocation(reader, result), residue);
    }

    /// <summary>Builds the standard projection result pair.</summary>
    private static Relation ProjectionResult(CarrierLocation projected, IElement residue) =>
        new(projected, residue);

    /// <summary>Returns positive magnitude of signed ticks.</summary>
    private static ulong MagnitudeOf(long ticks) =>
        ticks >= 0 ? (ulong)ticks : checked((ulong)-(Int128)ticks);

    /// <summary>Applies a sign to an unsigned tick magnitude.</summary>
    private static long SignedTicks(ulong ticks, int sign) =>
        sign < 0 ? checked((long)-(Int128)ticks) : checked((long)ticks);

    /// <summary>Scales a location exactly between two basis extents.</summary>
    private static bool TryScale(
        CarrierLocation location,
        long sourceBasis,
        long targetBasis,
        out CarrierLocation scaled)
    {
        if (sourceBasis == 0)
        {
            scaled = default;
            return false;
        }

        var quotient = BigInteger.DivRem((BigInteger)location.Ticks * targetBasis, sourceBasis, out var remainder);
        if (!remainder.IsZero)
        {
            scaled = default;
            return false;
        }

        scaled = new CarrierLocation(location.Carrier, checked((long)quotient));
        return true;
    }

    #endregion
}
